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
            Loaded += MainPage_Loaded;
            WidgetWebView.NavigationCompleted += WidgetWebView_NavigationCompleted;
            WidgetWebView.NavigationFailed += WidgetWebView_NavigationFailed;
            WidgetWebView.ScriptNotify += WidgetWebView_ScriptNotify;
            WidgetWebView.NavigationStarting += WidgetWebView_NavigationStarting;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Loading: ms-appx-web:///GameBar/Widget.html";
            WidgetWebView.Navigate(new Uri("ms-appx-web:///GameBar/Widget.html"));
        }

        private void WidgetWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            StatusText.Text = args.IsSuccess ? "Widget loaded" : $"Load failed: {args.WebErrorStatus}";
        }

        private void WidgetWebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            StatusText.Text = $"Navigation failed: {e.WebErrorStatus}";
        }

        private async void WidgetWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs e)
        {
            if (e?.Uri == null || !string.Equals(e.Uri.Scheme, "easyshortcut", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Cancel = true;
            var action = e.Uri.Host?.Trim().ToLowerInvariant() ?? string.Empty;
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
                StatusText.Text = $"Unknown cmd: {action}";
                return;
            }

            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(groupId);
                StatusText.Text = $"Helper launched: {action}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Launch failed: {action} / {ex.GetType().Name}";
            }
        }

        private async void WidgetWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var value = e?.Value ?? string.Empty;
            if (!value.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var action = value.Substring(4).Trim().ToLowerInvariant();
            await LaunchHelperActionAsync(action);
        }

        private async System.Threading.Tasks.Task LaunchHelperActionAsync(string action)
        {
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
                StatusText.Text = $"Unknown cmd: {action}";
                return;
            }

            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(groupId);
                StatusText.Text = $"Helper launched: {action}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Launch failed: {action} / {ex.GetType().Name}";
            }
        }
    }
}
