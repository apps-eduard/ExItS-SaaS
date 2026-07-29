using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform Organization aggregate — SaaS customer/account boundary.
/// Not a Clinic, Store, Branch, Register, subscription, or billing record.
/// </summary>
public sealed class PlatformOrganization
{
    private static readonly Regex SlugPattern = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public PlatformOrganizationId Id { get; }
    public string DisplayName { get; private set; }
    public string Slug { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PlatformOrganization(
        PlatformOrganizationId id,
        string displayName,
        string slug,
        OrganizationStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        DisplayName = displayName;
        Slug = slug;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PlatformOrganization Create(
        string displayName,
        string slug,
        DateTimeOffset utcNow,
        PlatformOrganizationId? id = null)
    {
        EnsureUtc(utcNow);
        var name = PlatformUser.NormalizeDisplayName(displayName);
        var normalizedSlug = NormalizeSlug(slug);

        return new PlatformOrganization(
            id ?? PlatformOrganizationId.New(),
            name,
            normalizedSlug,
            OrganizationStatus.Active,
            utcNow,
            utcNow);
    }

    public void Rename(string displayName, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotClosed();
        DisplayName = PlatformUser.NormalizeDisplayName(displayName);
        UpdatedAtUtc = utcNow;
    }

    public void ChangeSlug(string slug, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotClosed();
        Slug = NormalizeSlug(slug);
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        TransitionTo(OrganizationStatus.Suspended, utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == OrganizationStatus.Closed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationStatusTransition,
                "A closed Platform Organization cannot be reactivated.");
        }

        TransitionTo(OrganizationStatus.Active, utcNow);
    }

    public void Close(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        TransitionTo(OrganizationStatus.Closed, utcNow);
    }

    private void TransitionTo(OrganizationStatus target, DateTimeOffset utcNow)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            OrganizationStatus.Active => target is OrganizationStatus.Suspended or OrganizationStatus.Closed,
            OrganizationStatus.Suspended => target is OrganizationStatus.Active or OrganizationStatus.Closed,
            OrganizationStatus.Closed => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationStatusTransition,
                $"Cannot transition Platform Organization from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureNotClosed()
    {
        if (Status == OrganizationStatus.Closed)
        {
            throw new DomainException(
                DomainErrorCodes.OrganizationNotActive,
                "A closed Platform Organization cannot be updated.");
        }
    }

    internal static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationSlug,
                "Organization slug cannot be blank.");
        }

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 64 || !SlugPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationSlug,
                "Organization slug must be 2–64 characters of lowercase alphanumeric segments separated by single hyphens.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
