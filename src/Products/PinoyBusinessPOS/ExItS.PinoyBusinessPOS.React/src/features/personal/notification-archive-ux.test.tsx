import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ArchivedNotificationsPage } from "@/features/personal/ArchivedNotificationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

const recentItems = [
  {
    id: "recent-1",
    title: "Connection",
    preview: "Mica wants to connect with you.",
    relatedType: "PersonalConnectionRequest",
    relatedId: "conn-1",
    isRead: false,
    createdAtUtc: new Date().toISOString(),
    readAtUtc: null,
  },
];

const archivedPage1 = {
  items: [
    {
      id: "arch-1",
      title: "Connection",
      preview: "Mica accepted your connection request.",
      relatedType: "PersonalConnectionRequest",
      relatedId: "conn-old",
      isRead: true,
      createdAtUtc: "2026-07-21T10:00:00.000Z",
      readAtUtc: "2026-07-21T11:00:00.000Z",
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 30,
};

const archivedPage2 = {
  items: [
    {
      id: "arch-2",
      title: "Connection",
      preview: "Reminder sent",
      relatedType: "PersonalTodo",
      relatedId: "todo-1",
      isRead: true,
      createdAtUtc: "2026-06-14T10:00:00.000Z",
      readAtUtc: "2026-06-14T11:00:00.000Z",
    },
  ],
  totalCount: 2,
  page: 2,
  pageSize: 30,
};

function jsonResponse(status: number, body: unknown) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

describe("notification archive UX", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-CSRF", token: "t" });
        }
        if (url.includes("/notifications/unread-count")) {
          return jsonResponse(200, { unreadCount: 1 });
        }
        if (url.includes("scope=archived")) {
          const page = new URL(url, "http://local").searchParams.get("page") ?? "1";
          return jsonResponse(200, page === "2" ? archivedPage2 : archivedPage1);
        }
        if (url.includes("/notifications") && url.includes("scope=recent")) {
          return jsonResponse(200, recentItems);
        }
        if (url.includes("/connections")) {
          return jsonResponse(200, [
            { id: "conn-old", status: "Accepted", requesterUserIdentityId: "a", targetUserIdentityId: "b" },
          ]);
        }
        if (url.includes("/read") && init?.method === "POST") {
          return jsonResponse(200, { ...recentItems[0], isRead: true });
        }
        return jsonResponse(404, { title: "not found" });
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  function renderAt(path: string) {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return render(
      <PreferencesProvider>
        <I18nProvider>
          <QueryClientProvider client={client}>
            <MemoryRouter initialEntries={[path]}>
              <Routes>
                <Route path="/personal/notifications" element={<NotificationsPage />} />
                <Route path="/personal/notifications/archived" element={<ArchivedNotificationsPage />} />
                <Route path="/personal/invitations" element={<div>invitations</div>} />
                <Route path="/personal/todo" element={<div>todo</div>} />
              </Routes>
            </MemoryRouter>
          </QueryClientProvider>
        </I18nProvider>
      </PreferencesProvider>,
    );
  }

  it("shows recent inbox entry to archived notifications", async () => {
    renderAt("/personal/notifications");
    expect(await screen.findByTestId("notifications-view-archived")).toBeInTheDocument();
    expect(screen.getByText(/Mica wants to connect/i)).toBeInTheDocument();
  });

  it("loads archived page with month grouping and load more append", async () => {
    const user = userEvent.setup();
    renderAt("/personal/notifications/archived");
    expect(await screen.findByTestId("personal-notifications-archived-page")).toBeInTheDocument();
    expect(screen.getByText(/July 2026/i)).toBeInTheDocument();
    expect(screen.getByText(/Mica accepted your connection request/i)).toBeInTheDocument();
    expect(screen.getByText("Connected")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Accept$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Decline$/i })).not.toBeInTheDocument();

    await user.click(screen.getByTestId("archived-notifications-load-more"));
    await waitFor(() => {
      expect(screen.getByText(/June 2026/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/Reminder sent/i)).toBeInTheDocument();
    const rows = screen.getAllByTestId(/archived-notification-row-/);
    expect(rows).toHaveLength(2);
  });

  it("shows archive empty state", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-CSRF", token: "t" });
        }
        if (url.includes("scope=archived")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 30 });
        }
        if (url.includes("/connections")) {
          return jsonResponse(200, []);
        }
        return jsonResponse(404, {});
      }),
    );
    renderAt("/personal/notifications/archived");
    expect(await screen.findByText(/No archived notifications yet/i)).toBeInTheDocument();
  });
});
