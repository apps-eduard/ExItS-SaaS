import type { PosCustomerCreditSummary, PosCustomerListItem } from "@/api/pos/pos-customers-client";
import { decryptPayload, encryptPayload } from "@/offline/crypto";
import { getActiveOfflineCryptoKeyForScope } from "@/offline/local-store-key";
import type { OfflineDb } from "@/offline/db";
import type { CachedCustomerCreditRecord, CachedCustomerRecord } from "@/offline/types";

/**
 * Encrypted Business customer cache (RMAP-21E).
 *
 * Write-through from a successful online read only — this cache never invents a customer, a
 * status, or a balance. Customer identity fields and outstanding balances are encrypted at rest
 * with the organization scope key, so a shared browser profile cannot read another scope's
 * customers. Every read fails closed to empty/null: an unreadable cache must never look like an
 * authoritative "no customers" or "nothing owed" answer.
 */

const CUSTOMER_AAD_PREFIX = "customer-cache";
const CREDIT_AAD_PREFIX = "customer-credit-cache";

function customerAad(customerId: string): string {
  return `${CUSTOMER_AAD_PREFIX}|${customerId}`;
}

function creditAad(customerId: string): string {
  return `${CREDIT_AAD_PREFIX}|${customerId}`;
}

async function sealJson(scopeBinding: string, value: unknown, associatedData: string) {
  const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
  return encryptPayload(key, new TextEncoder().encode(JSON.stringify(value)), associatedData);
}

async function openSealed<T>(
  scopeBinding: string,
  envelope: { ciphertext: ArrayBuffer; iv: ArrayBuffer },
  associatedData: string,
): Promise<T | null> {
  try {
    const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
    const plaintext = await decryptPayload(key, envelope, associatedData);
    return JSON.parse(new TextDecoder().decode(plaintext)) as T;
  } catch {
    // Wrong scope key, tampered row, or corrupt envelope — drop the row rather than guess.
    return null;
  }
}

async function toCustomerRecord(
  scopeBinding: string,
  customer: PosCustomerListItem,
  cachedAtUtc: string,
): Promise<CachedCustomerRecord> {
  const envelope = await sealJson(scopeBinding, customer, customerAad(customer.customerId));
  return {
    customerId: customer.customerId,
    organizationId: customer.organizationId,
    status: customer.status,
    updatedAtUtc: customer.updatedAtUtc,
    cachedAtUtc,
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  };
}

/**
 * Merge a fetched page into the cache.
 *
 * This is a merge and not a replace: the customer list is searched and paged, so a filtered page
 * is not proof that the customers missing from it were deleted.
 */
export async function cacheCustomers(
  db: OfflineDb,
  scopeBinding: string,
  customers: ReadonlyArray<PosCustomerListItem>,
): Promise<void> {
  if (customers.length === 0) {
    return;
  }
  const cachedAtUtc = new Date().toISOString();
  const records = await Promise.all(
    customers.map((customer) => toCustomerRecord(scopeBinding, customer, cachedAtUtc)),
  );
  const tx = db.transaction("customers", "readwrite");
  for (const record of records) {
    await tx.store.put(record);
  }
  await tx.done;
}

export async function cacheCustomer(
  db: OfflineDb,
  scopeBinding: string,
  customer: PosCustomerListItem,
): Promise<void> {
  await cacheCustomers(db, scopeBinding, [customer]);
}

export async function listCachedCustomers(
  db: OfflineDb,
  scopeBinding: string,
): Promise<PosCustomerListItem[]> {
  let rows: CachedCustomerRecord[];
  try {
    rows = await db.getAll("customers");
  } catch {
    return [];
  }
  const decrypted = await Promise.all(
    rows.map((row) =>
      openSealed<PosCustomerListItem>(
        scopeBinding,
        { ciphertext: row.ciphertext, iv: row.iv },
        customerAad(row.customerId),
      ),
    ),
  );
  return decrypted.filter((customer): customer is PosCustomerListItem => customer != null);
}

export async function getCachedCustomer(
  db: OfflineDb,
  scopeBinding: string,
  customerId: string,
): Promise<PosCustomerListItem | null> {
  try {
    const row = await db.get("customers", customerId);
    if (!row) {
      return null;
    }
    return await openSealed<PosCustomerListItem>(
      scopeBinding,
      { ciphertext: row.ciphertext, iv: row.iv },
      customerAad(customerId),
    );
  } catch {
    return null;
  }
}

export async function cacheCustomerCreditSummary(
  db: OfflineDb,
  scopeBinding: string,
  summary: PosCustomerCreditSummary,
): Promise<void> {
  const envelope = await sealJson(scopeBinding, summary, creditAad(summary.customerId));
  const record: CachedCustomerCreditRecord = {
    customerId: summary.customerId,
    cachedAtUtc: new Date().toISOString(),
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  };
  const tx = db.transaction("customerCredit", "readwrite");
  await tx.store.put(record);
  await tx.done;
}

export async function getCachedCustomerCreditSummary(
  db: OfflineDb,
  scopeBinding: string,
  customerId: string,
): Promise<PosCustomerCreditSummary | null> {
  try {
    const row = await db.get("customerCredit", customerId);
    if (!row) {
      return null;
    }
    return await openSealed<PosCustomerCreditSummary>(
      scopeBinding,
      { ciphertext: row.ciphertext, iv: row.iv },
      creditAad(customerId),
    );
  } catch {
    return null;
  }
}

/** Local-first search over the cached list, matching the server's display name / mobile search. */
export function filterCachedCustomers(
  customers: ReadonlyArray<PosCustomerListItem>,
  options: { search?: string; status?: string } = {},
): PosCustomerListItem[] {
  const search = options.search?.trim().toLowerCase() ?? "";
  const status = options.status?.trim().toLowerCase() ?? "";
  return customers.filter((customer) => {
    if (status && customer.status.toLowerCase() !== status) {
      return false;
    }
    if (!search) {
      return true;
    }
    return (
      customer.displayName.toLowerCase().includes(search) ||
      (customer.mobileNumber ?? "").toLowerCase().includes(search)
    );
  });
}
