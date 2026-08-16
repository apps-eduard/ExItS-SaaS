using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// V1 fee formula duplicated from Platform <c>BranchDeliveryPolicy.CalculateFee</c>.
/// Keep in sync when Platform delivery pricing changes.
/// </summary>
public static class CustomerOrderDeliveryFeeCalculator
{
    public sealed record Quote(
        decimal DistanceKm,
        decimal ExtraDistanceKm,
        decimal DistanceCharge,
        decimal DeliveryFee,
        bool FreeDeliveryApplied);

    public static Quote Calculate(
        CustomerOrderBranchDeliveryPolicySnapshot policy,
        decimal merchandiseSubtotal,
        decimal distanceKm)
    {
        if (merchandiseSubtotal < policy.MinimumOrderAmount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                $"Merchandise subtotal must be at least {policy.MinimumOrderAmount:0.00}.");
        }

        if (distanceKm > policy.MaximumDeliveryDistanceKm)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                $"Distance exceeds the maximum delivery distance of {policy.MaximumDeliveryDistanceKm:0.###} km.");
        }

        if (policy.FreeDeliveryThreshold is decimal threshold && merchandiseSubtotal >= threshold)
        {
            return new Quote(distanceKm, 0m, 0m, 0m, FreeDeliveryApplied: true);
        }

        var extraDistance = Math.Max(0m, distanceKm - policy.IncludedDistanceKm);
        var distanceCharge = SaleMoney.RoundMoney(extraDistance * policy.AdditionalFeePerKm);
        var fee = SaleMoney.RoundMoney(policy.BaseDeliveryFee + distanceCharge);
        return new Quote(distanceKm, extraDistance, distanceCharge, fee, FreeDeliveryApplied: false);
    }
}
