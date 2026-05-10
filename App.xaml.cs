using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        // Keep widget instances alive for the lifetime of each Game Bar window.
        private XboxGameBarWidget _mainWidget;
        private XboxGameBarWidget _settingsWidget;
        private readonly Dictionary<CoreWindow, string> _widgetWindows = new Dictionary<CoreWindow, string>();

        public App()
        {
            InitializeComponent();
            try
            {
                PackageVersion version = Package.Current.Id.Version;
                DiagnosticsLog.Write($"App ctor version={version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("App ctor version read failed", ex);
                DiagnosticsLog.Write("App ctor version=unknown");
            }
            Suspending += OnSuspending;
            UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("OnLaunched fail", ex);
                ShowEmergencyFallback();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            try
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

                    XboxGameBarWidget widget = new XboxGameBarWidget(
                        widgetArgs,
                        Window.Current.CoreWindow,
                        rootFrame);

                    if (string.Equals(widgetArgs.AppExtensionId, "GamingWidgetSettings", StringComparison.OrdinalIgnoreCase))
                    {
                        _settingsWidget = widget;
                        _widgetWindows[Window.Current.CoreWindow] = "GamingWidgetSettings";
                        DiagnosticsLog.Write("Settings widget activated: GamingWidgetSettings");
                        rootFrame.Navigate(typeof(WidgetSettingsPage), widget);
                        DiagnosticsLog.Write("WidgetSettingsPage navigated");
                    }
                    else if (string.Equals(widgetArgs.AppExtensionId, "GamingWidget", StringComparison.OrdinalIgnoreCase))
                    {
                        _mainWidget = widget;
                        _widgetWindows[Window.Current.CoreWindow] = "GamingWidget";
                        rootFrame.Navigate(typeof(WidgetPage), widget);
                    }
                    else
                    {
                        DiagnosticsLog.Write($"Unknown Game Bar widget AppExtensionId={widgetArgs.AppExtensionId}");
                        return;
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
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("OnActivated fail", ex);
                ShowEmergencyFallback();
            }
        }

        private void GameBarWindow_Closed(object sender, CoreWindowEventArgs e)
        {
            DiagnosticsLog.Write("GameBarWindow_Closed");
            if (sender is CoreWindow coreWindow && _widgetWindows.TryGetValue(coreWindow, out string appExtensionId))
            {
                if (string.Equals(appExtensionId, "GamingWidgetSettings", StringComparison.OrdinalIgnoreCase))
                {
                    _settingsWidget = null;
                }
                else
                {
                    _mainWidget = null;
                }

                _widgetWindows.Remove(coreWindow);
            }

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
            if (e.Exception != null)
            {
                DiagnosticsLog.WriteException("UnhandledException detail", e.Exception);
            }
            e.Handled = true;
            ShowEmergencyFallback();
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            DiagnosticsLog.Write($"AppDomain.UnhandledException terminating={e.IsTerminating}");
            DiagnosticsLog.WriteException("AppDomain.UnhandledException detail", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            DiagnosticsLog.WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
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
                    Text = "Quick Buttons for Game Bar is intended to be used from Xbox Game Bar.\n\nOpen Xbox Game Bar with Win+G, then launch Quick Buttons for Game Bar from the Widget menu.",
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
