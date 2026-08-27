namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Dev/Testing-only documentation of intended BNPL capability fixtures for Local Validation identities.
/// Capabilities are not persisted in a BNPL grant DB. Trusted transport (future) must supply
/// these facts into BnplAccessContext. Preset labels are bundles — never authorize by preset name.
/// Capability identifier strings match BNPL Domain BnplCapabilityCodes / BnplCapabilityPresets
/// (including bnpl.customer.read / bnpl.customer.manage from BNPL-03).
/// </summary>
public static class BnplLocalValidationAccessFixtures
{
    public const string MariaSantosKey = "maria-santos";
    public const string CarloReyesKey = "carlo-reyes";
    public const string AnaCruzKey = "ana-cruz";
    public const string DanielGarciaKey = "daniel-garcia";

    public const string OwnerPreset = "Owner";
    public const string SalesPreset = "Sales";

    /// <summary>ABC Owner — BNPL product access + Owner capability bundle (org-wide branch intent).</summary>
    public static BnplLocalValidationAccessFixture MariaSantos { get; } = new(
        MariaSantosKey,
        HasBnplProductAccess: true,
        CapabilityPreset: OwnerPreset,
        OrganizationWideBranchAccess: true);

    /// <summary>ABC Cashier — BNPL product access + Sales capability bundle (branch-restricted intent).</summary>
    public static BnplLocalValidationAccessFixture CarloReyes { get; } = new(
        CarloReyesKey,
        HasBnplProductAccess: true,
        CapabilityPreset: SalesPreset,
        OrganizationWideBranchAccess: false);

    /// <summary>XYZ Owner — POS only; no BNPL product access.</summary>
    public static BnplLocalValidationAccessFixture AnaCruz { get; } = new(
        AnaCruzKey,
        HasBnplProductAccess: false,
        CapabilityPreset: null,
        OrganizationWideBranchAccess: false);

    /// <summary>XYZ Cashier — POS only; no BNPL product access.</summary>
    public static BnplLocalValidationAccessFixture DanielGarcia { get; } = new(
        DanielGarciaKey,
        HasBnplProductAccess: false,
        CapabilityPreset: null,
        OrganizationWideBranchAccess: false);

    public static IReadOnlyList<BnplLocalValidationAccessFixture> All { get; } =
    [
        MariaSantos,
        CarloReyes,
        AnaCruz,
        DanielGarcia
    ];
}

public sealed record BnplLocalValidationAccessFixture(
    string IdentityKey,
    bool HasBnplProductAccess,
    string? CapabilityPreset,
    bool OrganizationWideBranchAccess);
