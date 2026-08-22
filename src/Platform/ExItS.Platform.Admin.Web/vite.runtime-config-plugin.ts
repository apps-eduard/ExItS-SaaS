import type { Plugin } from "vite";

function isTruthyEnv(value: string | undefined): boolean {
  if (!value) {
    return false;
  }
  switch (value.trim().toLowerCase()) {
    case "1":
    case "true":
    case "yes":
      return true;
    default:
      return false;
  }
}

function sanitizeBuildSha(raw: string | undefined): string {
  const value = (raw ?? "").trim();
  return /^[A-Za-z0-9._-]+$/.test(value) ? value : "unknown";
}

/**
 * Serves `/config.js` for host Vite the same way Docker's 40-exits-runtime-config.sh does,
 * so Local Validation tools (weak passwords, test-user picker) work without baking secrets.
 */
export function exitsRuntimeConfigPlugin(): Plugin {
  return {
    name: "exits-platform-admin-runtime-config",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const pathOnly = req.url?.split("?")[0] ?? "";
        if (pathOnly !== "/config.js") {
          next();
          return;
        }

        const sameOrigin = isTruthyEnv(process.env.PLATFORM_API_SAME_ORIGIN);
        const toolsEnabled = isTruthyEnv(process.env.LOCAL_VALIDATION_TOOLS_ENABLED);
        const configuredUrl = (process.env.PLATFORM_API_PUBLIC_URL ?? "").trim();
        const platformApiBaseUrl = sameOrigin ? "" : configuredUrl;
        const buildSha = sanitizeBuildSha(
          process.env.VITE_BUILD_SHA ?? process.env.EXITS_GIT_SHA,
        );

        const body =
          `window.__EXITS_PLATFORM_ADMIN_WEB__={` +
          `app:"Platform Admin React",` +
          `platformApiBaseUrl:${JSON.stringify(platformApiBaseUrl)},` +
          `platformApiSameOrigin:${sameOrigin},` +
          `localValidationToolsEnabled:${toolsEnabled},` +
          `buildSha:${JSON.stringify(buildSha)}` +
          `};\n`;

        res.statusCode = 200;
        res.setHeader("Content-Type", "application/javascript; charset=utf-8");
        res.setHeader("Cache-Control", "no-store");
        res.end(body);
      });
    },
  };
}
