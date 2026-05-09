using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI.Core;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class WidgetPage : Page
    {
        private string _resolutionAction1;
        private string _resolutionAction2;
        private string _resolutionAction3;
        private WidgetSettings _settings;
        private XboxGameBarWidget _gameBarWidget;
        private bool _eventsHooked;
        private bool _resolutionDetectionAttempted;

        private const string ActionInsert = "insert";
        private const string ActionAltInsert = "altinsert";
        private const string ActionCustom1 = "custom1";
        private const string ActionCustom2 = "custom2";
        private const string ActionCustom3 = "custom3";
        private const string ActionLosslessScaling = "losslessscaling";
        private const string ActionDetectResolutionPresets = "detect-resolution-presets";
        private const string ActionSetResolution1200 = "set-resolution-1920-1200";
        private const string ActionSetResolution1080 = "set-resolution-1920-1080";
        private const string ActionSetResolution1050 = "set-resolution-1680-1050";
        private const string ActionSetResolution900 = "set-resolution-1600-900";
        private const string ActionSetResolution720 = "set-resolution-1280-720";

        // Must stay in sync with desktop:ParameterGroup GroupId values in Package.appxmanifest.
        private const string GroupInsert = "InsertCommand";
        private const string GroupAltInsert = "AltInsertCommand";
        private const string GroupCustom1 = "Custom1Command";
        private const string GroupCustom2 = "Custom2Command";
        private const string GroupCustom3 = "Custom3Command";
        private const string GroupLosslessScaling = "LosslessScalingCommand";
        private const string GroupDetectResolutionPresets = "DetectResolutionPresetsCommand";
        private const string GroupSetResolution1200 = "SetResolution1920x1200Command";
        private const string GroupSetResolution1080 = "SetResolution1920x1080Command";
        private const string GroupSetResolution1050 = "SetResolution1680x1050Command";
        private const string GroupSetResolution900 = "SetResolution1600x900Command";
        private const string GroupSetResolution720 = "SetResolution1280x720Command";

        public WidgetPage()
        {
            InitializeComponent();
            _settings = WidgetSettingsStore.Load();
            DiagnosticsLog.Write("WidgetPage ctor");
            Loaded += WidgetPage_Loaded;
        }

        private void WidgetPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettingsToUi();
            InitializeResolutionSectionDeferred();
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPointerOver");
        }

        private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPressed");
        }

        private void Button_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackground");
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackground");
        }

        private void ApplyStateBrush(Button button, string key)
        {
            if (button == null)
            {
                return;
            }

            var paletteTag = (button.Tag as string)?.ToLowerInvariant() ?? "default";
            var prefix = paletteTag switch
            {
                "capture" => "CaptureButton",
                "overlay" => "OverlayButton",
                _ => "DefaultButton"
            };

            var resourceKey = key switch
            {
                "ButtonBackgroundPressed" => $"{prefix}BackgroundPressed",
                "ButtonBackgroundPointerOver" => $"{prefix}BackgroundPointerOver",
                _ => $"{prefix}Background"
            };

            if (Resources.ContainsKey(resourceKey) && Resources[resourceKey] is Brush rootBrush)
            {
                button.Background = rootBrush;
            }
        }

        private async System.Threading.Tasks.Task<bool> LaunchHelperActionAsync(string action)
        {
            DiagnosticsLog.Write($"LaunchHelperAction start action={action}");

            string groupId = action switch
            {
                ActionInsert => GroupInsert,
                ActionAltInsert => GroupAltInsert,
                ActionCustom1 => GroupCustom1,
                ActionCustom2 => GroupCustom2,
                ActionCustom3 => GroupCustom3,
                ActionLosslessScaling => GroupLosslessScaling,
                ActionDetectResolutionPresets => GroupDetectResolutionPresets,
                ActionSetResolution1200 => GroupSetResolution1200,
                ActionSetResolution1080 => GroupSetResolution1080,
                ActionSetResolution1050 => GroupSetResolution1050,
                ActionSetResolution900 => GroupSetResolution900,
                ActionSetResolution720 => GroupSetResolution720,
                _ => null
            };

            if (string.IsNullOrEmpty(groupId))
            {
                DiagnosticsLog.Write($"LaunchHelperAction unknown action={action}");
                return false;
            }

            try
            {
                DiagnosticsLog.Write($"LaunchFullTrust group={groupId}");
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(groupId);
                DiagnosticsLog.Write($"LaunchFullTrust success action={action}");
                return true;
            }
            catch (Exception ex)
            {
                DiagnosticsLog.Write($"LaunchFullTrust fail action={action} ex={ex.GetType().Name} msg={ex.Message}");
                return false;
            }
        }

        private void InitializeResolutionSectionDeferred()
        {
            _resolutionDetectionAttempted = false;
            DisplayResolutionSection.Visibility = Visibility.Visible;
            ConfigureResolutionButtons(
                "Detect", null,
                string.Empty, null,
                string.Empty, null);
            ApplyPresetVisibility(true, false, false);
        }

        private async System.Threading.Tasks.Task EnsureResolutionSectionInitializedAsync()
        {
            if (_resolutionDetectionAttempted)
            {
                return;
            }

            _resolutionDetectionAttempted = true;

            ConfigureResolutionButtons(
                "Detecting...", null,
                string.Empty, null,
                string.Empty, null);
            ApplyPresetVisibility(true, false, false);

            DateTimeOffset launchStamp = DateTimeOffset.UtcNow;
            bool launched = await LaunchHelperActionAsync(ActionDetectResolutionPresets);
            if (!launched)
            {
                ConfigureResolutionButtons(
                    "Unavailable", null,
                    string.Empty, null,
                    string.Empty, null);
                ApplyPresetVisibility(true, false, false);
                return;
            }

            ResolutionFeatureState state = await ResolutionFeatureStateStore.WaitForStateRefreshAsync(launchStamp, 2200);
            if (!state.Available)
            {
                ConfigureResolutionButtons(
                    "Unavailable", null,
                    string.Empty, null,
                    string.Empty, null);
                ApplyPresetVisibility(true, false, false);
                return;
            }

            if (state.Group == ResolutionPresetGroup.Group1200)
            {
                ConfigureResolutionButtons(
                    "1200p", ActionSetResolution1200,
                    "1080p", ActionSetResolution1080,
                    "1050p", ActionSetResolution1050);
                ApplyPresetVisibility(state.Support1200p, state.Support1080p, state.Support1050p);
                return;
            }

            if (state.Group == ResolutionPresetGroup.Group1080)
            {
                ConfigureResolutionButtons(
                    "1080p", ActionSetResolution1080,
                    "900p", ActionSetResolution900,
                    "720p", ActionSetResolution720);
                ApplyPresetVisibility(state.Support1080p, state.Support900p, state.Support720p);
                return;
            }

            ConfigureResolutionButtons(
                "Unavailable", null,
                string.Empty, null,
                string.Empty, null);
            ApplyPresetVisibility(true, false, false);
        }

        private void ConfigureResolutionButtons(
            string label1,
            string action1,
            string label2,
            string action2,
            string label3,
            string action3)
        {
            ResolutionButton1.Content = label1;
            ResolutionButton2.Content = label2;
            ResolutionButton3.Content = label3;
            _resolutionAction1 = action1;
            _resolutionAction2 = action2;
            _resolutionAction3 = action3;
        }

        private void ApplyPresetVisibility(bool showFirst, bool showSecond, bool showThird)
        {
            ResolutionButton1.Visibility = showFirst ? Visibility.Visible : Visibility.Collapsed;
            ResolutionButton2.Visibility = showSecond ? Visibility.Visible : Visibility.Collapsed;
            ResolutionButton3.Visibility = showThird ? Visibility.Visible : Visibility.Collapsed;

            _resolutionAction1 = showFirst ? _resolutionAction1 : null;
            _resolutionAction2 = showSecond ? _resolutionAction2 : null;
            _resolutionAction3 = showThird ? _resolutionAction3 : null;

            bool anyVisible = showFirst || showSecond || showThird;
            DisplayResolutionSection.Visibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;
            ApplySectionOrder();
        }

        private async void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionInsert);
        }

        private async void AltInsertButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionAltInsert);
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionLosslessScaling);
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenGameBarSettingsAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _gameBarWidget = e.Parameter as XboxGameBarWidget;

            if (_eventsHooked)
            {
                return;
            }

            Window.Current.Activated += CurrentWindow_Activated;
            WidgetSettingsStore.SettingsSaved += WidgetSettingsStore_SettingsSaved;
            if (_gameBarWidget != null)
            {
                _gameBarWidget.SettingsClicked += Widget_SettingsClicked;
            }

            _eventsHooked = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_eventsHooked)
            {
                Window.Current.Activated -= CurrentWindow_Activated;
                WidgetSettingsStore.SettingsSaved -= WidgetSettingsStore_SettingsSaved;
                if (_gameBarWidget != null)
                {
                    _gameBarWidget.SettingsClicked -= Widget_SettingsClicked;
                }

                _eventsHooked = false;
            }

            base.OnNavigatedFrom(e);
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            await OpenGameBarSettingsAsync();
        }

        private void CurrentWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                return;
            }

            ReloadSettings();
        }

        private async void WidgetSettingsStore_SettingsSaved(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ReloadSettings();
            });
        }

        private async void CustomButton1_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCustomSlotAsync("custom1", ActionCustom1);
        }

        private async void CustomButton2_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCustomSlotAsync("custom2", ActionCustom2);
        }

        private async void CustomButton3_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCustomSlotAsync("custom3", ActionCustom3);
        }

        private async Task ExecuteCustomSlotAsync(string slotId, string action)
        {
            if (!_settings.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) || !WidgetSettingsStore.IsConfigured(slot))
            {
                await OpenGameBarSettingsAsync();
                return;
            }

            await LaunchHelperActionAsync(action);
        }

        private async Task OpenGameBarSettingsAsync()
        {
            if (_gameBarWidget == null)
            {
                DiagnosticsLog.Write("OpenGameBarSettings skipped because widget context is null.");
                return;
            }

            try
            {
                var control = new XboxGameBarWidgetControl(_gameBarWidget);
                await control.ActivateAsync("Widget2Settings");
            }
            catch (Exception ex)
            {
                DiagnosticsLog.Write($"Activate settings widget via control failed msg={ex.Message}");

                try
                {
                    await _gameBarWidget.ActivateSettingsAsync();
                }
                catch (Exception fallbackEx)
                {
                    DiagnosticsLog.Write($"ActivateSettingsAsync fallback failed msg={fallbackEx.Message}");
                }
            }
        }

        private void ApplySettingsToUi()
        {
            _settings = WidgetSettingsStore.Normalize(_settings);
            LosslessShortcutTextBlock.Text = $"({FormatShortcut(_settings.BuiltInLosslessKeys)})";

            CustomButton1.Content = GetCustomButtonText("custom1");
            CustomButton2.Content = GetCustomButtonText("custom2");
            CustomButton3.Content = GetCustomButtonText("custom3");

            ApplySectionOrder();
        }

        private void ReloadSettings()
        {
            _settings = WidgetSettingsStore.Load();
            ApplySettingsToUi();
        }

        private string GetCustomButtonText(string slotId)
        {
            if (_settings.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) && WidgetSettingsStore.IsConfigured(slot))
            {
                return FormatShortcut(slot.Keys);
            }

            return "Not Set";
        }

        private void ApplySectionOrder()
        {
            var map = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["overlay"] = OverlaySection,
                ["resolution"] = DisplayResolutionSection,
                ["custom"] = CustomSection
            };

            ReorderableSectionsPanel.Children.Clear();
            foreach (string section in _settings.SectionOrder)
            {
                if (map.TryGetValue(section, out FrameworkElement element))
                {
                    ReorderableSectionsPanel.Children.Add(element);
                }
            }

            if (!ReorderableSectionsPanel.Children.Contains(OverlaySection))
            {
                ReorderableSectionsPanel.Children.Add(OverlaySection);
            }

            if (!ReorderableSectionsPanel.Children.Contains(DisplayResolutionSection))
            {
                ReorderableSectionsPanel.Children.Add(DisplayResolutionSection);
            }

            if (!ReorderableSectionsPanel.Children.Contains(CustomSection))
            {
                ReorderableSectionsPanel.Children.Add(CustomSection);
            }
        }

        private async void ResolutionButton1_Click(object sender, RoutedEventArgs e)
        {
            if (!_resolutionDetectionAttempted)
            {
                await EnsureResolutionSectionInitializedAsync();
            }

            if (!string.IsNullOrEmpty(_resolutionAction1))
            {
                await LaunchHelperActionAsync(_resolutionAction1);
            }
        }

        private async void ResolutionButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!_resolutionDetectionAttempted)
            {
                await EnsureResolutionSectionInitializedAsync();
            }

            if (!string.IsNullOrEmpty(_resolutionAction2))
            {
                await LaunchHelperActionAsync(_resolutionAction2);
            }
        }

        private async void ResolutionButton3_Click(object sender, RoutedEventArgs e)
        {
            if (!_resolutionDetectionAttempted)
            {
                await EnsureResolutionSectionInitializedAsync();
            }

            if (!string.IsNullOrEmpty(_resolutionAction3))
            {
                await LaunchHelperActionAsync(_resolutionAction3);
            }
        }

        private static string FormatShortcut(IReadOnlyList<string> keys)
        {
            return WidgetSettingsStore.IsValidKeys(keys) ? string.Join(" + ", keys) : "Not Set";
        }

    }
}
