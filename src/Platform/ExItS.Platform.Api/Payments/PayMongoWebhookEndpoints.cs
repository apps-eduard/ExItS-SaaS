using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Payments;

internal static class PayMongoWebhookEndpoints
{
    public static IEndpointRouteBuilder MapPayMongoWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        Map(app, "/api/v1/platform/webhooks/paymongo");
        Map(app, "/api/webhooks/paymongo");
        return app;
    }

    private static void Map(IEndpointRouteBuilder app, string path)
    {
        app.MapPost(path, HandleAsync)
            .AllowAnonymous()
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        ApplyTrustedHostedCheckoutPayment applyPaid,
        IOptions<PayMongoOptions> options,
        CancellationToken cancellationToken)
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string raw;
        using (var reader = new StreamReader(http.Request.Body))
        {
            raw = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        var signature = http.Request.Headers["Paymongo-Signature"].ToString();
        if (!PayMongoWebhookSignature.Matches(raw, signature, secret))
        {
            return Results.Unauthorized();
        }

        if (!PayMongoCheckoutPayload.TryParsePaidEvent(raw, out var eventId, out var state)
            || !state.IsPaid
            || state.PaidAmount is not decimal amount
            || string.IsNullOrWhiteSpace(state.CurrencyCode))
        {
            return Results.Ok(new { ignored = true });
        }

        var result = await applyPaid
            .ExecuteAsync(state.SessionId, amount, state.CurrencyCode, eventId, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return Results.Ok(new { status = "accepted" });
        }

        if (result.ErrorCode is ApplicationErrorCodes.PaymentAmountMismatch
            or ApplicationErrorCodes.PaymentCurrencyMismatch)
        {
            return Results.UnprocessableEntity();
        }

        if (result.ErrorCode == ApplicationErrorCodes.PaymentNotFound)
        {
            return Results.NotFound();
        }

        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}
