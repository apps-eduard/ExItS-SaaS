import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { PersonalMyQrPage } from "@/features/personal/social/PersonalMyQrPage";

vi.mock("@/api/platform/public-identity-client", () => ({
  getMyPublicIdentity: vi.fn(async () => ({
    publicUserId: "EX-4827-1936",
    qrPayload: "exits://qr/v1/personal/EX-4827-1936",
    displayName: "Ada Owner",
    status: "Active",
  })),
}));

vi.mock("@/features/qr/QrCodeImage", () => ({
  QrCodeImage: ({ payload }: { payload: string }) => (
    <img data-testid="personal-my-qr-image" alt="qr" data-payload={payload} />
  ),
}));

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter>
            <PersonalMyQrPage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalMyQrPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("loads public identity and shows QR payload without secrets", async () => {
    renderPage();
    expect(await screen.findByTestId("personal-public-id")).toHaveTextContent("EX-4827-1936");
    expect(screen.getByTestId("personal-qr-display-name")).toHaveTextContent("Ada Owner");
    const img = screen.getByTestId("personal-my-qr-image");
    expect(img).toHaveAttribute("data-payload", "exits://qr/v1/personal/EX-4827-1936");
    expect(screen.getByTestId("personal-my-qr-page").textContent).not.toMatch(/@/);
    await waitFor(() => expect(screen.getByTestId("personal-qr-copy")).toBeVisible());
  });
});
