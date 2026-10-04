import { describe, expect, it } from "vitest";
import type { ReactElement } from "react";
import { Navigate, type RouteObject } from "react-router-dom";
import { appRoutes } from "@/app/router";
import { OrgSalesDocumentsPage } from "@/features/organization/sales-documents/OrgSalesDocumentsPage";

function flatten(routes: RouteObject[]): RouteObject[] {
  return routes.flatMap((route) => [route, ...(route.children ? flatten(route.children) : [])]);
}

describe("sales-document education routes", () => {
  const routes = flatten(appRoutes);

  it("renders the React page at /org/sales-documents", () => {
    const route = routes.find((item) => item.path === "sales-documents");
    const element = route?.element as ReactElement | undefined;
    expect(element?.type).toBe(OrgSalesDocumentsPage);
  });

  it("redirects the legacy organization URL to the React route", () => {
    const route = routes.find((item) => item.path === "organization/sales-documents");
    const element = route?.element as ReactElement<{ to: string; replace?: boolean }> | undefined;
    expect(element?.type).toBe(Navigate);
    expect(element?.props.to).toBe("/org/sales-documents");
    expect(element?.props.replace).toBe(true);
  });
});
