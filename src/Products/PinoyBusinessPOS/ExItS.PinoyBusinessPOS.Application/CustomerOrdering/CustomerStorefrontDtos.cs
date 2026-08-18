namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record CustomerStorefrontCategoryDto(Guid CategoryId, string Name);

public sealed record CustomerStorefrontProductDto(
    Guid ProductId,
    string Name,
    string? Sku,
    string UnitOfMeasure,
    Guid? CategoryId,
    decimal UnitPrice,
    bool IsAvailable,
    bool TracksInventory = false,
    decimal? AvailableQuantity = null,
    string AvailabilityStatus = CustomerStorefrontAvailability.Untracked,
    bool HasImage = false,
    int? ImageVersion = null,
    string ImageSource = "None");

public sealed record CustomerStorefrontBranchDto(
    Guid BranchId,
    string Name,
    bool PickupEnabled,
    bool DeliveryEnabled,
    bool CustomerOrderingOperational,
    bool PickupOperational,
    bool DeliveryOperational,
    bool OnlineOrdersPaused,
    string? StoreStatusMessage);

public sealed record CustomerStorefrontDto(
    Guid OrganizationId,
    string OrganizationDisplayName,
    bool CanCustomerOrder,
    bool CanCustomerDelivery,
    IReadOnlyList<CustomerStorefrontCategoryDto> Categories,
    IReadOnlyList<CustomerStorefrontProductDto> Products,
    int ProductTotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<CustomerStorefrontBranchDto> Branches);
