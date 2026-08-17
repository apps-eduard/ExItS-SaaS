using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Admin.Localization;
using ExItS.Platform.Admin.Models;
using Microsoft.Extensions.Localization;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Native form POSTs for public registration/activation/reset. Interactive Server OnClick
/// cannot be relied on when the Blazor circuit is not connected (Mailpit links, Docker hosts).
/// </summary>
internal static class AdminPublicAuthFormEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static WebApplication MapAdminPublicAuthForms(this WebApplication app)
    {
        app.MapPost("/admin/register/submit", RegisterAsync).AllowAnonymous().DisableAntiforgery();
        app.MapPost("/admin/activate-account/complete", ActivateAsync).AllowAnonymous().DisableAntiforgery();
        app.MapPost("/admin/reset-password/complete", ResetAsync).AllowAnonymous().DisableAntiforgery();
        app.MapPost("/admin/forgot-password/submit", ForgotAsync).AllowAnonymous().DisableAntiforgery();
        return app;
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        IStringLocalizer<AdminResources> localizer)
    {
        var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
        if (!IsAccepted(form["AcceptTerms"]))
        {
            return Redirect("/admin/register", localizer["Register_TermsRequired"]);
        }

        var displayName = form["DisplayName"].ToString().Trim();
        var email = form["Email"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(email))
        {
            return Redirect("/admin/register", localizer["Common_ActionFailed"]);
        }

        var (ok, detail, _) = await PostAsync<PersonalRegistrationAckDto>(
            httpClientFactory,
            "/api/v1/platform/auth/register",
            new RegisterPersonalAccountRequest(displayName, email),
            http.RequestAborted).ConfigureAwait(false);
        if (!ok)
        {
            return Redirect("/admin/register", detail ?? localizer["Common_ActionFailed"]);
        }

        return Results.Redirect("/admin/register?done=1");
    }

    private static async Task<IResult> ActivateAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        IStringLocalizer<AdminResources> localizer)
    {
        var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
        var token = form["Token"].ToString();
        var password = form["Password"].ToString();
        var confirm = form["ConfirmPassword"].ToString();
        var activatePath = "/admin/activate-account" + TokenQuery(token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect(activatePath, "Verification token is missing. Open the link from your email.");
        }

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirm))
        {
            return Redirect(activatePath, localizer["Activate_PasswordRequired"]);
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            return Redirect(activatePath, localizer["Activate_PasswordMismatch"]);
        }

        var (ok, detail, _) = await PostAsync<PlatformCredentialStatusDto>(
            httpClientFactory,
            "/api/v1/platform/auth/activate-account",
            new ActivatePersonalAccountRequest(token, password),
            http.RequestAborted).ConfigureAwait(false);
        if (!ok)
        {
            return Redirect(activatePath, detail ?? localizer["Common_ActionFailed"]);
        }

        return Results.Redirect(
            "/admin/login?notice=" + Uri.EscapeDataString(localizer["Activate_Success"]));
    }

    private static async Task<IResult> ResetAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        IStringLocalizer<AdminResources> localizer)
    {
        var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
        var token = form["Token"].ToString();
        var password = form["Password"].ToString();
        var confirm = form["ConfirmPassword"].ToString();
        var resetPath = "/admin/reset-password" + TokenQuery(token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect(resetPath, localizer["ResetPassword_TokenMissing"]);
        }

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirm))
        {
            return Redirect(resetPath, localizer["ResetPassword_PasswordRequired"]);
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            return Redirect(resetPath, localizer["Activate_PasswordMismatch"]);
        }

        var (ok, detail, _) = await PostAsync<PlatformCredentialStatusDto>(
            httpClientFactory,
            "/api/v1/platform/auth/reset-password",
            new ResetPasswordRequest(token, password),
            http.RequestAborted).ConfigureAwait(false);
        if (!ok)
        {
            return Redirect(resetPath, detail ?? localizer["Common_ActionFailed"]);
        }

        return Results.Redirect(
            "/admin/login?notice=" + Uri.EscapeDataString(localizer["ResetPassword_Success"]));
    }

    private static async Task<IResult> ForgotAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        IStringLocalizer<AdminResources> localizer)
    {
        var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
        var usernameOrEmail = form["UsernameOrEmail"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            return Redirect("/admin/forgot-password", localizer["Common_ActionFailed"]);
        }

        var (ok, detail, _) = await PostAsync<CredentialWorkflowAckDto>(
            httpClientFactory,
            "/api/v1/platform/auth/forgot-password",
            new ForgotPasswordRequest(usernameOrEmail),
            http.RequestAborted).ConfigureAwait(false);
        if (!ok)
        {
            return Redirect("/admin/forgot-password", detail ?? localizer["Common_ActionFailed"]);
        }

        return Results.Redirect("/admin/forgot-password?done=1");
    }

    private static bool IsAccepted(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

    private static string TokenQuery(string? token) =>
        string.IsNullOrWhiteSpace(token)
            ? string.Empty
            : "?token=" + Uri.EscapeDataString(token);

    private static IResult Redirect(string path, string error)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return Results.Redirect(path + separator + "error=" + Uri.EscapeDataString(error));
    }

    private static async Task<(bool Ok, string? Detail, T? Data)> PostAsync<T>(
        IHttpClientFactory httpClientFactory,
        string path,
        object body,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.PostAsJsonAsync(path, body, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return (true, null, data);
        }

        return (false, await ReadDetailAsync(response, cancellationToken).ConfigureAwait(false), default);
    }

    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (document.RootElement.TryGetProperty("detail", out var detail)
                && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }

            if (document.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
            // Fall through to status-code text.
        }

        return response.ReasonPhrase;
    }
}
