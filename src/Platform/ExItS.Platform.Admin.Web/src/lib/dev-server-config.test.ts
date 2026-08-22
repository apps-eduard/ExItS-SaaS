import { describe, expect, it } from "vitest";
import localCompose from "../../../../../deploy/docker/compose.local-validation.yaml?raw";
import productionCompose from "../../../../../deploy/docker/compose.production.yaml?raw";
import viteConfig from "../../vite.config.ts?raw";

describe("Vite and Local Validation launcher bind/config", () => {
  it("binds the Vite development server to all interfaces on 8095", () => {
    expect(viteConfig).toContain("host: true");
    expect(viteConfig).toContain("port: 8095");
    expect(viteConfig).toContain('"/api"');
    expect(viteConfig).toContain("exitsRuntimeConfigPlugin");
    expect(viteConfig).not.toMatch(/100\.120\.79\.81/);
  });

  it("keeps Local Validation React same-origin and does not bake a Tailscale IP", () => {
    expect(localCompose).toContain("PLATFORM_API_SAME_ORIGIN");
    expect(localCompose).toContain("PLATFORM_API_PROXY_TARGET");
    expect(localCompose).toContain("http://localhost:8095");
    expect(localCompose).toContain("http://127.0.0.1:8095");
    expect(localCompose).not.toMatch(/100\.120\.79\.81/);
    expect(productionCompose).not.toMatch(/100\.120\.79\.81/);
    expect(productionCompose).not.toMatch(/LOCAL_VALIDATION_TOOLS_ENABLED\s*:\s*"?true"?/);
  });
});
