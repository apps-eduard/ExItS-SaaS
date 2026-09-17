import { afterEach, describe, expect, it, vi } from "vitest";
import {
  buildReturnTo,
  canUseSafeHistoryBack,
  isSafeAppReturnPath,
  linkStateWithReturn,
  navigateWithReturn,
  resolveSmartBackTo,
  smartBackFallbacks,
  takeSmartBackTo,
} from "@/navigation/smart-back";

describe("smart-back", () => {
  afterEach(() => {
    sessionStorage.clear();
  });

  it("builds returnTo with path search and hash", () => {
    expect(buildReturnTo("/customers/business/1", "?tab=receivables&page=2", "#list")).toBe(
      "/customers/business/1?tab=receivables&page=2#list",
    );
  });

  it("rejects external and auth return paths", () => {
    expect(isSafeAppReturnPath("https://evil.example/phish")).toBe(false);
    expect(isSafeAppReturnPath("//evil.example")).toBe(false);
    expect(isSafeAppReturnPath("/login")).toBe(false);
    expect(isSafeAppReturnPath("/select-workspace")).toBe(false);
    expect(isSafeAppReturnPath("/customers/business/1?filter=open")).toBe(true);
  });

  it("rejects return loops to the current location", () => {
    expect(
      isSafeAppReturnPath("/sell/sales/abc/summary", {
        currentLocation: "/sell/sales/abc/summary",
      }),
    ).toBe(false);
  });

  it("resolves priority: state → query → storage → fallback", () => {
    const current = "/sell/sales/sale-1/summary";
    sessionStorage.setItem(
      `exits.smartBack.returnTo:${current.split("?")[0]}`,
      "/customers/business/1?filter=open",
    );

    expect(
      resolveSmartBackTo({
        locationState: { returnTo: "/suppliers/xyz", smartBack: true },
        currentPathname: current,
        searchParams: new URLSearchParams("returnTo=/purchasing/orders"),
        fallback: smartBackFallbacks.sell,
      }),
    ).toBe("/suppliers/xyz");

    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: current,
        searchParams: new URLSearchParams(
          "returnTo=" + encodeURIComponent("/purchasing/orders"),
        ),
        fallback: smartBackFallbacks.sell,
      }),
    ).toBe("/purchasing/orders");

    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: current,
        searchParams: new URLSearchParams(),
        fallback: smartBackFallbacks.sell,
      }),
    ).toBe("/customers/business/1?filter=open");

    sessionStorage.clear();
    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: current,
        searchParams: new URLSearchParams(),
        fallback: smartBackFallbacks.sell,
      }),
    ).toBe("/sell");
  });

  it("rejects external returnTo in query", () => {
    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: "/sell/sales/1/summary",
        searchParams: new URLSearchParams("returnTo=https://evil.test/x"),
        fallback: "/sell",
      }),
    ).toBe("/sell");
  });

  it("navigateWithReturn captures origin and stores for refresh", () => {
    const navigate = vi.fn();
    navigateWithReturn(navigate, "/sell/sales/1/summary", {
      pathname: "/customers/business/99",
      search: "?filter=open",
      hash: "",
    });
    expect(navigate).toHaveBeenCalledWith("/sell/sales/1/summary", {
      replace: undefined,
      state: {
        returnTo: "/customers/business/99?filter=open",
        smartBack: true,
      },
    });
    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: "/sell/sales/1/summary",
        fallback: "/sell",
      }),
    ).toBe("/customers/business/99?filter=open");
  });

  it("takeSmartBackTo clears storage", () => {
    navigateWithReturn(vi.fn(), "/purchasing/direct-purchases/abc", {
      pathname: "/suppliers/s1",
      search: "",
    });
    const taken = takeSmartBackTo({
      locationState: null,
      currentPathname: "/purchasing/direct-purchases/abc",
      fallback: "/purchasing/direct-purchases",
    });
    expect(taken).toBe("/suppliers/s1");
    expect(
      resolveSmartBackTo({
        locationState: null,
        currentPathname: "/purchasing/direct-purchases/abc",
        fallback: "/purchasing/direct-purchases",
      }),
    ).toBe("/purchasing/direct-purchases");
  });

  it("linkStateWithReturn builds smartBack state", () => {
    expect(
      linkStateWithReturn({
        pathname: "/customers/business/1",
        search: "?filter=open",
      }),
    ).toEqual({
      returnTo: "/customers/business/1?filter=open",
      smartBack: true,
    });
  });

  it("canUseSafeHistoryBack only when no explicit return and same-origin referrer", () => {
    expect(
      canUseSafeHistoryBack({
        hasExplicitReturnTo: true,
        historyLength: 3,
        referrer: "http://localhost/customers",
        currentLocation: "/sell/sales/1/summary",
      }),
    ).toBe(false);

    expect(
      canUseSafeHistoryBack({
        hasExplicitReturnTo: false,
        historyLength: 3,
        referrer: `${window.location.origin}/customers/business/1?filter=open`,
        currentLocation: "/sell/sales/1/summary",
      }),
    ).toBe(true);

    expect(
      canUseSafeHistoryBack({
        hasExplicitReturnTo: false,
        historyLength: 3,
        referrer: "https://evil.example/x",
        currentLocation: "/sell/sales/1/summary",
      }),
    ).toBe(false);
  });
});
