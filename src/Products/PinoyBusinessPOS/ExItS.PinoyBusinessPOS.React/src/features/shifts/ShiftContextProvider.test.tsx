import { describe, expect, it, vi, beforeEach } from "vitest";
import { act, render, screen, waitFor } from "@testing-library/react";
import { ShiftContextProvider, useShiftContext } from "@/features/shifts/ShiftContextProvider";

const getCurrentCashierShift = vi.fn();

vi.mock("@/api/pos/pos-shifts-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-shifts-client")>();
  return {
    ...actual,
    getCurrentCashierShift: (...args: unknown[]) => getCurrentCashierShift(...args),
  };
});

let workspaceMock: {
  boundWorkspace: {
    organizationId: string;
    branchId: string | null;
  } | null;
  sessionGrant: { productLocalRoleCode: string } | null;
  posDevice: null;
  deviceEnforcementEnabled: boolean;
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/access/pos-capabilities", () => ({
  canViewShifts: () => true,
}));

function Probe() {
  const { loading, currentShift, errorMessage } = useShiftContext();
  return (
    <div>
      <span data-testid="shift-loading">{String(loading)}</span>
      <span data-testid="shift-id">{currentShift?.shiftId ?? "none"}</span>
      <span data-testid="shift-error">{errorMessage ?? "none"}</span>
    </div>
  );
}

describe("ShiftContextProvider", () => {
  beforeEach(() => {
    getCurrentCashierShift.mockReset();
    workspaceMock = {
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        branchId: null,
      },
      sessionGrant: { productLocalRoleCode: "Owner" },
      posDevice: null,
      deviceEnforcementEnabled: false,
    };
  });

  it("does not stay loading when Manage Business has no branch", async () => {
    render(
      <ShiftContextProvider>
        <Probe />
      </ShiftContextProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("shift-loading")).toHaveTextContent("false");
    });
    expect(getCurrentCashierShift).not.toHaveBeenCalled();
    expect(screen.getByTestId("shift-id")).toHaveTextContent("none");
  });

  it("ignores stale refresh results when branch identity changes mid-flight", async () => {
    let resolveFirst: (value: unknown) => void = () => undefined;
    getCurrentCashierShift.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveFirst = resolve;
        }),
    );
    getCurrentCashierShift.mockResolvedValueOnce({
      shiftId: "shift-2",
      status: "Open",
      shiftNumber: "S-2",
      registerId: "reg-1",
    });

    workspaceMock.boundWorkspace = {
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    };

    const { rerender } = render(
      <ShiftContextProvider>
        <Probe />
      </ShiftContextProvider>,
    );

    await waitFor(() => {
      expect(getCurrentCashierShift).toHaveBeenCalledTimes(1);
    });

    workspaceMock.boundWorkspace = {
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    };
    rerender(
      <ShiftContextProvider>
        <Probe />
      </ShiftContextProvider>,
    );

    await waitFor(() => {
      expect(getCurrentCashierShift).toHaveBeenCalledTimes(2);
    });

    await act(async () => {
      resolveFirst({
        shiftId: "shift-stale",
        status: "Open",
        shiftNumber: "S-stale",
        registerId: "reg-stale",
      });
    });

    await waitFor(() => {
      expect(screen.getByTestId("shift-id")).toHaveTextContent("shift-2");
      expect(screen.getByTestId("shift-loading")).toHaveTextContent("false");
    });
  });
});
