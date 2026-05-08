using System;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class MainPage : Page
    {
        private const string ActionInsert = "insert";
        private const string ActionAltInsert = "altinsert";
        private const string ActionHome = "home";
        private const string ActionEnd = "end";
        private const string ActionLosslessScaling = "losslessscaling";
        private const string ActionQuit = "quit";

        // Must stay in sync with desktop:ParameterGroup GroupId values in both manifests.
        private const string GroupInsert = "InsertCommand";
        private const string GroupAltInsert = "AltInsertCommand";
        private const string GroupHome = "HomeCommand";
        private const string GroupEnd = "EndCommand";
        private const string GroupLosslessScaling = "LosslessScalingCommand";
        private const string GroupQuit = "QuitCommand";

        public MainPage()
        {
            InitializeComponent();
            DiagnosticsLog.Write("MainPage ctor");
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPointerOver");
        }

        private void Button_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPointerOver");
        }

        private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPressed");
        }

        private void Button_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackgroundPointerOver");
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ApplyStateBrush(sender as Button, "ButtonBackground");
        }

        private static void ApplyStateBrush(Button button, string key)
        {
            if (button == null)
            {
                return;
            }

            (Color normal, Color hover, Color pressed) palette = button.Name switch
            {
                "CaptureButton" => (ColorFrom("#8C2B5E98"), ColorFrom("#8C3A6FAB"), ColorFrom("#8C457ABC")),
                "InsertButton" => (ColorFrom("#2C5535"), ColorFrom("#3A6B45"), ColorFrom("#447A50")),
                "AltInsertButton" => (ColorFrom("#2C5535"), ColorFrom("#3A6B45"), ColorFrom("#447A50")),
                _ => (ColorFrom("#8C44484F"), ColorFrom("#8C545963"), ColorFrom("#8C5B616B"))
            };

            var color = key switch
            {
                "ButtonBackgroundPressed" => palette.pressed,
                "ButtonBackgroundPointerOver" => palette.hover,
                _ => palette.normal
            };

            button.Background = new SolidColorBrush(color);
        }

        private static Color ColorFrom(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8)
            {
                return Color.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            }

            return Color.FromArgb(
                0xFF,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
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
    }
}
