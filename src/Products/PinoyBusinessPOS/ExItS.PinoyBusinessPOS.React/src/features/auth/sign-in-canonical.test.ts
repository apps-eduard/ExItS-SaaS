import { describe, expect, it } from "vitest";
import { appRoutes } from "@/app/router";

describe("canonical sign-in route", () => {
  it("exposes exactly one /sign-in route element", () => {
    const sessionRoot = appRoutes[0];
    const children = "children" in sessionRoot ? sessionRoot.children : undefined;
    expect(children).toBeDefined();
    const signInRoutes = (children ?? []).filter(
      (route) => "path" in route && route.path === "/sign-in",
    );
    expect(signInRoutes).toHaveLength(1);
  });
});
