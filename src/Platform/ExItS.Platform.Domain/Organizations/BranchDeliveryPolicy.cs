using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Per-branch delivery pricing policy. Separate from <see cref="OrganizationBranch"/> identity/location.
/// V1 uses straight-line distance via <see cref="IDeliveryDistanceCalculator"/> (Haversine).
/// </summary>
public sealed class BranchDeliveryPolicy
{
    public const int DistanceScale = 3;
    public const int MoneyScale = 2;

    public OrganizationBranchId BranchId { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public decimal MinimumOrderAmount { get; private set; }
    public decimal BaseDeliveryFee { get; private set; }
    public decimal IncludedDistanceKm { get; private set; }
    public decimal AdditionalFeePerKm { get; private set; }
    public decimal MaximumDeliveryDistanceKm { get; private set; }
    public decimal? FreeDeliveryThreshold { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BranchDeliveryPolicy(
        OrganizationBranchId branchId,
        PlatformOrganizationId organizationId,
        decimal minimumOrderAmount,
        decimal baseDeliveryFee,
        decimal includedDistanceKm,
        decimal additionalFeePerKm,
        decimal maximumDeliveryDistanceKm,
        decimal? freeDeliveryThreshold,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        BranchId = branchId;
        OrganizationId = organizationId;
        MinimumOrderAmount = minimumOrderAmount;
        BaseDeliveryFee = baseDeliveryFee;
        IncludedDistanceKm = includedDistanceKm;
        AdditionalFeePerKm = additionalFeePerKm;
        MaximumDeliveryDistanceKm = maximumDeliveryDistanceKm;
        FreeDeliveryThreshold = freeDeliveryThreshold;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BranchDeliveryPolicy CreateDefault(
        OrganizationBranchId branchId,
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow) =>
        Create(
            branchId,
            organizationId,
            minimumOrderAmount: 0m,
            baseDeliveryFee: 0m,
            includedDistanceKm: 0m,
            additionalFeePerKm: 0m,
            maximumDeliveryDistanceKm: 10m,
            freeDeliveryThreshold: null,
            utcNow);

    public static BranchDeliveryPolicy Create(
        OrganizationBranchId branchId,
        PlatformOrganizationId organizationId,
        decimal minimumOrderAmount,
        decimal baseDeliveryFee,
        decimal includedDistanceKm,
        decimal additionalFeePerKm,
        decimal maximumDeliveryDistanceKm,
        decimal? freeDeliveryThreshold,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(branchId);
        ArgumentNullException.ThrowIfNull(organizationId);
        DomainTime.EnsureUtc(utcNow);
        Validate(
            minimumOrderAmount,
            baseDeliveryFee,
            includedDistanceKm,
            additionalFeePerKm,
            maximumDeliveryDistanceKm,
            freeDeliveryThreshold);
        return new(
            branchId,
            organizationId,
            RoundMoney(minimumOrderAmount),
            RoundMoney(baseDeliveryFee),
            RoundDistance(includedDistanceKm),
            RoundMoney(additionalFeePerKm),
            RoundDistance(maximumDeliveryDistanceKm),
            freeDeliveryThreshold is null ? null : RoundMoney(freeDeliveryThreshold.Value),
            utcNow,
            utcNow);
    }

    public static BranchDeliveryPolicy Rehydrate(
        OrganizationBranchId branchId,
        PlatformOrganizationId organizationId,
        decimal minimumOrderAmount,
        decimal baseDeliveryFee,
        decimal includedDistanceKm,
        decimal additionalFeePerKm,
        decimal maximumDeliveryDistanceKm,
        decimal? freeDeliveryThreshold,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            branchId,
            organizationId,
            minimumOrderAmount,
            baseDeliveryFee,
            includedDistanceKm,
            additionalFeePerKm,
            maximumDeliveryDistanceKm,
            freeDeliveryThreshold,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        decimal minimumOrderAmount,
        decimal baseDeliveryFee,
        decimal includedDistanceKm,
        decimal additionalFeePerKm,
        decimal maximumDeliveryDistanceKm,
        decimal? freeDeliveryThreshold,
        DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        Validate(
            minimumOrderAmount,
            baseDeliveryFee,
            includedDistanceKm,
            additionalFeePerKm,
            maximumDeliveryDistanceKm,
            freeDeliveryThreshold);
        MinimumOrderAmount = RoundMoney(minimumOrderAmount);
        BaseDeliveryFee = RoundMoney(baseDeliveryFee);
        IncludedDistanceKm = RoundDistance(includedDistanceKm);
        AdditionalFeePerKm = RoundMoney(additionalFeePerKm);
        MaximumDeliveryDistanceKm = RoundDistance(maximumDeliveryDistanceKm);
        FreeDeliveryThreshold = freeDeliveryThreshold is null ? null : RoundMoney(freeDeliveryThreshold.Value);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Calculates delivery fee for a known distance and merchandise subtotal.
    /// Throws when unavailable (min order / max distance).
    /// When <paramref name="allowBeyondMaximumDistance"/> is true, the maximum-distance
    /// reject is skipped; fee still uses the actual distance (no clamp).
    /// </summary>
    public DeliveryFeeQuote CalculateFee(
        decimal merchandiseSubtotal,
        decimal distanceKm,
        bool allowBeyondMaximumDistance = false)
    {
        var subtotal = RoundMoney(merchandiseSubtotal);
        var distance = RoundDistance(distanceKm);

        if (subtotal < MinimumOrderAmount)
        {
            throw new DomainException(
                DomainErrorCodes.DeliveryMinimumOrderNotMet,
                $"Merchandise subtotal must be at least {MinimumOrderAmount} for delivery.");
        }

        if (!allowBeyondMaximumDistance && distance > MaximumDeliveryDistanceKm)
        {
            throw new DomainException(
                DomainErrorCodes.DeliveryDistanceExceedsMaximum,
                $"Delivery distance exceeds the maximum of {MaximumDeliveryDistanceKm} km.");
        }

        if (FreeDeliveryThreshold is decimal threshold && subtotal >= threshold)
        {
            return new DeliveryFeeQuote(distance, 0m, 0m, 0m, true);
        }

        var extraDistance = Math.Max(0m, distance - IncludedDistanceKm);
        var distanceCharge = RoundMoney(extraDistance * AdditionalFeePerKm);
        var fee = RoundMoney(BaseDeliveryFee + distanceCharge);
        return new DeliveryFeeQuote(distance, RoundDistance(extraDistance), distanceCharge, fee, false);
    }

    public bool IsCompleteForPublicDelivery =>
        MaximumDeliveryDistanceKm > 0m;

    private static void Validate(
        decimal minimumOrderAmount,
        decimal baseDeliveryFee,
        decimal includedDistanceKm,
        decimal additionalFeePerKm,
        decimal maximumDeliveryDistanceKm,
        decimal? freeDeliveryThreshold)
    {
        if (minimumOrderAmount < 0m || baseDeliveryFee < 0m || additionalFeePerKm < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryPolicy,
                "Delivery money amounts cannot be negative.");
        }

        if (includedDistanceKm < 0m || maximumDeliveryDistanceKm <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryPolicy,
                "Included distance cannot be negative and maximum distance must be positive.");
        }

        if (includedDistanceKm > maximumDeliveryDistanceKm)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryPolicy,
                "Included distance cannot exceed maximum delivery distance.");
        }

        if (freeDeliveryThreshold is decimal threshold && threshold < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryPolicy,
                "Free delivery threshold cannot be negative.");
        }
    }

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, MoneyScale, MidpointRounding.AwayFromZero);

    public static decimal RoundDistance(decimal value) =>
        Math.Round(value, DistanceScale, MidpointRounding.AwayFromZero);
}

public sealed record DeliveryFeeQuote(
    decimal DistanceKm,
    decimal ExtraDistanceKm,
    decimal DistanceCharge,
    decimal DeliveryFee,
    bool FreeDeliveryApplied);

/// <summary>Provider-agnostic distance calculation. V1 = geodesic / Haversine.</summary>
public interface IDeliveryDistanceCalculator
{
    /// <summary>Returns distance in kilometres between two WGS84 points.</summary>
    decimal CalculateDistanceKm(decimal originLatitude, decimal originLongitude, decimal destinationLatitude, decimal destinationLongitude);
}

public sealed class HaversineDeliveryDistanceCalculator : IDeliveryDistanceCalculator
{
    private const double EarthRadiusKm = 6371.0088;

    public decimal CalculateDistanceKm(
        decimal originLatitude,
        decimal originLongitude,
        decimal destinationLatitude,
        decimal destinationLongitude)
    {
        if (!OrganizationBranch.IsValidLatitude(originLatitude)
            || !OrganizationBranch.IsValidLongitude(originLongitude)
            || !OrganizationBranch.IsValidLatitude(destinationLatitude)
            || !OrganizationBranch.IsValidLongitude(destinationLongitude))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranchCoordinates,
                "Coordinates must be valid WGS84 latitude/longitude values.");
        }

        var lat1 = DegreesToRadians((double)originLatitude);
        var lat2 = DegreesToRadians((double)destinationLatitude);
        var dLat = DegreesToRadians((double)(destinationLatitude - originLatitude));
        var dLon = DegreesToRadians((double)(destinationLongitude - originLongitude));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var km = EarthRadiusKm * c;
        return BranchDeliveryPolicy.RoundDistance((decimal)km);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
