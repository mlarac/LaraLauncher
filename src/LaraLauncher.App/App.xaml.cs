using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using LaraLauncher.App.Input;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Services;
using LaraLauncher.Infrastructure.Audio;
using LaraLauncher.Infrastructure.Input;
using LaraLauncher.Infrastructure.Persistence;
using LaraLauncher.Infrastructure.Weather;
using LaraLauncher.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace LaraLauncher.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private SingleInstanceGuard? _singleInstanceGuard;
    private IAppLogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logger = new FileAppLogger();
        RegisterGlobalExceptionHandlers();

        _singleInstanceGuard = new SingleInstanceGuard();
        if (!_singleInstanceGuard.IsPrimaryInstance)
        {
            _logger.LogInfo("Second instance detected; signaling primary instance and exiting.");
            _singleInstanceGuard.SignalPrimaryInstance();
            Shutdown();
            return;
        }

        _logger.LogInfo("Starting LaraLauncher...");

        var services = new ServiceCollection();
        ConfigureServices(services, _logger);
        _serviceProvider = services.BuildServiceProvider();

        bool forceWindowed = e.Args.Any(arg =>
            arg.Equals("--windowed", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-w", StringComparison.OrdinalIgnoreCase));

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.ConfigureStartupWindowOverride(forceWindowed);
        mainWindow.ApplyWindowMode(!forceWindowed);

        _singleInstanceGuard.SecondInstanceSignaled += (_, _) =>
        {
            Dispatcher.BeginInvoke(mainWindow.RestoreAndActivate);
        };
        _singleInstanceGuard.StartListeningForSecondInstances();

        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IAppLogger logger)
    {
        // Logging & Audio
        services.AddSingleton(logger);
        services.AddSingleton<INavigationSoundService, NavigationSoundService>();

        // Persistence & Windows Infrastructure
        services.AddSingleton<IConfigRepository>(_ => new JsonConfigRepository());
        services.AddSingleton<IconCacheService>(_ => new IconCacheService());
        services.AddSingleton<IInstalledAppsScanner, WindowsInstalledAppsScanner>();
        services.AddSingleton<IAppLauncherService, WindowsAppLauncherService>();
        services.AddSingleton<IStartupService, WindowsStartupService>();
        services.AddSingleton<ISystemPowerService, WindowsPowerService>();

        // Weather (Open-Meteo + Local Disk Cache Decorator)
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
        services.AddSingleton<IWeatherCacheStore>(_ => new JsonWeatherCacheStore());
        services.AddSingleton<OpenMeteoWeatherService>();
        services.AddSingleton<ILocationSearchService>(sp => sp.GetRequiredService<OpenMeteoWeatherService>());
        services.AddSingleton<IWeatherService>(sp => new CachedWeatherService(
            sp.GetRequiredService<OpenMeteoWeatherService>(),
            sp.GetRequiredService<IWeatherCacheStore>()));

        // Input Providers & Router (Keyboard/Remote + XInput Gamepad)
        services.AddSingleton<XInputGamepadProvider>();
        services.AddSingleton<IInputProvider>(sp => sp.GetRequiredService<XInputGamepadProvider>());
        services.AddSingleton<WpfInputRouter>();

        // ViewModels
        services.AddSingleton<AppManagerViewModel>();
        services.AddSingleton<WeatherViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            _logger?.LogError("Unhandled AppDomain exception.", args.ExceptionObject as Exception);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.LogError("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError("Unhandled UI dispatcher exception.", e.Exception);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInfo("Shutting down LaraLauncher.");
        _singleInstanceGuard?.Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
