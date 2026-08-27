import { describe, expect, it } from "vitest";
import type { RouteObject } from "react-router-dom";
import { appRoutes } from "@/app/router";
import {
  PERSONAL_GUIDE_CATEGORIES,
  PERSONAL_GUIDE_FEATURES,
  PERSONAL_GUIDE_FEATURE_CODES,
} from "@/features/personal/guide/personal-guide-features";
import { catalogs } from "@/i18n/messages";

function collectRoutePaths(routes: RouteObject[], parent = ""): string[] {
  const paths: string[] = [];
  for (const route of routes) {
    const raw = route.path ?? "";
    const joined = raw.startsWith("/")
      ? raw
      : `${parent.replace(/\/$/, "")}/${raw}`.replace(/\/+/g, "/");
    const path = joined === "/" ? "/" : joined.replace(/\/$/, "") || "/";
    if (route.path !== undefined) {
      paths.push(path === "" ? "/" : path);
    }
    if (route.children) {
      paths.push(...collectRoutePaths(route.children, path === "/" ? "" : path));
    }
  }
  return paths;
}

function routeExists(defined: string[], candidate: string): boolean {
  if (defined.includes(candidate)) {
    return true;
  }
  return defined.some((path) => {
    if (!path.includes(":")) {
      return false;
    }
    const pattern = new RegExp(
      `^${path.replace(/[.*+?^${}()|[\]\\]/g, "\\$&").replace(/\\:[^/]+/g, "[^/]+")}$`,
    );
    return pattern.test(candidate);
  });
}

describe("personal guide feature definitions", () => {
  const definedPaths = collectRoutePaths(appRoutes);

  it("uses unique feature codes", () => {
    expect(PERSONAL_GUIDE_FEATURE_CODES.size).toBe(PERSONAL_GUIDE_FEATURES.length);
  });

  it("uses valid categories and non-empty copy keys", () => {
    for (const feature of PERSONAL_GUIDE_FEATURES) {
      expect(PERSONAL_GUIDE_CATEGORIES).toContain(feature.category);
      expect(feature.code.trim().length).toBeGreaterThan(0);
      expect(feature.titleKey.length).toBeGreaterThan(0);
      expect(feature.descriptionKey.length).toBeGreaterThan(0);
      expect(feature.bulletKeys.length).toBeGreaterThan(0);
      expect(catalogs.en[feature.titleKey].trim().length).toBeGreaterThan(0);
      expect(catalogs.en[feature.descriptionKey].trim().length).toBeGreaterThan(0);
      for (const bullet of feature.bulletKeys) {
        expect(catalogs.en[bullet].trim().length).toBeGreaterThan(0);
      }
    }
  });

  it("maps Try It routes to existing defined routes", () => {
    for (const feature of PERSONAL_GUIDE_FEATURES) {
      expect(feature.route.startsWith("/"), `${feature.code} ${feature.route}`).toBe(true);
      expect(routeExists(definedPaths, feature.route), `${feature.code} -> ${feature.route}`).toBe(
        true,
      );
    }
  });

  it("does not invent unfinished financial-product guides", () => {
    const codes = [...PERSONAL_GUIDE_FEATURE_CODES].join(" ");
    expect(codes).not.toMatch(/bnpl|pawn|loan|plm|ppm/i);
  });
});
