import { describe, expect, it } from "vitest";
import {
  INTENT_GLOBAL_INFO,
  INTENT_GLOBAL_SECONDARY,
  INTENT_GLOBAL_SUCCESS,
} from "@/features/ui-standards/intent-color-global-default";
import {
  TABLE_EXPORT_GLOBAL_COLOR,
  TABLE_EXPORT_GLOBAL_CSV_INTENT,
  TABLE_EXPORT_GLOBAL_EXCEL_INTENT,
  TABLE_EXPORT_GLOBAL_PDF_INTENT,
  TABLE_EXPORT_GLOBAL_PRINT_INTENT,
} from "@/features/ui-standards/table-export-global-default";

describe("Table export icon global default", () => {
  it("locks CSV, Excel, PDF, and Print intents", () => {
    expect(TABLE_EXPORT_GLOBAL_CSV_INTENT).toBe("primary");
    expect(TABLE_EXPORT_GLOBAL_EXCEL_INTENT).toBe("secondary");
    expect(TABLE_EXPORT_GLOBAL_PDF_INTENT).toBe("success");
    expect(TABLE_EXPORT_GLOBAL_PRINT_INTENT).toBe("info");
  });

  it("locks the icon colors", () => {
    expect(TABLE_EXPORT_GLOBAL_COLOR.csv).toBe("var(--exits-primary)");
    expect(TABLE_EXPORT_GLOBAL_COLOR.xlsx).toBe(INTENT_GLOBAL_SECONDARY);
    expect(TABLE_EXPORT_GLOBAL_COLOR.pdf).toBe(INTENT_GLOBAL_SUCCESS);
    expect(TABLE_EXPORT_GLOBAL_COLOR.print).toBe(INTENT_GLOBAL_INFO);
  });
});
