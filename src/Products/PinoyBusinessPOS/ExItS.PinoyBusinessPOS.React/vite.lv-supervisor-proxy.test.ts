import { describe, expect, it } from "vitest";
import {
  createLvSupervisorProxy,
  LV_SUPERVISOR_PROXY_PREFIX,
  rewriteLvSupervisorProxyPath,
  resolveLvSupervisorProxyTarget,
} from "./vite.lv-supervisor-proxy";

describe("vite.lv-supervisor-proxy", () => {
  it("rewrites proxy prefix to supervisor paths", () => {
    expect(rewriteLvSupervisorProxyPath(`${LV_SUPERVISOR_PROXY_PREFIX}/health/services`)).toBe(
      "/health/services",
    );
    expect(rewriteLvSupervisorProxyPath(`${LV_SUPERVISOR_PROXY_PREFIX}/services/restart-all`)).toBe(
      "/services/restart-all",
    );
  });

  it("defaults to loopback :8099", () => {
    expect(resolveLvSupervisorProxyTarget(undefined)).toBe("http://127.0.0.1:8099");
  });

  it("creates a proxy entry for the prefix", () => {
    const proxy = createLvSupervisorProxy();
    expect(proxy[LV_SUPERVISOR_PROXY_PREFIX]).toBeTruthy();
    expect(proxy[LV_SUPERVISOR_PROXY_PREFIX].target).toBe("http://127.0.0.1:8099");
  });
});
