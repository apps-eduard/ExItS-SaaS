const AUTH_VALUE_SENTINELS = [
  "sessionToken",
  "SessionToken",
  "access_token",
  "refresh_token",
] as const;

function scanStorage(storage: Storage, label: string): string[] {
  const hits: string[] = [];
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index) ?? "";
    const value = storage.getItem(key) ?? "";
    for (const sentinel of AUTH_VALUE_SENTINELS) {
      if (key.includes(sentinel) || value.includes(sentinel)) {
        hits.push(`${label}:${key}`);
      }
    }
  }
  return hits;
}

export function collectWebStorageAuthHits(): string[] {
  return [
    ...scanStorage(window.localStorage, "localStorage"),
    ...scanStorage(window.sessionStorage, "sessionStorage"),
  ];
}
