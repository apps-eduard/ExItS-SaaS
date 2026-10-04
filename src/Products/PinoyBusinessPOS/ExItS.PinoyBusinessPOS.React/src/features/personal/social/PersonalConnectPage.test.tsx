import { beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  createPersonalContact,
  listPersonalConnectionRequests,
  listPersonalContacts,
  requestPersonalConnection,
  resolvePublicUserId,
} from "@/api/platform/personal-people-client";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { PersonalConnectPage } from "@/features/personal/social/PersonalConnectPage";

const session = vi.hoisted(() => ({ status: "unauthenticated" }));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({ status: session.status }),
}));

vi.mock("@/api/platform/personal-people-client", () => ({
  resolvePublicUserId: vi.fn(),
  listPersonalContacts: vi.fn(async () => []),
  listPersonalConnectionRequests: vi.fn(async () => []),
  createPersonalContact: vi.fn(),
  requestPersonalConnection: vi.fn(),
}));

const target = {
  publicUserId: "EX-4827-1936",
  userIdentityId: "11111111-1111-1111-1111-111111111111",
  displayName: "Bea Santos",
  status: "Active",
  isSelf: false,
  maskedEmail: "b***@example.com",
};

function SignInProbe() {
  const location = useLocation();
  return <div data-testid="sign-in-landed">{location.pathname}{location.search}</div>;
}

function renderConnect(path = "/connect/EX-4827-1936") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[path]}>
            <Routes>
              <Route path="/connect/:publicUserId" element={<PersonalConnectPage />} />
              <Route path="/sign-in" element={<SignInProbe />} />
              <Route path="/personal" element={<div data-testid="personal-home" />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalConnectPage", () => {
  beforeEach(() => {
    session.status = "authenticated";
    vi.mocked(resolvePublicUserId).mockReset();
    vi.mocked(listPersonalContacts).mockReset();
    vi.mocked(listPersonalConnectionRequests).mockReset();
    vi.mocked(createPersonalContact).mockReset();
    vi.mocked(requestPersonalConnection).mockReset();
    vi.mocked(listPersonalContacts).mockResolvedValue([]);
    vi.mocked(listPersonalConnectionRequests).mockResolvedValue([]);
    vi.mocked(resolvePublicUserId).mockResolvedValue(target);
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it("sends a signed-out visitor to sign-in with a relative continue path", async () => {
    session.status = "unauthenticated";
    renderConnect();
    expect(await screen.findByTestId("sign-in-landed")).toHaveTextContent(
      "/sign-in?continue=" + encodeURIComponent("/connect/EX-4827-1936"),
    );
    expect(window.localStorage.getItem("exits.personal.connectIntent")).toContain("EX-4827-1936");
    expect(requestPersonalConnection).not.toHaveBeenCalled();
  });

  it("rejects a malformed public ID without resolving it", () => {
    renderConnect("/connect/not-an-id");
    expect(screen.getByText("This connection link is not valid.")).toBeInTheDocument();
    expect(resolvePublicUserId).not.toHaveBeenCalled();
  });

  it("shows the viewer's own QR with no connect action", async () => {
    vi.mocked(resolvePublicUserId).mockResolvedValue({ ...target, isSelf: true });
    renderConnect();
    expect(await screen.findByTestId("personal-connect-self")).toHaveTextContent(
      "This is your own ExItS QR.",
    );
    expect(screen.queryByTestId("personal-connect-submit")).not.toBeInTheDocument();
  });

  it("shows a generic unavailable message when the ID cannot be resolved", async () => {
    vi.mocked(resolvePublicUserId).mockRejectedValue(
      new PlatformApiError(404, { errorCode: "application.user.not_found", detail: "missing" }),
    );
    renderConnect();
    expect(await screen.findByRole("alert")).toHaveTextContent("This ExItS ID is unavailable.");
    expect(screen.queryByText(/example.com/)).not.toBeInTheDocument();
  });

  it("requires an explicit Connect tap before creating a request", async () => {
    const user = userEvent.setup();
    vi.mocked(createPersonalContact).mockResolvedValue({
      id: "contact-1",
      displayName: "Bea Santos",
      resolvedUserIdentityId: target.userIdentityId,
      resolvedPublicUserId: target.publicUserId,
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
    });
    vi.mocked(requestPersonalConnection).mockResolvedValue({
      id: "req-1",
      requesterUserIdentityId: "me",
      targetUserIdentityId: target.userIdentityId,
      requesterContactId: "contact-1",
      requesterDisplayName: "Ada",
      status: "Pending",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      expiresAtUtc: "2026-02-01T00:00:00Z",
      direction: "Sent",
    });
    renderConnect();
    expect(await screen.findByTestId("personal-connect-confirm")).toHaveTextContent(
      "Connect with Bea Santos?",
    );
    expect(screen.getByTestId("personal-connect-id")).toHaveTextContent("EX-4827-1936");
    expect(screen.queryByText(/example.com/)).not.toBeInTheDocument();
    expect(requestPersonalConnection).not.toHaveBeenCalled();
    await user.click(screen.getByTestId("personal-connect-submit"));
    await waitFor(() => expect(requestPersonalConnection).toHaveBeenCalledWith("contact-1"));
    expect(createPersonalContact).toHaveBeenCalledWith({
      displayName: "Bea Santos",
      resolvedUserIdentityId: target.userIdentityId,
      resolvedPublicUserId: "EX-4827-1936",
    });
    expect(await screen.findByTestId("personal-connect-sent")).toHaveTextContent("Request sent");
    expect(window.localStorage.getItem("exits.personal.connectIntent")).toBeNull();
  });

  it("cancel removes the stored intent and does not create a request", async () => {
    const user = userEvent.setup();
    window.localStorage.setItem(
      "exits.personal.connectIntent",
      JSON.stringify({
        publicUserId: "EX-4827-1936",
        continuePath: "/connect/EX-4827-1936",
        savedAtUtc: Date.now(),
      }),
    );
    renderConnect();
    await user.click(await screen.findByTestId("personal-connect-cancel"));
    expect(await screen.findByTestId("personal-home")).toBeInTheDocument();
    expect(requestPersonalConnection).not.toHaveBeenCalled();
    expect(window.localStorage.getItem("exits.personal.connectIntent")).toBeNull();
    expect(window.sessionStorage.getItem("exits.personal.connectIntent")).toBeNull();
  });

  it("shows already connected, sent, received, and blocked states without a new request", async () => {
    const user = userEvent.setup();
    const contact = {
      id: "contact-1",
      displayName: "Bea Santos",
      resolvedUserIdentityId: target.userIdentityId,
      resolvedPublicUserId: target.publicUserId,
      linkedUserIdentityId: target.userIdentityId,
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
    };
    vi.mocked(listPersonalContacts).mockResolvedValue([contact]);
    renderConnect();
    expect(await screen.findByTestId("personal-connect-already")).toHaveTextContent(
      "You're already connected",
    );
    cleanup();

    vi.mocked(listPersonalContacts).mockResolvedValue([{ ...contact, linkedUserIdentityId: null }]);
    vi.mocked(listPersonalConnectionRequests).mockResolvedValue([
      {
        id: "req-1",
        requesterUserIdentityId: "me",
        targetUserIdentityId: target.userIdentityId,
        requesterContactId: "contact-1",
        requesterDisplayName: "Ada",
        status: "Pending",
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
        expiresAtUtc: "2026-02-01T00:00:00Z",
        direction: "Sent",
      },
    ]);
    renderConnect();
    expect(await screen.findByTestId("personal-connect-sent")).toHaveTextContent(
      "Connection request already sent",
    );
    await user.click(screen.getByTestId("personal-connect-continue"));
    expect(await screen.findByTestId("personal-home")).toBeInTheDocument();
    cleanup();

    vi.mocked(listPersonalConnectionRequests).mockResolvedValue([
      {
        id: "req-2",
        requesterUserIdentityId: target.userIdentityId,
        targetUserIdentityId: "me",
        requesterContactId: "their-contact",
        requesterDisplayName: "Bea Santos",
        status: "Pending",
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
        expiresAtUtc: "2026-02-01T00:00:00Z",
        direction: "Received",
      },
    ]);
    renderConnect();
    expect(await screen.findByTestId("personal-connect-received")).toBeInTheDocument();
    expect(screen.getByTestId("personal-connect-invitations")).toHaveAttribute(
      "href",
      "/personal/invitations",
    );
    cleanup();

    vi.mocked(listPersonalConnectionRequests).mockResolvedValue([]);
    vi.mocked(listPersonalContacts).mockResolvedValue([
      { ...contact, linkedUserIdentityId: null, blockedAtUtc: "2026-01-02T00:00:00Z" },
    ]);
    renderConnect();
    expect(await screen.findByTestId("personal-connect-blocked")).toHaveTextContent(
      "Connection is not available.",
    );
    expect(requestPersonalConnection).not.toHaveBeenCalled();
  });
});
