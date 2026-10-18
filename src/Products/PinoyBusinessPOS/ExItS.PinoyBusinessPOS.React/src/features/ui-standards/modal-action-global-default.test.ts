import { describe, expect, it } from "vitest";
import {
  MODAL_GLOBAL_ADD_SAVE_ICON,
  MODAL_GLOBAL_CANCEL_APPEARANCE,
  MODAL_GLOBAL_CANCEL_EMPHASIS,
  MODAL_GLOBAL_CANCEL_INTENT,
  MODAL_GLOBAL_EDIT_SAVE_ICON,
  MODAL_GLOBAL_SAVE_APPEARANCE,
  MODAL_GLOBAL_SAVE_ICON,
  MODAL_GLOBAL_SAVE_INTENT,
} from "@/features/ui-standards/modal-action-global-default";

describe("Modal action global default", () => {
  it("locks Cancel to danger glossy and Save to primary glossy", () => {
    expect(MODAL_GLOBAL_CANCEL_INTENT).toBe("danger");
    expect(MODAL_GLOBAL_CANCEL_APPEARANCE).toBe("glossy");
    expect(MODAL_GLOBAL_CANCEL_EMPHASIS).toBe("strong");
    expect(MODAL_GLOBAL_SAVE_INTENT).toBe("primary");
    expect(MODAL_GLOBAL_SAVE_APPEARANCE).toBe("glossy");
  });

  it("locks the Save icon by action", () => {
    expect(MODAL_GLOBAL_SAVE_ICON).toBe("save-check");
    expect(MODAL_GLOBAL_EDIT_SAVE_ICON).toBe("save-pen");
    expect(MODAL_GLOBAL_ADD_SAVE_ICON).toBe("save-plus");
  });
});
