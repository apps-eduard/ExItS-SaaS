/**
 * Copy a UI Standards playground snippet to the clipboard.
 * Callers show toast on success/failure — no second toast system.
 */
export async function copyUiSnippet(snippet: string): Promise<boolean> {
  const text = snippet.trim();
  if (!text) {
    return false;
  }
  try {
    if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text);
      return true;
    }
  } catch {
    /* fall through */
  }
  try {
    const ta = document.createElement("textarea");
    ta.value = text;
    ta.setAttribute("readonly", "");
    ta.style.position = "fixed";
    ta.style.left = "-9999px";
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand("copy");
    document.body.removeChild(ta);
    return ok;
  } catch {
    return false;
  }
}

/** Build a compact multi-line JSX prop block from selected demo state. */
export function formatJsxProps(props: Record<string, string | boolean | undefined>): string {
  const lines: string[] = [];
  for (const [key, value] of Object.entries(props)) {
    if (value === undefined || value === false) {
      continue;
    }
    if (value === true) {
      lines.push(`  ${key}`);
      continue;
    }
    lines.push(`  ${key}=${JSON.stringify(value)}`);
  }
  return lines.join("\n");
}
