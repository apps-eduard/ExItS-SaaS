import { describe, expect, it } from "vitest";
import {
  POS_API_PROXY_PREFIX,
  rewritePosApiProxyPath,
  resolvePosApiProxyTarget,
} from "./vite.pos-api-proxy";

describe("pos api proxy", () => {
  it("defaults to loopback 8092", () => {
    expect(resolvePosApiProxyTarget()).toBe("http://127.0.0.1:8092");
  });

  it("strips the /pos-api prefix", () => {
    expect(rewritePosApiProxyPath(`${POS_API_PROXY_PREFIX}/api/v1/pos/catalog/products`)).toBe(
      "/api/v1/pos/catalog/products",
    );
    expect(rewritePosApiProxyPath(POS_API_PROXY_PREFIX)).toBe("/");
  });
});
