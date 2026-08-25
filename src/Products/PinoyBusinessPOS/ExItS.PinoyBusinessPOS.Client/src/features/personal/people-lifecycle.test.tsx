import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AddPersonPage } from "@/features/personal/AddPersonPage";
import { InvitationsPage } from "@/features/personal/InvitationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { PeoplePage } from "@/features/personal/PeoplePage";
import { PersonDetailPage } from "@/features/personal/PersonDetailPage";
import { PersonalShell } from "@/features/personal/PersonalShell";
import {
  buildPeopleRows,
  deriveConnectionStatus,
} from "@/features/personal/people-status";
import type {
  PersonalContactDto,
  PersonalInAppNotificationDto,
  PersonalUtangInvitationDto,
} from "@/api/platform/personal-types";

const contactA: PersonalContactDto = {
  id: "c1",
  displayName: "Juan Dela Cruz",
  linkedUserIdentityId: null,
  status: "Active",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
};

const contactConnected: PersonalContactDto = {
  id: "c2",
  displayName: "Ana Cruz",
  linkedUserIdentityId: "u-linked",
  status: "Active",
  createdAtUtc: "2026-08-20T00:00:00.000Z",
};

const pendingInvite: PersonalUtangInvitationDto = {
  id: "inv1",
  debtRelationshipId: "rel1",
  inviteeContactId: "c1",
  invitedByUserIdentityId: "me",
  status: "Pending",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
  updatedAtUtc: "2026-08-25T00:00:00.000Z",
  expiresAtUtc: "2026-09-01T00:00:00.000Z",
};

function jsonResponse(status: number, body: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  };
}

function renderPeopleApp(path: string) {
  const router = createMemoryRouter(
    [
      {
        path: "/",
        element: <PersonalShell />,
        children: [
          { path: "personal/people", element: <PeoplePage /> },
          { path: "personal/people/add", element: <AddPersonPage /> },
          { path: "personal/people/:contactId", element: <PersonDetailPage /> },
          { path: "personal/invitations", element: <InvitationsPage /> },
          { path: "personal/notifications", element: <NotificationsPage /> },
        ],
      },
    ],
    { initialEntries: [path] },
  );

  return render(
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe("people status derivation", () => {
  it("marks unlinked contacts as Not connected and pending invites as Request pending", () => {
    expect(deriveConnectionStatus(contactA, []).status).toBe("not_connected");
    expect(deriveConnectionStatus(contactA, [pendingInvite]).status).toBe("request_pending");
    expect(deriveConnectionStatus(contactConnected, []).status).toBe("connected");
  });

  it("does not put unlink or block actions into list row models", () => {
    const rows = buildPeopleRows({
      contacts: [contactA, contactConnected],
      invitations: [pendingInvite],
      lent: [],
      borrowed: [],
    });
    expect(rows).toHaveLength(2);
    expect(JSON.stringify(rows)).not.toMatch(/Unlink|Block|Delete/i);
  });
});

describe("People lifecycle UX", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.sessionStorage.clear();
  });

  it("renders empty People state", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/utang/invitations")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/relationships/")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );

    renderPeopleApp("/personal/people");
    expect(await screen.findByRole("heading", { name: "People" })).toBeInTheDocument();
    expect(screen.getByText("No people yet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Add person" })).toBeInTheDocument();
  });

  it("renders people rows without destructive list actions", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, [contactA, contactConnected]);
        }
        if (url.includes("/utang/invitations")) {
          return jsonResponse(200, [pendingInvite]);
        }
        if (url.includes("/relationships/")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );

    renderPeopleApp("/personal/people");
    expect(await screen.findByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(screen.getByText("Request pending")).toBeInTheDocument();
    expect(screen.getByText("Ana Cruz")).toBeInTheDocument();
    expect(screen.getByText("Connected")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /unlink/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /block/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();
  });

  it("requires identity confirmation before Add and does not auto-link or invite", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/resolve-public-id") && init?.method === "POST") {
        return jsonResponse(200, {
          publicUserId: "EX-1234-5678",
          userIdentityId: "user-b",
          displayName: "Maria Santos",
          maskedEmail: "m****@exits.local",
          status: "Active",
          isSelf: false,
        });
      }
      if (url.includes("/utang/contacts") && init?.method === "POST") {
        const body = JSON.parse(String(init.body)) as {
          displayName: string;
          linkedUserIdentityId?: string;
        };
        expect(body.displayName).toBe("Maria Santos");
        expect(body.linkedUserIdentityId).toBeUndefined();
        return jsonResponse(201, {
          id: "c-new",
          displayName: "Maria Santos",
          linkedUserIdentityId: null,
          status: "Active",
          createdAtUtc: "2026-08-25T00:00:00.000Z",
        });
      }
      if (url.includes("/utang/contacts")) {
        return jsonResponse(200, []);
      }
      if (url.includes("/utang/invitations")) {
        return jsonResponse(200, []);
      }
      if (url.includes("/relationships/")) {
        return jsonResponse(200, []);
      }
      if (url.includes("/notifications")) {
        return jsonResponse(200, []);
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    const user = userEvent.setup();
    renderPeopleApp("/personal/people/add");

    await user.type(screen.getByLabelText("ExItS ID"), "EX-1234-5678");
    await user.click(screen.getByRole("button", { name: "Find person" }));

    const confirmation = await screen.findByTestId("identity-confirmation");
    expect(within(confirmation).getByText("Maria Santos")).toBeInTheDocument();
    expect(within(confirmation).getByText("EX-1234-5678")).toBeInTheDocument();

    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("/invitations"))).toBe(
      false,
    );
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("/notifications"))).toBe(
      false,
    );

    await user.click(screen.getByRole("button", { name: "Add person" }));
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          (call) => String(call[0]).includes("/utang/contacts") && call[1]?.method === "POST",
        ),
      ).toBe(true);
    });

    const createCall = fetchMock.mock.calls.find(
      (call) => String(call[0]).includes("/utang/contacts") && call[1]?.method === "POST",
    );
    expect(createCall).toBeTruthy();
    expect(String(createCall?.[1]?.body)).not.toContain("user-b");
    expect(
      fetchMock.mock.calls.some(
        (call) =>
          String(call[0]).includes("/invitations") &&
          (call[1]?.method === "POST" || call[1]?.method === "post"),
      ),
    ).toBe(false);
  });

  it("shows incoming invitations and Accept leads to Connected after refresh", async () => {
    let linked = false;
    let invitations: PersonalUtangInvitationDto[] = [
      {
        id: "inv-in",
        debtRelationshipId: "rel-in",
        inviteeContactId: "c-other",
        invitedByUserIdentityId: "sender",
        status: "Pending",
        createdAtUtc: "2026-08-25T00:00:00.000Z",
        updatedAtUtc: "2026-08-25T00:00:00.000Z",
        expiresAtUtc: "2026-09-01T00:00:00.000Z",
      },
    ];

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/utang/invitations/accept") && init?.method === "POST") {
          linked = true;
          invitations = [];
          return jsonResponse(200, {
            invitationId: "inv-in",
            debtRelationshipId: "rel-in",
            linkedContactId: "c-me",
            linkedUserIdentityId: "me",
            createdOrganizationMembership: false,
            grantedProductRole: false,
          });
        }
        if (url.includes("/utang/invitations/decline")) {
          invitations = [];
          return jsonResponse(200, { ...invitations[0], status: "Declined" });
        }
        if (url.includes("/utang/invitations")) {
          return jsonResponse(200, invitations);
        }
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, [
            {
              id: "c-me",
              displayName: "Me",
              linkedUserIdentityId: linked ? "peer" : null,
              status: "Active",
              createdAtUtc: "2026-08-25T00:00:00.000Z",
            },
          ]);
        }
        if (url.includes("/relationships/")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/invitations?token=invite-token");
    expect(await screen.findByText("Personal Utang request")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Accept" }));
    await waitFor(() => expect(linked).toBe(true));
  });

  it("Decline removes pending without inventing a link", async () => {
    let invitations: PersonalUtangInvitationDto[] = [
      {
        id: "inv-in",
        debtRelationshipId: "rel-in",
        inviteeContactId: "c-other",
        invitedByUserIdentityId: "sender",
        status: "Pending",
        createdAtUtc: "2026-08-25T00:00:00.000Z",
        updatedAtUtc: "2026-08-25T00:00:00.000Z",
        expiresAtUtc: "2026-09-01T00:00:00.000Z",
      },
    ];
    let acceptCalled = false;

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/utang/invitations/decline") && init?.method === "POST") {
          invitations = [];
          return jsonResponse(200, {
            id: "inv-in",
            debtRelationshipId: "rel-in",
            inviteeContactId: "c-other",
            invitedByUserIdentityId: "sender",
            status: "Declined",
            createdAtUtc: "2026-08-25T00:00:00.000Z",
            updatedAtUtc: "2026-08-25T00:00:00.000Z",
            expiresAtUtc: "2026-09-01T00:00:00.000Z",
          });
        }
        if (url.includes("/utang/invitations/accept")) {
          acceptCalled = true;
          return jsonResponse(400, {});
        }
        if (url.includes("/utang/invitations")) {
          return jsonResponse(200, invitations);
        }
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/invitations?token=invite-token");
    await user.click(await screen.findByRole("button", { name: "Decline" }));
    await waitFor(() => expect(invitations).toHaveLength(0));
    expect(acceptCalled).toBe(false);
  });

  it("notification tap deep-links to invitations and mark-read does not revoke invitations", async () => {
    const notifications: PersonalInAppNotificationDto[] = [
      {
        id: "n1",
        title: "Eduard sent you a Personal Utang request",
        preview: "Open invitations to respond.",
        relatedType: "PersonalUtangInvitation",
        relatedId: "inv-in",
        isRead: false,
        createdAtUtc: "2026-08-25T00:00:00.000Z",
      },
    ];
    let invitations: PersonalUtangInvitationDto[] = [
      {
        id: "inv-in",
        debtRelationshipId: "rel-in",
        inviteeContactId: "c-other",
        invitedByUserIdentityId: "sender",
        status: "Pending",
        createdAtUtc: "2026-08-25T00:00:00.000Z",
        updatedAtUtc: "2026-08-25T00:00:00.000Z",
        expiresAtUtc: "2026-09-01T00:00:00.000Z",
      },
    ];

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/notifications/") && url.includes("/read") && init?.method === "POST") {
          notifications[0] = { ...notifications[0]!, isRead: true };
          return jsonResponse(200, notifications[0]);
        }
        if (url.includes("/notifications")) {
          return jsonResponse(200, notifications);
        }
        if (url.includes("/utang/invitations")) {
          return jsonResponse(200, invitations);
        }
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/notifications");
    await user.click(
      await screen.findByRole("button", { name: /Eduard sent you a Personal Utang request/i }),
    );
    expect(await screen.findByRole("heading", { name: "Invitations" })).toBeInTheDocument();
    expect(screen.getByText("Personal Utang request")).toBeInTheDocument();
    expect(invitations).toHaveLength(1);
    expect(invitations[0]?.status).toBe("Pending");
  });
});
