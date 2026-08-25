import type { MessageKey } from "@/i18n/messages";
import {
  classifyCashCountMode,
  type CashCountModeKind,
} from "@/features/shifts/shift-cash-history";

export function cashCountModeMessageKey(mode: string | null | undefined): MessageKey {
  const kind: CashCountModeKind = classifyCashCountMode(mode);
  switch (kind) {
    case "Required":
      return "shift.cashCountModeRequired";
    case "Off":
      return "shift.cashCountModeOff";
    case "Optional":
    case "Unknown":
    default:
      return "shift.cashCountModeOptional";
  }
}
