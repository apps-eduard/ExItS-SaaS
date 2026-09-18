import { describe, expect, it } from "vitest";
import {
  isValidClassificationTotal,
  parseReturnQuantityInput,
  roundReturnQuantity,
} from "@/features/returns/return-classification";

describe("return-classification", () => {
  it("validates when sellable + damaged equals returned", () => {
    expect(isValidClassificationTotal(5, 3, 2)).toBe(true);
  });

  it("rejects when classification total differs", () => {
    expect(isValidClassificationTotal(5, 2, 2)).toBe(false);
  });

  it("rounds to three decimals for weighted quantities", () => {
    expect(roundReturnQuantity(1.23456)).toBe(1.235);
    expect(isValidClassificationTotal(1.25, 1.0, 0.2499)).toBe(true);
  });

  it("parses invalid quantity as zero", () => {
    expect(parseReturnQuantityInput("abc")).toBe(0);
    expect(parseReturnQuantityInput("-2")).toBe(0);
  });
});
