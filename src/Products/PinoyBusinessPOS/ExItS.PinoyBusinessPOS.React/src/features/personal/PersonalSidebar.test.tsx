import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { PersonalSidebar } from "@/features/personal/PersonalSidebar";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

const switchToBusiness = vi.fn();

vi.mock("@/features/preferences/usePreferencesDestinationClick", () => ({
  usePreferencesDestinationClick: () => () => false,
}));

vi.mock("@/features/personal/useNotificationsDestinationClick", () => ({
  useNotificationsDestinationClick: () => () => false,
}));

vi.mock("@/workspace/use-switch-to-business", () => ({
  useSwitchToBusiness: () => ({
    canSwitch: true,
    switching: false,
    switchToBusiness,
    online: true,
  }),
}));

describe("PersonalSidebar switch", () => {
  beforeEach(() => {
    switchToBusiness.mockReset();
  });

  it("switches a personal owner into the business instead of the organization-only workspace page", async () => {
    render(
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={["/personal"]}>
            <PersonalSidebar />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>,
    );

    const control = screen.getByTestId("personal-sidebar-switch-workspace");
    expect(control).toHaveTextContent("Switch to business");
    expect(control).not.toHaveAttribute("href", "/workspace");
    await userEvent.click(control);
    expect(switchToBusiness).toHaveBeenCalledOnce();
  });
});
