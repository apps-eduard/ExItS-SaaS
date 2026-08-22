import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SignInPage } from "@/features/auth/SignInPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    probeExternalAuthProvider: vi.fn(async () => "disabled" as const),
  };
});

function renderSignInPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter>
              <SignInPage />
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("SignInPage login UX", () => {
  it("renders MAUI-inspired auth shell with tabs and social row", async () => {
    renderSignInPage();
    expect(screen.getByTestId("sign-in-page")).toBeInTheDocument();
    expect(screen.getByTestId("auth-experience-hero")).toBeInTheDocument();
    expect(screen.getByTestId("auth-experience-sheet")).toBeInTheDocument();
    expect(screen.getByTestId("auth-tab-sign-in")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("auth-tab-sign-up")).toBeInTheDocument();
    expect(screen.getByTestId("auth-social-row")).toBeInTheDocument();
    expect(screen.getByTestId("auth-google-button")).toBeInTheDocument();
    expect(screen.getByTestId("auth-facebook-button")).toBeInTheDocument();
    expect(screen.getByTestId("auth-pin-button")).toBeInTheDocument();
    expect(screen.getByText("Expert IT Solutions")).toBeInTheDocument();
    expect(screen.getByText("Pinoy Business POS")).toBeInTheDocument();
  });
});
