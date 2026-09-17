import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import type { PlatformOrganizationDto } from "@/api/platform/organization-profile-client";
import {
  __orgProfileTestUtils,
  OrgProfilePage,
} from "@/features/organization/OrgProfilePage";

const getOrganization = vi.fn();

vi.mock("@/api/platform/organization-profile-client", () => ({
  getOrganization: (...args: unknown[]) => getOrganization(...args),
  updateOrganizationProfile: vi.fn(),
  updateOrganizationBranding: vi.fn(),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    sessionGrant: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      productRole: "Owner",
    },
  }),
}));

vi.mock("@/access/pos-capabilities", () => ({
  hasOrganizationManagementAuthority: () => true,
}));

function org(partial: Partial<PlatformOrganizationDto> = {}): PlatformOrganizationDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    displayName: "Mica store",
    slug: "mica-store",
    status: "Active",
    publicOrganizationId: "ORG421278",
    updatedAtUtc: "2026-09-16T00:00:00Z",
    createdAtUtc: "2026-09-01T00:00:00Z",
    ...partial,
    profile: {
      legalName: null,
      contactEmail: "mica@gmail.com",
      contactPhone: null,
      addressLine1: null,
      addressLine2: null,
      city: null,
      region: null,
      postalCode: null,
      countryCode: "PH",
      timeZoneId: null,
      locale: null,
      currencyCode: null,
      ...partial.profile,
    },
    branding: {
      brandDisplayName: null,
      logoUrl: null,
      primaryColor: null,
      accentColor: null,
      ...partial.branding,
    },
  };
}

function wrap(ui: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </MemoryRouter>,
  );
}

describe("__orgProfileTestUtils", () => {
  it("builds word-aware initials", () => {
    expect(__orgProfileTestUtils.orgInitials("Mica store")).toBe("MS");
    expect(__orgProfileTestUtils.orgInitials("Acme")).toBe("AC");
  });

  it("treats country-only address as incomplete", () => {
    const gaps = __orgProfileTestUtils.profileGaps(org());
    expect(gaps).toContain("phone");
    expect(gaps).toContain("address");
    expect(gaps).not.toContain("email");
  });
});

describe("OrgProfilePage", () => {
  beforeEach(() => {
    getOrganization.mockReset();
    getOrganization.mockResolvedValue(org());
  });

  it("renders identity, incomplete notice, and contact rows", async () => {
    wrap(<OrgProfilePage />);
    expect(await screen.findByTestId("org-profile-page")).toBeInTheDocument();
    expect(screen.getByTestId("org-profile-initials")).toHaveTextContent("MS");
    expect(screen.getByTestId("org-profile-public-id")).toHaveTextContent("ORG421278");
    expect(screen.getByTestId("org-profile-incomplete")).toBeInTheDocument();
    expect(screen.getByTestId("org-profile-phone")).toHaveAttribute("data-empty", "true");
    expect(screen.getByTestId("org-profile-email")).toHaveAttribute("data-empty", "false");
    expect(screen.getByTestId("org-profile-address")).toHaveAttribute("data-empty", "true");
    expect(screen.getByTestId("org-profile-edit")).toHaveAttribute(
      "aria-label",
      "orgProfile.edit",
    );
  });

  it("copies public organization ID", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal("navigator", {
      ...navigator,
      clipboard: { writeText },
    });
    wrap(<OrgProfilePage />);
    await screen.findByTestId("org-profile-copy-id");
    await user.click(screen.getByTestId("org-profile-copy-id"));
    expect(writeText).toHaveBeenCalledWith("ORG421278");
    vi.unstubAllGlobals();
  });
});
