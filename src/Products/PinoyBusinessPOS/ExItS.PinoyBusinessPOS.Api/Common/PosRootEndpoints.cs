using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Browser-friendly service card at <c>/</c>, plus JSON for API clients.
/// </summary>
internal static class PosRootEndpoints
{
    public const string PhaseMarker = "P10-WP08-phase-10-closeout";

    public static WebApplication MapPosRootEndpoint(this WebApplication app)
    {
        app.MapGet("/", (HttpRequest request, IHostEnvironment env, IConfiguration config) =>
            {
                var localValidation = config.GetValue("LocalValidation:Enabled", false);
                var payload = new
                {
                    service = "ExItS.PinoyBusinessPOS.Api",
                    displayName = "ExItS PinoyBusinessPOS API",
                    status = "running",
                    environment = env.EnvironmentName,
                    localValidation,
                    phase = PhaseMarker,
                    health = "/health",
                    readiness = "/health/ready",
                    note = "This is an API service, not a website. There is no POS web UI on this port — use the MAUI POS client."
                };

                if (WantsHtml(request))
                {
                    return Results.Content(
                        BuildHtml(
                            title: "ExItS PinoyBusinessPOS API",
                            portHint: "8092 (Local Validation local)",
                            environment: env.EnvironmentName,
                            localValidation: localValidation,
                            links:
                            [
                                ("Liveness", "/health"),
                                ("Readiness", "/health/ready"),
                                ("Platform API", "http://127.0.0.1:8091/"),
                                ("Platform Admin UI", "http://127.0.0.1:8090/admin/login")
                            ],
                            note: "JSON API only — POS UI is the MAUI app, not this URL."),
                        MediaTypeNames.Text.Html,
                        Encoding.UTF8);
                }

                return Results.Json(payload);
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    private static bool WantsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildHtml(
        string title,
        string portHint,
        string environment,
        bool localValidation,
        IReadOnlyList<(string Label, string Href)> links,
        string note)
    {
        var enc = HtmlEncoder.Default;
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.Append("<title>").Append(enc.Encode(title)).Append("</title>");
        sb.Append("""
<style>
  :root { color-scheme: light dark; font-family: Segoe UI, system-ui, sans-serif; }
  body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #10140f; color: #eaf1e7; }
  main { width: min(36rem, calc(100% - 2rem)); background: #1a2218; border: 1px solid #2f3c2a; border-radius: 12px; padding: 1.5rem 1.75rem; }
  h1 { margin: 0 0 .35rem; font-size: 1.35rem; }
  .badge { display: inline-block; padding: .15rem .55rem; border-radius: 999px; background: #1f3d24; color: #8fd49a; font-size: .75rem; font-weight: 600; }
  dl { display: grid; grid-template-columns: 8.5rem 1fr; gap: .4rem .75rem; margin: 1rem 0; font-size: .92rem; }
  dt { color: #9aaf93; } dd { margin: 0; word-break: break-word; }
  ul { margin: .75rem 0 0; padding-left: 1.1rem; }
  a { color: #9dcf88; }
  .note { margin-top: 1rem; color: #9aaf93; font-size: .85rem; line-height: 1.4; }
</style></head><body><main>
""");
        sb.Append("<p class=\"badge\">Status: running</p>");
        sb.Append("<h1>").Append(enc.Encode(title)).Append("</h1>");
        sb.Append("<dl>");
        sb.Append("<dt>Service</dt><dd>ExItS.PinoyBusinessPOS.Api</dd>");
        sb.Append("<dt>Typical port</dt><dd>").Append(enc.Encode(portHint)).Append("</dd>");
        sb.Append("<dt>Environment</dt><dd>").Append(enc.Encode(environment)).Append("</dd>");
        sb.Append("<dt>Local Validation</dt><dd>").Append(localValidation ? "enabled" : "disabled").Append("</dd>");
        sb.Append("<dt>Phase</dt><dd>").Append(enc.Encode(PhaseMarker)).Append("</dd>");
        sb.Append("</dl><p><strong>Useful links</strong></p><ul>");
        foreach (var (label, href) in links)
        {
            sb.Append("<li><a href=\"").Append(enc.Encode(href)).Append("\">")
                .Append(enc.Encode(label)).Append("</a> <code>")
                .Append(enc.Encode(href)).Append("</code></li>");
        }

        sb.Append("</ul><p class=\"note\">").Append(enc.Encode(note)).Append("</p>");
        sb.Append("</main></body></html>");
        return sb.ToString();
    }
}
