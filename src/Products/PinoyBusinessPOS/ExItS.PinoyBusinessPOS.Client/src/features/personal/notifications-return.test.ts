import { afterEach, describe, expect, it } from "vitest";
import {
  isNotificationsReturnPath,
  peekNotificationsReturnTo,
  rememberNotificationsReturnTo,
  resolveNotificationsReturnTo,
  takeNotificationsReturnTo,
  workspaceDestinationFromReturn,
} from "@/features/personal/notifications-return";

describe("notifications-return", () => {
  afterEach(() => {
    sessionStorage.clear();
  });

  it("rejects notifications self-path and external urls", () => {
    expect(isNotificationsReturnPath("/personal/notifications")).toBe(false);
    expect(isNotificationsReturnPath("/personal/notifications?x=1")).toBe(false);
    expect(isNotificationsReturnPath("//evil.example")).toBe(false);
    expect(isNotificationsReturnPath("https://evil.example")).toBe(false);
    expect(isNotificationsReturnPath("/org/products")).toBe(true);
    expect(isNotificationsReturnPath("/personal/utang")).toBe(true);
  });

  it("remembers and resolves personal return path", () => {
    rememberNotificationsReturnTo("/personal/utang");
    expect(peekNotificationsReturnTo(null)?.returnTo).toBe("/personal/utang");
    expect(resolveNotificationsReturnTo(null)?.returnTo).toBe("/personal/utang");
    expect(takeNotificationsReturnTo()).toBeNull();
  });

  it("prefers location state and clears storage", () => {
    rememberNotificationsReturnTo("/personal/more");
    const resolved = resolveNotificationsReturnTo({ returnTo: "/personal/utang" });
    expect(resolved).toEqual({ returnTo: "/personal/utang" });
    expect(sessionStorage.getItem("exits.notifications.returnTo")).toBeNull();
  });

  it("merges workspace from storage when state only has returnTo", () => {
    rememberNotificationsReturnTo("/org/products", {
      organizationId: "org-1",
      organizationDisplayName: "Acme",
      branchId: null,
      branchName: null,
      experience: "manage_business",
    });
    const resolved = resolveNotificationsReturnTo({ returnTo: "/org/products" });
    expect(resolved?.workspace?.organizationId).toBe("org-1");
  });

  it("stores workspace snapshot for org return", () => {
    rememberNotificationsReturnTo("/org/products", {
      organizationId: "org-1",
      organizationDisplayName: "Acme",
      branchId: "br-1",
      branchName: "Main",
      experience: "manage_business",
    });
    const ctx = resolveNotificationsReturnTo(null);
    expect(ctx?.returnTo).toBe("/org/products");
    expect(ctx?.workspace?.organizationId).toBe("org-1");
    const destination = workspaceDestinationFromReturn(ctx!);
    expect(destination?.route).toBe("/org/products");
    expect(destination?.labelKey).toBe("experience.manageBusiness");
  });
});
