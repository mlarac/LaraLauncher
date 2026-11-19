using System.Net.Http;
using System.Windows;
using LaraLauncher.App.Input;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Services;
using LaraLauncher.Infrastructure.Persistence;
using LaraLauncher.Infrastructure.Weather;
using LaraLauncher.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace LaraLauncher.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        bool startWindowed = e.Args.Any(arg =>
            arg.Equals("--windowed", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-w", StringComparison.OrdinalIgnoreCase));

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.ApplyWindowMode(!startWindowed);
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Persistence & Windows Infrastructure
        services.AddSingleton<IConfigRepository>(_ => new JsonConfigRepository());
        services.AddSingleton<IconCacheService>(_ => new IconCacheService());
        services.AddSingleton<IInstalledAppsScanner, WindowsInstalledAppsScanner>();
        services.AddSingleton<IAppLauncherService, WindowsAppLauncherService>();

        // Weather (Open-Meteo + Local Disk Cache Decorator)
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
        services.AddSingleton<IWeatherCacheStore>(_ => new JsonWeatherCacheStore());
        services.AddSingleton<OpenMeteoWeatherService>();
        services.AddSingleton<ILocationSearchService>(sp => sp.GetRequiredService<OpenMeteoWeatherService>());
        services.AddSingleton<IWeatherService>(sp => new CachedWeatherService(
            sp.GetRequiredService<OpenMeteoWeatherService>(),
            sp.GetRequiredService<IWeatherCacheStore>()));

        // Input routing (external IInputProvider implementations such as XInput register here in Phase 5)
        services.AddSingleton<WpfInputRouter>();

        // ViewModels
        services.AddSingleton<AppManagerViewModel>();
        services.AddSingleton<WeatherViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
