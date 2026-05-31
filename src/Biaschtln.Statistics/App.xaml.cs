using System.Windows;
using Biaschtln.Statistics.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Biaschtln.Statistics;

/// <summary>
/// Interaction logic for App.xaml. Baut den DI-Container auf und loest das
/// Hauptfenster daraus auf (kein StartupUri).
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Zentrale Registrierung von Services und ViewModels. Wird in den folgenden
    /// Arbeitspaketen (CSV-Import, Statistik, Filter, weitere ViewModels) erweitert.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();
        base.OnExit(e);
    }
}
