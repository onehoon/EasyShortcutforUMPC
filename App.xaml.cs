using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class App : Application
    {
        private XboxGameBarWidget _gameBarWidget;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (Window.Current.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (!e.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                Window.Current.Activate();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args is XboxGameBarWidgetActivatedEventArgs widgetArgs)
            {
                if (Window.Current.Content is not Frame rootFrame)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    Window.Current.Content = rootFrame;
                }

                if (_gameBarWidget == null)
                {
                    _gameBarWidget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
                }

                if (rootFrame.Content == null || widgetArgs.IsLaunchActivation)
                {
                    rootFrame.Navigate(typeof(MainPage), _gameBarWidget);
                }

                Window.Current.Closed -= GameBarWindow_Closed;
                Window.Current.Closed += GameBarWindow_Closed;
                Window.Current.Activate();
                return;
            }

            base.OnActivated(args);
        }

        private void GameBarWindow_Closed(object sender, CoreWindowEventArgs e)
        {
            _gameBarWidget = null;
            Window.Current.Closed -= GameBarWindow_Closed;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load page '{e.SourcePageType.FullName}'.");
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
