/**
 * Browser Web Crypto helpers for offline payload envelopes.
 * Not equivalent to native SecureStorage / Keystore / Keychain.
 */

export function toArrayBuffer(view: Uint8Array): ArrayBuffer {
  return view.buffer.slice(view.byteOffset, view.byteOffset + view.byteLength) as ArrayBuffer;
}

export async function sha256Hex(bytes: Uint8Array): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", toArrayBuffer(bytes));
  return [...new Uint8Array(digest)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

export async function importScopeAesKey(rawMaterial: Uint8Array): Promise<CryptoKey> {
  return crypto.subtle.importKey("raw", toArrayBuffer(rawMaterial), { name: "AES-GCM" }, false, [
    "encrypt",
    "decrypt",
  ]);
}

/**
 * LEGACY_MIGRATION only — FIX01 scope-derived key.
 * New writes must use the random DEK from local-store-key after PIN unlock.
 */
export async function deriveScopeKeyFromBinding(binding: string): Promise<CryptoKey> {
  const material = new TextEncoder().encode(`exits-offline-v1:${binding}`);
  const hash = new Uint8Array(await crypto.subtle.digest("SHA-256", toArrayBuffer(material)));
  return importScopeAesKey(hash);
}

export type EncryptedEnvelope = {
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

export type WrappedDekEnvelope = {
  ciphertext: Uint8Array;
  iv: ArrayBuffer;
};

export async function wrapDek(wrapKey: CryptoKey, rawDek: Uint8Array): Promise<WrappedDekEnvelope> {
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const ciphertext = await crypto.subtle.encrypt(
    {
      name: "AES-GCM",
      iv,
      additionalData: new TextEncoder().encode("exits-offline-dek-wrap:v1"),
    },
    wrapKey,
    toArrayBuffer(rawDek),
  );
  return { ciphertext: new Uint8Array(ciphertext), iv: toArrayBuffer(iv) };
}

export async function unwrapDek(wrapKey: CryptoKey, envelope: WrappedDekEnvelope): Promise<Uint8Array> {
  const plain = await crypto.subtle.decrypt(
    {
      name: "AES-GCM",
      iv: envelope.iv,
      additionalData: new TextEncoder().encode("exits-offline-dek-wrap:v1"),
    },
    wrapKey,
    toArrayBuffer(envelope.ciphertext),
  );
  return new Uint8Array(plain);
}

export async function encryptPayload(
  key: CryptoKey,
  plaintext: Uint8Array,
  associatedData: string,
): Promise<EncryptedEnvelope> {
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const ciphertext = await crypto.subtle.encrypt(
    {
      name: "AES-GCM",
      iv,
      additionalData: new TextEncoder().encode(associatedData),
    },
    key,
    toArrayBuffer(plaintext),
  );
  return { ciphertext, iv: toArrayBuffer(iv) };
}

export async function decryptPayload(
  key: CryptoKey,
  envelope: EncryptedEnvelope,
  associatedData: string,
): Promise<Uint8Array> {
  const plain = await crypto.subtle.decrypt(
    {
      name: "AES-GCM",
      iv: envelope.iv,
      additionalData: new TextEncoder().encode(associatedData),
    },
    key,
    envelope.ciphertext,
  );
  return new Uint8Array(plain);
}
