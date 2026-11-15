import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Skeleton } from "./Skeleton";

describe("Skeleton", () => {
  it("announces loading and accepts a specific label", () => {
    render(
      <>
        <Skeleton />
        <Skeleton label="Loading balance" />
      </>,
    );
    expect(screen.getByRole("status", { name: "Loading" })).toBeInTheDocument();
    expect(screen.getByRole("status", { name: "Loading balance" })).toBeInTheDocument();
  });
});
