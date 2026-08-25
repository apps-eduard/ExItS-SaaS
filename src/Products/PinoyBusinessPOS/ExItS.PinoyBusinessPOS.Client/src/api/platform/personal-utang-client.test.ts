import { describe, expect, it, vi } from "vitest";
import {
  confirmPersonalUtangEntry,
  disputePersonalUtangEntry,
  formatDueLabel,
  isUtangConcurrencyConflict,
  listLentRelationships,
  listPersonalContacts,
  listPersonalUtangHistory,
} from "@/api/platform/personal-utang-client";
import { PlatformApiError } from "@/api/platform/platform-http";

describe("personal-utang-client", () => {
  it("parses contact list payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => [
          {
            Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            DisplayName: "Ana",
            Phone: null,
            Email: null,
            LinkedUserIdentityId: null,
            Status: "Active",
            CreatedAtUtc: "2026-08-21T00:00:00Z",
          },
        ],
        text: async () => "",
      })),
    );

    const contacts = await listPersonalContacts();
    expect(contacts).toHaveLength(1);
    expect(contacts[0]?.displayName).toBe("Ana");
    vi.unstubAllGlobals();
  });

  it("parses shared-ledger relationship and confirmation fields", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/relationships/lent")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                Id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                Perspective: "Lent",
                CreditorUserIdentityId: "11111111-1111-1111-1111-111111111111",
                DebtorContactId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                CurrencyCode: "PHP",
                CurrentBalance: 100,
                Status: "Active",
                Version: 2,
                UpdatedAtUtc: "2026-08-21T00:00:00Z",
                IsSharedLedger: true,
                IsPrivate: false,
              },
            ],
            text: async () => "",
          };
        }
        if (url.includes("/history")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                Id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                RelationshipId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                EntryType: "Loan",
                Amount: 100,
                SignedDelta: 100,
                BalanceAfter: 100,
                CreatedByUserIdentityId: "11111111-1111-1111-1111-111111111111",
                CreatedAtUtc: "2026-08-21T00:00:00Z",
                Status: "Pending",
                CanConfirm: true,
                CanDispute: true,
                CanCancel: false,
                AffectsBalance: false,
                IsSharedLedger: true,
              },
            ],
            text: async () => "",
          };
        }
        return { ok: false, status: 404, json: async () => ({}), text: async () => "" };
      }),
    );

    const rows = await listLentRelationships();
    expect(rows[0]?.isSharedLedger).toBe(true);
    expect(rows[0]?.isPrivate).toBe(false);

    const history = await listPersonalUtangHistory("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    expect(history[0]?.status).toBe("Pending");
    expect(history[0]?.canConfirm).toBe(true);
    expect(history[0]?.affectsBalance).toBe(false);
    vi.unstubAllGlobals();
  });

  it("posts confirm and dispute entry actions", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/antiforgery/token")) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
          text: async (): Promise<string> => "",
        };
      }
      expect(init?.method ?? "GET").toBe("POST");
      if (url.includes("/confirm")) {
        return {
          ok: true,
          status: 200,
          json: async () => ({
            id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            relationshipId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            entryType: "Payment",
            amount: 50,
            signedDelta: -50,
            balanceAfter: 50,
            createdByUserIdentityId: "22222222-2222-2222-2222-222222222222",
            createdAtUtc: "2026-08-21T00:00:00Z",
            status: "Confirmed",
            canConfirm: false,
            canDispute: false,
            canCancel: false,
            affectsBalance: true,
            isSharedLedger: true,
          }),
          text: async () => "",
        };
      }
      return {
        ok: true,
        status: 200,
        json: async () => ({
          id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          relationshipId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          entryType: "Payment",
          amount: 50,
          signedDelta: -50,
          balanceAfter: 100,
          createdByUserIdentityId: "22222222-2222-2222-2222-222222222222",
          createdAtUtc: "2026-08-21T00:00:00Z",
          status: "Disputed",
          disputeReason: "Amount incorrect",
          canConfirm: false,
          canDispute: false,
          canCancel: false,
          affectsBalance: false,
          isSharedLedger: true,
        }),
        text: async () => "",
      };
    });
    vi.stubGlobal("fetch", fetchMock);

    const confirmed = await confirmPersonalUtangEntry(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "cccccccc-cccc-cccc-cccc-cccccccccccc",
      { expectedVersion: 3 },
    );
    expect(confirmed.status).toBe("Confirmed");
    expect(
      fetchMock.mock.calls.some((call) => String(call[0]).includes("/confirm")),
    ).toBe(true);

    const disputed = await disputePersonalUtangEntry(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "cccccccc-cccc-cccc-cccc-cccccccccccc",
      { expectedVersion: 3, reason: "Amount incorrect" },
    );
    expect(disputed.status).toBe("Disputed");
    expect(disputed.disputeReason).toBe("Amount incorrect");
    vi.unstubAllGlobals();
  });

  it("detects concurrency conflicts", () => {
    expect(
      isUtangConcurrencyConflict(
        new PlatformApiError(409, { errorCode: "application.concurrency_conflict" }),
      ),
    ).toBe(true);
    expect(isUtangConcurrencyConflict(new PlatformApiError(400, { errorCode: "x" }))).toBe(false);
  });

  it("classifies due dates", () => {
    const now = new Date("2026-08-21T12:00:00Z");
    expect(formatDueLabel("2026-08-20T00:00:00Z", now).kind).toBe("overdue");
    expect(formatDueLabel("2026-08-22T00:00:00Z", now).kind).toBe("dueSoon");
    expect(formatDueLabel("2026-09-21T00:00:00Z", now).kind).toBe("upcoming");
    expect(formatDueLabel(null, now).kind).toBe("none");
  });
});
