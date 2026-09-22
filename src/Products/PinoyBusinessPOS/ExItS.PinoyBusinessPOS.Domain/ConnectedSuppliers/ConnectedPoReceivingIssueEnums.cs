using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>Lifecycle of a connected-PO receiving-issue case (seller review). Member names are persistence codes.</summary>
public enum ConnectedPoReceivingIssueStatus
{
    PendingSellerReview = 0,
    Resolved = 1
}

/// <summary>Buyer-reported discrepancy kind retained on an issue line. Member names are persistence codes.</summary>
public enum ConnectedPoReceivingIssueLineKind
{
    Missing = 0,
    Damaged = 1
}

/// <summary>Seller resolution for buyer-reported missing / not-delivered qty. Member names are persistence codes.</summary>
public enum ConnectedPoMissingResolution
{
    FoundAtSeller = 0,
    NeverShipped = 1,
    LostInTransit = 2,
    DeliveredDisputed = 3,
    ReplacementPlanned = 4,
    Other = 5
}

/// <summary>Seller resolution for buyer-reported damaged qty. Member names are persistence codes.</summary>
public enum ConnectedPoDamagedResolution
{
    AcceptedNoReturn = 0,
    ReturnRequested = 1,
    ReplacementApproved = 2,
    Disputed = 3,
    Other = 4
}

public static class ConnectedPoReceivingIssueStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ConnectedPoReceivingIssueStatus.PendingSellerReview),
        nameof(ConnectedPoReceivingIssueStatus.Resolved)
    ];

    public static string ToCode(ConnectedPoReceivingIssueStatus status) => status.ToString();

    public static ConnectedPoReceivingIssueStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueStatus,
                $"Status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }

    public static bool TryParse(string? code, out ConnectedPoReceivingIssueStatus status)
    {
        status = ConnectedPoReceivingIssueStatus.PendingSellerReview;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return Enum.TryParse(code.Trim(), ignoreCase: false, out status)
            && Codes.Contains(status.ToString(), StringComparer.Ordinal);
    }
}

public static class ConnectedPoReceivingIssueLineKinds
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ConnectedPoReceivingIssueLineKind.Missing),
        nameof(ConnectedPoReceivingIssueLineKind.Damaged)
    ];

    public static string ToCode(ConnectedPoReceivingIssueLineKind kind) => kind.ToString();

    public static ConnectedPoReceivingIssueLineKind Parse(string? code)
    {
        if (!TryParse(code, out var kind))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLineKind,
                $"Line kind must be one of: {string.Join(", ", Codes)}.");
        }

        return kind;
    }

    public static bool TryParse(string? code, out ConnectedPoReceivingIssueLineKind kind)
    {
        kind = ConnectedPoReceivingIssueLineKind.Missing;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return Enum.TryParse(code.Trim(), ignoreCase: false, out kind)
            && Codes.Contains(kind.ToString(), StringComparer.Ordinal);
    }
}

public static class ConnectedPoMissingResolutions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ConnectedPoMissingResolution.FoundAtSeller),
        nameof(ConnectedPoMissingResolution.NeverShipped),
        nameof(ConnectedPoMissingResolution.LostInTransit),
        nameof(ConnectedPoMissingResolution.DeliveredDisputed),
        nameof(ConnectedPoMissingResolution.ReplacementPlanned),
        nameof(ConnectedPoMissingResolution.Other)
    ];

    public static string ToCode(ConnectedPoMissingResolution resolution) => resolution.ToString();

    public static ConnectedPoMissingResolution Parse(string? code)
    {
        if (!TryParse(code, out var resolution))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoMissingResolution,
                $"Missing resolution must be one of: {string.Join(", ", Codes)}.");
        }

        return resolution;
    }

    public static bool TryParse(string? code, out ConnectedPoMissingResolution resolution)
    {
        resolution = ConnectedPoMissingResolution.FoundAtSeller;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return Enum.TryParse(code.Trim(), ignoreCase: false, out resolution)
            && Codes.Contains(resolution.ToString(), StringComparer.Ordinal);
    }

    /// <summary>Whether this resolution restores seller tracked stock via a compensating movement.</summary>
    public static bool RestoresSellerStock(ConnectedPoMissingResolution resolution) =>
        resolution is ConnectedPoMissingResolution.FoundAtSeller
            or ConnectedPoMissingResolution.NeverShipped;
}

public static class ConnectedPoDamagedResolutions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ConnectedPoDamagedResolution.AcceptedNoReturn),
        nameof(ConnectedPoDamagedResolution.ReturnRequested),
        nameof(ConnectedPoDamagedResolution.ReplacementApproved),
        nameof(ConnectedPoDamagedResolution.Disputed),
        nameof(ConnectedPoDamagedResolution.Other)
    ];

    public static string ToCode(ConnectedPoDamagedResolution resolution) => resolution.ToString();

    public static ConnectedPoDamagedResolution Parse(string? code)
    {
        if (!TryParse(code, out var resolution))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoDamagedResolution,
                $"Damaged resolution must be one of: {string.Join(", ", Codes)}.");
        }

        return resolution;
    }

    public static bool TryParse(string? code, out ConnectedPoDamagedResolution resolution)
    {
        resolution = ConnectedPoDamagedResolution.AcceptedNoReturn;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return Enum.TryParse(code.Trim(), ignoreCase: false, out resolution)
            && Codes.Contains(resolution.ToString(), StringComparer.Ordinal);
    }
}
