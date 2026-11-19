import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import {
  PersonalContactStackedSheet,
  personalContactCanViewActivity,
  personalContactShowPreviousClosed,
  personalContactShowSettle,
  personalContactShowUpdateBalance,
} from "@/features/personal/PersonalContactStackedSheet";
import type { PeopleRowModel } from "@/features/personal/people-status";
import type { PersonalContactDto } from "@/api/platform/personal-types";

const stackedSheetsCss = readFileSync(
  resolve(__dirname, "../../styles/personal-contact-stacked-sheets.css"),
  "utf8",
);

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: { userId: "me", displayName: "Me" },
    signOut: async () => ({ ok: true as const, nextRoute: "/sign-in" }),
  }),
}));

vi.mock("@/lib/motion", async () => {
  const actual = await vi.importActual<typeof import("@/lib/motion")>("@/lib/motion");
  return {
    ...actual,
    usePrefersReducedMotion: () => true,
    prefersReducedMotion: () => true,
    motionDurationMs: () => 0,
  };
});

const localContact: PersonalContactDto = {
  id: "c-local",
  displayName: "Peter Paul",
  resolvedUserIdentityId: null,
  resolvedPublicUserId: null,
  linkedUserIdentityId: null,
  status: "Active",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
  phone: "09171234567",
  email: "peter@example.com",
};

function localRow(overrides?: Partial<PeopleRowModel>): PeopleRowModel {
  return {
    contact: localContact,
    connectionStatus: "local",
    identityLine: "local",
    previousClosed: [],
    lentTotal: 0,
    lentCount: 0,
    oweTotal: 0,
    oweCount: 0,
    ...overrides,
  };
}

function renderSheet(row: PeopleRowModel, onOpenChange = vi.fn()) {
  return render(
    <AppProviders>
      <PersonalContactStackedSheet
        open
        row={row}
        statusLabel="Local contact"
        onOpenChange={onOpenChange}
      />
    </AppProviders>,
  );
}

describe("personalContact eligibility helpers", () => {
  it("shows settle only for ledger-owner outstanding balance", () => {
    expect(personalContactShowSettle(localRow())).toBe(false);
    expect(
      personalContactShowSettle(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 100,
            dueDateUtc: null,
            isLedgerOwner: true,
            isSharedLedger: false,
          },
        }),
      ),
    ).toBe(true);
    expect(
      personalContactShowSettle(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 100,
            dueDateUtc: null,
            isLedgerOwner: false,
            isSharedLedger: true,
          },
        }),
      ),
    ).toBe(false);
    expect(
      personalContactShowSettle(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 0,
            dueDateUtc: null,
            isLedgerOwner: true,
            isSharedLedger: false,
          },
        }),
      ),
    ).toBe(false);
  });

  it("shows update balance for ledger-owner active relationship", () => {
    expect(personalContactShowUpdateBalance(localRow())).toBe(false);
    expect(
      personalContactShowUpdateBalance(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 0,
            dueDateUtc: null,
            isLedgerOwner: true,
            isSharedLedger: false,
          },
        }),
      ),
    ).toBe(true);
    expect(
      personalContactShowUpdateBalance(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 50,
            dueDateUtc: null,
            isLedgerOwner: false,
            isSharedLedger: true,
          },
        }),
      ),
    ).toBe(false);
  });

  it("gates activity and previous closed", () => {
    expect(personalContactCanViewActivity(localRow())).toBe(false);
    expect(
      personalContactCanViewActivity(
        localRow({
          utang: {
            relationshipId: "rel-1",
            direction: "lent",
            balance: 10,
            dueDateUtc: null,
            isLedgerOwner: true,
            isSharedLedger: false,
          },
        }),
      ),
    ).toBe(true);
    expect(personalContactShowPreviousClosed(localRow())).toBe(false);
    expect(
      personalContactShowPreviousClosed(
        localRow({
          previousClosed: [
            {
              relationshipId: "rel-closed",
              direction: "lent",
              updatedAtUtc: "2026-01-01T00:00:00.000Z",
              isLedgerOwner: true,
              isSharedLedger: false,
            },
          ],
        }),
      ),
    ).toBe(true);
  });
});

describe("PersonalContactStackedSheet", () => {
  beforeEach(() => {
    document.documentElement.dataset.motion = "reduced";
  });

  afterEach(() => {
    delete document.documentElement.dataset.motion;
  });

  it("opens with stationary identity and 4-column quick action cards", async () => {
    renderSheet(localRow());

    expect(await screen.findByTestId("personal-contact-sheet-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-name-c-local")).toHaveTextContent(
      "Peter Paul",
    );
    expect(screen.getByTestId("personal-contact-sheet-status-c-local-kind")).toHaveTextContent(
      "Local Contact",
    );
    expect(screen.queryByTestId("personal-contact-sheet-status-c-local-direction")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-edit-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-lent-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-owe-c-local")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-contact-sheet-qa-settle-c-local")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-details-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-activity-c-local")).toBeDisabled();
    expect(screen.getByTestId("personal-contact-sheet-qa-previous-c-local")).toBeDisabled();
    expect(screen.queryByTestId("personal-contact-sheet-qa-balance-c-local")).not.toBeInTheDocument();
  });

  it("shows settle and update balance when eligible and swaps to edit", async () => {
    const user = userEvent.setup();
    renderSheet(
      localRow({
        utang: {
          relationshipId: "rel-1",
          direction: "lent",
          balance: 250,
          dueDateUtc: null,
          isLedgerOwner: true,
          isSharedLedger: false,
        },
      }),
    );

    expect(await screen.findByTestId("personal-contact-sheet-identity-summary-c-local-direction")).toHaveTextContent(
      "I Lent",
    );
    expect(screen.getByTestId("personal-contact-sheet-identity-summary-c-local-kind")).toHaveTextContent(
      "Local Contact",
    );
    expect(screen.getByTestId("personal-contact-sheet-identity-summary-c-local-amount")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-contact-sheet-status-c-local")).not.toBeInTheDocument();

    expect(screen.getByTestId("personal-contact-sheet-qa-settle-c-local")).toBeInTheDocument();
    const balanceQa = screen.getByTestId("personal-contact-sheet-qa-balance-c-local");
    expect(within(balanceQa).getByText("Update")).toBeInTheDocument();
    expect(balanceQa).toHaveAccessibleName("Update balance");
    expect(screen.getByTestId("personal-contact-sheet-qa-activity-c-local")).toBeEnabled();
    expect(within(screen.getByTestId("personal-contact-sheet-qa-previous-c-local")).getByText("Close")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-previous-c-local")).toHaveAccessibleName(
      "Previous closed",
    );

    await user.click(screen.getByTestId("personal-contact-sheet-qa-edit-c-local"));

    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-local")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-contact-sheet-edit-name-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-name-c-local")).toHaveTextContent(
      "Peter Paul",
    );
    expect(screen.queryByTestId("personal-contact-sheet-actions-c-local")).not.toBeInTheDocument();
  });

  it("opens view details, activity, previous closed, and update balance panels", async () => {
    const user = userEvent.setup();
    renderSheet(
      localRow({
        utang: {
          relationshipId: "rel-1",
          direction: "lent",
          balance: 80,
          dueDateUtc: null,
          isLedgerOwner: true,
          isSharedLedger: false,
        },
        previousClosed: [
          {
            relationshipId: "rel-closed",
            direction: "owe",
            updatedAtUtc: "2026-02-01T00:00:00.000Z",
            isLedgerOwner: true,
            isSharedLedger: false,
          },
        ],
      }),
    );

    await user.click(await screen.findByTestId("personal-contact-sheet-qa-details-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-details-c-local")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("personal-contact-sheet-details-back-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-actions-c-local")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("personal-contact-sheet-qa-activity-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-activity-c-local")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-contact-sheet-activity-c-local")).toHaveTextContent("Peter Paul");
    await user.click(screen.getByTestId("personal-contact-sheet-activity-back-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-actions-c-local")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("personal-contact-sheet-qa-previous-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-previous-c-local")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-contact-sheet-previous-item-rel-closed")).toBeInTheDocument();
    await user.click(screen.getByTestId("personal-contact-sheet-previous-back-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-actions-c-local")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("personal-contact-sheet-qa-balance-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-balance-c-local")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-contact-sheet-balance-projected-c-local")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-name-c-local")).toHaveTextContent(
      "Peter Paul",
    );
  });

  it("locks identity profile CSS to centered vertical stack", () => {
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__profile\s*\{[^}]*flex-direction:\s*column/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__kind-badge\s*\{[^}]*border-radius:\s*999px/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__profile-balance\s*\{[^}]*font-size:\s*1\.35rem/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__overdue-badge\s*\{[^}]*var\(--exits-warning/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__relation--lent\s*\{[^}]*var\(--exits-severity-success/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__relation--owe\s*\{[^}]*var\(--exits-severity-info/s,
    );
    // List-row one-row summary remains for People cards
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__balance-summary-top\s*\{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\)\s+auto\s+minmax\(0,\s*1fr\)/s,
    );
  });

  it("cancel on edit returns to quick actions without closing", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    renderSheet(localRow(), onOpenChange);

    await user.click(await screen.findByTestId("personal-contact-sheet-qa-edit-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-local")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("personal-contact-sheet-edit-cancel-c-local"));

    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-actions-c-local")).toBeInTheDocument();
    });
    expect(onOpenChange).not.toHaveBeenCalled();
  });

  it("force-light chrome stays on the sheet root", async () => {
    renderSheet(localRow());
    const root = await screen.findByTestId("personal-contact-sheet-c-local");
    expect(root).toHaveAttribute("data-force-light", "true");
    expect(root).toHaveAttribute("data-theme", "light");
  });

  it("keeps upper identity stationary while lower body is the scroll region", async () => {
    const user = userEvent.setup();
    renderSheet(localRow());

    const root = await screen.findByTestId("personal-contact-sheet-c-local");
    expect(screen.getByTestId("personal-contact-sheet-c-local-upper")).toBeInTheDocument();
    const lowerScroll = screen.getByTestId("personal-contact-sheet-c-local-lower-scroll");
    expect(lowerScroll).toBeInTheDocument();
    expect(lowerScroll).toHaveClass("exits-stacked-sheets__lower-scroll");
    // Content-hugging scrollport: overflow-y auto is OK; height must not be forced to 100%.
    expect(lowerScroll).not.toHaveStyle({ height: "100%" });
    expect(root).toHaveClass("exits-stacked-sheets");

    await user.click(screen.getByTestId("personal-contact-sheet-qa-edit-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-local")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-contact-sheet-name-c-local")).toHaveTextContent(
      "Peter Paul",
    );
    expect(screen.getByTestId("personal-contact-sheet-c-local-upper")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-c-local-lower-scroll")).toContainElement(
      screen.getByTestId("personal-contact-sheet-edit-c-local"),
    );
  });

  it("caps sheet max-height at viewport minus Personal topbar — no artificial rem ceiling", () => {
    // Formula: 100dvh − (--exits-shell-chrome-height + safe-area-inset-top)
    expect(stackedSheetsCss).toMatch(
      /--exits-stacked-sheets-top-offset:\s*calc\(\s*var\(--exits-shell-chrome-height,\s*4rem\)\s*\+\s*env\(safe-area-inset-top,\s*0px\)\s*\)/,
    );
    expect(stackedSheetsCss).toMatch(
      /--exits-stacked-sheets-max-height:\s*calc\(100dvh\s*-\s*var\(--exits-stacked-sheets-top-offset\)\)/,
    );
    expect(stackedSheetsCss).toMatch(
      /\.exits-stacked-sheets__dialog\s*\{[^}]*max-height:\s*var\(--exits-stacked-sheets-max-height\)/s,
    );
    // Artificial rem caps left a large gap under the topbar while lower body scrolled early.
    expect(stackedSheetsCss).not.toMatch(/max-height:\s*min\(\s*44rem/);
    expect(stackedSheetsCss).not.toMatch(/max-height:\s*min\(\s*40rem/);
    expect(stackedSheetsCss).toMatch(
      /\.exits-stacked-sheets__lower-scroll\s*\{[^}]*overflow-y:\s*auto/s,
    );
    expect(stackedSheetsCss).toMatch(
      /\.personal-contact-sheet__list\s*\{[^}]*overflow:\s*visible/s,
    );
  });

  it("escape from actions closes; escape from edit returns to actions", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    renderSheet(localRow(), onOpenChange);

    await screen.findByTestId("personal-contact-sheet-c-local");
    await user.click(screen.getByTestId("personal-contact-sheet-qa-edit-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-local")).toBeInTheDocument();
    });

    await user.keyboard("{Escape}");
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-actions-c-local")).toBeInTheDocument();
    });
    expect(onOpenChange).not.toHaveBeenCalled();

    await user.keyboard("{Escape}");
    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });

  it("close control dismisses even from edit panel", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    renderSheet(localRow(), onOpenChange);

    await user.click(await screen.findByTestId("personal-contact-sheet-qa-edit-c-local"));
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-local")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("personal-contact-sheet-c-local-close"));
    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });
});

describe("PersonalContactStackedSheet shared contact", () => {
  beforeEach(() => {
    document.documentElement.dataset.motion = "reduced";
  });

  afterEach(() => {
    delete document.documentElement.dataset.motion;
  });

  it("labels connected contacts as Shared Contact and keeps edit view-only", async () => {
    const user = userEvent.setup();
    const connected = localRow({
      connectionStatus: "connected",
      identityLine: "exits",
      publicUserId: "EX-1111-2222",
      contact: {
        ...localContact,
        id: "c-shared",
        displayName: "Ana Cruz",
        linkedUserIdentityId: "u-linked",
        resolvedPublicUserId: "EX-1111-2222",
      },
    });

    render(
      <AppProviders>
        <PersonalContactStackedSheet
          open
          row={connected}
          statusLabel="Connected"
          onOpenChange={vi.fn()}
        />
      </AppProviders>,
    );

    expect(await screen.findByTestId("personal-contact-sheet-status-c-shared-kind")).toHaveTextContent(
      "Shared Contact",
    );
    const editQa = screen.getByTestId("personal-contact-sheet-qa-edit-c-shared");
    expect(within(editQa).getByText("Edit")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("personal-contact-sheet-qa-details-c-shared")).getByText("Details"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-qa-details-c-shared")).toHaveAccessibleName(
      "View details",
    );

    await user.click(editQa);
    await waitFor(() => {
      expect(screen.getByTestId("personal-contact-sheet-edit-c-shared")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("personal-contact-sheet-edit-save-c-shared")).not.toBeInTheDocument();
  });

  it("labels connected contacts as Shared Contact on the identity balance summary", async () => {
    const connected = localRow({
      connectionStatus: "connected",
      identityLine: "exits",
      publicUserId: "EX-1111-2222",
      contact: {
        ...localContact,
        id: "c-shared-bal",
        displayName: "Ana Cruz",
        linkedUserIdentityId: "u-linked",
        resolvedPublicUserId: "EX-1111-2222",
      },
      utang: {
        relationshipId: "rel-shared",
        direction: "owe",
        balance: 40,
        dueDateUtc: "2020-01-15T00:00:00.000Z",
        isLedgerOwner: true,
        isSharedLedger: true,
      },
    });

    render(
      <AppProviders>
        <PersonalContactStackedSheet
          open
          row={connected}
          statusLabel="Connected"
          onOpenChange={vi.fn()}
        />
      </AppProviders>,
    );

    expect(
      await screen.findByTestId("personal-contact-sheet-identity-summary-c-shared-bal-direction"),
    ).toHaveTextContent("I Owe");
    expect(screen.getByTestId("personal-contact-sheet-identity-summary-c-shared-bal-kind")).toHaveTextContent(
      "Shared Contact",
    );
    expect(screen.getByTestId("personal-contact-sheet-identity-summary-c-shared-bal-amount")).toBeInTheDocument();
    expect(screen.getByTestId("personal-contact-sheet-identity-summary-c-shared-bal-due")).toHaveTextContent(
      /Overdue:/,
    );
  });
});
