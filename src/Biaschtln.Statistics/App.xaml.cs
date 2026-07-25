using System.IO;
using System.Windows;
using Biaschtln.Statistics.Services;
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
        // Services
        services.AddSingleton<ICsvOrderImporter, CsvOrderImporter>();
        services.AddSingleton<IOrderDataService, OrderDataService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IOrderFilterService, OrderFilterService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ICsvExporter, CsvExporter>();

        // ViewModels + Views
        services.AddSingleton<FilterViewModel>();
        services.AddSingleton<CategorySalesViewModel>();
        services.AddSingleton<PreparationStaffViewModel>();
        services.AddSingleton<AnalyticsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services.GetRequiredService<MainWindow>().Show();

        // Als Startargumente übergebene CSV-Pfade direkt vorladen ("Öffnen mit"/Drag-auf-Exe).
        var paths = e.Args.Where(File.Exists).ToArray();
        if (paths.Length > 0)
        {
            _services.GetRequiredService<MainViewModel>().LoadPaths(paths);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();
        base.OnExit(e);
    }
}
