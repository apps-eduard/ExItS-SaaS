import type { Plugin } from "vite";

const BLOCKED_SW_PATH =
  /^\/(?:sw\.js(?:\.map)?|dev-sw\.js(?:\.map)?|workbox-[^/?#]+\.js(?:\.map)?)(?:[?#]|$)/i;

/**
 * In Vite development, never SPA-fallback HTML for service-worker script URLs.
 * Returning index.html as /sw.js can leave browsers with a broken/stale worker
 * that shadows the live React source (especially on Android emulator Chrome).
 */
export function blockServiceWorkerScriptsInDev(): Plugin {
  return {
    name: "exits-block-service-worker-scripts-in-dev",
    apply: "serve",
    configureServer(server) {
      server.middlewares.use((request, response, next) => {
        const url = request.url ?? "";
        const pathname = url.split("?")[0] ?? "";
        if (!BLOCKED_SW_PATH.test(pathname)) {
          next();
          return;
        }

        response.statusCode = 404;
        response.setHeader("Content-Type", "text/plain; charset=utf-8");
        response.setHeader("Cache-Control", "no-store");
        response.end("Service worker scripts are disabled during Vite development.");
      });
    },
  };
}

export function isBlockedServiceWorkerDevPath(pathname: string): boolean {
  return BLOCKED_SW_PATH.test(pathname);
}
