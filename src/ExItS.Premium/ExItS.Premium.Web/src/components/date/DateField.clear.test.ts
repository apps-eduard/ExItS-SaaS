import { describe, expect, it } from "vitest";
import dateFieldCss from "./DateField.module.scss?raw";

describe("Date field clear", () => {
  it("keeps the 2rem clear control and extends its touch area to 44px under 768px", () => {
    expect(dateFieldCss).toMatch(/\.clear\s*\{[^}]*width:\s*2rem;\s*height:\s*2rem;/);
    expect(dateFieldCss).toMatch(
      /\.clear\s*\{[\s\S]*@media \(max-width: 767px\)\s*\{[^}]*&::before\s*\{[^}]*width:\s*44px;\s*height:\s*44px;/,
    );
  });
});
