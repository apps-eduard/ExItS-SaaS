import { StatusIndicator } from "@/components/exits/StatusIndicator";

function toneForValue(value: string): "success" | "warning" | "danger" | "info" | "neutral" {
  const v = value.toLowerCase();
  if (
    v === "ready" ||
    v === "approved" ||
    v === "implemented" ||
    v === "complete" ||
    v === "completed"
  ) {
    return "success";
  }
  if (
    v === "actionneeded" ||
    v === "needsupdate" ||
    v === "notstarted" ||
    v === "partial" ||
    v === "inprogress" ||
    v === "required" ||
    v === "draft"
  ) {
    return "warning";
  }
  if (v === "rejected" || v === "blocked" || v === "failed") {
    return "danger";
  }
  if (v === "recommended" || v === "optional") {
    return "info";
  }
  return "neutral";
}

export function PrivacyStatusTag({ value }: { value: string }) {
  return <StatusIndicator label={value} tone={toneForValue(value)} />;
}
