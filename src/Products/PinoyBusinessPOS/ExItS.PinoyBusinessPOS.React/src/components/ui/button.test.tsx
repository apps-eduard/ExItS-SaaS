import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { Button, buttonVariants } from "@/components/ui/button";

describe("Button shape and treatment pilots", () => {
  it("keeps default API visually equivalent to standard + flat", () => {
    const { rerender } = render(<Button type="button">Save</Button>);
    const plain = screen.getByRole("button", { name: "Save" }).className;
    rerender(
      <Button type="button" shape="standard" treatment="flat">
        Save
      </Button>,
    );
    expect(screen.getByRole("button", { name: "Save" }).className).toBe(plain);
    expect(plain).toContain("rounded-[var(--exits-radius-md)]");
    expect(plain).not.toContain("rounded-full");
    expect(plain).not.toContain("shadow-[var(--exits-shadow-sm)]");
  });

  it("renders soft and pill shapes", () => {
    const soft = buttonVariants({ shape: "soft" });
    const pill = buttonVariants({ shape: "pill" });
    expect(soft).toContain("rounded-[var(--exits-radius-soft)]");
    expect(pill).toContain("rounded-full");
  });

  it("renders elevated and gradient treatments", () => {
    const elevated = buttonVariants({ treatment: "elevated" });
    const gradientPrimary = buttonVariants({ variant: "default", treatment: "gradient" });
    expect(elevated).toContain("shadow-[var(--exits-shadow-sm)]");
    expect(elevated).toContain("hover:-translate-y-px");
    expect(gradientPrimary).toContain("bg-gradient-to-b");
    expect(gradientPrimary).toContain("from-[var(--exits-primary)]");
  });

  it("still supports existing variants and icon-only aria-label", () => {
    render(
      <>
        <Button type="button" variant="secondary">
          Cancel
        </Button>
        <Button type="button" variant="ghost">
          Back
        </Button>
        <Button type="button" variant="outline">
          Download
        </Button>
        <Button type="button" variant="destructive">
          Delete
        </Button>
        <Button type="button" variant="success">
          Approve
        </Button>
        <Button type="button" size="icon" aria-label="Edit" title="Edit">
          *
        </Button>
      </>,
    );
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Back" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Edit" })).toHaveAttribute("title", "Edit");
  });
});
