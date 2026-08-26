import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: { userId: "me", displayName: "Me" },
    signOut: async () => ({ ok: true as const, nextRoute: "/sign-in" }),
  }),
}));
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, Outlet, RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { AddLocalPersonPage } from "@/features/personal/AddLocalPersonPage";
import { AddPersonPage } from "@/features/personal/AddPersonPage";
import { InvitationsPage } from "@/features/personal/InvitationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { PeoplePage } from "@/features/personal/PeoplePage";
import { PersonDetailPage } from "@/features/personal/PersonDetailPage";
import { buildPeopleRows, deriveConnectionStatus } from "@/features/personal/people-status";
import type {
  PersonalConnectionRequestDto,
  PersonalContactDto,
  PersonalInAppNotificationDto,
} from "@/api/platform/personal-types";

const contactA: PersonalContactDto = {
  id: "c1",
  displayName: "Juan Dela Cruz",
  resolvedUserIdentityId: "user-b",
  resolvedPublicUserId: "EX-1234-5678",
  linkedUserIdentityId: null,
  status: "Active",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
};

const contactConnected: PersonalContactDto = {
  id: "c2",
  displayName: "Ana Cruz",
  resolvedUserIdentityId: "u-linked",
  resolvedPublicUserId: "EX-9999-0000",
  linkedUserIdentityId: "u-linked",
  connectedAtUtc: "2026-08-20T00:00:00.000Z",
  status: "Active",
  createdAtUtc: "2026-08-20T00:00:00.000Z",
};

const pendingConnection: PersonalConnectionRequestDto = {
  id: "req1",
  requesterUserIdentityId: "me",
  targetUserIdentityId: "user-b",
  requesterContactId: "c1",
  requesterDisplayName: "Me",
  requesterPublicUserId: "EX-0000-1111",
  targetPublicUserId: "EX-1234-5678",
  status: "Pending",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
  updatedAtUtc: "2026-08-25T00:00:00.000Z",
  expiresAtUtc: "2026-09-01T00:00:00.000Z",
  direction: "Sent",
};

function jsonResponse(status: number, body: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  };
}

function withAntiforgery(
  handler: (input: RequestInfo | URL, init?: RequestInit) => ReturnType<typeof jsonResponse> | Promise<ReturnType<typeof jsonResponse>>,
) {
  return async (input: RequestInfo | URL, init?: RequestInit) => {
    if (String(input).includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-csrf-token" });
    }
    return handler(input, init);
  };
}

function TestPersonalShell() {
  return <Outlet />;
}

function renderPeopleApp(path: string) {
  const router = createMemoryRouter(
    [
      {
        path: "/",
        element: <div data-testid="generic-root">root</div>,
      },
      {
        path: "/personal",
        element: <TestPersonalShell />,
        children: [
          { path: "people", element: <PeoplePage /> },
          { path: "people/add/local", element: <AddLocalPersonPage /> },
          { path: "people/add", element: <AddPersonPage /> },
          { path: "people/:contactId", element: <PersonDetailPage /> },
          { path: "invitations", element: <InvitationsPage /> },
          { path: "notifications", element: <NotificationsPage /> },
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
  it("marks resolved unlinked contacts as Not connected and pending requests as Request pending", () => {
    expect(deriveConnectionStatus(contactA, []).status).toBe("not_connected");
    expect(deriveConnectionStatus(contactA, [pendingConnection]).status).toBe("request_sent");
    expect(deriveConnectionStatus(contactConnected, []).status).toBe("connected");
  });

  it("marks incoming pending requests as Request received for the peer contact", () => {
    const incoming: PersonalConnectionRequestDto = {
      ...pendingConnection,
      id: "req-in",
      direction: "Received",
      requesterUserIdentityId: "user-b",
      targetUserIdentityId: "me",
      requesterContactId: "other-side-contact",
    };
    expect(deriveConnectionStatus(contactA, [incoming]).status).toBe("request_received");
  });

  it("does not put unlink or block actions into list row models", () => {
    const rows = buildPeopleRows({
      contacts: [contactA, contactConnected],
      connectionRequests: [pendingConnection],
      lent: [],
      borrowed: [],
    });
    expect(rows).toHaveLength(2);
    expect(JSON.stringify(rows)).not.toMatch(/Unlink|Block|Delete/i);
  });
});

describe("People lifecycle UX", () => {
  beforeEach(() => {
    clearPlatformAntiforgeryToken();
  });

  afterEach(() => {
    clearPlatformAntiforgeryToken();
    vi.unstubAllGlobals();
  });

  it("renders How to add and empty people list", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL) => {
          const url = String(input);
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, []);
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, []);
          }
          if (url.includes("/relationships/")) {
            return jsonResponse(200, []);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    renderPeopleApp("/personal/people");
    expect(await screen.findByText("How to add")).toBeInTheDocument();
    expect(screen.getByText("Without ExItS ID")).toBeInTheDocument();
    expect(screen.getByText("With ExItS Personal ID")).toBeInTheDocument();
    expect(screen.getByText(/0 with ExItS ID · 0 local only/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Connection requests/i })).toBeInTheDocument();
  });

  it("opens people info dialog from info button", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL) => {
          const url = String(input);
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, []);
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, []);
          }
          if (url.includes("/relationships/")) {
            return jsonResponse(200, []);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/people");
    await user.click(await screen.findByRole("button", { name: "About People" }));
    expect(await screen.findByRole("heading", { name: "About People" })).toBeInTheDocument();
    expect(screen.getByText(/Connection consent and Utang records are separate/i)).toBeInTheDocument();
  });

  it("submits local contact form without connection request", async () => {
    const fetchMock = vi.fn(
      withAntiforgery(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/utang/contacts") && init?.method === "POST") {
          const body = JSON.parse(String(init.body)) as { displayName: string };
          expect(body.displayName).toBe("Pedro Cruz");
          return jsonResponse(201, {
            id: "c-local",
            displayName: "Pedro Cruz",
            linkedUserIdentityId: null,
            status: "Active",
            createdAtUtc: "2026-08-25T00:00:00.000Z",
          });
        }
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/personal/connections")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/relationships/")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const user = userEvent.setup();
    renderPeopleApp("/personal/people/add/local");
    await user.type(screen.getByLabelText(/^Name/i), "Pedro Cruz");
    await user.click(screen.getByRole("button", { name: "Add person" }));
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          (call) => String(call[0]).includes("/utang/contacts") && call[1]?.method === "POST",
        ),
      ).toBe(true);
    });
    expect(
      fetchMock.mock.calls.some((call) => String(call[0]).includes("/connection-request")),
    ).toBe(false);
  });

  it("renders people rows without destructive list actions", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL) => {
          const url = String(input);
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, [contactA, contactConnected]);
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, [pendingConnection]);
          }
          if (url.includes("/relationships/")) {
            return jsonResponse(200, []);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    renderPeopleApp("/personal/people");
    expect(await screen.findByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(screen.getByText("Request sent")).toBeInTheDocument();
    expect(screen.getByText("Ana Cruz")).toBeInTheDocument();
    expect(screen.getByText("Connected")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /unlink/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /block/i })).not.toBeInTheDocument();
  });

  it("persists resolved identity on add without auto connection request", async () => {
    const fetchMock = vi.fn(
      withAntiforgery(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/resolve-public-id") && init?.method === "POST") {
          const headers = init.headers as Headers;
          expect(headers.get("X-XSRF-TOKEN")).toBe("test-csrf-token");
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
            resolvedUserIdentityId: string;
            resolvedPublicUserId: string;
          };
          expect(body.displayName).toBe("Maria Santos");
          expect(body.resolvedUserIdentityId).toBe("user-b");
          expect(body.resolvedPublicUserId).toBe("EX-1234-5678");
          return jsonResponse(201, {
            id: "c-new",
            displayName: "Maria Santos",
            resolvedUserIdentityId: "user-b",
            resolvedPublicUserId: "EX-1234-5678",
            linkedUserIdentityId: null,
            status: "Active",
            createdAtUtc: "2026-08-25T00:00:00.000Z",
          });
        }
        if (url.includes("/utang/contacts")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/personal/connections")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/relationships/")) {
          return jsonResponse(200, []);
        }
        if (url.includes("/notifications")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const user = userEvent.setup();
    renderPeopleApp("/personal/people/add");

    await user.type(screen.getByLabelText("ExItS ID"), "EX-1234-5678");
    await user.click(screen.getByRole("button", { name: "Find person" }));

    const confirmation = await screen.findByTestId("identity-confirmation");
    expect(within(confirmation).getByText("Maria Santos")).toBeInTheDocument();

    expect(
      fetchMock.mock.calls.some(
        (call) => String(call[0]).includes("/connection-request") && call[1]?.method === "POST",
      ),
    ).toBe(false);

    await user.click(screen.getByRole("button", { name: "Add person" }));
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          (call) => String(call[0]).includes("/utang/contacts") && call[1]?.method === "POST",
        ),
      ).toBe(true);
    });
  });

  it("shows Request connection on not connected detail and creates pending request", async () => {
    let connections: PersonalConnectionRequestDto[] = [];
    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL, init?: RequestInit) => {
          const url = String(input);
          if (url.includes("/connection-request") && init?.method === "POST") {
            connections = [{ ...pendingConnection, id: "req-new" }];
            return jsonResponse(201, connections[0]);
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, connections);
          }
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, [contactA]);
          }
          if (url.includes("/relationships/")) {
            return jsonResponse(200, []);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/people/c1");
    expect(await screen.findByRole("button", { name: "Request connection" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Request connection" }));
    await waitFor(() => expect(connections).toHaveLength(1));
  });

  it("shows incoming connection requests with accept and decline by id", async () => {
    let linked = false;
    let connections: PersonalConnectionRequestDto[] = [
      {
        ...pendingConnection,
        id: "req-in",
        direction: "Received",
        requesterDisplayName: "Eduard",
        requesterPublicUserId: "EX-5555-5555",
      },
    ];

    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL, init?: RequestInit) => {
          const url = String(input);
          if (url.includes("/connections/req-in/accept") && init?.method === "POST") {
            linked = true;
            connections = [];
            return jsonResponse(200, { ...pendingConnection, id: "req-in", status: "Accepted" });
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, connections);
          }
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, [
              {
                ...contactA,
                linkedUserIdentityId: linked ? "user-b" : null,
              },
            ]);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/invitations");
    expect(await screen.findByText("Eduard")).toBeInTheDocument();
    expect(screen.queryByLabelText(/invitation token/i)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Accept" }));
    await waitFor(() => expect(linked).toBe(true));
  });

  it("notification tap deep-links to invitations and mark-read does not revoke pending request", async () => {
    const notifications: PersonalInAppNotificationDto[] = [
      {
        id: "n1",
        title: "Connection request",
        preview: "Eduard sent you a connection request.",
        relatedType: "PersonalConnectionRequest",
        relatedId: "req-in",
        isRead: false,
        createdAtUtc: "2026-08-25T00:00:00.000Z",
      },
    ];
    const connections: PersonalConnectionRequestDto[] = [
      {
        ...pendingConnection,
        id: "req-in",
        direction: "Received",
        requesterDisplayName: "Eduard",
      },
    ];

    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL, init?: RequestInit) => {
          const url = String(input);
          if (url.includes("/notifications/") && url.includes("/read") && init?.method === "POST") {
            notifications[0] = { ...notifications[0]!, isRead: true };
            return jsonResponse(200, notifications[0]);
          }
          if (url.includes("/notifications")) {
            return jsonResponse(200, notifications);
          }
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, connections);
          }
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, []);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    const user = userEvent.setup();
    renderPeopleApp("/personal/notifications");
    await user.click(await screen.findByRole("button", { name: /Connection\. Unread/i }));
    expect(await screen.findByRole("heading", { level: 1, name: "Connection requests" })).toBeInTheDocument();
    expect(screen.getByText("Eduard")).toBeInTheDocument();
    expect(screen.getByText("wants to connect with you")).toBeInTheDocument();
    expect(connections[0]?.status).toBe("Pending");
    expect(notifications[0]?.isRead).toBe(true);
  });

  it("People row shows Request sent after outgoing pending request without treating it as Utang invitation", async () => {
    const connections: PersonalConnectionRequestDto[] = [pendingConnection];
    vi.stubGlobal(
      "fetch",
      vi.fn(
        withAntiforgery(async (input: RequestInfo | URL) => {
          const url = String(input);
          if (url.includes("/personal/connections")) {
            return jsonResponse(200, connections);
          }
          if (url.includes("/utang/contacts")) {
            return jsonResponse(200, [contactA]);
          }
          if (url.includes("/utang/relationships/")) {
            return jsonResponse(200, []);
          }
          if (url.includes("/utang/invitations")) {
            return jsonResponse(200, [{ id: "utang-only", status: "Pending" }]);
          }
          return jsonResponse(404, {});
        }),
      ),
    );

    renderPeopleApp("/personal/people");
    expect(await screen.findByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(screen.getByText("Request sent")).toBeInTheDocument();
    expect(screen.queryByText(/Utang invitation/i)).not.toBeInTheDocument();
  });
});
