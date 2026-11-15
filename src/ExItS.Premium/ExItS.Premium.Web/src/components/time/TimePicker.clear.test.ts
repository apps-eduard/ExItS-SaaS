import { describe, expect, it } from "vitest";
import timePickerCss from "./TimePicker.module.scss?raw";

describe("Time and date-time clear", () => {
  it("keeps the 2rem clear control and extends its touch area to 44px under 768px", () => {
    expect(timePickerCss).toMatch(/\.clear\s*\{[^}]*width:\s*2rem;\s*height:\s*2rem;/);
    expect(timePickerCss).toMatch(
      /\.clear\s*\{[\s\S]*@media \(max-width: 767px\)\s*\{[^}]*&::before\s*\{[^}]*width:\s*44px;\s*height:\s*44px;/,
    );
  });
});
