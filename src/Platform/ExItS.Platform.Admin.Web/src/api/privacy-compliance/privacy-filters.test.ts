import { describe, expect, it } from "vitest";
import {
  isImportantGap,
  matchesCategorySegment,
  matchesDocuments,
  matchesDpoNpc,
  matchesPias,
} from "@/api/privacy-compliance/privacy-filters";
import type { ComplianceRequirementDto } from "@/api/privacy-compliance/privacy-types";

function req(partial: Partial<ComplianceRequirementDto>): ComplianceRequirementDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    code: "DOC-1",
    title: "Sample",
    category: "Internal",
    description: "",
    requirementLevel: "Required",
    status: "Approved",
    ownerRole: "Platform",
    version: "1",
    effectiveDate: null,
    lastReviewedDate: null,
    nextReviewDate: null,
    notes: null,
    sourceReference: null,
    requiresDpoLegalVerification: false,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    evidenceCount: 0,
    ...partial,
  };
}

describe("privacy-filters", () => {
  it("matches document categories like Blazor", () => {
    expect(matchesDocuments(req({ category: "CustomerFacing" }), null)).toBe(true);
    expect(matchesDocuments(req({ category: "DataInventory" }), null)).toBe(false);
    expect(matchesDocuments(req({ category: "Internal" }), "Internal")).toBe(true);
    expect(matchesDocuments(req({ category: "Internal" }), "CustomerFacing")).toBe(false);
  });

  it("matches PIAs by category or code", () => {
    expect(matchesPias(req({ category: "PrivacyImpactAssessment" }))).toBe(true);
    expect(matchesPias(req({ category: "Internal", code: "PIA-CORE" }))).toBe(true);
    expect(matchesPias(req({ category: "Internal", code: "DOC-1" }))).toBe(false);
  });

  it("matches DPO/NPC including regulatory readiness", () => {
    expect(matchesDpoNpc(req({ category: "DpoNpc" }))).toBe(true);
    expect(matchesDpoNpc(req({ category: "RegulatoryReadiness" }))).toBe(true);
    expect(matchesDpoNpc(req({ category: "Internal" }))).toBe(false);
  });

  it("identifies important gaps without inventing ready state", () => {
    expect(isImportantGap(req({ requirementLevel: "Required", status: "NotStarted" }))).toBe(true);
    expect(isImportantGap(req({ requirementLevel: "Required", status: "NeedsUpdate" }))).toBe(true);
    expect(isImportantGap(req({ requirementLevel: "Required", status: "Approved" }))).toBe(false);
    expect(isImportantGap(req({ requirementLevel: "Recommended", status: "NotStarted" }))).toBe(
      false,
    );
  });

  it("routes category segments", () => {
    expect(matchesCategorySegment(req({ category: "Retention" }), "retention")).toBe(true);
    expect(matchesCategorySegment(req({ category: "VendorProcessor" }), "vendors")).toBe(true);
    expect(matchesCategorySegment(req({ category: "IncidentBreach" }), "incidents")).toBe(true);
  });
});
