import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { exitsRuntimeConfigPlugin } from "./vite.runtime-config-plugin";

const rootDir = path.dirname(fileURLToPath(import.meta.url));

function resolveDevApiProxyTarget(): string {
  const configured = process.env.VITE_PLATFORM_API_PROXY_TARGET?.trim();
  return configured && configured.length > 0 ? configured : "http://127.0.0.1:8091";
}

/** Tailscale/LAN PublicHost from Local Validation launcher (`-PublicHost`). */
function resolveDevPublicHost(): string | undefined {
  const host = (
    process.env.ADMIN_DEV_PUBLIC_HOST ??
    process.env.POS_DEV_PUBLIC_HOST ??
    ""
  ).trim();
  return host.length > 0 ? host : undefined;
}

function resolveAllowedHosts(): string[] {
  const publicHost = resolveDevPublicHost();
  return [
    "127.0.0.1",
    "localhost",
    "10.0.2.2",
    ...(publicHost ? [publicHost] : []),
  ];
}

const publicHost = resolveDevPublicHost();

export default defineConfig({
  plugins: [react(), tailwindcss(), exitsRuntimeConfigPlugin()],
  resolve: {
    alias: {
      "@": path.resolve(rootDir, "./src"),
    },
  },
  define: {
    "import.meta.env.VITE_BUILD_SHA": JSON.stringify(
      process.env.VITE_BUILD_SHA ?? process.env.EXITS_GIT_SHA ?? "",
    ),
  },
  server: {
    // Bind all interfaces so Tailscale/LAN can open :8095; /api still proxies to loopback API.
    host: true,
    port: 8095,
    strictPort: true,
    // Android emulator Host 10.0.2.2; ADMIN_DEV_PUBLIC_HOST for Tailscale/LAN.
    allowedHosts: resolveAllowedHosts(),
    ...(publicHost
      ? {
          hmr: {
            host: publicHost,
            clientPort: 8095,
            protocol: "ws",
          },
        }
      : {}),
    proxy: {
      "/api": {
        target: resolveDevApiProxyTarget(),
        changeOrigin: true,
        secure: false,
        // Preserve cookies/credentials through the proxy for session auth.
        configure: (proxy) => {
          proxy.on("proxyRes", (proxyRes) => {
            const cookies = proxyRes.headers["set-cookie"];
            if (!cookies) {
              return;
            }
            // Local Validation is HTTP — strip Secure so cookies work on localhost and Tailscale HTTP.
            proxyRes.headers["set-cookie"] = cookies.map((cookie) =>
              cookie.replace(/;\s*Secure/gi, "").replace(/;\s*SameSite=None/gi, "; SameSite=Lax"),
            );
          });
        },
      },
    },
  },
  preview: {
    host: true,
    port: 4173,
    strictPort: true,
    allowedHosts: resolveAllowedHosts(),
  },
});
