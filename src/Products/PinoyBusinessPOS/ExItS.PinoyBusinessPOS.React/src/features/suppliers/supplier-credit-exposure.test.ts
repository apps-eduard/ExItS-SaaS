import { describe, expect, it } from "vitest";
import {
  computeSupplierCreditExposure,
  countSupplierPayablesByFilter,
  filterSupplierPayables,
  formatUtilizationPercent,
} from "@/features/suppliers/supplier-credit-exposure";

describe("computeSupplierCreditExposure", () => {
  it("computes used/available/utilization for limit 30000 / outstanding 643 / reserved 747", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: 30000,
      outstanding: 643,
      reservedByActivePos: 747,
    });
    expect(exposure.usedCredit).toBe(1390);
    expect(exposure.availableCredit).toBe(28610);
    expect(exposure.utilizationPercent).toBeCloseTo(4.6333, 2);
    expect(formatUtilizationPercent(exposure.utilizationPercent!)).toBe("4.6");
    expect(exposure.progressPercent).toBeCloseTo(4.6, 1);
    expect(exposure.isOverLimit).toBe(false);
    expect(exposure.hasApprovedLimit).toBe(true);
  });

  it("returns 0% when limit exists and used is zero", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: 30000,
      outstanding: 0,
      reservedByActivePos: 0,
    });
    expect(exposure.usedCredit).toBe(0);
    expect(exposure.availableCredit).toBe(30000);
    expect(exposure.utilizationPercent).toBe(0);
    expect(exposure.progressPercent).toBe(0);
  });

  it("includes reservations in utilization and excludes them from payable concerns", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: 10000,
      outstanding: 0,
      reservedByActivePos: 2500,
    });
    expect(exposure.usedCredit).toBe(2500);
    expect(exposure.utilizationPercent).toBe(25);
    expect(exposure.availableCredit).toBe(7500);
  });

  it("handles over-limit with progress capped at 100 while preserving financials", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: 1000,
      outstanding: 800,
      reservedByActivePos: 400,
    });
    expect(exposure.usedCredit).toBe(1200);
    expect(exposure.availableCredit).toBe(-200);
    expect(exposure.utilizationPercent).toBe(120);
    expect(exposure.progressPercent).toBe(100);
    expect(exposure.isOverLimit).toBe(true);
  });

  it("does not divide by zero when there is no approved limit", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: null,
      outstanding: 500,
      reservedByActivePos: 100,
    });
    expect(exposure.hasApprovedLimit).toBe(false);
    expect(exposure.utilizationPercent).toBeNull();
    expect(exposure.progressPercent).toBeNull();
    expect(exposure.availableCredit).toBeNull();
    expect(exposure.usedCredit).toBe(600);
  });

  it("treats zero limit as no approved credit", () => {
    const exposure = computeSupplierCreditExposure({
      approvedCreditLimit: 0,
      outstanding: 10,
      reservedByActivePos: 0,
    });
    expect(exposure.hasApprovedLimit).toBe(false);
    expect(exposure.utilizationPercent).toBeNull();
  });
});

describe("filterSupplierPayables", () => {
  const rows = [
    { status: "Open", isOverdue: false, balance: 100 },
    { status: "PartiallyPaid", isOverdue: true, balance: 50 },
    { status: "Paid", isOverdue: false, balance: 0 },
    { status: "Voided", isOverdue: false, balance: 0 },
    { status: "Open", isOverdue: true, balance: 20 },
  ];

  it("filters open / overdue / paid / all with counts", () => {
    expect(filterSupplierPayables(rows, "open")).toHaveLength(3);
    expect(filterSupplierPayables(rows, "overdue")).toHaveLength(2);
    expect(filterSupplierPayables(rows, "paid")).toHaveLength(1);
    expect(filterSupplierPayables(rows, "all")).toHaveLength(5);
    expect(countSupplierPayablesByFilter(rows)).toEqual({
      open: 3,
      overdue: 2,
      paid: 1,
      all: 5,
    });
  });
});
