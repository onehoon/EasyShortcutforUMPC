using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            DiagnosticsLog.Write("MainPage ctor");
        }

        private async System.Threading.Tasks.Task LaunchHelperActionAsync(string action)
        {
            DiagnosticsLog.Write($"LaunchHelperAction start action={action}");

            string groupId = action switch
            {
                "insert" => "InsertCommand",
                "home" => "HomeCommand",
                "end" => "EndCommand",
                "capture" => "CaptureCommand",
                "quit" => "QuitCommand",
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
            await LaunchHelperActionAsync("insert");
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync("home");
        }

        private async void EndButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync("end");
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchHelperActionAsync("capture");
        }

        private async void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirm = new()
            {
                Title = "Force Quit",
                Content = "Run Alt+F4 now?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await LaunchHelperActionAsync("quit");
            }
        }
    }
}
