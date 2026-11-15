import { describe, expect, it } from "vitest";
import searchCss from "./SearchField.module.scss?raw";

describe("SearchField", () => {
  it("keeps the 2rem clear control and extends its touch area to 44px under 768px", () => {
    expect(searchCss).toContain("width: 2rem");
    expect(searchCss).toContain("height: 2rem");
    expect(searchCss).toMatch(/@media \(max-width: 767px\)[\s\S]*width:\s*44px;\s*height:\s*44px;/);
  });
});
