/** Suggest a branch code from a display name (editable before create; server remains authoritative). */
export function suggestBranchCode(name: string): string {
  const cleaned = name
    .trim()
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-");

  if (!cleaned) {
    return "";
  }

  let code = cleaned.slice(0, 32);
  if (code.length === 1) {
    code = `${code}0`;
  }
  if (!/^[A-Z0-9]/.test(code)) {
    code = `B${code}`.slice(0, 32);
  }
  if (!/[A-Z0-9]$/.test(code)) {
    code = `${code.slice(0, 31)}0`;
  }
  return code;
}

/** User-facing lifecycle label: Inactive is shown as Suspended. */
export function normalizeBranchStatusFilter(status: string): "Active" | "Suspended" | "Archived" | "Other" {
  const value = status.trim().toLowerCase();
  if (value === "active") {
    return "Active";
  }
  if (value === "inactive" || value === "suspended") {
    return "Suspended";
  }
  if (value === "archived") {
    return "Archived";
  }
  return "Other";
}
