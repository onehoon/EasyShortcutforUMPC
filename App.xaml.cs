using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class App : Application
    {
        private XboxGameBarWidget _gameBarWidget;

        public App()
        {
            InitializeComponent();
            DiagnosticsLog.Write("App ctor");
            Suspending += OnSuspending;
            UnhandledException += OnUnhandledException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            DiagnosticsLog.Write($"OnLaunched prelaunch={e.PrelaunchActivated} args='{e.Arguments}'");
            var rootFrame = EnsureRootFrame();

            if (!e.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    DiagnosticsLog.Write("OnLaunched navigate StandalonePage");
                    rootFrame.Navigate(typeof(StandalonePage), null);
                }

                DiagnosticsLog.Write("OnLaunched activate window");
                Window.Current.Activate();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            DiagnosticsLog.Write($"OnActivated kind={args?.Kind}");
            if (args == null)
            {
                DiagnosticsLog.Write("OnActivated args is null");
                ShowEmergencyFallback();
                return;
            }

            XboxGameBarWidgetActivatedEventArgs widgetArgs = null;

            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = args as IProtocolActivatedEventArgs;
                if (protocolArgs?.Uri?.Scheme == "ms-gamebarwidget")
                {
                    widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                    DiagnosticsLog.Write($"OnActivated protocol={protocolArgs.Uri}");
                }
            }

            if (widgetArgs != null && widgetArgs.IsLaunchActivation)
            {
                DiagnosticsLog.Write($"Widget activation appExtension={widgetArgs.AppExtensionId}");
                var rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;

                _gameBarWidget = new XboxGameBarWidget(
                    widgetArgs,
                    Window.Current.CoreWindow,
                    rootFrame);

                if (string.Equals(widgetArgs.AppExtensionId, "Widget2Settings", StringComparison.OrdinalIgnoreCase))
                {
                    rootFrame.Navigate(typeof(WidgetSettingsPage), _gameBarWidget);
                }
                else
                {
                    rootFrame.Navigate(typeof(WidgetPage), _gameBarWidget);
                }

                Window.Current.Closed += GameBarWindow_Closed;
                Window.Current.Activate();
                return;
            }

            var fallbackFrame = EnsureRootFrame();
            if (fallbackFrame.Content == null)
            {
                DiagnosticsLog.Write("OnActivated fallback navigate StandalonePage");
                fallbackFrame.Navigate(typeof(StandalonePage), null);
            }

            DiagnosticsLog.Write("OnActivated fallback activate window");
            Window.Current.Activate();
        }

        private void GameBarWindow_Closed(object sender, CoreWindowEventArgs e)
        {
            DiagnosticsLog.Write("GameBarWindow_Closed");
            _gameBarWidget = null;
            Window.Current.Closed -= GameBarWindow_Closed;
        }

        private Frame EnsureRootFrame()
        {
            if (Window.Current.Content is Frame rootFrame)
            {
                DiagnosticsLog.Write("EnsureRootFrame reuse existing");
                return rootFrame;
            }

            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = rootFrame;
            DiagnosticsLog.Write("EnsureRootFrame created new frame");
            return rootFrame;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            DiagnosticsLog.Write($"NavigationFailed page={e.SourcePageType?.FullName} msg={e.Exception?.Message}");
            e.Handled = true;
            ShowEmergencyFallback();
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            DiagnosticsLog.Write("OnSuspending");
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            DiagnosticsLog.Write($"UnhandledException msg={e.Message}");
            e.Handled = true;
            ShowEmergencyFallback();
        }

        private void ShowEmergencyFallback()
        {
            try
            {
                var root = new Grid
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 26, 28, 33)),
                    Padding = new Thickness(24)
                };

                var message = new TextBlock
                {
                    Text = "Easy Shortcut for UMPC is intended to be used from Xbox Game Bar.\n\nOpen Xbox Game Bar with Win+G, then launch Easy Shortcut for UMPC from the Widget menu.",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                };

                root.Children.Add(message);
                Window.Current.Content = root;
                Window.Current.Activate();
            }
            catch (Exception ex)
            {
                DiagnosticsLog.Write($"EmergencyFallback fail msg={ex.Message}");
            }
        }
    }
}
