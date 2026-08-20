import { describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { ReactNode } from "react";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { SessionCartProvider, useSessionCart } from "@/cart/SessionCartProvider";

const sampleProduct = (id: string, price: number, name: string): PosCatalogProductDto => ({
  productId: id,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name,
  unitOfMeasure: "pc",
  sellingMode: "Unit",
  sellingPrice: price,
  status: "Active",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
});

function wrapper({ children }: { children: ReactNode }) {
  return <SessionCartProvider>{children}</SessionCartProvider>;
}

describe("SessionCartProvider", () => {
  it("adds, increments, decrements, removes, and totals lines", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });

    act(() => {
      result.current.addProduct(sampleProduct("p1", 10, "Item A"));
    });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.subtotal).toBe(10);

    act(() => {
      result.current.addProduct(sampleProduct("p1", 10, "Item A"));
    });
    expect(result.current.lineCount).toBe(2);
    expect(result.current.subtotal).toBe(20);

    act(() => {
      result.current.addProduct(sampleProduct("p2", 5, "Item B"), 2);
    });
    expect(result.current.lineCount).toBe(4);
    expect(result.current.subtotal).toBe(30);

    act(() => {
      result.current.decrementLine("p2");
    });
    expect(result.current.lineCount).toBe(3);
    expect(result.current.subtotal).toBe(25);

    act(() => {
      result.current.removeLine("p1");
    });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.subtotal).toBe(5);

    act(() => {
      result.current.clear();
    });
    expect(result.current.lines).toEqual([]);
    expect(result.current.lineCount).toBe(0);
    expect(result.current.subtotal).toBe(0);
  });

  it("keeps cart lines when category context changes externally", () => {
    const { result, rerender } = renderHook(
      ({ categoryId }: { categoryId: string }) => {
        void categoryId;
        return useSessionCart();
      },
      {
        wrapper,
        initialProps: { categoryId: "all" },
      },
    );

    act(() => {
      result.current.addProduct(sampleProduct("p1", 12, "Stable Item"));
    });

    rerender({ categoryId: "cat-drinks" });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.lines[0]?.name).toBe("Stable Item");

    rerender({ categoryId: "cat-snacks" });
    expect(result.current.lineCount).toBe(1);
  });
});
