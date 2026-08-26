import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TooltipProvider } from "@/components/ui/tooltip";

describe("core primitive accessibility", () => {
  it("exposes accessible names for labeled input and button", async () => {
    const user = userEvent.setup();
    const onClick = () => undefined;
    render(
      <TooltipProvider>
        <Label htmlFor="org-name">Organization name</Label>
        <Input id="org-name" />
        <Button type="button" onClick={onClick}>
          Primary action
        </Button>
      </TooltipProvider>,
    );

    expect(screen.getByLabelText("Organization name")).toBeInTheDocument();
    const action = screen.getByRole("button", { name: "Primary action" });
    action.focus();
    expect(action).toHaveFocus();
    await user.keyboard("{Enter}");
  });
});
