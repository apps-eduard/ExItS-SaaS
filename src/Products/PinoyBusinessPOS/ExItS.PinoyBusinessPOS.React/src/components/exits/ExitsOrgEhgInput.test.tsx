import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExitsOrgEhgInput } from "@/components/exits/ExitsOrgEhgInput";
import {
  ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-input-glass-global-default";

describe("ExitsOrgEhgInput", () => {
  it("locks Org EHG input-glass recipe and updates value", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ExitsOrgEhgInput
        label="Name"
        value=""
        onChange={onChange}
        testId="ehg-name"
      />,
    );

    const field = screen.getByTestId("ehg-name-field");
    expect(field).toHaveAttribute("data-recipe", ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND);
    expect(field).toHaveAttribute("data-appearance", "stacked");
    expect(field).toHaveAttribute("data-size", "md");
    expect(field).toHaveAttribute("data-card-shape", "standard");
    expect(field).toHaveAttribute("data-has-icon", "false");

    await user.type(screen.getByTestId("ehg-name"), "Ana");
    expect(onChange).toHaveBeenCalled();
  });

  it("locks Org EHG search-glass icon-bare recipe", () => {
    render(
      <ExitsOrgEhgInput
        variant="search"
        label="Search"
        value=""
        onChange={() => undefined}
        clearable
        testId="ehg-search"
      />,
    );

    const field = screen.getByTestId("ehg-search-field");
    expect(field).toHaveAttribute("data-recipe", ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND);
    expect(field).toHaveAttribute("data-has-icon", "true");
    expect(field).toHaveAttribute("data-icon-look", "bare");
    expect(field).toHaveAttribute("data-icon", "icon-bare");
    /* text — avoids native search clear beside Org EHG __clear */
    expect(screen.getByTestId("ehg-search")).toHaveAttribute("type", "text");
    expect(field.querySelector(".org-ehg-qa-input-glass__field .org-ehg-qa-input-glass__label")).toBeTruthy();
  });
});
