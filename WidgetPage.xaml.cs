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

        private const string ActionInsert = "insert";
        private const string ActionAltInsert = "altinsert";
        private const string ActionHome = "home";
        private const string ActionEnd = "end";
        private const string ActionLosslessScaling = "losslessscaling";
        private const string ActionQuit = "quit";

        // Must stay in sync with desktop:ParameterGroup GroupId values in Package.appxmanifest.
        private const string GroupInsert = "InsertCommand";
        private const string GroupAltInsert = "AltInsertCommand";
        private const string GroupHome = "HomeCommand";
        private const string GroupEnd = "EndCommand";
        private const string GroupLosslessScaling = "LosslessScalingCommand";
        private const string GroupQuit = "QuitCommand";

        public WidgetPage()
        {
            InitializeComponent();
            DiagnosticsLog.Write("WidgetPage ctor");
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

        private async System.Threading.Tasks.Task LaunchHelperActionAsync(string action)
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
                _ => null
            };

            if (string.IsNullOrEmpty(groupId))
            {
                DiagnosticsLog.Write($"LaunchHelperAction unknown action={action}");
                return;
            }

            try
            {
                DiagnosticsLog.Write($"LaunchFullTrust group={groupId}");
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(groupId);
                DiagnosticsLog.Write($"LaunchFullTrust success action={action}");
            }
            catch (Exception ex)
            {
                DiagnosticsLog.Write($"LaunchFullTrust fail action={action} ex={ex.GetType().Name} msg={ex.Message}");
            }
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
    }
}
