using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IPickupSettings" />
public sealed class PickupSettings : IPickupSettings
{
    /// <summary>Standard-Abholstation-Benutzer.</summary>
    public const string DefaultPickupUser = "Abholstation";

    private string _pickupUser = DefaultPickupUser;

    public string PickupUser
    {
        get => _pickupUser;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized == _pickupUser)
            {
                return;
            }

            _pickupUser = normalized;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Changed;

    public bool IsPickup(OrderLine order) =>
        !string.IsNullOrEmpty(_pickupUser) &&
        string.Equals(order.User, _pickupUser, StringComparison.OrdinalIgnoreCase);
}
