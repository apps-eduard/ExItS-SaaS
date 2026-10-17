import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Bell, BellOff } from "lucide-react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";

import { ExitsCheckbox } from "@/components/exits/ExitsCheckbox";

function Controlled({ initial = true }: { initial?: boolean }) {
  const [checked, setChecked] = useState(initial);
  return (
    <ExitsCheckbox
      label="Notifications"
      icon={Bell}
      uncheckedIcon={BellOff}
      checked={checked}
      onCheckedChange={setChecked}
      testId="cb"
    />
  );
}

describe("ExitsCheckbox", () => {
  it("toggles an icon checkbox by click and keyboard", async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    const checkbox = screen.getByRole("checkbox", { name: "Notifications" });
    expect(checkbox).toHaveAttribute("aria-checked", "true");
    expect(checkbox).toHaveClass("exits-checkbox--icon");

    await user.click(checkbox);
    expect(checkbox).toHaveAttribute("aria-checked", "false");

    checkbox.focus();
    await user.keyboard(" ");
    expect(checkbox).toHaveAttribute("aria-checked", "true");
  });

  it("does not change when disabled", async () => {
    const user = userEvent.setup();
    const onCheckedChange = vi.fn();
    render(
      <ExitsCheckbox label="Show" checked={false} disabled onCheckedChange={onCheckedChange} />,
    );
    await user.click(screen.getByRole("checkbox", { name: "Show" }));
    expect(onCheckedChange).not.toHaveBeenCalled();
  });
});
