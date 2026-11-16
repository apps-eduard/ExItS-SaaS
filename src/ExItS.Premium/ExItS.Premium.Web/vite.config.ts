import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const platformTarget = "http://127.0.0.1:8091";

export default defineConfig({
  plugins: [react()],
  server: {
    host: "127.0.0.1",
    port: 5180,
    strictPort: true,
    proxy: {
      "/platform-api": {
        target: platformTarget,
        changeOrigin: true,
        secure: false,
        cookieDomainRewrite: "",
        rewrite: (path) => path.replace(/^\/platform-api/, "") || "/",
        configure: (proxy) => {
          const events = proxy as unknown as {
            on: (event: string, listener: (response: { headers: Record<string, string | string[] | undefined> }) => void) => void;
          };
          events.on("proxyRes", (proxyRes) => {
            const setCookie = proxyRes.headers["set-cookie"];
            if (!setCookie) return;
            const values = Array.isArray(setCookie) ? setCookie : [setCookie];
            proxyRes.headers["set-cookie"] = values.map((value) => value.replace(/;\s*Secure/gi, ""));
          });
        },
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    css: true,
  },
});
