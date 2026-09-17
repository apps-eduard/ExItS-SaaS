import { createServer } from "node:net";
import { afterAll, describe, expect, it } from "vitest";
import {
  DEV_PORT_TARGETS,
  collectDevPortHealth,
  probeTcpPort,
} from "./vite.dev-port-health";

describe("vite.dev-port-health", () => {
  const servers: ReturnType<typeof createServer>[] = [];

  afterAll(async () => {
    await Promise.all(
      servers.map(
        (server) =>
          new Promise<void>((resolve) => {
            server.close(() => resolve());
          }),
      ),
    );
  });

  it("lists Local Validation ports with names and no host fields", () => {
    expect(DEV_PORT_TARGETS.length).toBeGreaterThanOrEqual(8);
    expect(DEV_PORT_TARGETS.map((t) => t.port)).toEqual(
      expect.arrayContaining([8091, 8092, 5177, 8095]),
    );
    for (const target of DEV_PORT_TARGETS) {
      expect(target.name.trim().length).toBeGreaterThan(0);
      expect(JSON.stringify(target)).not.toMatch(/127\.0\.0\.1|localhost|http/i);
    }
  });

  it("probes an open loopback port as up and a closed port as down", async () => {
    const server = createServer();
    servers.push(server);
    await new Promise<void>((resolve, reject) => {
      server.once("error", reject);
      server.listen(0, "127.0.0.1", () => resolve());
    });
    const address = server.address();
    expect(address && typeof address === "object").toBe(true);
    const openPort = (address as { port: number }).port;

    expect(await probeTcpPort(openPort)).toBe(true);
    expect(await probeTcpPort(1)).toBe(false);
  });

  it("collectDevPortHealth returns up flags for provided targets", async () => {
    const server = createServer();
    servers.push(server);
    await new Promise<void>((resolve, reject) => {
      server.once("error", reject);
      server.listen(0, "127.0.0.1", () => resolve());
    });
    const openPort = (server.address() as { port: number }).port;

    const result = await collectDevPortHealth([
      { port: openPort, name: "Open probe" },
      { port: 1, name: "Closed probe" },
    ]);

    expect(result.ports).toEqual([
      { port: openPort, name: "Open probe", up: true },
      { port: 1, name: "Closed probe", up: false },
    ]);
    expect(result.checkedAtUtc).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  });
});
