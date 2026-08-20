using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>Development-stage organization scope header for POS APIs.</summary>
public static class PosOrganizationHeaders
{
    public const string OrganizationHeaderName = "X-Pos-Organization-Id";
    public const string ActorHeaderName = "X-Dev-Platform-User-Id";
    public const string BranchHeaderName = "X-Pos-Branch-Id";
}

internal static class PosOrganizationScope
{
    public static bool TryGetOrganizationId(HttpRequest request, out Guid organizationId, out IResult? problem)
    {
        organizationId = default;
        problem = null;

        if (IsIntrospectionUnavailable(request.HttpContext))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.PlatformAuthUnavailable,
                "Authentication service is temporarily unavailable. Try again shortly.",
                StatusCodes.Status503ServiceUnavailable);
            return false;
        }

        if (IsBearerDenied(request.HttpContext))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.ActorRequired,
                "Bearer access token is inactive or invalid.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        if (TryGetBearerOrganizationId(request.HttpContext, out organizationId))
        {
            return true;
        }

        var environment = request.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment))
        {
            problem = PosDevelopmentEnvironment.DevelopmentHeadersUnavailable();
            return false;
        }

        if (!request.Headers.TryGetValue(PosOrganizationHeaders.OrganizationHeaderName, out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.OrganizationRequired,
                $"Header '{PosOrganizationHeaders.OrganizationHeaderName}' is required.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        if (!Guid.TryParse(values.First(), out organizationId) || organizationId == Guid.Empty)
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidOrganizationId,
                "Organization id must be a non-empty GUID.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    public static bool TryGetActorId(HttpRequest request, out Guid actorId, out IResult? problem)
    {
        actorId = default;
        problem = null;

        if (IsIntrospectionUnavailable(request.HttpContext))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.PlatformAuthUnavailable,
                "Authentication service is temporarily unavailable. Try again shortly.",
                StatusCodes.Status503ServiceUnavailable);
            return false;
        }

        if (IsBearerDenied(request.HttpContext))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.ActorRequired,
                "Bearer access token is inactive or invalid.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        if (TryGetBearerUserId(request.HttpContext, out actorId))
        {
            return true;
        }

        var environment = request.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment))
        {
            problem = PosDevelopmentEnvironment.DevelopmentHeadersUnavailable();
            return false;
        }

        if (!request.Headers.TryGetValue(PosOrganizationHeaders.ActorHeaderName, out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.ActorRequired,
                $"Header '{PosOrganizationHeaders.ActorHeaderName}' is required for repayment recording and reversal.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        if (!Guid.TryParse(values.First(), out actorId) || actorId == Guid.Empty)
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidRepaymentActor,
                "Actor id must be a non-empty GUID.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    public static bool TryGetOptionalBranchId(HttpRequest request, out Guid? branchId)
    {
        branchId = null;
        if (!request.Headers.TryGetValue(PosOrganizationHeaders.BranchHeaderName, out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return true;
        }

        if (!Guid.TryParse(values.First(), out var parsed) || parsed == Guid.Empty)
        {
            return false;
        }

        branchId = parsed;
        return true;
    }

    public static bool TryGetBranchId(HttpRequest request, out Guid branchId, out IResult? problem)
    {
        branchId = default;
        problem = null;
        if (!TryGetOptionalBranchId(request, out var optional) || optional is null)
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidBranchId,
                $"Header '{PosOrganizationHeaders.BranchHeaderName}' is required for branch inventory transfers.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        branchId = optional.Value;
        return true;
    }

    private static bool IsBearerDenied(HttpContext context) =>
        context.Items.TryGetValue(PosAuthItems.Denied, out var denied) && denied is true;

    private static bool IsIntrospectionUnavailable(HttpContext context) =>
        context.Items.TryGetValue(PosAuthItems.IntrospectionUnavailable, out var unavailable)
        && unavailable is true;

    private static bool TryGetBearerUserId(HttpContext context, out Guid userId)
    {
        userId = default;
        if (context.Items.TryGetValue(PosAuthItems.UserId, out var value) && value is Guid id && id != Guid.Empty)
        {
            userId = id;
            return true;
        }

        return false;
    }

    private static bool TryGetBearerOrganizationId(HttpContext context, out Guid organizationId)
    {
        organizationId = default;
        if (context.Items.TryGetValue(PosAuthItems.OrganizationId, out var value)
            && value is Guid id
            && id != Guid.Empty)
        {
            organizationId = id;
            return true;
        }

        return false;
    }
}
