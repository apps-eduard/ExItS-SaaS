using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>Development-stage organization scope header for POS APIs.</summary>
public static class PosOrganizationHeaders
{
    public const string OrganizationHeaderName = "X-Pos-Organization-Id";
    public const string ActorHeaderName = "X-Dev-Platform-User-Id";
}

internal static class PosOrganizationScope
{
    public static bool TryGetOrganizationId(HttpRequest request, out Guid organizationId, out IResult? problem)
    {
        organizationId = default;
        problem = null;

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
}
