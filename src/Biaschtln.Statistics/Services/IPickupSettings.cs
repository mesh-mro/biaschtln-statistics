using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <summary>
/// Hält den als "Abholstation" geltenden Benutzer. Dessen Bestellungen werden von Gästen
/// selbst abgeholt (kein Tischplatz) und daher nicht der Tisch-Auswertung zugerechnet.
/// Standard ist <see cref="PickupSettings.DefaultPickupUser"/>, per UI umstellbar.
/// </summary>
public interface IPickupSettings
{
    /// <summary>Der Benutzername, dessen Bestellungen als Abholung (kein Tisch) gelten.</summary>
    string PickupUser { get; set; }

    /// <summary>Wird ausgelöst, wenn sich <see cref="PickupUser"/> geändert hat.</summary>
    event EventHandler? Changed;

    /// <summary>True, wenn die Position dem Abholstation-Benutzer zuzuordnen ist.</summary>
    bool IsPickup(OrderLine order);
}
