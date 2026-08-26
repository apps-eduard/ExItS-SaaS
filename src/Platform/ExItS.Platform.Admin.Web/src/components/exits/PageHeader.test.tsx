import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";

describe("PageHeader", () => {
  it("renders a compact title, muted description, and aligned actions", () => {
    render(
      <PageHeader
        title="Overview"
        description="Monitor organizations, subscriptions and platform activity."
        actions={<Button type="button">Refresh</Button>}
      />,
    );

    expect(screen.getByRole("heading", { name: "Overview" })).toBeInTheDocument();
    expect(
      screen.getByText("Monitor organizations, subscriptions and platform activity."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Refresh" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Overview" })?.closest(".rounded-md")).toBeNull();
  });
});
