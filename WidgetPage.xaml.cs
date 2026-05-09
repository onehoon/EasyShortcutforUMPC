using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class WidgetPage : Page
    {
        private bool _isQuitDialogOpen;
        private string _resolutionAction1;
        private string _resolutionAction2;
        private string _resolutionAction3;

        private const string ActionInsert = "insert";
        private const string ActionAltInsert = "altinsert";
        private const string ActionHome = "home";
        private const string ActionEnd = "end";
        private const string ActionLosslessScaling = "losslessscaling";
        private const string ActionQuit = "quit";
        private const string ActionDetectResolutionPresets = "detect-resolution-presets";
        private const string ActionSetResolution1200 = "set-resolution-1920-1200";
        private const string ActionSetResolution1080 = "set-resolution-1920-1080";
        private const string ActionSetResolution1050 = "set-resolution-1680-1050";
        private const string ActionSetResolution900 = "set-resolution-1600-900";
        private const string ActionSetResolution720 = "set-resolution-1280-720";

        // Must stay in sync with desktop:ParameterGroup GroupId values in Package.appxmanifest.
        private const string GroupInsert = "InsertCommand";
        private const string GroupAltInsert = "AltInsertCommand";
        private const string GroupHome = "HomeCommand";
        private const string GroupEnd = "EndCommand";
        private const string GroupLosslessScaling = "LosslessScalingCommand";
        private const string GroupQuit = "QuitCommand";
        private const string GroupDetectResolutionPresets = "DetectResolutionPresetsCommand";
        private const string GroupSetResolution1200 = "SetResolution1920x1200Command";
        private const string GroupSetResolution1080 = "SetResolution1920x1080Command";
        private const string GroupSetResolution1050 = "SetResolution1680x1050Command";
        private const string GroupSetResolution900 = "SetResolution1600x900Command";
        private const string GroupSetResolution720 = "SetResolution1280x720Command";

        public WidgetPage()
        {
            InitializeComponent();
            DiagnosticsLog.Write("WidgetPage ctor");
            Loaded += WidgetPage_Loaded;
        }

        private async void WidgetPage_Loaded(object sender, RoutedEventArgs e)
        {
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
                ActionHome => GroupHome,
                ActionEnd => GroupEnd,
                ActionLosslessScaling => GroupLosslessScaling,
                ActionQuit => GroupQuit,
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
                DisplayResolutionSection.Visibility = Visibility.Visible;
                return;
            }

            if (state.Group == ResolutionPresetGroup.Group1080)
            {
                ConfigureResolutionButtons(
                    "1080p", ActionSetResolution1080,
                    "900p", ActionSetResolution900,
                    "720p", ActionSetResolution720);
                DisplayResolutionSection.Visibility = Visibility.Visible;
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

        private async void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionInsert);
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionHome);
        }

        private async void AltInsertButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionAltInsert);
        }

        private async void EndButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionEnd);
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync(ActionLosslessScaling);
        }

        private async void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isQuitDialogOpen)
            {
                return;
            }

            _isQuitDialogOpen = true;
            try
            {
                ContentDialog confirm = new()
                {
                    Title = "Close App",
                    Content = "Send Alt+F4 shortcut now?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No",
                    DefaultButton = ContentDialogButton.Close
                };

                ContentDialogResult result = await confirm.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await LaunchHelperActionAsync(ActionQuit);
                }
            }
            finally
            {
                _isQuitDialogOpen = false;
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
    }
}
