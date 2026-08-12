using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Customers;

/// <summary>
/// Full linked-customer statement authorization context after Platform link proof
/// and POS correlation both succeed. Contains only identifiers later statement APIs need.
/// </summary>
public sealed record AuthorizedLinkedCustomerContext(
    Guid PersonalUserId,
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid LinkedCustomerAppUserId,
    Guid PosCustomerId);

public sealed record LinkedCustomerPlatformAuthorizationProof(
    Guid PersonalUserId,
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid LinkedCustomerAppUserId);

public enum LinkedCustomerPlatformAuthorizationOutcome
{
    Authorized = 1,
    Denied = 2,
    NotFound = 3
}

public sealed record LinkedCustomerPlatformAuthorizationResult(
    LinkedCustomerPlatformAuthorizationOutcome Outcome,
    LinkedCustomerPlatformAuthorizationProof? Proof);

/// <summary>
/// Platform-owned link proof. POS Application must not query the Platform database.
/// WP04 wires an HTTP adapter that forwards the Personal session to Platform.
/// </summary>
public interface ILinkedCustomerPlatformAuthorization
{
    Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fail-closed composer: Platform Personal link proof + POS organization-scoped correlation.
/// Does not trust a client-supplied user id. Does not match by email, name, or phone.
/// </summary>
public sealed class AuthorizeLinkedCustomerStatementAccess
{
    private const string NotFoundMessage = "Linked customer was not found.";
    private const string DeniedMessage = "Linked customer access is denied.";

    private readonly ILinkedCustomerPlatformAuthorization _platform;
    private readonly IPOSCustomerRepository _customers;

    public AuthorizeLinkedCustomerStatementAccess(
        ILinkedCustomerPlatformAuthorization platform,
        IPOSCustomerRepository customers)
    {
        _platform = platform;
        _customers = customers;
    }

    public async Task<ApplicationResult<AuthorizedLinkedCustomerContext>> ExecuteAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        Guid? posCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || platformBusinessCustomerId == Guid.Empty)
        {
            return NotFound();
        }

        var platform = await _platform
            .VerifyAsync(organizationId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (platform.Outcome == LinkedCustomerPlatformAuthorizationOutcome.Denied)
        {
            return ApplicationResult<AuthorizedLinkedCustomerContext>.Failure(
                ApplicationErrorCodes.LinkedCustomerDenied,
                DeniedMessage);
        }

        if (platform.Outcome != LinkedCustomerPlatformAuthorizationOutcome.Authorized
            || platform.Proof is null
            || platform.Proof.PersonalUserId == Guid.Empty
            || platform.Proof.LinkedCustomerAppUserId == Guid.Empty
            || platform.Proof.OrganizationId != organizationId
            || platform.Proof.PlatformBusinessCustomerId != platformBusinessCustomerId)
        {
            return NotFound();
        }

        var orgId = PosOrganizationId.From(organizationId);
        var matches = await _customers
            .CountByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (matches != 1)
        {
            return NotFound();
        }

        var posCustomer = await _customers
            .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (posCustomer is null
            || posCustomer.OrganizationId != orgId
            || posCustomer.PlatformBusinessCustomerId is null
            || posCustomer.PlatformBusinessCustomerId != platformBusinessCustomerId)
        {
            return NotFound();
        }

        if (posCustomerId is Guid expectedPosCustomerId
            && expectedPosCustomerId != posCustomer.Id.Value)
        {
            return NotFound();
        }

        return ApplicationResult<AuthorizedLinkedCustomerContext>.Success(
            new AuthorizedLinkedCustomerContext(
                platform.Proof.PersonalUserId,
                organizationId,
                platformBusinessCustomerId,
                platform.Proof.LinkedCustomerAppUserId,
                posCustomer.Id.Value));
    }

    private static ApplicationResult<AuthorizedLinkedCustomerContext> NotFound() =>
        ApplicationResult<AuthorizedLinkedCustomerContext>.Failure(
            ApplicationErrorCodes.LinkedCustomerNotFound,
            NotFoundMessage);
}
