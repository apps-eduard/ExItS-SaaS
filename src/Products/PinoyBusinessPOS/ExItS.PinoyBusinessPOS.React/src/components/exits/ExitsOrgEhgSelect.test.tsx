import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExitsOrgEhgSelect } from "@/components/exits/ExitsOrgEhgSelect";
import { ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-select-glass-global-default";

describe("ExitsOrgEhgSelect", () => {
  it("locks Org EHG select-glass recipe and selects an option", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ExitsOrgEhgSelect
        label="Status"
        value=""
        onChange={onChange}
        testId="ehg-status"
        options={[
          { value: "open", label: "Open" },
          { value: "closed", label: "Closed" },
        ]}
      />,
    );

    const field = screen.getByTestId("ehg-status-field");
    expect(field).toHaveAttribute("data-recipe", ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND);
    expect(field).toHaveAttribute("data-appearance", "stacked");
    expect(field).toHaveAttribute("data-size", "md");
    expect(field).toHaveAttribute("data-card-shape", "standard");

    await user.click(screen.getByTestId("ehg-status"));
    await user.click(screen.getByTestId("ehg-status-option-open"));
    expect(onChange).toHaveBeenCalledWith("open");
  });
});
