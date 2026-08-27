import { describe, expect, it, vi } from "vitest";
import {
  enqueuePersonalContactCreate,
  enqueuePersonalRelationshipCreate,
  enqueuePersonalUtangEntry,
} from "@/offline/personal-utang-offline";
import {
  enqueuePersonalTodoCreate,
  enqueuePersonalTodoTransition,
} from "@/offline/personal-todo-offline";
import { PersonalWebOnlineOnlyError } from "@/runtime/personal-web-runtime-policy";
import { PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY } from "@/runtime/personal-web-legacy-outbox-policy";
import { organizationWebRuntimePolicy } from "@/runtime/organization-web-runtime-policy";

describe("Personal Web offline enqueue", () => {
  const scope = {
    db: {} as never,
    scopeBinding: "personal-scope",
    userId: "user-1",
  };

  it("cannot enqueue Todo create from the Web runtime path", async () => {
    await expect(
      enqueuePersonalTodoCreate({
        ...scope,
        todoId: "11111111-1111-4111-8111-111111111111",
        ownerUserIdentityId: "user-1",
        todo: { title: "Pay hospital" },
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });

  it("cannot enqueue Todo completion from the Web runtime path", async () => {
    await expect(
      enqueuePersonalTodoTransition({
        ...scope,
        operationId: "22222222-2222-4222-8222-222222222222",
        todoId: "11111111-1111-4111-8111-111111111111",
        transition: "complete",
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });

  it("cannot enqueue People contact create from the Web runtime path", async () => {
    await expect(
      enqueuePersonalContactCreate({
        ...scope,
        contactId: "33333333-3333-4333-8333-333333333333",
        contact: { displayName: "Ben" },
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });

  it("cannot enqueue relationship create from the Web runtime path", async () => {
    await expect(
      enqueuePersonalRelationshipCreate({
        ...scope,
        relationshipId: "44444444-4444-4444-8444-444444444444",
        perspective: "Lent",
        contactId: "33333333-3333-4333-8333-333333333333",
        ownerUserIdentityId: "user-1",
        initialLoanAmount: 500,
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });

  it("cannot enqueue Utang Loan from the Web runtime path", async () => {
    await expect(
      enqueuePersonalUtangEntry({
        ...scope,
        entryId: "55555555-5555-4555-8555-555555555555",
        relationshipId: "44444444-4444-4444-8444-444444444444",
        entryType: "Loan",
        amount: 500,
        ownerUserIdentityId: "user-1",
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });

  it("cannot enqueue Utang Payment from the Web runtime path", async () => {
    await expect(
      enqueuePersonalUtangEntry({
        ...scope,
        entryId: "66666666-6666-4666-8666-666666666666",
        relationshipId: "44444444-4444-4444-8444-444444444444",
        entryType: "Payment",
        amount: 100,
        ownerUserIdentityId: "user-1",
      }),
    ).rejects.toBeInstanceOf(PersonalWebOnlineOnlyError);
  });
});

describe("Personal Web policy coexistence", () => {
  it("keeps Organization Web online-only unchanged", () => {
    expect(organizationWebRuntimePolicy.offlineSession).toBe(false);
    expect(organizationWebRuntimePolicy.offlineQueueing).toBe(false);
  });

  it("preserves legacy Personal outbox drain policy", () => {
    expect(PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY).toBe("preserve-and-drain-when-online");
  });
});

describe("service worker Personal mutation policy", () => {
  it("documents NetworkOnly API caching (no SW mutation replay)", async () => {
    const fs = await import("node:fs/promises");
    const path = await import("node:path");
    const configPath = path.resolve(process.cwd(), "vite.config.ts");
    const source = await fs.readFile(configPath, "utf8");
    expect(source).toContain('handler: "NetworkOnly"');
    expect(source).toMatch(/\/api\//);
    expect(source).not.toMatch(/BackgroundSyncPlugin/);
  });
});

vi.mock("@/offline/outbox", () => ({
  enqueueEncryptedOperation: vi.fn(),
}));
