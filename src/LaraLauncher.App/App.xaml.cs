using System.Windows;
using LaraLauncher.App.Input;
using LaraLauncher.App.ViewModels;
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
        // Input routing (external IInputProvider implementations such as XInput register here in Phase 5)
        services.AddSingleton<WpfInputRouter>();

        // ViewModels
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
