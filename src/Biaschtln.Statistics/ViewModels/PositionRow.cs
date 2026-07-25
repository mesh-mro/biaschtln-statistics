using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Eine Bestellposition in tabellarischer Form — für die Datentabelle und den CSV-Export
/// der Positionsliste. Deutsche Feldnamen dienen zugleich als Spalten-/CSV-Überschriften.
/// </summary>
public sealed record PositionRow(
    DateTime Zeitpunkt,
    int Bestellung,
    string Tisch,
    string Artikel,
    string Kategorie,
    decimal Preis,
    int? Zubereitung,
    string Benutzer,
    string? Zahlung,
    bool Bezahlt,
    bool Storno)
{
    public static PositionRow From(OrderLine o) => new(
        o.OrderedAt,
        o.OrderId,
        o.Table,
        o.Article,
        o.Category,
        o.Price,
        o.PreparationSeconds,
        o.User,
        o.PaymentMethod,
        o.IsPaid,
        o.IsCanceled);
}
