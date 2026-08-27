import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import type {
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { openOfflineDatabase, organizationScopeKey, personalScopeKey } from "@/offline/db";
import { personalOfflineEligibility } from "@/offline/personal-offline-context";
import {
  cachePersonalContacts,
  cachePersonalEntries,
  cachePersonalRelationships,
  cachePersonalUserIdentityId,
  getCachedPersonalRelationship,
  getCachedPersonalUserIdentityId,
  listCachedPersonalContacts,
  listCachedPersonalEntries,
  listCachedPersonalRelationships,
} from "@/offline/personal-utang-cache";

const contact: PersonalContactDto = {
  id: "11111111-1111-4111-8111-111111111111",
  displayName: "Aling Nena",
  phone: "09171234567",
  email: "nena@example.com",
  linkedUserIdentityId: null,
  publicUserId: null,
  linkedMaskedEmail: null,
  linkedMaskedPhone: null,
  status: "Active",
  createdAtUtc: "2026-02-01T00:00:00.000Z",
};

const relationship: PersonalDebtRelationshipSummaryDto = {
  id: "22222222-2222-4222-8222-222222222222",
  perspective: "Lent",
  creditorUserIdentityId: "99999999-9999-4999-8999-999999999999",
  creditorContactId: null,
  debtorUserIdentityId: null,
  debtorContactId: contact.id,
  currencyCode: "PHP",
  currentBalance: 1250.5,
  dueDateUtc: null,
  status: "Active",
  version: 7,
  updatedAtUtc: "2026-02-02T00:00:00.000Z",
  isSharedLedger: false,
  isPrivate: true,
};

const entry: PersonalUtangEntryDto = {
  id: "33333333-3333-4333-8333-333333333333",
  relationshipId: relationship.id,
  entryType: "Payment",
  amount: 250.5,
  signedDelta: -250.5,
  balanceAfter: 1000,
  notes: "bayad sa Lunes",
  dueDateUtc: null,
  createdByUserIdentityId: "99999999-9999-4999-8999-999999999999",
  createdAtUtc: "2026-02-03T00:00:00.000Z",
  status: "Confirmed",
  resolvedByUserIdentityId: null,
  resolvedAtUtc: null,
  disputeReason: null,
  canConfirm: false,
  canDispute: false,
  canCancel: false,
  affectsBalance: true,
  isSharedLedger: false,
  intent: "Regular",
  settlementBalanceSnapshot: null,
  isSettlement: false,
};

async function openPersonal(userId: string) {
  const scopeBinding = personalScopeKey(userId);
  const db = await openOfflineDatabase("Personal", scopeBinding);
  return { db, scopeBinding };
}

describe("RMAP-21F Personal Utang cache", () => {
  it("round-trips contacts, relationships and history", async () => {
    const { db, scopeBinding } = await openPersonal("cache-roundtrip");

    await cachePersonalContacts(db, scopeBinding, [contact]);
    await cachePersonalRelationships(db, scopeBinding, "Lent", [relationship]);
    await cachePersonalEntries(db, scopeBinding, [entry]);

    expect(await listCachedPersonalContacts(db, scopeBinding)).toEqual([
      { ...contact, origin: "Server", serverId: contact.id },
    ]);
    expect(await listCachedPersonalRelationships(db, scopeBinding, "Lent")).toEqual([
      { ...relationship, origin: "Server", serverId: relationship.id },
    ]);
    expect(await getCachedPersonalRelationship(db, scopeBinding, relationship.id)).toMatchObject({
      currentBalance: 1250.5,
      version: 7,
    });
    expect(await listCachedPersonalEntries(db, scopeBinding, relationship.id)).toEqual([
      { ...entry, origin: "Server", serverId: entry.id },
    ]);
  });

  it("overwrites a stale unlinked contact cache with linked server fields", async () => {
    const { db, scopeBinding } = await openPersonal("cache-link-refresh");
    await cachePersonalContacts(db, scopeBinding, [contact]);

    const linked: PersonalContactDto = {
      ...contact,
      linkedUserIdentityId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
      publicUserId: "EX-1234-5678",
    };
    await cachePersonalContacts(db, scopeBinding, [linked]);

    const cached = await listCachedPersonalContacts(db, scopeBinding);
    expect(cached).toHaveLength(1);
    expect(cached[0]).toMatchObject({
      id: contact.id,
      linkedUserIdentityId: linked.linkedUserIdentityId,
      publicUserId: "EX-1234-5678",
      origin: "Server",
    });
  });

  it("stores names, amounts and notes only as ciphertext", async () => {
    const { db, scopeBinding } = await openPersonal("cache-ciphertext");

    await cachePersonalContacts(db, scopeBinding, [contact]);
    await cachePersonalRelationships(db, scopeBinding, "Lent", [relationship]);
    await cachePersonalEntries(db, scopeBinding, [entry]);

    const raw = JSON.stringify([
      await db.getAll("personalContacts"),
      await db.getAll("personalRelationships"),
      await db.getAll("personalEntries"),
    ]);
    expect(raw).not.toContain("Aling Nena");
    expect(raw).not.toContain("09171234567");
    expect(raw).not.toContain("nena@example.com");
    expect(raw).not.toContain("bayad sa Lunes");
    expect(raw).not.toContain("1250.5");
    expect(raw).not.toContain("250.5");
  });

  it("cannot be read with another Personal user's scope key", async () => {
    const mine = await openPersonal("cache-owner");
    await cachePersonalContacts(mine.db, mine.scopeBinding, [contact]);

    const otherKey = personalScopeKey("cache-intruder");
    expect(await listCachedPersonalContacts(mine.db, otherKey)).toEqual([]);
    expect(await getCachedPersonalRelationship(mine.db, otherKey, relationship.id)).toBeNull();
  });

  it("keeps one Personal user's utang out of another Personal user's database", async () => {
    const first = await openPersonal("cache-user-one");
    const second = await openPersonal("cache-user-two");

    await cachePersonalContacts(first.db, first.scopeBinding, [contact]);

    expect(await listCachedPersonalContacts(second.db, second.scopeBinding)).toEqual([]);
  });

  it("refuses to write Personal utang into an Organization database", async () => {
    const scopeBinding = organizationScopeKey({
      userId: "org-user",
      organizationId: "44444444-4444-4444-8444-444444444444",
      branchId: "55555555-5555-4555-8555-555555555555",
      installationDeviceId: "66666666-6666-4666-8666-666666666666",
    });
    const db = await openOfflineDatabase("Organization", scopeBinding);

    await expect(cachePersonalContacts(db, scopeBinding, [contact])).rejects.toThrow(
      /scope mismatch/i,
    );
    await expect(
      cachePersonalRelationships(db, scopeBinding, "Lent", [relationship]),
    ).rejects.toThrow(/scope mismatch/i);
    await expect(cachePersonalEntries(db, scopeBinding, [entry])).rejects.toThrow(
      /scope mismatch/i,
    );
    expect(await db.getAll("personalContacts")).toEqual([]);
  });

  it("remembers the Personal identity id so an offline debt can name its owner", async () => {
    const { db } = await openPersonal("cache-identity");

    expect(await getCachedPersonalUserIdentityId(db)).toBeNull();
    await cachePersonalUserIdentityId(db, "99999999-9999-4999-8999-999999999999");
    expect(await getCachedPersonalUserIdentityId(db)).toBe("99999999-9999-4999-8999-999999999999");
  });

  it("separates the Lent and Borrowed projections", async () => {
    const { db, scopeBinding } = await openPersonal("cache-perspective");

    await cachePersonalRelationships(db, scopeBinding, "Borrowed", [
      { ...relationship, perspective: "Borrowed" },
    ]);

    expect(await listCachedPersonalRelationships(db, scopeBinding, "Lent")).toEqual([]);
    expect(await listCachedPersonalRelationships(db, scopeBinding, "Borrowed")).toHaveLength(1);
  });
});

describe("RMAP-21F Personal store eligibility", () => {
  it("opens for an unlocked Personal principal", () => {
    expect(personalOfflineEligibility({ userId: "user-1", accountClass: "Personal" })).toEqual({
      eligible: true,
      userId: "user-1",
      scopeBinding: personalScopeKey("user-1"),
    });
  });

  it("never opens for organization staff", () => {
    expect(
      personalOfflineEligibility({
        userId: "staff-1",
        accountClass: "Organization",
        organizationContextLocked: true,
        homeOrganizationId: "44444444-4444-4444-8444-444444444444",
      }),
    ).toEqual({ eligible: false, reason: "staff-locked" });
  });

  it("never opens for an owner who is currently in their organization context", () => {
    expect(personalOfflineEligibility({ userId: "owner-1", accountClass: "Organization" })).toEqual(
      { eligible: false, reason: "not-personal" },
    );
  });

  it("never opens for a Platform admin or an unknown account class", () => {
    expect(personalOfflineEligibility({ userId: "admin-1", accountClass: "Platform" })).toEqual({
      eligible: false,
      reason: "not-personal",
    });
    expect(personalOfflineEligibility({ userId: "who-1" })).toEqual({
      eligible: false,
      reason: "not-personal",
    });
    expect(personalOfflineEligibility(null)).toEqual({ eligible: false, reason: "no-session" });
  });
});
