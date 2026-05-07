using System;
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
    }
}
