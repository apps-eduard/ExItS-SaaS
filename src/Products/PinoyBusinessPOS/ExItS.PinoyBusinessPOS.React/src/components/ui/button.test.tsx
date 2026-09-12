import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { Button, buttonIconMotion, buttonVariants } from "@/components/ui/button";

describe("Button shape and treatment (locked standard)", () => {
  it("defaults to auto shape (control-radius) while explicit standard stays fixed", () => {
    render(<Button type="button">Save</Button>);
    const plain = screen.getByRole("button", { name: "Save" }).className;
    expect(plain).toContain("rounded-[var(--exits-control-radius)]");
    expect(plain).not.toContain("rounded-full");
    expect(plain).not.toContain("shadow-[var(--exits-shadow-sm)]");

    const standard = buttonVariants({ shape: "standard", treatment: "flat" });
    expect(standard).toContain("rounded-[var(--exits-radius-md)]");
    expect(standard).not.toContain("rounded-[var(--exits-control-radius)]");
  });

  it("renders soft, pill, and round shapes", () => {
    const soft = buttonVariants({ shape: "soft" });
    const pill = buttonVariants({ shape: "pill" });
    const round = buttonVariants({ shape: "round", size: "icon" });
    expect(soft).toContain("rounded-[var(--exits-radius-soft)]");
    expect(pill).toContain("rounded-full");
    expect(round).toContain("rounded-full");
    expect(round).toContain("size-[var(--exits-control-height)]");
  });

  it("applies base press feedback and reduced-motion guards", () => {
    const base = buttonVariants();
    expect(base).toContain("active:scale-[0.985]");
    expect(base).toContain("motion-reduce:active:scale-100");
    expect(base).toContain("group/button");
    const elevated = buttonVariants({ treatment: "elevated" });
    expect(elevated).toContain("hover:-translate-y-px");
    expect(elevated).toContain("motion-reduce:hover:translate-y-0");
  });

  it("renders elevated and gradient treatments", () => {
    const elevated = buttonVariants({ treatment: "elevated" });
    const gradientPrimary = buttonVariants({ variant: "default", treatment: "gradient" });
    expect(elevated).toContain("shadow-[var(--exits-shadow-sm)]");
    expect(elevated).toContain("hover:-translate-y-px");
    expect(gradientPrimary).toContain("bg-gradient-to-b");
    expect(gradientPrimary).toContain("from-[var(--exits-primary)]");
  });

  it("exposes contextual icon motion helpers without a second Button component", () => {
    expect(buttonIconMotion.continue).toContain("group-hover/button:translate-x-0.5");
    expect(buttonIconMotion.refresh).toContain("group-hover/button:rotate-[20deg]");
    expect(buttonIconMotion.continue).toContain("motion-reduce:");
  });

  it("still supports existing variants and round icon-only aria-label", () => {
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
        <Button type="button" size="icon" shape="round" aria-label="Edit" title="Edit">
          *
        </Button>
        <Button type="button" disabled>
          Disabled
        </Button>
      </>,
    );
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Back" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    const edit = screen.getByRole("button", { name: "Edit" });
    expect(edit).toHaveAttribute("title", "Edit");
    expect(edit.className).toContain("rounded-full");
    expect(screen.getByRole("button", { name: "Disabled" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Disabled" }).className).toContain(
      "disabled:scale-100",
    );
  });
});
