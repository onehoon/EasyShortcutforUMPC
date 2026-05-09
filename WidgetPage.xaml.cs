using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
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

        private async void WidgetPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettingsToUi();
            await InitializeResolutionSectionAsync();
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

        private async System.Threading.Tasks.Task InitializeResolutionSectionAsync()
        {
            DisplayResolutionSection.Visibility = Visibility.Collapsed;

            DateTimeOffset launchStamp = DateTimeOffset.UtcNow;
            bool launched = await LaunchHelperActionAsync(ActionDetectResolutionPresets);
            if (!launched)
            {
                return;
            }

            ResolutionFeatureState state = await ResolutionFeatureStateStore.WaitForStateRefreshAsync(launchStamp, 2200);
            if (!state.Available)
            {
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
            }
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
            await OpenSettingsDialogAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _gameBarWidget = e.Parameter as XboxGameBarWidget;
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
                await OpenSettingsDialogAsync(slotId);
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
                await _gameBarWidget.ActivateSettingsAsync();
            }
            catch (Exception ex)
            {
                DiagnosticsLog.Write($"ActivateSettingsAsync failed msg={ex.Message}");
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
            if (!string.IsNullOrEmpty(_resolutionAction1))
            {
                await LaunchHelperActionAsync(_resolutionAction1);
            }
        }

        private async void ResolutionButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_resolutionAction2))
            {
                await LaunchHelperActionAsync(_resolutionAction2);
            }
        }

        private async void ResolutionButton3_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_resolutionAction3))
            {
                await LaunchHelperActionAsync(_resolutionAction3);
            }
        }

        private async Task OpenSettingsDialogAsync(string focusCustomSlotId = null)
        {
            if (!string.IsNullOrEmpty(focusCustomSlotId))
            {
                WidgetSettings focusedDraft = CloneSettings(_settings);
                bool savedFocused = await ShowCustomShortcutEditorAsync(focusedDraft, focusCustomSlotId);
                if (savedFocused)
                {
                    _settings = WidgetSettingsStore.Normalize(focusedDraft);
                    WidgetSettingsStore.Save(_settings);
                    ApplySettingsToUi();
                }

                return;
            }

            WidgetSettings draft = CloneSettings(_settings);
            ContentDialog dialog = BuildSettingsDialog(draft);
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _settings = WidgetSettingsStore.Normalize(draft);
                WidgetSettingsStore.Save(_settings);
                ApplySettingsToUi();
            }
        }

        private ContentDialog BuildSettingsDialog(WidgetSettings draft)
        {
            var losslessCurrent = new TextBlock
            {
                Text = $"Current: {FormatShortcut(draft.BuiltInLosslessKeys)}",
                Foreground = new SolidColorBrush(Windows.UI.Colors.White),
                Opacity = 0.9,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var customCurrent1 = new TextBlock { Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom1")}", Foreground = new SolidColorBrush(Windows.UI.Colors.White), Opacity = 0.9 };
            var customCurrent2 = new TextBlock { Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom2")}", Foreground = new SolidColorBrush(Windows.UI.Colors.White), Opacity = 0.9 };
            var customCurrent3 = new TextBlock { Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom3")}", Foreground = new SolidColorBrush(Windows.UI.Colors.White), Opacity = 0.9 };

            var orderRowsPanel = new StackPanel { Spacing = 6 };
            void RefreshOrderRows()
            {
                orderRowsPanel.Children.Clear();
                for (int i = 0; i < draft.SectionOrder.Count; i++)
                {
                    string section = draft.SectionOrder[i];
                    var row = new Grid { ColumnSpacing = 8 };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var name = new TextBlock
                    {
                        Text = GetSectionDisplayName(section),
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Windows.UI.Colors.White)
                    };

                    var up = new Button { Content = "▲", MinWidth = 40, IsEnabled = i > 0 };
                    var down = new Button { Content = "▼", MinWidth = 40, IsEnabled = i < draft.SectionOrder.Count - 1 };

                    int index = i;
                    up.Click += (_, __) =>
                    {
                        if (index <= 0)
                        {
                            return;
                        }

                        string temp = draft.SectionOrder[index - 1];
                        draft.SectionOrder[index - 1] = draft.SectionOrder[index];
                        draft.SectionOrder[index] = temp;
                        RefreshOrderRows();
                    };

                    down.Click += (_, __) =>
                    {
                        if (index >= draft.SectionOrder.Count - 1)
                        {
                            return;
                        }

                        string temp = draft.SectionOrder[index + 1];
                        draft.SectionOrder[index + 1] = draft.SectionOrder[index];
                        draft.SectionOrder[index] = temp;
                        RefreshOrderRows();
                    };

                    Grid.SetColumn(name, 0);
                    Grid.SetColumn(up, 1);
                    Grid.SetColumn(down, 2);
                    row.Children.Add(name);
                    row.Children.Add(up);
                    row.Children.Add(down);
                    orderRowsPanel.Children.Add(row);
                }
            }

            RefreshOrderRows();

            var body = new StackPanel { Spacing = 12 };
            body.Children.Add(new TextBlock { Text = "Built-in Shortcuts", FontSize = 16, FontWeight = Windows.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Colors.White) });
            body.Children.Add(new TextBlock { Text = "Lossless Scaling", Foreground = new SolidColorBrush(Windows.UI.Colors.White) });
            body.Children.Add(losslessCurrent);

            var lsButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var changeLs = new Button { Content = "Change Shortcut" };
            var resetLs = new Button { Content = "Reset" };
            changeLs.Click += async (_, __) =>
            {
                List<string> updated = await ShowShortcutPickerAsync("Lossless Scaling", draft.BuiltInLosslessKeys, allowEmpty: false);
                if (updated == null)
                {
                    return;
                }

                draft.BuiltInLosslessKeys = updated;
                losslessCurrent.Text = $"Current: {FormatShortcut(draft.BuiltInLosslessKeys)}";
            };
            resetLs.Click += (_, __) =>
            {
                draft.BuiltInLosslessKeys = new List<string>(WidgetSettingsDefaults.DefaultLosslessKeys);
                losslessCurrent.Text = $"Current: {FormatShortcut(draft.BuiltInLosslessKeys)}";
            };
            lsButtons.Children.Add(changeLs);
            lsButtons.Children.Add(resetLs);
            body.Children.Add(lsButtons);

            body.Children.Add(new TextBlock { Text = "Custom Shortcuts", FontSize = 16, FontWeight = Windows.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Colors.White), Margin = new Thickness(0, 8, 0, 0) });
            body.Children.Add(BuildCustomSettingRow("custom1", "Custom 1", draft, customCurrent1));
            body.Children.Add(customCurrent1);
            body.Children.Add(BuildCustomSettingRow("custom2", "Custom 2", draft, customCurrent2));
            body.Children.Add(customCurrent2);
            body.Children.Add(BuildCustomSettingRow("custom3", "Custom 3", draft, customCurrent3));
            body.Children.Add(customCurrent3);

            body.Children.Add(new TextBlock { Text = "Layout Order", FontSize = 16, FontWeight = Windows.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Colors.White), Margin = new Thickness(0, 8, 0, 0) });
            body.Children.Add(new TextBlock { Text = "Fixed: Lossless Scaling + Settings", Foreground = new SolidColorBrush(Windows.UI.Colors.White), Opacity = 0.9 });
            body.Children.Add(orderRowsPanel);

            var layoutButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var resetLayout = new Button { Content = "Reset Layout" };
            resetLayout.Click += (_, __) =>
            {
                draft.SectionOrder = new List<string>(WidgetSettingsDefaults.DefaultSectionOrder);
                RefreshOrderRows();
            };
            layoutButtons.Children.Add(resetLayout);
            body.Children.Add(layoutButtons);

            body.Children.Add(new TextBlock { Text = "Reset", FontSize = 16, FontWeight = Windows.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Colors.White), Margin = new Thickness(0, 8, 0, 0) });
            var resetAll = new Button { Content = "Reset custom shortcuts" };
            resetAll.Click += (_, __) =>
            {
                draft.CustomShortcuts["custom1"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 1", Keys = new List<string>() };
                draft.CustomShortcuts["custom2"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 2", Keys = new List<string>() };
                draft.CustomShortcuts["custom3"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 3", Keys = new List<string>() };
                customCurrent1.Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom1")}";
                customCurrent2.Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom2")}";
                customCurrent3.Text = $"Current shortcut: {GetCustomTextForSettings(draft, "custom3")}";
            };
            body.Children.Add(resetAll);

            return new ContentDialog
            {
                Title = "Settings",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new ScrollViewer
                {
                    Content = body,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 480
                }
            };
        }

        private FrameworkElement BuildCustomSettingRow(string slotId, string title, WidgetSettings draft, TextBlock currentText)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock
            {
                Text = title,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Windows.UI.Colors.White)
            });

            var edit = new Button { Content = "Edit" };
            var reset = new Button { Content = "Reset" };

            edit.Click += async (_, __) =>
            {
                bool saved = await ShowCustomShortcutEditorAsync(draft, slotId);
                if (saved)
                {
                    currentText.Text = $"Current shortcut: {GetCustomTextForSettings(draft, slotId)}";
                }
            };

            reset.Click += (_, __) =>
            {
                draft.CustomShortcuts[slotId] = new CustomShortcutSlot
                {
                    Enabled = false,
                    Label = GetSlotDisplayName(slotId),
                    Keys = new List<string>()
                };
                currentText.Text = $"Current shortcut: {GetCustomTextForSettings(draft, slotId)}";
            };

            row.Children.Add(edit);
            row.Children.Add(reset);
            return row;
        }

        private async Task<bool> ShowCustomShortcutEditorAsync(WidgetSettings draft, string slotId)
        {
            if (!draft.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot))
            {
                return false;
            }

            List<string> currentKeys = slot.Keys ?? new List<string>();
            string currentModifier = "None";
            string currentKey = "Not Set";
            SplitShortcut(currentKeys, out currentModifier, out currentKey);

            var labelBox = new TextBox { Text = string.IsNullOrWhiteSpace(slot.Label) ? GetSlotDisplayName(slotId) : slot.Label };
            var enabledToggle = new ToggleSwitch { IsOn = slot.Enabled, Header = "Enabled" };

            var modifierCombo = new ComboBox();
            foreach (string modifier in GetModifierOptions())
            {
                modifierCombo.Items.Add(modifier);
            }

            modifierCombo.SelectedItem = GetModifierOptions().Contains(currentModifier) ? currentModifier : "None";

            var keyCombo = new ComboBox();
            foreach (string key in GetKeyOptions())
            {
                keyCombo.Items.Add(key);
            }

            keyCombo.SelectedItem = GetKeyOptions().Contains(currentKey) ? currentKey : "Not Set";

            var content = new StackPanel { Spacing = 8 };
            content.Children.Add(new TextBlock { Text = "Button label" });
            content.Children.Add(labelBox);
            content.Children.Add(enabledToggle);
            content.Children.Add(new TextBlock { Text = "Modifier" });
            content.Children.Add(modifierCombo);
            content.Children.Add(new TextBlock { Text = "Key" });
            content.Children.Add(keyCombo);

            ContentDialog editor = new()
            {
                Title = $"Edit {GetSlotDisplayName(slotId)}",
                Content = content,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Reset",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await editor.ShowAsync();
            if (result == ContentDialogResult.None)
            {
                return false;
            }

            if (result == ContentDialogResult.Secondary)
            {
                slot.Enabled = false;
                slot.Label = GetSlotDisplayName(slotId);
                slot.Keys = new List<string>();
                return true;
            }

            string selectedModifier = modifierCombo.SelectedItem as string ?? "None";
            string selectedKey = keyCombo.SelectedItem as string ?? "Not Set";
            List<string> newKeys = ComposeShortcut(selectedModifier, selectedKey);

            if (enabledToggle.IsOn && newKeys.Count == 0)
            {
                ContentDialog invalid = new()
                {
                    Title = "Invalid Shortcut",
                    Content = "Enabled shortcuts must include a valid key.",
                    CloseButtonText = "OK"
                };
                await invalid.ShowAsync();
                return false;
            }

            slot.Enabled = enabledToggle.IsOn;
            slot.Label = string.IsNullOrWhiteSpace(labelBox.Text) ? GetSlotDisplayName(slotId) : labelBox.Text.Trim();
            slot.Keys = newKeys;
            return true;
        }

        private async Task<List<string>> ShowShortcutPickerAsync(string title, IReadOnlyList<string> currentKeys, bool allowEmpty)
        {
            SplitShortcut(currentKeys, out string currentModifier, out string currentKey);

            var modifierCombo = new ComboBox();
            foreach (string modifier in GetModifierOptions())
            {
                modifierCombo.Items.Add(modifier);
            }

            modifierCombo.SelectedItem = GetModifierOptions().Contains(currentModifier) ? currentModifier : "None";

            var keyCombo = new ComboBox();
            foreach (string key in GetKeyOptions())
            {
                keyCombo.Items.Add(key);
            }

            keyCombo.SelectedItem = GetKeyOptions().Contains(currentKey) ? currentKey : "Not Set";

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock { Text = "Modifier" });
            panel.Children.Add(modifierCombo);
            panel.Children.Add(new TextBlock { Text = "Key" });
            panel.Children.Add(keyCombo);

            ContentDialog picker = new()
            {
                Title = $"Edit {title}",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await picker.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            List<string> keys = ComposeShortcut(modifierCombo.SelectedItem as string ?? "None", keyCombo.SelectedItem as string ?? "Not Set");
            if (!allowEmpty && keys.Count == 0)
            {
                ContentDialog invalid = new()
                {
                    Title = "Invalid Shortcut",
                    Content = "A valid key is required.",
                    CloseButtonText = "OK"
                };
                await invalid.ShowAsync();
                return null;
            }

            return keys;
        }

        private static List<string> ComposeShortcut(string modifier, string key)
        {
            var output = new List<string>();
            if (!string.IsNullOrWhiteSpace(modifier) && !string.Equals(modifier, "None", StringComparison.OrdinalIgnoreCase))
            {
                output.AddRange(modifier.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(key) && !string.Equals(key, "Not Set", StringComparison.OrdinalIgnoreCase))
            {
                output.Add(key.Trim());
            }

            return output;
        }

        private static string FormatShortcut(IReadOnlyList<string> keys)
        {
            return WidgetSettingsStore.IsValidKeys(keys) ? string.Join(" + ", keys) : "Not Set";
        }

        private static void SplitShortcut(IReadOnlyList<string> keys, out string modifier, out string key)
        {
            modifier = "None";
            key = "Not Set";
            if (!WidgetSettingsStore.IsValidKeys(keys))
            {
                return;
            }

            if (keys.Count == 1)
            {
                key = keys[0];
                return;
            }

            key = keys[keys.Count - 1];
            modifier = string.Join(" + ", keys.Take(keys.Count - 1));
        }

        private static IReadOnlyList<string> GetModifierOptions()
        {
            return new[] { "None", "Ctrl", "Alt", "Shift", "Ctrl + Alt", "Ctrl + Shift", "Alt + Shift", "Ctrl + Alt + Shift" };
        }

        private static IReadOnlyList<string> GetKeyOptions()
        {
            return new[]
            {
                "Not Set",
                "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
                "0","1","2","3","4","5","6","7","8","9",
                "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
                "Insert","Delete","Home","End","Page Up","Page Down","Space","Tab","Escape",
                "Arrow Up","Arrow Down","Arrow Left","Arrow Right"
            };
        }

        private static string GetSectionDisplayName(string sectionId)
        {
            return sectionId switch
            {
                "overlay" => "OptiScaler Overlay",
                "resolution" => "Display Resolution",
                "custom" => "Custom",
                _ => sectionId
            };
        }

        private static string GetSlotDisplayName(string slotId)
        {
            return slotId switch
            {
                "custom1" => "Custom 1",
                "custom2" => "Custom 2",
                "custom3" => "Custom 3",
                _ => "Custom"
            };
        }

        private static string GetCustomTextForSettings(WidgetSettings settings, string slotId)
        {
            return settings.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) && WidgetSettingsStore.IsConfigured(slot)
                ? FormatShortcut(slot.Keys)
                : "Not Set";
        }

        private static WidgetSettings CloneSettings(WidgetSettings source)
        {
            var clone = new WidgetSettings
            {
                Version = source.Version,
                BuiltInLosslessKeys = new List<string>(source.BuiltInLosslessKeys ?? new List<string>()),
                SectionOrder = new List<string>(source.SectionOrder ?? new List<string>())
            };

            clone.CustomShortcuts = new Dictionary<string, CustomShortcutSlot>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source.CustomShortcuts)
            {
                clone.CustomShortcuts[pair.Key] = new CustomShortcutSlot
                {
                    Enabled = pair.Value.Enabled,
                    Label = pair.Value.Label,
                    Keys = new List<string>(pair.Value.Keys ?? new List<string>())
                };
            }

            foreach (string slotId in new[] { "custom1", "custom2", "custom3" })
            {
                if (!clone.CustomShortcuts.ContainsKey(slotId))
                {
                    clone.CustomShortcuts[slotId] = new CustomShortcutSlot { Enabled = false, Label = GetSlotDisplayName(slotId), Keys = new List<string>() };
                }
            }

            if (clone.SectionOrder.Count != 3)
            {
                clone.SectionOrder = new List<string>(WidgetSettingsDefaults.DefaultSectionOrder);
            }

            return clone;
        }
    }
}
