using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class MainPage : Page
    {
        private const string ActionInsert = "insert";
        private const string ActionHome = "home";
        private const string ActionEnd = "end";
        private const string ActionLosslessScaling = "losslessscaling";
        private const string ActionQuit = "quit";

        // Must stay in sync with desktop:ParameterGroup GroupId values in both manifests.
        private const string GroupInsert = "InsertCommand";
        private const string GroupHome = "HomeCommand";
        private const string GroupEnd = "EndCommand";
        private const string GroupLosslessScaling = "LosslessScalingCommand";
        private const string GroupQuit = "QuitCommand";

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
                ActionInsert => GroupInsert,
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
