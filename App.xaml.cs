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

namespace Quick_Buttons_for_Game_Bar
{
    public sealed partial class App : Application
    {
        // Keep widget instances alive for the lifetime of each Game Bar window.
        private XboxGameBarWidget _mainWidget;
        private XboxGameBarWidget _settingsWidget;
        private readonly Dictionary<CoreWindow, string> _widgetWindows = new Dictionary<CoreWindow, string>();

        public App()
        {
            try
            {
                UnhandledException += OnUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            }
            catch
            {
                // Never throw from App constructor.
            }

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("App InitializeComponent failed", ex);
            }

            try
            {
                Suspending += OnSuspending;
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("App Suspending hook failed", ex);
            }

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
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                DiagnosticsLog.Write($"OnLaunched prelaunch={e.PrelaunchActivated} args='{e.Arguments}'");
                if (!e.PrelaunchActivated)
                {
                    ShowEmergencyFallback();
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

                if (args is XboxGameBarWidgetActivatedEventArgs widgetArgs)
                {
                    if (widgetArgs.IsLaunchActivation)
                    {
                        ActivateGameBarWidget(widgetArgs);
                        return;
                    }

                    DiagnosticsLog.Write($"Widget reactivation ignored appExtension={widgetArgs.AppExtensionId}");
                    return;
                }

                DiagnosticsLog.Write("OnActivated non-widget fallback");
                ShowEmergencyFallback();
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

        private void ActivateGameBarWidget(XboxGameBarWidgetActivatedEventArgs widgetArgs)
        {
            DiagnosticsLog.Write($"Widget activation appExtension={widgetArgs?.AppExtensionId}");

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = rootFrame;

            XboxGameBarWidget widget;
            try
            {
                widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("XboxGameBarWidget creation failed", ex);
                ShowEmergencyFallback();
                return;
            }

            if (string.Equals(widgetArgs.AppExtensionId, "GamingWidgetSettings", StringComparison.OrdinalIgnoreCase))
            {
                _settingsWidget = widget;
                _widgetWindows[Window.Current.CoreWindow] = "GamingWidgetSettings";
                DiagnosticsLog.Write("Settings widget activated: GamingWidgetSettings");
                if (!SafeNavigate(rootFrame, typeof(WidgetSettingsPage), widget, "WidgetSettingsPage"))
                {
                    ShowEmergencyFallback();
                    return;
                }
            }
            else if (string.Equals(widgetArgs.AppExtensionId, "GamingWidget", StringComparison.OrdinalIgnoreCase))
            {
                _mainWidget = widget;
                _widgetWindows[Window.Current.CoreWindow] = "GamingWidget";
                if (!SafeNavigate(rootFrame, typeof(WidgetPage), widget, "WidgetPage"))
                {
                    ShowEmergencyFallback();
                    return;
                }
            }
            else
            {
                DiagnosticsLog.Write($"Unknown Game Bar widget AppExtensionId={widgetArgs.AppExtensionId}");
                ShowEmergencyFallback();
                return;
            }

            try
            {
                Window.Current.Closed += GameBarWindow_Closed;
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException("Window.Closed hook failed", ex);
            }

            Window.Current.Activate();
        }

        private bool SafeNavigate(Frame frame, Type pageType, object parameter, string pageName)
        {
            try
            {
                bool navigated = frame.Navigate(pageType, parameter);
                DiagnosticsLog.Write($"{pageName} navigated={navigated}");
                return navigated;
            }
            catch (Exception ex)
            {
                DiagnosticsLog.WriteException($"{pageName} navigate failed", ex);
                return false;
            }
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
                    Text = "Quick Buttons for Game Bar is an Xbox Game Bar widget.\n\nTo use it:\n1. Press Win + G.\n2. Open the Widget menu.\n3. Select Quick Buttons for Game Bar.\n4. Pin the widget if desired.",
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
                try
                {
                    DiagnosticsLog.Write($"EmergencyFallback failed: {ex.Message}");
                }
                catch
                {
                    // Final safety net. Never throw.
                }
            }
        }
    }
}
