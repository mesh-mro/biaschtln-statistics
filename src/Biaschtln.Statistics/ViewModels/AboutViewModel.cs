using System.Reflection;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// ViewModel des Info-/About-Dialogs. Liest Programmname, Version, Hersteller und Copyright
/// aus den Assembly-Metadaten (siehe .csproj) und hält die Lizenzangabe. WPF-frei.
/// </summary>
public sealed class AboutViewModel
{
    public AboutViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();

        ProductName = GetAttribute<AssemblyProductAttribute>(assembly)?.Product is { Length: > 0 } product
            ? product
            : "Biaschtln-Statistik";

        Company = GetAttribute<AssemblyCompanyAttribute>(assembly)?.Company is { Length: > 0 } company
            ? company
            : "Markus Roider";

        Copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly)?.Copyright is { Length: > 0 } copyright
            ? copyright
            : "Copyright © 2026 Markus Roider";

        Description = GetAttribute<AssemblyDescriptionAttribute>(assembly)?.Description is { Length: > 0 } description
            ? description
            : "Auswertung von CSV-Exporten aus dem Kassensystem Biaschtln.";

        Version = ResolveVersion(assembly);
    }

    /// <summary>Programmname.</summary>
    public string ProductName { get; }

    /// <summary>Versionsangabe (informational bevorzugt, sonst Assembly-Version).</summary>
    public string Version { get; }

    /// <summary>Hersteller.</summary>
    public string Company { get; }

    /// <summary>Copyright-Zeile.</summary>
    public string Copyright { get; }

    /// <summary>Kurzbeschreibung des Programms.</summary>
    public string Description { get; }

    /// <summary>Lizenzangabe.</summary>
    public string License => "MIT-Lizenz";

    private static string ResolveVersion(Assembly assembly)
    {
        var informational = GetAttribute<AssemblyInformationalVersionAttribute>(assembly)?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Build-Metadaten (z. B. "1.0.0+abc123") abschneiden.
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    private static T? GetAttribute<T>(Assembly assembly) where T : Attribute =>
        assembly.GetCustomAttribute<T>();
}
