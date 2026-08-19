import { describe, expect, it } from "vitest";
import productionCompose from "../../../../../deploy/docker/compose.production.yaml?raw";
import runtimeConfigScript from "../../../../../deploy/docker/platform-admin-web/40-exits-runtime-config.sh?raw";
import publicConfig from "../../public/config.js?raw";

describe("runtime /config.js contract", () => {
  it("writes only non-secret runtime keys and defaults tools to false", () => {
    expect(runtimeConfigScript).toContain("localValidationToolsEnabled");
    expect(runtimeConfigScript).toContain("LOCAL_VALIDATION_TOOLS_ENABLED");
    expect(runtimeConfigScript).toContain("tools_enabled=false");
    expect(runtimeConfigScript).not.toMatch(/PASSWORD|SECRET|TOKEN|SHARED/i);
  });

  it("keeps the Vite public config.js fail-closed and secret-free", () => {
    expect(publicConfig).not.toMatch(/PASSWORD|SECRET|TOKEN/i);
    expect(publicConfig).not.toMatch(/localValidationToolsEnabled\s*:\s*true/);
  });

  it("does not enable Test User tools in production compose", () => {
    expect(productionCompose).not.toMatch(/LOCAL_VALIDATION_TOOLS_ENABLED\s*:\s*"?true"?/);
  });
});
