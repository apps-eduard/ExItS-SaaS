using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Immutable delivery destination and fee snapshot recorded at submit.
/// Null on pickup orders. Fee policy fields are snapshots so later branch edits never rewrite history.
/// </summary>
public sealed class CustomerOrderDeliverySnapshot
{
    public const int RecipientNameMaxLength = 128;
    public const int RecipientPhoneMaxLength = 32;
    public const int AddressLineMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int NotesMaxLength = 512;
    public const int DistanceScale = 3;

    public string RecipientName { get; }
    public string? RecipientPhone { get; }
    public string AddressLine1 { get; }
    public string? AddressLine2 { get; }
    public string? City { get; }
    public string? DeliveryNotes { get; }
    public decimal DestinationLatitude { get; }
    public decimal DestinationLongitude { get; }
    public decimal BranchLatitudeSnapshot { get; }
    public decimal BranchLongitudeSnapshot { get; }
    public decimal DistanceKm { get; }
    public decimal MinimumOrderAmountSnapshot { get; }
    public decimal BaseDeliveryFeeSnapshot { get; }
    public decimal IncludedDistanceKmSnapshot { get; }
    public decimal AdditionalFeePerKmSnapshot { get; }
    public decimal MaximumDeliveryDistanceKmSnapshot { get; }
    public decimal? FreeDeliveryThresholdSnapshot { get; }
    public decimal DistanceCharge { get; }
    public decimal FinalDeliveryFee { get; }
    public bool FreeDeliveryApplied { get; }
    /// <summary>
    /// True when the max-distance reject was bypassed via seller preference at submit time.
    /// Immutable — later preference changes must not rewrite historical orders.
    /// </summary>
    public bool DistanceExceptionApplied { get; }

    private CustomerOrderDeliverySnapshot(
        string recipientName,
        string? recipientPhone,
        string addressLine1,
        string? addressLine2,
        string? city,
        string? deliveryNotes,
        decimal destinationLatitude,
        decimal destinationLongitude,
        decimal branchLatitudeSnapshot,
        decimal branchLongitudeSnapshot,
        decimal distanceKm,
        decimal minimumOrderAmountSnapshot,
        decimal baseDeliveryFeeSnapshot,
        decimal includedDistanceKmSnapshot,
        decimal additionalFeePerKmSnapshot,
        decimal maximumDeliveryDistanceKmSnapshot,
        decimal? freeDeliveryThresholdSnapshot,
        decimal distanceCharge,
        decimal finalDeliveryFee,
        bool freeDeliveryApplied,
        bool distanceExceptionApplied)
    {
        RecipientName = recipientName;
        RecipientPhone = recipientPhone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        DeliveryNotes = deliveryNotes;
        DestinationLatitude = destinationLatitude;
        DestinationLongitude = destinationLongitude;
        BranchLatitudeSnapshot = branchLatitudeSnapshot;
        BranchLongitudeSnapshot = branchLongitudeSnapshot;
        DistanceKm = distanceKm;
        MinimumOrderAmountSnapshot = minimumOrderAmountSnapshot;
        BaseDeliveryFeeSnapshot = baseDeliveryFeeSnapshot;
        IncludedDistanceKmSnapshot = includedDistanceKmSnapshot;
        AdditionalFeePerKmSnapshot = additionalFeePerKmSnapshot;
        MaximumDeliveryDistanceKmSnapshot = maximumDeliveryDistanceKmSnapshot;
        FreeDeliveryThresholdSnapshot = freeDeliveryThresholdSnapshot;
        DistanceCharge = distanceCharge;
        FinalDeliveryFee = finalDeliveryFee;
        FreeDeliveryApplied = freeDeliveryApplied;
        DistanceExceptionApplied = distanceExceptionApplied;
    }

    public static CustomerOrderDeliverySnapshot Create(
        string recipientName,
        string? recipientPhone,
        string addressLine1,
        string? addressLine2,
        string? city,
        string? deliveryNotes,
        decimal destinationLatitude,
        decimal destinationLongitude,
        decimal branchLatitudeSnapshot,
        decimal branchLongitudeSnapshot,
        decimal distanceKm,
        decimal minimumOrderAmountSnapshot,
        decimal baseDeliveryFeeSnapshot,
        decimal includedDistanceKmSnapshot,
        decimal additionalFeePerKmSnapshot,
        decimal maximumDeliveryDistanceKmSnapshot,
        decimal? freeDeliveryThresholdSnapshot,
        decimal distanceCharge,
        decimal finalDeliveryFee,
        bool freeDeliveryApplied,
        bool distanceExceptionApplied = false)
    {
        var fee = SaleMoney.RoundMoney(finalDeliveryFee);
        var charge = SaleMoney.RoundMoney(distanceCharge);

        if (fee < 0m || charge < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                "Delivery fee amounts cannot be negative.");
        }

        if (freeDeliveryApplied && fee != 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDeliveryFee,
                "Free delivery requires a zero final delivery fee.");
        }

        EnsureCoordinate(destinationLatitude, destinationLongitude);
        EnsureCoordinate(branchLatitudeSnapshot, branchLongitudeSnapshot);

        return new CustomerOrderDeliverySnapshot(
            NormalizeRequired(recipientName, RecipientNameMaxLength, "Recipient name"),
            NormalizeOptional(recipientPhone, RecipientPhoneMaxLength),
            NormalizeRequired(addressLine1, AddressLineMaxLength, "Address line 1"),
            NormalizeOptional(addressLine2, AddressLineMaxLength),
            NormalizeOptional(city, CityMaxLength),
            NormalizeOptional(deliveryNotes, NotesMaxLength),
            RoundCoordinate(destinationLatitude),
            RoundCoordinate(destinationLongitude),
            RoundCoordinate(branchLatitudeSnapshot),
            RoundCoordinate(branchLongitudeSnapshot),
            RoundDistance(distanceKm),
            SaleMoney.RoundMoney(minimumOrderAmountSnapshot),
            SaleMoney.RoundMoney(baseDeliveryFeeSnapshot),
            RoundDistance(includedDistanceKmSnapshot),
            SaleMoney.RoundMoney(additionalFeePerKmSnapshot),
            RoundDistance(maximumDeliveryDistanceKmSnapshot),
            freeDeliveryThresholdSnapshot is null
                ? null
                : SaleMoney.RoundMoney(freeDeliveryThresholdSnapshot.Value),
            charge,
            fee,
            freeDeliveryApplied,
            distanceExceptionApplied);
    }

    public static CustomerOrderDeliverySnapshot Rehydrate(
        string recipientName,
        string? recipientPhone,
        string addressLine1,
        string? addressLine2,
        string? city,
        string? deliveryNotes,
        decimal destinationLatitude,
        decimal destinationLongitude,
        decimal branchLatitudeSnapshot,
        decimal branchLongitudeSnapshot,
        decimal distanceKm,
        decimal minimumOrderAmountSnapshot,
        decimal baseDeliveryFeeSnapshot,
        decimal includedDistanceKmSnapshot,
        decimal additionalFeePerKmSnapshot,
        decimal maximumDeliveryDistanceKmSnapshot,
        decimal? freeDeliveryThresholdSnapshot,
        decimal distanceCharge,
        decimal finalDeliveryFee,
        bool freeDeliveryApplied,
        bool distanceExceptionApplied = false) =>
        new(
            recipientName,
            recipientPhone,
            addressLine1,
            addressLine2,
            city,
            deliveryNotes,
            destinationLatitude,
            destinationLongitude,
            branchLatitudeSnapshot,
            branchLongitudeSnapshot,
            distanceKm,
            minimumOrderAmountSnapshot,
            baseDeliveryFeeSnapshot,
            includedDistanceKmSnapshot,
            additionalFeePerKmSnapshot,
            maximumDeliveryDistanceKmSnapshot,
            freeDeliveryThresholdSnapshot,
            distanceCharge,
            finalDeliveryFee,
            freeDeliveryApplied,
            distanceExceptionApplied);

    private static void EnsureCoordinate(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                "Delivery coordinates must be valid WGS84 latitude/longitude values.");
        }
    }

    private static decimal RoundCoordinate(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.AwayFromZero);

    private static decimal RoundDistance(decimal value)
    {
        if (value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                "Distance cannot be negative.");
        }

        return decimal.Round(value, DistanceScale, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                $"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                $"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDelivery,
                $"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
