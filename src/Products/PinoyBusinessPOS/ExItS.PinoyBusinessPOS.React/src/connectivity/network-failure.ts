/**
 * Classify genuine transport failures vs application/authorization errors.
 * 401/403 and other HTTP failures must not be treated as offline.
 */
export function isLikelyNetworkFailure(error: unknown): boolean {
  if (!(error instanceof Error)) {
    return false;
  }
  const name = error.name;
  const message = error.message.toLowerCase();
  if (name === "TypeError" || name === "NetworkError" || name === "AbortError") {
    return true;
  }
  return (
    message.includes("failed to fetch") ||
    message.includes("networkerror") ||
    message.includes("network request failed") ||
    message.includes("load failed")
  );
}
