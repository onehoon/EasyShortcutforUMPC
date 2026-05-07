using System;
using Windows.UI.Xaml.Controls;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            WidgetWebView.Navigate(new Uri("ms-appx-web:///GameBar/Widget.html"));
        }
    }
}
