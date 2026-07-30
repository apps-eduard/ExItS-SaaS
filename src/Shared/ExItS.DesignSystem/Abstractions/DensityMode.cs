namespace ExItS.DesignSystem.Abstractions;

/// <summary>
/// Layout density used by design-system components. Compact is the default for
/// PinoyBusinessPOS (cashier information density) while remaining touch-friendly
/// (minimum ~44px targets). Comfortable increases padding and control height.
/// </summary>
public enum DensityMode
{
    Compact,
    Comfortable,
}
