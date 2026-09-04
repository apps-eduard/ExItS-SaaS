import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { CashHandlingSettingsPage } from "@/features/settings/CashHandlingSettingsPage";
import { formatDenominationCurrency } from "@/lib/format-money";
import {
  getOperationalSetup,
  listCashDenominations,
  replaceCashDenominations,
  updateOperationalSetup,
} from "@/api/pos/pos-operational-setup-client";

vi.mock("@/access/pos-capabilities", () => ({
  hasOrganizationManagementAuthority: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "22222222-2222-2222-2222-222222222222",
    },
    sessionGrant: { organizationManagementAuthority: true },
  }),
}));

vi.mock("@/api/pos/pos-operational-setup-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-operational-setup-client")>(
    "@/api/pos/pos-operational-setup-client",
  );
  return {
    ...actual,
    getOperationalSetup: vi.fn(),
    listCashDenominations: vi.fn(),
    replaceCashDenominations: vi.fn(),
    updateOperationalSetup: vi.fn(),
  };
});

const pageSource = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "CashHandlingSettingsPage.tsx"),
  "utf8",
);
const globalsCss = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../../styles/globals.css"),
  "utf8",
);
const switchSource = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../../components/ui/switch.tsx"),
  "utf8",
);

const setupDto = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  storeDisplayName: "Demo Store",
  currencyCode: "PHP",
  taxPricingMode: "Inclusive",
  taxRatePercent: 12,
  cashCountMode: "Required",
  openingCashCountMode: "Required",
  closingCashCountMode: "Required",
  isComplete: true,
  isCompleted: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  createdBy: "user",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  updatedBy: "user",
};

const denomItems = [
  {
    denominationId: "d1",
    value: 1000,
    isEnabled: true,
    sortOrder: 0,
    displayLabel: null,
  },
  {
    denominationId: "d2",
    value: 0.25,
    isEnabled: true,
    sortOrder: 1,
    displayLabel: null,
  },
];

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CashHandlingSettingsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("CashHandlingSettingsPage", () => {
  beforeEach(() => {
    vi.mocked(getOperationalSetup).mockResolvedValue(setupDto as never);
    vi.mocked(listCashDenominations).mockResolvedValue(denomItems as never);
    vi.mocked(updateOperationalSetup).mockResolvedValue(setupDto as never);
    vi.mocked(replaceCashDenominations).mockResolvedValue(undefined as never);
  });

  it("renders policy switches and preserves opening/closing settings", async () => {
    renderPage();

    expect(await screen.findByTestId("cash-handling-page")).toBeInTheDocument();
    const opening = screen.getByTestId("cash-handling-require-opening");
    const closing = screen.getByTestId("cash-handling-require-closing");
    expect(opening).toHaveAttribute("role", "switch");
    expect(opening).toHaveAttribute("aria-checked", "true");
    expect(opening).toHaveTextContent("ON");
    expect(closing).toHaveAttribute("role", "switch");
    expect(closing).toHaveAttribute("aria-checked", "true");
    expect(closing).toHaveTextContent("ON");
    expect(screen.getByTestId("cash-handling-policy")).toHaveTextContent(
      "cashHandling.policyTitle",
    );
  });

  it("saves policy with unchanged contract and Save icon", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("cash-handling-page");

    await user.click(screen.getByTestId("cash-handling-require-closing"));
    await user.click(screen.getByTestId("cash-handling-save-policy"));

    expect(updateOperationalSetup).toHaveBeenCalledWith(
      expect.objectContaining({
        organizationId: "11111111-1111-1111-1111-111111111111",
      }),
      expect.objectContaining({
        openingCashCountMode: "Required",
        closingCashCountMode: "Optional",
      }),
    );
    expect(pageSource).toMatch(/<Save[\s\S]*aria-hidden/);
  });

  it("formats denominations as PHP currency and keeps fractional values", async () => {
    renderPage();
    await screen.findByTestId("cash-handling-denoms-list");

    expect(formatDenominationCurrency(1000)).toMatch(/₱\s?1,000/);
    expect(formatDenominationCurrency(0.25)).toMatch(/₱\s?0\.25/);
    expect(formatDenominationCurrency(0.1)).toMatch(/₱\s?0\.10/);
    expect(screen.getByTestId("cash-handling-denom-1000")).toHaveTextContent(
      formatDenominationCurrency(1000),
    );
    expect(screen.getByTestId("cash-handling-denom-0.25")).toHaveTextContent(
      formatDenominationCurrency(0.25),
    );
    expect(screen.getByTestId("cash-handling-remove-1000")).toHaveAttribute(
      "aria-label",
      expect.stringContaining(formatDenominationCurrency(1000)),
    );
  });

  it("keeps direct-entry amount input without stepper controls", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("cash-handling-page");

    const amount = screen.getByTestId("cash-handling-add-value");
    expect(amount.tagName).toBe("INPUT");
    expect(amount).toHaveAttribute("type", "number");
    expect(amount).toHaveAttribute("step", "any");
    expect(amount.className).toMatch(/exits-input--no-spin/);
    expect(pageSource).not.toMatch(/stepper|increment|decrement/i);
    expect(pageSource).not.toMatch(/Minus|ChevronUp|ChevronDown/);
    expect(pageSource).toMatch(/<Plus[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/<RotateCcw[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/Trash2/);

    await user.clear(amount);
    await user.type(amount, "0.25");
    expect(amount).toHaveValue(0.25);
  });

  it("uses density tokens and responsive denomination grid", () => {
    expect(globalsCss).toContain(".cash-handling-denom-grid");
    expect(globalsCss).toMatch(
      /\.cash-handling-denom-grid\s*\{[\s\S]*?grid-template-columns:\s*repeat\(2/,
    );
    expect(globalsCss).toMatch(
      /\.cash-handling-denom-row[\s\S]*?min-height:\s*var\(--exits-control-height\)/,
    );
    expect(globalsCss).toContain(".exits-switch");
    expect(switchSource).toContain('role="switch"');
    expect(switchSource).toContain("aria-checked");
  });
});
