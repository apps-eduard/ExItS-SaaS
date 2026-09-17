import net from "node:net";
import type { Plugin } from "vite";

export const DEV_PORT_HEALTH_PATH = "/__dev__/port-health";

export type DevPortTarget = {
  port: number;
  name: string;
};

/** Local Validation stack ports — names only; UI never shows host/IP. */
export const DEV_PORT_TARGETS: readonly DevPortTarget[] = [
  { port: 8090, name: "Platform Admin" },
  { port: 8091, name: "Platform API" },
  { port: 8092, name: "POS API" },
  { port: 8093, name: "Organization Web" },
  { port: 8094, name: "Personal Web" },
  { port: 8095, name: "React Admin" },
  { port: 5177, name: "React POS" },
  { port: 8025, name: "Mailpit" },
  { port: 15533, name: "Platform DB" },
  { port: 15534, name: "POS DB" },
] as const;

export type DevPortHealthRow = DevPortTarget & {
  up: boolean;
};

export type DevPortHealthResponse = {
  checkedAtUtc: string;
  ports: DevPortHealthRow[];
};

export function probeTcpPort(
  port: number,
  host = "127.0.0.1",
  timeoutMs = 600,
): Promise<boolean> {
  return new Promise((resolve) => {
    const socket = net.connect({ host, port }, () => {
      socket.destroy();
      resolve(true);
    });
    socket.on("error", () => {
      socket.destroy();
      resolve(false);
    });
    socket.setTimeout(timeoutMs, () => {
      socket.destroy();
      resolve(false);
    });
  });
}

export async function collectDevPortHealth(
  targets: readonly DevPortTarget[] = DEV_PORT_TARGETS,
): Promise<DevPortHealthResponse> {
  const ports = await Promise.all(
    targets.map(async (target) => ({
      ...target,
      up: await probeTcpPort(target.port),
    })),
  );
  return {
    checkedAtUtc: new Date().toISOString(),
    ports,
  };
}

/**
 * Dev-only middleware: GET /__dev__/port-health → JSON of loopback TCP probes.
 * apply: "serve" so it never ships in production builds.
 */
export function createDevPortHealthPlugin(): Plugin {
  return {
    name: "exits-dev-port-health",
    apply: "serve",
    configureServer(server) {
      server.middlewares.use(async (request, response, next) => {
        const pathname = (request.url ?? "").split("?")[0] ?? "";
        if (pathname !== DEV_PORT_HEALTH_PATH) {
          next();
          return;
        }

        if (request.method !== "GET" && request.method !== "HEAD") {
          response.statusCode = 405;
          response.setHeader("Allow", "GET, HEAD");
          response.end();
          return;
        }

        try {
          const payload = await collectDevPortHealth();
          response.statusCode = 200;
          response.setHeader("Content-Type", "application/json; charset=utf-8");
          response.setHeader("Cache-Control", "no-store");
          if (request.method === "HEAD") {
            response.end();
            return;
          }
          response.end(JSON.stringify(payload));
        } catch (error) {
          response.statusCode = 500;
          response.setHeader("Content-Type", "application/json; charset=utf-8");
          response.end(
            JSON.stringify({
              error: error instanceof Error ? error.message : "port health failed",
            }),
          );
        }
      });
    },
  };
}
