import { beforeEach, describe, expect, it, vi } from "vitest";
import { listRegisters, listRegistersAvailableForShift } from "@/api/pos/pos-registers-client";
import {
  closeCashierShift,
  getCurrentCashierShift,
  openCashierShift,
} from "@/api/pos/pos-shifts-client";
import {
  getOperationalSetup,
  resolveOpeningCashRequired,
  resolveOpeningCashVisible,
} from "@/api/pos/pos-operational-setup-client";
import { PosApiError } from "@/api/pos/pos-http";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const registerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const shiftId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

function openShiftJson(extra: Record<string, unknown> = {}) {
  return {
    shiftId,
    organizationId: workspace.organizationId,
    shiftNumber: "S-1001",
    status: "Open",
    actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    registerId,
    registerCode: "REG-1",
    registerName: "Front",
    businessDate: "2026-08-21",
    openingCashAmount: 500,
    openingCashCounted: true,
    effectiveCashCountMode: "Required",
    openedAtUtc: "2026-08-21T01:00:00Z",
    openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    createdAtUtc: "2026-08-21T01:00:00Z",
    updatedAtUtc: "2026-08-21T01:00:00Z",
    ...extra,
  };
}

describe("pos-registers-client / pos-shifts-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("lists available registers with branch-scoped headers", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify([{ registerId, registerCode: "REG-1", name: "Front", status: "Active" }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const items = await listRegistersAvailableForShift(workspace);
    expect(items).toHaveLength(1);
    expect(items[0].registerCode).toBe("REG-1");

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const headers = new Headers(init?.headers);
    expect(headers.get("X-Pos-Organization-Id")).toBe(workspace.organizationId);
    expect(headers.get("X-Pos-Branch-Id")).toBe(workspace.branchId);
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("/registers/available-for-shift");
  });

  it("lists registers with filters", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    await listRegisters(workspace, { status: "Active", hasOpenShift: false, page: 2 });
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("status=Active");
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("hasOpenShift=false");
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("page=2");
  });

  it("loads operational setup for opening cash policy", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          organizationId: workspace.organizationId,
          isComplete: true,
          currencyCode: "PHP",
          cashCountMode: "Optional",
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const setup = await getOperationalSetup(workspace);
    expect(setup.cashCountMode).toBe("Optional");
    expect(resolveOpeningCashRequired("Optional")).toBe(false);
    expect(resolveOpeningCashVisible("Optional")).toBe(true);
    expect(resolveOpeningCashVisible("Off")).toBe(false);
    expect(resolveOpeningCashRequired("Required")).toBe(true);
    expect(resolveOpeningCashRequired("")).toBe(false);
  });

  it("treats current shift 404 as null", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ detail: "not found" }), {
        status: 404,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(getCurrentCashierShift(workspace)).resolves.toBeNull();
  });

  it("opens and closes a shift against real paths", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(JSON.stringify(openShiftJson()), {
          status: 201,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(openShiftJson({ status: "Closed", closingCashAmount: 600 })), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

    const opened = await openCashierShift(workspace, {
      registerId,
      openingCashAmount: 500,
    });
    expect(opened.shiftId).toBe(shiftId);
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("/cashier-shifts");
    expect(JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body))).toMatchObject({
      registerId,
      openingCashAmount: 500,
    });

    const closed = await closeCashierShift(workspace, shiftId, { closingCashAmount: 600 });
    expect(closed.status).toBe("Closed");
    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain(`/cashier-shifts/${shiftId}/close`);
  });

  it("surfaces denied open as PosApiError", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ detail: "Forbidden", errorCode: "capability.denied" }), {
        status: 403,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(
      openCashierShift(workspace, { registerId, openingCashAmount: 0 }),
    ).rejects.toBeInstanceOf(PosApiError);
  });
});
