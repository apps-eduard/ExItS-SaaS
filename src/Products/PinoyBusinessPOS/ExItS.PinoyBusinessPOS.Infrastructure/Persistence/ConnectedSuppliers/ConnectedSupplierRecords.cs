using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedSupplierRelationshipRecord
{
    public Guid Id { get; set; } public Guid BuyerOrganizationId { get; set; } public Guid SupplierOrganizationId { get; set; }
    public int Status { get; set; } public DateTimeOffset RequestedAtUtc { get; set; } public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; } public Guid? RespondedByUserId { get; set; }
    public DateTimeOffset? DisconnectedAtUtc { get; set; }
    /// <summary>0 = Buyer (legacy default), 1 = Supplier (Business Customer invitation).</summary>
    public int InitiatedByParty { get; set; }
    public string? BuyerDisplayNameSnapshot { get; set; }
    public string? BuyerPublicOrganizationIdSnapshot { get; set; }
    public string? SupplierDisplayNameSnapshot { get; set; }
    public string? SupplierPublicOrganizationIdSnapshot { get; set; }
    /// <summary>0 = SelectedOnly (legacy default), 1 = AllEligible.</summary>
    public int CatalogSharingMode { get; set; }
    public decimal? CustomerDiscountPercent { get; set; }
    public bool UseOrganizationPaymentTimingDefaults { get; set; } = true;
    public bool AllowPayBeforeFulfillment { get; set; } = true;
    public bool AllowPayOnDeliveryOrReceipt { get; set; } = true;
    public bool AllowSupplierCredit { get; set; }
    public int CustomerDefaultPaymentTiming { get; set; } = (int)ConnectedPoPaymentTiming.PayBeforeFulfillment;
    public Guid? SupplierBranchId { get; set; }
    public string? SupplierBranchNameSnapshot { get; set; }
    public Guid[] SharedSupplierBranchIds { get; set; } = [];
    /// <summary>0 = Custom, 1 = OrganizationMember.</summary>
    public int ContactSource { get; set; }
    public Guid? OrganizationMemberId { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactDepartment { get; set; }
    public string? ContactRole { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? DeliveryInstructions { get; set; }
    /// <summary>null = inherit, "allow", or "block".</summary>
    public string? CustomerDeliveryOverride { get; set; }
    public string? BillingContactNotes { get; set; }
    public string? InternalNotes { get; set; }
    public List<ConnectedSupplierRelationshipCategoryDiscountOverrideRecord> CategoryDiscountOverrides { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } public uint Xmin { get; set; }
}

internal sealed class ConnectedSupplierRelationshipCategoryDiscountOverrideRecord
{
    public Guid RelationshipId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal DiscountPercent { get; set; }
}

internal sealed class OrganizationConnectedCommerceSettingsRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool AllowPayBeforeFulfillment { get; set; } = true;
    public bool AllowPayOnDeliveryOrReceipt { get; set; } = true;
    public bool AllowSupplierCredit { get; set; }
    public int DefaultPaymentTiming { get; set; } = (int)ConnectedPoPaymentTiming.PayBeforeFulfillment;
    public decimal DefaultB2bDiscountPercent { get; set; }
    public int ProposalReservationHoldHours { get; set; } = 24;
    public bool ReturnsAllowed { get; set; } = true;
    public int? ReturnWindowDays { get; set; }
    public int ReceivingIssueWindowDays { get; set; } = 2;
    public bool RequireReturnApproval { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<OrganizationConnectedCommerceCategoryRuleRecord> CategoryRules { get; set; } = [];
    public List<OrganizationConnectedCommerceCategoryReturnRuleRecord> CategoryReturnRules { get; set; } = [];
}

internal sealed class OrganizationConnectedCommerceCategoryRuleRecord
{
    public Guid OrganizationConnectedCommerceSettingsId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal DiscountPercent { get; set; }
}

internal sealed class OrganizationConnectedCommerceCategoryReturnRuleRecord
{
    public Guid OrganizationConnectedCommerceSettingsId { get; set; }
    public Guid CategoryId { get; set; }
    public short Mode { get; set; }
    public bool? ReturnsAllowed { get; set; }
    public int? ReturnWindowDays { get; set; }
}
internal sealed class SupplierProductExposureRecord
{
    public Guid Id { get; set; } public Guid SupplierOrganizationId { get; set; } public Guid ProductId { get; set; }
    public string? SkuSnapshot { get; set; } public string NameSnapshot { get; set; }=string.Empty;
    public string? CategoryNameSnapshot { get; set; } public string UnitOfMeasureCode { get; set; }=string.Empty;
    public decimal SupplierOrderPrice { get; set; } public bool IsOrderable { get; set; } public bool IsExposed { get; set; }
    public long SyncVersion { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
internal sealed class ConnectedBuyerProductShareRecord
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; }
    public Guid SupplierProductId { get; set; }
    public bool IsShared { get; set; }
    public decimal? BuyerSpecificPoPrice { get; set; }
    public long SyncVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
internal sealed class BuyerSupplierProductLinkRecord
{
    public Guid Id { get; set; } public Guid RelationshipId { get; set; } public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; } public Guid BuyerProductId { get; set; } public Guid SupplierProductId { get; set; }
    public string? SupplierSkuSnapshot { get; set; } public string SupplierNameSnapshot { get; set; }=string.Empty;
    public string UnitOfMeasureCode { get; set; }=string.Empty; public decimal LastKnownOrderPrice { get; set; }
    public Guid? BuyerPurchaseUnitId { get; set; }
    public decimal MultiplierToBase { get; set; } = 1m;
    public string? PackageLabel { get; set; }
    public bool IsActive { get; set; } public long SyncVersion { get; set; } public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } public uint Xmin { get; set; }
}
internal sealed class ConnectedPurchaseOrderRecord
{
    public Guid Id { get; set; } public Guid RelationshipId { get; set; } public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; } public Guid BuyerPurchaseOrderId { get; set; } public string? BuyerPoNumber { get; set; }
    public DateOnly OrderDate { get; set; } public string? Notes { get; set; } public int Status { get; set; }
    public decimal TotalAmount { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? DeclinedAtUtc { get; set; }
    public DateTimeOffset? PreparingAtUtc { get; set; }
    public DateTimeOffset? FulfilledAtUtc { get; set; }
    public DateTimeOffset? WithdrawnAtUtc { get; set; }
    public int? DeclineReason { get; set; }
    public string? DeclineNote { get; set; }
    public int PaymentTerm { get; set; }
    public int PaymentTiming { get; set; } = (int)ConnectedPoPaymentTiming.PayBeforeFulfillment;
    public string? FulfillmentMethod { get; set; }
    public string? ConfirmedFulfillmentMethod { get; set; }
    public int? ProposedPaymentTerm { get; set; }
    public int? ProposedPaymentTiming { get; set; }
    public int? ConfirmedPaymentTerm { get; set; }
    public int? ConfirmedPaymentTiming { get; set; }
    public decimal CreditPostedAmount { get; set; }
    public DateTimeOffset? ChangesProposedAtUtc { get; set; }
    public Guid? ChangesProposedByUserId { get; set; }
    public DateTimeOffset? BuyerRespondedAtUtc { get; set; }
    public Guid? BuyerRespondedByUserId { get; set; }
    public int InventoryReservationState { get; set; }
    public DateTimeOffset? InventoryReservationExpiresAtUtc { get; set; }
    public int InventoryReservationRevision { get; set; }
    public List<ConnectedPurchaseOrderLineRecord> Lines { get; set; }=[]; public uint Xmin { get; set; }
}
internal sealed class ConnectedPurchaseOrderLineRecord
{
    public Guid ConnectedPurchaseOrderId { get; set; } public int LineNumber { get; set; } public Guid ProductId { get; set; }
    public string NameSnapshot { get; set; }=string.Empty; public string? SkuSnapshot { get; set; }     public decimal Qty { get; set; }
    public decimal? ProposedQty { get; set; }
    public decimal? ConfirmedQty { get; set; }
    public int Availability { get; set; }
    public decimal UnitPriceSnapshot { get; set; } public decimal LineTotal { get; set; } public string UnitOfMeasureCode { get; set; }=string.Empty;
    public decimal? ProposedUnitPrice { get; set; }
    public decimal? ConfirmedUnitPrice { get; set; }
}

internal sealed class ConnectedPoInventoryReservationRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ConnectedPurchaseOrderId { get; set; }
    public int Revision { get; set; }
    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public int Type { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public int Version { get; set; }
}

internal static class ConnectedSupplierEntityMapper
{
    public static ConnectedSupplierRelationship ToDomain(ConnectedSupplierRelationshipRecord r)=>ConnectedSupplierRelationship.Rehydrate(
        ConnectedSupplierRelationshipId.From(r.Id),PosOrganizationId.From(r.BuyerOrganizationId),PosOrganizationId.From(r.SupplierOrganizationId),
        (ConnectedSupplierRelationshipStatus)r.Status,r.RequestedAtUtc,r.RequestedByUserId,r.RespondedAtUtc,r.RespondedByUserId,
        r.DisconnectedAtUtc,r.CreatedAtUtc,r.UpdatedAtUtc,
        r.BuyerDisplayNameSnapshot,r.BuyerPublicOrganizationIdSnapshot,
        r.SupplierDisplayNameSnapshot,r.SupplierPublicOrganizationIdSnapshot,
        (CatalogSharingMode)r.CatalogSharingMode,
        r.CustomerDiscountPercent,
        r.SupplierBranchId,
        r.SupplierBranchNameSnapshot,
        (ConnectionInitiatedByParty)r.InitiatedByParty,
        r.SharedSupplierBranchIds,
        (RelationshipContactSource)r.ContactSource,
        r.OrganizationMemberId,
        r.ContactPersonName,
        r.ContactDepartment,
        r.ContactRole,
        r.ContactPhone,
        r.ContactEmail,
        r.PreferredContactMethod,
        r.DeliveryInstructions,
        r.BillingContactNotes,
        r.InternalNotes,
        EffectiveDeliveryAllowance.ParseOverride(r.CustomerDeliveryOverride),
        r.UseOrganizationPaymentTimingDefaults,
        r.AllowPayBeforeFulfillment,
        r.AllowPayOnDeliveryOrReceipt,
        r.AllowSupplierCredit,
        (ConnectedPoPaymentTiming)r.CustomerDefaultPaymentTiming,
        r.CategoryDiscountOverrides
            .Select(x => new ConnectedCustomerCategoryDiscountOverride(x.CategoryId, x.DiscountPercent))
            .ToList());
    public static ConnectedSupplierRelationshipRecord ToRecord(ConnectedSupplierRelationship x)=>new(){Id=x.Id.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,Status=(int)x.Status,
        RequestedAtUtc=x.RequestedAtUtc,RequestedByUserId=x.RequestedByUserId,RespondedAtUtc=x.RespondedAtUtc,
        RespondedByUserId=x.RespondedByUserId,DisconnectedAtUtc=x.DisconnectedAtUtc,
        InitiatedByParty=(int)x.InitiatedByParty,
        BuyerDisplayNameSnapshot=x.BuyerDisplayNameSnapshot,BuyerPublicOrganizationIdSnapshot=x.BuyerPublicOrganizationIdSnapshot,
        SupplierDisplayNameSnapshot=x.SupplierDisplayNameSnapshot,SupplierPublicOrganizationIdSnapshot=x.SupplierPublicOrganizationIdSnapshot,
        CatalogSharingMode=(int)x.CatalogSharingMode,CustomerDiscountPercent=x.CustomerDiscountPercent,
        UseOrganizationPaymentTimingDefaults=x.UseOrganizationPaymentTimingDefaults,
        AllowPayBeforeFulfillment=x.AllowPayBeforeFulfillment,
        AllowPayOnDeliveryOrReceipt=x.AllowPayOnDeliveryOrReceipt,
        AllowSupplierCredit=x.AllowSupplierCredit,
        CustomerDefaultPaymentTiming=(int)x.CustomerDefaultPaymentTiming,
        SupplierBranchId=x.SupplierBranchId,SupplierBranchNameSnapshot=x.SupplierBranchNameSnapshot,
        SharedSupplierBranchIds=x.SharedSupplierBranchIds.ToArray(),
        ContactSource=(int)x.ContactSource,OrganizationMemberId=x.OrganizationMemberId,
        ContactPersonName=x.ContactPersonName,ContactDepartment=x.ContactDepartment,ContactRole=x.ContactRole,ContactPhone=x.ContactPhone,ContactEmail=x.ContactEmail,
        PreferredContactMethod=x.PreferredContactMethod,DeliveryInstructions=x.DeliveryInstructions,
        CustomerDeliveryOverride=EffectiveDeliveryAllowance.ToPersistence(x.CustomerDeliveryOverride),
        BillingContactNotes=x.BillingContactNotes,InternalNotes=x.InternalNotes,
        CategoryDiscountOverrides=x.CustomerCategoryDiscountOverrides
            .Select(o => new ConnectedSupplierRelationshipCategoryDiscountOverrideRecord
            {
                RelationshipId = x.Id.Value,
                CategoryId = o.CategoryId,
                DiscountPercent = o.DiscountPercent
            })
            .ToList(),
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(ConnectedSupplierRelationship x,ConnectedSupplierRelationshipRecord r)
    {r.Status=(int)x.Status;r.RespondedAtUtc=x.RespondedAtUtc;r.RespondedByUserId=x.RespondedByUserId;r.DisconnectedAtUtc=x.DisconnectedAtUtc;
     r.InitiatedByParty=(int)x.InitiatedByParty;
     r.CatalogSharingMode=(int)x.CatalogSharingMode;r.CustomerDiscountPercent=x.CustomerDiscountPercent;
     r.UseOrganizationPaymentTimingDefaults = x.UseOrganizationPaymentTimingDefaults;
     r.AllowPayBeforeFulfillment = x.AllowPayBeforeFulfillment;
     r.AllowPayOnDeliveryOrReceipt = x.AllowPayOnDeliveryOrReceipt;
     r.AllowSupplierCredit = x.AllowSupplierCredit;
     r.CustomerDefaultPaymentTiming = (int)x.CustomerDefaultPaymentTiming;
     r.SupplierBranchId=x.SupplierBranchId;r.SupplierBranchNameSnapshot=x.SupplierBranchNameSnapshot;
     r.SharedSupplierBranchIds=x.SharedSupplierBranchIds.ToArray();
     r.ContactSource=(int)x.ContactSource;r.OrganizationMemberId=x.OrganizationMemberId;
     r.ContactPersonName=x.ContactPersonName;r.ContactDepartment=x.ContactDepartment;r.ContactRole=x.ContactRole;r.ContactPhone=x.ContactPhone;r.ContactEmail=x.ContactEmail;
     r.PreferredContactMethod=x.PreferredContactMethod;r.DeliveryInstructions=x.DeliveryInstructions;
     r.CustomerDeliveryOverride=EffectiveDeliveryAllowance.ToPersistence(x.CustomerDeliveryOverride);
     r.BillingContactNotes=x.BillingContactNotes;r.InternalNotes=x.InternalNotes;
     r.CategoryDiscountOverrides = x.CustomerCategoryDiscountOverrides
         .Select(o => new ConnectedSupplierRelationshipCategoryDiscountOverrideRecord
         {
             RelationshipId = x.Id.Value,
             CategoryId = o.CategoryId,
             DiscountPercent = o.DiscountPercent
         })
         .ToList();
     r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static OrganizationConnectedCommerceSettings ToDomain(OrganizationConnectedCommerceSettingsRecord record) =>
        OrganizationConnectedCommerceSettings.Rehydrate(
            record.Id,
            PosOrganizationId.From(record.OrganizationId),
            record.AllowPayBeforeFulfillment,
            record.AllowPayOnDeliveryOrReceipt,
            record.AllowSupplierCredit,
            (ConnectedPoPaymentTiming)record.DefaultPaymentTiming,
            record.DefaultB2bDiscountPercent,
            record.CategoryRules
                .Select(r => new OrganizationConnectedCommerceCategoryRule(r.CategoryId, r.DiscountPercent))
                .ToList(),
            record.ProposalReservationHoldHours,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.ReturnsAllowed,
            record.ReturnWindowDays,
            record.ReceivingIssueWindowDays,
            record.RequireReturnApproval,
            record.CategoryReturnRules
                .Select(r => new OrganizationConnectedCommerceCategoryReturnRule(
                    r.CategoryId,
                    (ConnectedPoReturnPolicyMode)r.Mode,
                    r.ReturnsAllowed,
                    r.ReturnWindowDays))
                .ToList());

    public static OrganizationConnectedCommerceSettingsRecord ToRecord(OrganizationConnectedCommerceSettings settings) =>
        new()
        {
            Id = settings.SettingId,
            OrganizationId = settings.OrganizationId.Value,
            AllowPayBeforeFulfillment = settings.AllowPayBeforeFulfillment,
            AllowPayOnDeliveryOrReceipt = settings.AllowPayOnDeliveryOrReceipt,
            AllowSupplierCredit = settings.AllowSupplierCredit,
            DefaultPaymentTiming = (int)settings.DefaultPaymentTiming,
            DefaultB2bDiscountPercent = settings.DefaultB2bDiscountPercent,
            ProposalReservationHoldHours = settings.ProposalReservationHoldHours,
            ReturnsAllowed = settings.ReturnsAllowed,
            ReturnWindowDays = settings.ReturnWindowDays,
            ReceivingIssueWindowDays = settings.ReceivingIssueWindowDays,
            RequireReturnApproval = settings.RequireReturnApproval,
            CreatedAtUtc = settings.CreatedAtUtc,
            UpdatedAtUtc = settings.UpdatedAtUtc,
            CategoryRules = settings.CategoryRules
                .Select(r => new OrganizationConnectedCommerceCategoryRuleRecord
                {
                    OrganizationConnectedCommerceSettingsId = settings.SettingId,
                    CategoryId = r.CategoryId,
                    DiscountPercent = r.DiscountPercent
                })
                .ToList(),
            CategoryReturnRules = settings.CategoryReturnRules
                .Select(r => new OrganizationConnectedCommerceCategoryReturnRuleRecord
                {
                    OrganizationConnectedCommerceSettingsId = settings.SettingId,
                    CategoryId = r.CategoryId,
                    Mode = (short)r.Mode,
                    ReturnsAllowed = r.ReturnsAllowed,
                    ReturnWindowDays = r.ReturnWindowDays
                })
                .ToList()
        };

    public static void Apply(OrganizationConnectedCommerceSettings settings, OrganizationConnectedCommerceSettingsRecord record)
    {
        record.AllowPayBeforeFulfillment = settings.AllowPayBeforeFulfillment;
        record.AllowPayOnDeliveryOrReceipt = settings.AllowPayOnDeliveryOrReceipt;
        record.AllowSupplierCredit = settings.AllowSupplierCredit;
        record.DefaultPaymentTiming = (int)settings.DefaultPaymentTiming;
        record.DefaultB2bDiscountPercent = settings.DefaultB2bDiscountPercent;
        record.ProposalReservationHoldHours = settings.ProposalReservationHoldHours;
        record.ReturnsAllowed = settings.ReturnsAllowed;
        record.ReturnWindowDays = settings.ReturnWindowDays;
        record.ReceivingIssueWindowDays = settings.ReceivingIssueWindowDays;
        record.RequireReturnApproval = settings.RequireReturnApproval;
        record.UpdatedAtUtc = settings.UpdatedAtUtc;
        record.CategoryRules = settings.CategoryRules
            .Select(r => new OrganizationConnectedCommerceCategoryRuleRecord
            {
                OrganizationConnectedCommerceSettingsId = settings.SettingId,
                CategoryId = r.CategoryId,
                DiscountPercent = r.DiscountPercent
            })
            .ToList();
        record.CategoryReturnRules = settings.CategoryReturnRules
            .Select(r => new OrganizationConnectedCommerceCategoryReturnRuleRecord
            {
                OrganizationConnectedCommerceSettingsId = settings.SettingId,
                CategoryId = r.CategoryId,
                Mode = (short)r.Mode,
                ReturnsAllowed = r.ReturnsAllowed,
                ReturnWindowDays = r.ReturnWindowDays
            })
            .ToList();
    }

    public static SupplierProductExposure ToDomain(SupplierProductExposureRecord r, string? categoryNameOverride = null)=>SupplierProductExposure.Rehydrate(
        SupplierProductExposureId.From(r.Id),PosOrganizationId.From(r.SupplierOrganizationId),CatalogProductId.From(r.ProductId),
        r.SkuSnapshot,r.NameSnapshot,categoryNameOverride ?? r.CategoryNameSnapshot,r.UnitOfMeasureCode,r.SupplierOrderPrice,r.IsOrderable,r.IsExposed,
        r.SyncVersion,r.CreatedAtUtc,r.UpdatedAtUtc);
    public static SupplierProductExposureRecord ToRecord(SupplierProductExposure x)=>new(){Id=x.Id.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,
        ProductId=x.ProductId.Value,SkuSnapshot=x.SkuSnapshot,NameSnapshot=x.NameSnapshot,CategoryNameSnapshot=x.CategoryNameSnapshot,
        UnitOfMeasureCode=x.UnitOfMeasureCode,SupplierOrderPrice=x.SupplierOrderPrice,IsOrderable=x.IsOrderable,IsExposed=x.IsExposed,
        SyncVersion=x.SyncVersion,CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(SupplierProductExposure x,SupplierProductExposureRecord r)
    {r.SkuSnapshot=x.SkuSnapshot;r.NameSnapshot=x.NameSnapshot;r.CategoryNameSnapshot=x.CategoryNameSnapshot;r.UnitOfMeasureCode=x.UnitOfMeasureCode;
     r.SupplierOrderPrice=x.SupplierOrderPrice;r.IsOrderable=x.IsOrderable;r.IsExposed=x.IsExposed;r.SyncVersion=x.SyncVersion;r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static ConnectedBuyerProductShare ToDomain(ConnectedBuyerProductShareRecord r) =>
        ConnectedBuyerProductShare.Rehydrate(
            ConnectedBuyerProductShareId.From(r.Id),
            ConnectedSupplierRelationshipId.From(r.RelationshipId),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SupplierOrganizationId),
            CatalogProductId.From(r.SupplierProductId),
            r.IsShared,
            r.BuyerSpecificPoPrice,
            r.SyncVersion,
            r.CreatedAtUtc,
            r.UpdatedAtUtc);
    public static ConnectedBuyerProductShareRecord ToRecord(ConnectedBuyerProductShare x) => new()
    {
        Id=x.Id.Value, RelationshipId=x.RelationshipId.Value, BuyerOrganizationId=x.BuyerOrganizationId.Value,
        SupplierOrganizationId=x.SupplierOrganizationId.Value, SupplierProductId=x.SupplierProductId.Value,
        IsShared=x.IsShared, BuyerSpecificPoPrice=x.BuyerSpecificPoPrice, SyncVersion=x.SyncVersion,
        CreatedAtUtc=x.CreatedAtUtc, UpdatedAtUtc=x.UpdatedAtUtc
    };
    public static void Apply(ConnectedBuyerProductShare x, ConnectedBuyerProductShareRecord r)
    {
        r.IsShared=x.IsShared; r.BuyerSpecificPoPrice=x.BuyerSpecificPoPrice;
        r.SyncVersion=x.SyncVersion; r.UpdatedAtUtc=x.UpdatedAtUtc;
    }

    public static BuyerSupplierProductLink ToDomain(BuyerSupplierProductLinkRecord r)=>BuyerSupplierProductLink.Rehydrate(
        BuyerSupplierProductLinkId.From(r.Id),ConnectedSupplierRelationshipId.From(r.RelationshipId),PosOrganizationId.From(r.BuyerOrganizationId),
        PosOrganizationId.From(r.SupplierOrganizationId),CatalogProductId.From(r.BuyerProductId),CatalogProductId.From(r.SupplierProductId),
        r.SupplierSkuSnapshot,r.SupplierNameSnapshot,r.UnitOfMeasureCode,r.LastKnownOrderPrice,r.IsActive,r.SyncVersion,r.CreatedAtUtc,r.UpdatedAtUtc,
        r.BuyerPurchaseUnitId,r.MultiplierToBase,r.PackageLabel);
    public static BuyerSupplierProductLinkRecord ToRecord(BuyerSupplierProductLink x)=>new(){Id=x.Id.Value,RelationshipId=x.RelationshipId.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,BuyerProductId=x.BuyerProductId.Value,
        SupplierProductId=x.SupplierProductId.Value,SupplierSkuSnapshot=x.SupplierSkuSnapshot,SupplierNameSnapshot=x.SupplierNameSnapshot,
        UnitOfMeasureCode=x.UnitOfMeasureCode,LastKnownOrderPrice=x.LastKnownOrderPrice,BuyerPurchaseUnitId=x.BuyerPurchaseUnitId,
        MultiplierToBase=x.MultiplierToBase,PackageLabel=x.PackageLabel,IsActive=x.IsActive,SyncVersion=x.SyncVersion,
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(BuyerSupplierProductLink x,BuyerSupplierProductLinkRecord r)
    {r.SupplierSkuSnapshot=x.SupplierSkuSnapshot;r.SupplierNameSnapshot=x.SupplierNameSnapshot;r.UnitOfMeasureCode=x.UnitOfMeasureCode;
     r.LastKnownOrderPrice=x.LastKnownOrderPrice;r.BuyerPurchaseUnitId=x.BuyerPurchaseUnitId;r.MultiplierToBase=x.MultiplierToBase;
     r.PackageLabel=x.PackageLabel;r.IsActive=x.IsActive;r.SyncVersion=x.SyncVersion;r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static ConnectedPurchaseOrder ToDomain(ConnectedPurchaseOrderRecord r)
    {
        var status = (ConnectedPurchaseOrderStatus)r.Status;
        var lines = r.Lines.OrderBy(x => x.LineNumber).Select(x =>
        {
            var availability = (ConnectedPoLineAvailability)x.Availability;
            var confirmed = x.ConfirmedQty;
            if (confirmed is null
                && status is ConnectedPurchaseOrderStatus.Accepted
                    or ConnectedPurchaseOrderStatus.Preparing
                    or ConnectedPurchaseOrderStatus.Fulfilled)
            {
                confirmed = x.Qty;
                if (availability == ConnectedPoLineAvailability.Pending)
                {
                    availability = ConnectedPoLineAvailability.Available;
                }
            }

            return new ConnectedPurchaseOrderLine(
                CatalogProductId.From(x.ProductId),
                x.NameSnapshot,
                x.SkuSnapshot,
                x.Qty,
                x.UnitPriceSnapshot,
                x.LineTotal,
                x.UnitOfMeasureCode,
                x.ProposedQty,
                confirmed,
                availability,
                x.ProposedUnitPrice,
                x.ConfirmedUnitPrice);
        }).ToList();

        return ConnectedPurchaseOrder.Rehydrate(
            ConnectedPurchaseOrderId.From(r.Id),
            ConnectedSupplierRelationshipId.From(r.RelationshipId),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SupplierOrganizationId),
            PurchaseOrderId.From(r.BuyerPurchaseOrderId),
            r.BuyerPoNumber,
            r.OrderDate,
            r.Notes,
            status,
            r.TotalAmount,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.AcceptedAtUtc,
            r.DeclinedAtUtc,
            lines,
            r.PreparingAtUtc,
            r.FulfilledAtUtc,
            r.WithdrawnAtUtc,
            r.DeclineReason is int reason ? (ConnectedPoDeclineReason)reason : null,
            r.DeclineNote,
            (ConnectedPoPaymentTerm)r.PaymentTerm,
            (ConnectedPoPaymentTiming)r.PaymentTiming,
            r.ChangesProposedAtUtc,
            r.ChangesProposedByUserId,
            r.BuyerRespondedAtUtc,
            r.BuyerRespondedByUserId,
            r.ProposedPaymentTerm is int ppt ? (ConnectedPoPaymentTerm)ppt : null,
            r.ProposedPaymentTiming is int pptime ? (ConnectedPoPaymentTiming)pptime : null,
            r.ConfirmedPaymentTerm is int cpt ? (ConnectedPoPaymentTerm)cpt : null,
            r.ConfirmedPaymentTiming is int cptm ? (ConnectedPoPaymentTiming)cptm : null,
            r.CreditPostedAmount,
            (ConnectedPoInventoryReservationState)r.InventoryReservationState,
            r.InventoryReservationExpiresAtUtc,
            r.InventoryReservationRevision,
            r.FulfillmentMethod,
            r.ConfirmedFulfillmentMethod);
    }

    public static ConnectedPurchaseOrderRecord ToRecord(ConnectedPurchaseOrder x)=>new(){Id=x.Id.Value,RelationshipId=x.RelationshipId.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,BuyerPurchaseOrderId=x.BuyerPurchaseOrderId.Value,
        BuyerPoNumber=x.BuyerPoNumber,OrderDate=x.OrderDate,Notes=x.Notes,Status=(int)x.Status,TotalAmount=x.TotalAmount,
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc,AcceptedAtUtc=x.AcceptedAtUtc,DeclinedAtUtc=x.DeclinedAtUtc,
        PreparingAtUtc=x.PreparingAtUtc,FulfilledAtUtc=x.FulfilledAtUtc,WithdrawnAtUtc=x.WithdrawnAtUtc,
        DeclineReason=x.DeclineReason is null ? null : (int)x.DeclineReason.Value,DeclineNote=x.DeclineNote,
        PaymentTerm=(int)x.PaymentTerm,
        PaymentTiming=(int)x.PaymentTiming,
        FulfillmentMethod=x.FulfillmentMethod,
        ConfirmedFulfillmentMethod=x.ConfirmedFulfillmentMethod,
        ProposedPaymentTerm=x.ProposedPaymentTerm is null ? null : (int)x.ProposedPaymentTerm.Value,
        ProposedPaymentTiming=x.ProposedPaymentTiming is null ? null : (int)x.ProposedPaymentTiming.Value,
        ConfirmedPaymentTerm=x.ConfirmedPaymentTerm is null ? null : (int)x.ConfirmedPaymentTerm.Value,
        ConfirmedPaymentTiming=x.ConfirmedPaymentTiming is null ? null : (int)x.ConfirmedPaymentTiming.Value,
        CreditPostedAmount=x.CreditPostedAmount,
        ChangesProposedAtUtc=x.ChangesProposedAtUtc,ChangesProposedByUserId=x.ChangesProposedByUserId,
        BuyerRespondedAtUtc=x.BuyerRespondedAtUtc,BuyerRespondedByUserId=x.BuyerRespondedByUserId,
        InventoryReservationState=(int)x.InventoryReservationState,
        InventoryReservationExpiresAtUtc=x.InventoryReservationExpiresAtUtc,
        InventoryReservationRevision=x.InventoryReservationRevision,
        Lines=x.Lines.Select((l,i)=>new ConnectedPurchaseOrderLineRecord{ConnectedPurchaseOrderId=x.Id.Value,LineNumber=i+1,ProductId=l.ProductId.Value,
            NameSnapshot=l.NameSnapshot,SkuSnapshot=l.SkuSnapshot,Qty=l.Qty,ProposedQty=l.ProposedQty,ConfirmedQty=l.ConfirmedQty,
            Availability=(int)l.Availability,UnitPriceSnapshot=l.UnitPriceSnapshot,LineTotal=l.LineTotal,
            UnitOfMeasureCode=l.UnitOfMeasureCode,
            ProposedUnitPrice=l.ProposedUnitPrice,ConfirmedUnitPrice=l.ConfirmedUnitPrice}).ToList()};
    public static void Apply(ConnectedPurchaseOrder x,ConnectedPurchaseOrderRecord r)
    {
        r.Status=(int)x.Status;r.UpdatedAtUtc=x.UpdatedAtUtc;r.AcceptedAtUtc=x.AcceptedAtUtc;r.DeclinedAtUtc=x.DeclinedAtUtc;
        r.PreparingAtUtc=x.PreparingAtUtc;r.FulfilledAtUtc=x.FulfilledAtUtc;r.WithdrawnAtUtc=x.WithdrawnAtUtc;
        r.DeclineReason=x.DeclineReason is null ? null : (int)x.DeclineReason.Value;r.DeclineNote=x.DeclineNote;
        r.PaymentTerm=(int)x.PaymentTerm;
        r.PaymentTiming=(int)x.PaymentTiming;
        r.FulfillmentMethod=x.FulfillmentMethod;
        r.ConfirmedFulfillmentMethod=x.ConfirmedFulfillmentMethod;
        r.ProposedPaymentTerm=x.ProposedPaymentTerm is null ? null : (int)x.ProposedPaymentTerm.Value;
        r.ProposedPaymentTiming=x.ProposedPaymentTiming is null ? null : (int)x.ProposedPaymentTiming.Value;
        r.ConfirmedPaymentTerm=x.ConfirmedPaymentTerm is null ? null : (int)x.ConfirmedPaymentTerm.Value;
        r.ConfirmedPaymentTiming=x.ConfirmedPaymentTiming is null ? null : (int)x.ConfirmedPaymentTiming.Value;
        r.CreditPostedAmount=x.CreditPostedAmount;
        r.ChangesProposedAtUtc=x.ChangesProposedAtUtc;r.ChangesProposedByUserId=x.ChangesProposedByUserId;
        r.BuyerRespondedAtUtc=x.BuyerRespondedAtUtc;r.BuyerRespondedByUserId=x.BuyerRespondedByUserId;
        r.InventoryReservationState=(int)x.InventoryReservationState;
        r.InventoryReservationExpiresAtUtc=x.InventoryReservationExpiresAtUtc;
        r.InventoryReservationRevision=x.InventoryReservationRevision;
        var domainByProduct=x.Lines.ToDictionary(l => l.ProductId.Value);
        foreach(var lineRecord in r.Lines)
        {
            if(!domainByProduct.TryGetValue(lineRecord.ProductId, out var line))
            {
                continue;
            }

            lineRecord.ProposedQty=line.ProposedQty;
            lineRecord.ConfirmedQty=line.ConfirmedQty;
            lineRecord.Availability=(int)line.Availability;
            lineRecord.ProposedUnitPrice=line.ProposedUnitPrice;
            lineRecord.ConfirmedUnitPrice=line.ConfirmedUnitPrice;
        }
    }

    public static ConnectedPoInventoryReservation ToDomain(ConnectedPoInventoryReservationRecord r) =>
        ConnectedPoInventoryReservation.Rehydrate(
            ConnectedPoInventoryReservationId.From(r.Id),
            PosOrganizationId.From(r.OrganizationId),
            PosBranchId.From(r.BranchId),
            CatalogProductId.From(r.ProductId),
            ConnectedPurchaseOrderId.From(r.ConnectedPurchaseOrderId),
            r.Revision,
            r.Quantity,
            r.RemainingQuantity,
            (ConnectedPoReservationType)r.Type,
            (ConnectedPoReservationStatus)r.Status,
            r.CreatedAtUtc,
            r.ExpiresAtUtc,
            r.ReleasedAtUtc,
            r.Version);

    public static ConnectedPoInventoryReservationRecord ToRecord(ConnectedPoInventoryReservation x) => new()
    {
        Id = x.Id.Value,
        OrganizationId = x.OrganizationId.Value,
        BranchId = x.BranchId.Value,
        ProductId = x.ProductId.Value,
        ConnectedPurchaseOrderId = x.ConnectedPurchaseOrderId.Value,
        Revision = x.Revision,
        Quantity = x.Quantity,
        RemainingQuantity = x.RemainingQuantity,
        Type = (int)x.Type,
        Status = (int)x.Status,
        CreatedAtUtc = x.CreatedAtUtc,
        ExpiresAtUtc = x.ExpiresAtUtc,
        ReleasedAtUtc = x.ReleasedAtUtc,
        Version = x.Version
    };

    public static void Apply(ConnectedPoInventoryReservation x, ConnectedPoInventoryReservationRecord r)
    {
        r.Revision = x.Revision;
        r.Quantity = x.Quantity;
        r.RemainingQuantity = x.RemainingQuantity;
        r.Type = (int)x.Type;
        r.Status = (int)x.Status;
        r.ExpiresAtUtc = x.ExpiresAtUtc;
        r.ReleasedAtUtc = x.ReleasedAtUtc;
        r.Version = x.Version;
    }
}
