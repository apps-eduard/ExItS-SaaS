import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import { openOfflineDatabase, organizationScopeKey, personalScopeKey } from "@/offline/db";
import { listOutbox, listSafeOutboxMetadata } from "@/offline/outbox";
import {
  listCachedPersonalContacts,
  listCachedPersonalEntries,
  listCachedPersonalRelationships,
} from "@/offline/personal-utang-cache";
import {
  enqueuePersonalContactCreate,
  enqueuePersonalRelationshipCreate,
  enqueuePersonalUtangEntry,
  OfflinePersonalUtangRejectedError,
  rejectOfflineAdjustment,
} from "@/offline/personal-utang-offline";
import {
  collectLocalRefs,
  parseQueuedRequest,
  resolveLocalRefs,
  type QueuedRequestEnvelope,
} from "@/offline/queued-request";
import { mayAutoRetry, serverDedupeMode } from "@/offline/server-dedupe-policy";
import type { OfflineOperationRecord } from "@/offline/types";

const ownerUserIdentityId = "99999999-9999-4999-8999-999999999999";
const contactId = "11111111-1111-4111-8111-111111111111";
const relationshipId = "22222222-2222-4222-8222-222222222222";
const entryId = "33333333-3333-4333-8333-333333333333";

async function openPersonal(userId: string) {
  const scopeBinding = personalScopeKey(userId);
  const db = await openOfflineDatabase("Personal", scopeBinding);
  return { db, scopeBinding, userId };
}

async function decryptRequest(
  record: OfflineOperationRecord,
  scopeBinding: string,
): Promise<QueuedRequestEnvelope | null> {
  const key = await deriveScopeKeyFromBinding(scopeBinding);
  const plaintext = await decryptPayload(
    key,
    { ciphertext: record.ciphertext, iv: record.iv },
    `${record.scopeKind}|${record.operationType}|${record.operationId}`,
  );
  return parseQueuedRequest(new TextDecoder().decode(plaintext));
}

describe("RMAP-21F Personal Utang offline queue", () => {
  it("queues a contact against the Platform API and shows it locally right away", async () => {
    const scope = await openPersonal("personal-contact");

    const { operation, contact } = await enqueuePersonalContactCreate({
      ...scope,
      contactId,
      contact: { displayName: "  Aling Nena  ", phone: "0917", email: null },
    }, { allowOfflineEngine: true });

    expect(operation.scopeKind).toBe("Personal");
    expect(operation.organizationId).toBeNull();
    expect(operation.branchId).toBeNull();
    expect(operation.queueState).toBe("Pending");
    expect(contact.displayName).toBe("Aling Nena");

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request).toEqual({
      api: "platform",
      method: "POST",
      path: "/api/v1/personal/utang/contacts",
      body: {
        contactId,
        displayName: "Aling Nena",
        phone: "0917",
        email: null,
      },
    });

    const cached = await listCachedPersonalContacts(scope.db, scope.scopeBinding);
    expect(cached).toHaveLength(1);
    expect(cached[0]).toMatchObject({ id: contactId, displayName: "Aling Nena", origin: "Local" });
    expect(cached[0].serverId).toBeNull();
  });

  it("refuses to link a contact to an ExItS identity offline", async () => {
    const scope = await openPersonal("personal-contact-link");

    await expect(
      enqueuePersonalContactCreate({
        ...scope,
        contactId,
        contact: { displayName: "Kumpare" },
        linkedUserIdentityId: ownerUserIdentityId,
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({
      code: "offline.personal.contact.identity_link_not_supported",
    });
  });

  it("refuses a nameless contact", async () => {
    const scope = await openPersonal("personal-contact-nameless");

    await expect(
      enqueuePersonalContactCreate({ ...scope, contactId, contact: { displayName: "   " } }, { allowOfflineEngine: true }),
    ).rejects.toBeInstanceOf(OfflinePersonalUtangRejectedError);
  });

  it("queues a lent relationship that waits for its still-queued contact", async () => {
    const scope = await openPersonal("personal-relationship");

    const contact = await enqueuePersonalContactCreate({
      ...scope,
      contactId,
      contact: { displayName: "Tindera" },
    }, { allowOfflineEngine: true });

    const { operation, relationship } = await enqueuePersonalRelationshipCreate({
      ...scope,
      relationshipId,
      perspective: "Lent",
      contactId,
      contactIsLocal: true,
      dependsOnContactOperationId: contact.operation.operationId,
      ownerUserIdentityId,
      initialLoanAmount: 250.129,
      initialLoanNotes: "  sari-sari  ",
    }, { allowOfflineEngine: true });

    expect(operation.dependsOnOperationId).toBe(contactId);
    expect(relationship.currentBalance).toBe(250.13);

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe("/api/v1/personal/utang/relationships");
    expect(request?.body).toMatchObject({
      relationshipId,
      creditorUserIdentityId: ownerUserIdentityId,
      creditorContactId: null,
      debtorUserIdentityId: null,
      // The contact has no server id yet, so a placeholder travels with the request.
      debtorContactId: `{{local:${contactId}}}`,
      currencyCode: "PHP",
      initialLoanAmount: 250.13,
      initialLoanNotes: "sari-sari",
    });

    // Until the contact posts, the relationship cannot be sent.
    expect(collectLocalRefs(request!)).toEqual([contactId]);
    expect(resolveLocalRefs(request!, () => null)).toEqual({
      resolved: false,
      missing: [contactId],
    });

    const serverContactId = "44444444-4444-4444-8444-444444444444";
    const resolved = resolveLocalRefs(request!, () => serverContactId);
    expect(resolved.resolved).toBe(true);
    if (resolved.resolved) {
      expect(resolved.envelope.body).toMatchObject({ debtorContactId: serverContactId });
    }

    const cached = await listCachedPersonalRelationships(scope.db, scope.scopeBinding, "Lent");
    expect(cached).toHaveLength(1);
    expect(cached[0].origin).toBe("Local");
    const otherSide = await listCachedPersonalRelationships(
      scope.db,
      scope.scopeBinding,
      "Borrowed",
    );
    expect(otherSide).toEqual([]);
  });

  it("puts the signed-in person on the debtor side when they are the borrower", async () => {
    const scope = await openPersonal("personal-borrowed");

    const { operation } = await enqueuePersonalRelationshipCreate({
      ...scope,
      relationshipId,
      perspective: "Borrowed",
      contactId,
      ownerUserIdentityId,
      initialLoanAmount: 100,
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.body).toMatchObject({
      creditorUserIdentityId: null,
      creditorContactId: contactId,
      debtorUserIdentityId: ownerUserIdentityId,
      debtorContactId: null,
    });
  });

  it("refuses to record a debt against another ExItS account offline", async () => {
    const scope = await openPersonal("personal-counterparty");

    await expect(
      enqueuePersonalRelationshipCreate({
        ...scope,
        relationshipId,
        perspective: "Lent",
        contactId,
        ownerUserIdentityId,
        initialLoanAmount: 100,
        counterpartyUserIdentityId: "55555555-5555-4555-8555-555555555555",
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({
      code: "offline.personal.relationship.counterparty_identity_not_supported",
    });
  });

  it("refuses a relationship when this device has never learned the Personal identity id", async () => {
    const scope = await openPersonal("personal-owner-unknown");

    await expect(
      enqueuePersonalRelationshipCreate({
        ...scope,
        relationshipId,
        perspective: "Lent",
        contactId,
        ownerUserIdentityId: "",
        initialLoanAmount: 100,
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.personal.relationship.owner_unknown" });
  });

  it("queues a payment without pinning a stale expectedVersion", async () => {
    const scope = await openPersonal("personal-entry");

    const { operation, entry } = await enqueuePersonalUtangEntry({
      ...scope,
      entryId,
      relationshipId,
      entryType: "Payment",
      amount: 40,
      notes: "bayad",
      ownerUserIdentityId,
      localBalanceBefore: 250,
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe(`/api/v1/personal/utang/relationships/${relationshipId}/entries`);
    expect(request?.body).toEqual({
      entryId,
      entryType: "Payment",
      amount: 40,
      // A Loan or Payment is append-only and the server recomputes the balance, so a version
      // read hours ago must not reject a payment that really happened.
      expectedVersion: null,
      notes: "bayad",
      dueDateUtc: null,
    });
    expect(entry.signedDelta).toBe(-40);
    expect(entry.balanceAfter).toBe(210);

    const cached = await listCachedPersonalEntries(scope.db, scope.scopeBinding, relationshipId);
    expect(cached).toHaveLength(1);
    expect(cached[0].origin).toBe("Local");
  });

  it("routes an entry through a placeholder when its relationship is still queued", async () => {
    const scope = await openPersonal("personal-entry-dependent");

    const { operation } = await enqueuePersonalUtangEntry({
      ...scope,
      entryId,
      relationshipId,
      relationshipIsLocal: true,
      dependsOnRelationshipOperationId: relationshipId,
      entryType: "Loan",
      amount: 60,
      ownerUserIdentityId,
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe(
      `/api/v1/personal/utang/relationships/{{local:${relationshipId}}}/entries`,
    );
    expect(collectLocalRefs(request!)).toEqual([relationshipId]);

    const serverRelationshipId = "66666666-6666-4666-8666-666666666666";
    const resolved = resolveLocalRefs(request!, () => serverRelationshipId);
    expect(resolved.resolved).toBe(true);
    if (resolved.resolved) {
      expect(resolved.envelope.path).toBe(
        `/api/v1/personal/utang/relationships/${serverRelationshipId}/entries`,
      );
    }
  });

  it("refuses a zero or negative entry", async () => {
    const scope = await openPersonal("personal-entry-amount");

    await expect(
      enqueuePersonalUtangEntry({
        ...scope,
        entryId,
        relationshipId,
        entryType: "Payment",
        amount: 0,
        ownerUserIdentityId,
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.personal.entry.amount_invalid" });
  });

  it("keeps Adjustment online-only", () => {
    expect(() => rejectOfflineAdjustment()).toThrow(OfflinePersonalUtangRejectedError);
  });

  it("refuses to write a Personal debt into an Organization store", async () => {
    const scopeBinding = organizationScopeKey({
      userId: "staff-user",
      organizationId: "77777777-7777-4777-8777-777777777777",
      branchId: "88888888-8888-4888-8888-888888888888",
      installationDeviceId: "99999999-9999-4999-8999-999999999998",
    });
    const db = await openOfflineDatabase("Organization", scopeBinding);

    await expect(
      enqueuePersonalContactCreate({
        db,
        scopeBinding,
        userId: "staff-user",
        contactId,
        contact: { displayName: "Should never land here" },
      }, { allowOfflineEngine: true }),
    ).rejects.toThrow(/scope mismatch/i);

    expect(await listOutbox(db)).toEqual([]);
  });

  it("exposes only safe metadata for the Connection & Sync shell", async () => {
    const scope = await openPersonal("personal-metadata");

    await enqueuePersonalContactCreate({
      ...scope,
      contactId,
      contact: { displayName: "Aling Nena", phone: "09171234567" },
    }, { allowOfflineEngine: true });

    const metadata = await listSafeOutboxMetadata(scope.db);
    expect(metadata).toHaveLength(1);
    const serialized = JSON.stringify(metadata);
    expect(serialized).not.toContain("Aling Nena");
    expect(serialized).not.toContain("09171234567");
    expect(Object.keys(metadata[0]).sort()).toEqual([
      "attemptCount",
      "createdAt",
      "failureCode",
      "failureSummary",
      "operationId",
      "operationType",
      "queueState",
      "scopeKind",
      "serverReference",
    ]);
  });
});

describe("PERS-IDEM-01 server dedupe policy", () => {
  it("treats Personal Utang create routes as entity-id dedupe-safe", () => {
    expect(serverDedupeMode("personal.contact.create")).toBe("idempotency-key");
    expect(serverDedupeMode("personal.utang.relationship.create")).toBe("idempotency-key");
    expect(serverDedupeMode("personal.utang.entry.record")).toBe("idempotency-key");
    expect(serverDedupeMode("sale.checkout")).toBe("idempotency-key");
    expect(serverDedupeMode("repayment.create")).toBe("idempotency-key");
  });

  it("auto-retries a Personal money mutation whose outcome is unknown", () => {
    expect(mayAutoRetry("personal.utang.entry.record", "not-dispatched")).toBe(true);
    expect(mayAutoRetry("personal.utang.entry.record", "ambiguous-transport")).toBe(true);
    expect(mayAutoRetry("personal.utang.entry.record", "server-responded")).toBe(false);
  });

  it("auto-retries a POS mutation the server can deduplicate", () => {
    expect(mayAutoRetry("sale.checkout", "ambiguous-transport")).toBe(true);
    expect(mayAutoRetry("customer.create", "ambiguous-transport")).toBe(true);
  });
});
