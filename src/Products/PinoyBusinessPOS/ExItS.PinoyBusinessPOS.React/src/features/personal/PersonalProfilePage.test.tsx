import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PersonalProfilePage } from "@/features/personal/PersonalProfilePage";
import { deletePersonalAddress, getPersonalProfile, updatePersonalProfile } from "@/api/platform/start-business-client";

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({ refreshSession: vi.fn() }),
}));

vi.mock("@/api/platform/start-business-client", () => ({
  getPersonalProfile: vi.fn(),
  updatePersonalProfile: vi.fn(),
  uploadPersonalProfilePhoto: vi.fn(),
  savePersonalAddress: vi.fn(),
  deletePersonalAddress: vi.fn(),
  setPersonalAddressPrimary: vi.fn(),
}));

vi.mock("@/platform/geography/geography-client", () => ({
  listCountries: vi.fn(async () => [
    { code: "PH", name: "Philippines", isActive: true },
    { code: "SA", name: "Saudi Arabia", isActive: true },
  ]),
  getCountryAddressConfig: vi.fn(async (countryCode: string) => (
    countryCode === "SA"
      ? {
          countryCode: "SA",
          administrativeAreaLabel: "State / Province",
          cityLabel: "City",
          postalCodeLabel: "Postal / ZIP",
          requiresAdministrativeArea: true,
          requiresCity: true,
          requiresPostalCode: false,
          supportsBarangay: false,
        }
      : {
          countryCode: "PH",
          administrativeAreaLabel: "Province",
          cityLabel: "City / Municipality",
          postalCodeLabel: "Postal Code",
          requiresAdministrativeArea: true,
          requiresCity: true,
          requiresPostalCode: false,
          supportsBarangay: true,
        }
  )),
  listAdministrativeAreas: vi.fn(async (countryCode: string) => (
    countryCode === "PH"
      ? [
          { id: "0604", countryCode: "PH", code: "0604", name: "Aklan", type: "Province", parentId: null, isActive: true },
          { id: "1300", countryCode: "PH", code: "1300", name: "Metro Manila", type: "Province", parentId: null, isActive: true },
        ]
      : []
  )),
  listCities: vi.fn(async () => [
    { id: "0600401000", countryCode: "PH", administrativeAreaId: "0604", code: "0600401000", name: "Kalibo", isActive: true },
  ]),
  listBarangays: vi.fn(async () => [
    { id: "0600401001", cityId: "0600401000", code: "0600401001", name: "Poblacion" },
  ]),
  countryCodeForName: (countries: { code: string; name: string }[], countryOrCode: string) => {
    const match = countries.find((country) => country.code === countryOrCode || country.name === countryOrCode);
    return match?.code ?? "";
  },
}));

const profile = {
  userIdentityId: "11111111-1111-1111-1111-111111111111",
  accountProfileId: "22222222-2222-2222-2222-222222222222",
  username: "ana",
  displayName: "Ana Reyes",
  email: "ana@example.com",
  accountClass: "Personal",
  status: "Active",
  publicUserId: "EXITS-ANA",
  qrPayload: null,
  phone: "09170000000",
  firstName: "Ana",
  middleName: null,
  lastName: "Reyes",
  dateOfBirth: null,
  gender: null,
  nationality: null,
  profilePhotoUrl: null,
  alternativeMobile: null,
  country: "Philippines",
  addressLine1: "123 Example Street",
  addressLine2: null,
  barangay: "Poblacion",
  cityMunicipality: "Kalibo",
  province: "Aklan",
  provinceState: "Aklan",
  postalCode: "5600",
  isPrimary: true,
  addresses: [
    {
      id: "33333333-3333-3333-3333-333333333333",
      addressType: "Home",
      country: "Philippines",
      addressLine1: "123 Example Street",
      addressLine2: null,
      barangay: "Poblacion",
      cityMunicipality: "Kalibo",
      provinceState: "Aklan",
      postalCode: "5600",
      isPrimary: true,
      countryCode: "PH",
    },
    {
      id: "44444444-4444-4444-4444-444444444444",
      addressType: "Office",
      country: "Saudi Arabia",
      addressLine1: "King Fahd Road",
      addressLine2: null,
      barangay: null,
      cityMunicipality: "Riyadh",
      provinceState: "Riyadh Province",
      postalCode: "11564",
      isPrimary: false,
      countryCode: "SA",
    },
  ],
  showProfilePhoto: "Private",
  showDisplayName: "Connections",
  showCity: "Private",
  showMobile: "Private",
  showEmail: "Private",
  completionPercent: 83,
  missingForBase: ["Country"],
  missingForStaff: ["AddressLine1", "Barangay", "CityMunicipality", "ProvinceState"],
  missingForCustomer: ["CityMunicipality", "ProvinceState"],
};

function renderProfile(entry: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <ToastProvider>
            <MemoryRouter initialEntries={[entry]}>
              <PersonalProfilePage />
            </MemoryRouter>
          </ToastProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalProfilePage", () => {
  beforeEach(() => {
    vi.mocked(getPersonalProfile).mockResolvedValue(profile);
  });

  it("shows completeness and lists home and office addresses without a region field", async () => {
    renderProfile("/personal/profile");
    const completeness = await screen.findByTestId("personal-profile-completeness");
    expect(completeness).toHaveTextContent("83% complete");
    expect(completeness.querySelector("[role='progressbar']")).toHaveAttribute("aria-valuenow", "83");
    expect(screen.getByTestId("personal-profile-header-photo")).toBeInTheDocument();
    expect(screen.getByTestId("personal-profile-tab-overview")).toHaveAttribute("aria-selected", "true");
    expect(screen.queryByTestId("personal-profile-save")).not.toBeInTheDocument();
    expect(screen.queryByTestId("personal-profile-missing")).not.toBeInTheDocument();
    expect(screen.queryByTestId("personal-profile-region")).not.toBeInTheDocument();
    expect(screen.queryByText("Region")).not.toBeInTheDocument();
    expect(screen.getByText("123 Example Street")).toBeInTheDocument();
    expect(screen.queryByText("Saudi Arabia")).not.toBeInTheDocument();
    const showAll = screen.getByTestId("personal-address-show-all");
    expect(showAll).toHaveTextContent("Show all");
    expect(showAll).toHaveTextContent("2");
    expect(showAll.closest("section")).toHaveClass("personal-profile-section--address");
    const home = screen.getByTestId("personal-address-33333333-3333-3333-3333-333333333333");
    expect(home).toHaveTextContent("Country");
    expect(home).toHaveTextContent("Philippines");
    expect(home).toHaveTextContent("Postal code");
    expect(home).toHaveTextContent("5600");
    expect(screen.getByTestId("personal-address-primary-badge")).toHaveTextContent("Primary");
    expect(showAll.querySelector(".personal-address-show-all__chevron")).toBeTruthy();
    const user = userEvent.setup();
    await user.click(showAll);
    expect(showAll).toHaveTextContent("Primary only");
    expect(showAll.querySelector(".personal-address-show-all__chevron--open")).toBeTruthy();
    expect(screen.getByText("Saudi Arabia")).toBeInTheDocument();
    const privacy = screen.getByRole("heading", { name: "Privacy" });
    expect(privacy.closest("section")).toHaveClass("person-ribbon-card");
    expect(privacy).toHaveClass("personal-tracker-card__ribbon");
    expect(privacy.querySelector(".lucide-shield")).toBeTruthy();
  });

  it("keeps contact inside Personal information", async () => {
    const user = userEvent.setup();
    renderProfile("/personal/profile");
    await user.click(await screen.findByTestId("personal-profile-tab-personal"));
    expect(screen.getByRole("heading", { name: "Personal information" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Contact" })).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-profile-mobile")).toBeInTheDocument();
    expect(screen.getByTestId("personal-profile-gender").closest(".ui-standards-qdropdown")).toHaveClass("ui-standards-qdropdown--outlined");
    expect(screen.queryByText(/· Required/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Optional/)).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-profile-photo-upload-input")).toHaveAttribute("type", "file");
    expect(screen.queryByText("Nationality · Optional · Private")).not.toBeInTheDocument();
    expect(screen.queryByText(/Alternative mobile/)).not.toBeInTheDocument();
  });

  it("lists the staff fields required before accepting an invitation", async () => {
    renderProfile("/personal/profile?complete=staff&return=/personal/staff-invitations");
    const missing = await screen.findByTestId("personal-profile-missing");
    expect(missing).toHaveTextContent("Complete your profile before accepting.");
    expect(missing).toHaveTextContent("Address");
    expect(missing).toHaveTextContent("Barangay");
    expect(missing).toHaveTextContent("City / municipality");
    expect(missing).toHaveTextContent("Province");
  });

  it("uses barangay and Province for the Philippines and State / Province abroad", async () => {
    const user = userEvent.setup();
    renderProfile("/personal/profile");
    await user.click(await screen.findByTestId("personal-profile-tab-addresses"));
    const office = screen.getByTestId("personal-address-44444444-4444-4444-4444-444444444444");
    const edit = within(office).getByRole("button", { name: "Edit" });
    const remove = within(office).getByRole("button", { name: "Delete" });
    const primary = within(office).getByRole("button", { name: "Set as Primary" });
    expect(primary).toHaveClass("exits-btn--glossy");
    expect(primary.compareDocumentPosition(edit)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(office.querySelector(".personal-address-saved")!.compareDocumentPosition(edit)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(edit.parentElement).toHaveClass("justify-end");
    await user.click(remove);
    expect(await screen.findByTestId("personal-address-delete-confirm")).toHaveTextContent("Delete this address?");
    expect(deletePersonalAddress).not.toHaveBeenCalled();
    await user.click(screen.getByTestId("personal-address-delete-confirm-cancel"));
    await user.click(edit);
    expect(await screen.findByRole("heading", { name: "Edit address" })).toBeInTheDocument();
    expect(screen.getByTestId("personal-address-dialog").querySelector(".lucide-pencil")).toBeTruthy();
    await user.click(screen.getAllByRole("button", { name: "Cancel" })[0]!);
    await user.click(await screen.findByTestId("personal-address-add"));
    const dialog = await screen.findByTestId("personal-address-dialog");
    expect(dialog).toHaveClass("exits-modal--ribbon");
    expect(screen.getByRole("heading", { name: "Add Address" })).toBeInTheDocument();
    expect(dialog.querySelector(".lucide-plus")).toBeTruthy();
    expect(screen.getByTestId("personal-address-line1").closest(".ui-standards-qinput")).toHaveClass("ui-standards-qinput--outlined");
    expect(screen.getByTestId("personal-address-type").closest(".ui-standards-qdropdown")).toHaveClass("ui-standards-qdropdown--outlined");
    expect(await screen.findByTestId("personal-address-barangay")).toBeInTheDocument();
    expect(screen.getByTestId("personal-address-province").closest(".ui-standards-qdropdown")).toHaveTextContent("Province*");
    expect(screen.queryByText(/Required/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Optional/)).not.toBeInTheDocument();
    expect(screen.queryByText("Region")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-address-city")).toBeDisabled();

    await user.click(screen.getByTestId("personal-address-province"));
    await user.type(screen.getByTestId("personal-address-province-search"), "Aklan");
    await user.click(screen.getByTestId("personal-address-province-option-0604"));
    expect(screen.getByTestId("personal-address-city")).toBeEnabled();

    await user.click(screen.getByTestId("personal-address-country"));
    await user.type(screen.getByTestId("personal-address-country-search"), "Saudi");
    await user.click(await screen.findByTestId("personal-address-country-option-SA"));
    expect(screen.queryByTestId("personal-address-barangay")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-address-province").closest(".ui-standards-qinput, .ui-standards-qdropdown")).toHaveTextContent("State / Province*");
    expect(screen.getByTestId("personal-address-postal").closest(".ui-standards-qinput")).toHaveTextContent("Postal / ZIP");
    expect(screen.getByTestId("personal-address-postal").closest(".ui-standards-qinput")).not.toHaveTextContent("Optional");
    expect(screen.getByTestId("personal-address-city").closest(".ui-standards-qinput, .ui-standards-qdropdown")).toHaveTextContent("City*");
  });

  it("summarizes privacy changes in a top info toast", async () => {
    const user = userEvent.setup();
    vi.mocked(updatePersonalProfile).mockResolvedValue({ ...profile, showProfilePhoto: "Public" });
    renderProfile("/personal/profile");
    await user.click(await screen.findByTestId("personal-profile-tab-privacy"));
    await user.selectOptions(screen.getByRole("combobox", { name: /Profile photo visibility/ }), "Public");
    const save = screen.getAllByTestId("personal-profile-save").find((button) => !button.closest("[hidden]"));
    await user.click(save!);
    const toast = await screen.findByTestId("exits-toast");
    expect(toast).toHaveAttribute("data-tone", "info");
    expect(toast.closest("[data-testid='exits-toast-region-top']")).toBeTruthy();
    expect(toast).toHaveTextContent("Privacy updated");
    expect(toast).toHaveTextContent("Profile photo visibility: Private → Public");
    expect(screen.getByTestId("exits-toast-close")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-profile-success")).not.toBeInTheDocument();
  });
});
