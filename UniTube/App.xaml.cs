﻿using System;
using System.Threading.Tasks;

using Template10.Common;
using Template10.Utils;

using UniTube.Core.Authentication;
using UniTube.Helpers;
using UniTube.Services.SettingsServices;
using UniTube.Views;

using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace UniTube
{
    sealed partial class App : BootStrapper
    {
        public App()
        {
            InitializeComponent();

            ShowShellBackButton = false;
            SplashFactory = (s) => new Splash(s);

            var settings = SettingsService.Instance;
            if (settings.AppTheme != ElementTheme.Default)
            {
                RequestedTheme = settings.AppTheme.ToApplicationTheme();
            }
        }

        private async void OnDisplayOrientationChanged(DisplayInformation sender, object args)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                if (sender.CurrentOrientation == DisplayOrientations.Landscape || sender.CurrentOrientation == DisplayOrientations.LandscapeFlipped)
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        await statusBar.HideAsync();
                    }
                }
                else
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        await statusBar.ShowAsync();
                    }
                }
            }
        }

        public override UIElement CreateRootElement(IActivatedEventArgs e)
        {
            var navigationService = NavigationServiceFactory(BackButton.Attach, ExistingContent.Include);
            return new Controls.LoadingView
            {
                Content = navigationService.Frame,
                RingForeground = Resources["MainColorBrush"] as Brush
            };
        }

        public override async Task OnInitializeAsync(IActivatedEventArgs args)
        {
            var keys = PageKeys<Pages>();
            keys.TryAdd(Pages.History, typeof(HistoryPage));
            keys.TryAdd(Pages.Master, typeof(MasterPage));
            keys.TryAdd(Pages.Saved, typeof(SavedPage));
            keys.TryAdd(Pages.Settings, typeof(SettingsPage));

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            var display = DisplayInformation.GetForCurrentView();
            display.OrientationChanged += OnDisplayOrientationChanged;

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                if (display.ScreenWidthInRawPixels >= 1152 && display.ScreenHeightInRawPixels >= 768)
                {
                    ApplicationView.PreferredLaunchViewSize = new Size(1024, 675);
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                }

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null)
                {
                    titleBar.BackgroundColor = "#0000".ToColor();
                    titleBar.ForegroundColor = "#FFF".ToColor();

                    titleBar.InactiveBackgroundColor = "#0000".ToColor();
                    titleBar.InactiveForegroundColor = "#FFF".ToColor();

                    titleBar.ButtonBackgroundColor = "#0000".ToColor();
                    titleBar.ButtonForegroundColor = "#FFF".ToColor();

                    titleBar.ButtonHoverBackgroundColor = "#19FFFFFF".ToColor();
                    titleBar.ButtonHoverForegroundColor = "#FFF".ToColor();

                    titleBar.ButtonPressedBackgroundColor = "#33FFFFFF".ToColor();
                    titleBar.ButtonPressedForegroundColor = "#FFF".ToColor();

                    titleBar.ButtonInactiveBackgroundColor = "#0000".ToColor();
                    titleBar.ButtonInactiveForegroundColor = "#FFF".ToColor();
                }
            }

            var statusBarHideTask = Task.CompletedTask;

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = StatusBar.GetForCurrentView();
                if (statusBar != null)
                {
                    statusBar.BackgroundOpacity = 0;

                    statusBar.ForegroundColor = "#B2FFFFFF".ToColor();

                    if (display.CurrentOrientation == DisplayOrientations.Landscape || display.CurrentOrientation == DisplayOrientations.LandscapeFlipped)
                    {
                        statusBarHideTask = statusBar.HideAsync().AsTask();
                    }
                }
            }

            RegisterBackgroundTask();

            await statusBarHideTask;
        }

        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            NavigationService.Navigate(Pages.Master);
            return Task.CompletedTask;
        }

        private void RegisterBackgroundTask()
        {
            var taskRegistered = false;
            var liveTileTaskName = "LiveTileTask";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == liveTileTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder
                {
                    Name = liveTileTaskName,
                    TaskEntryPoint = "UniTube.Tasks.LiveTileTask",
                    CancelOnConditionLoss = true,
                    IsNetworkRequested = true
                };
                builder.SetTrigger(new TimeTrigger(15, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                var task = builder.Register();
            }
        }
    }
}
