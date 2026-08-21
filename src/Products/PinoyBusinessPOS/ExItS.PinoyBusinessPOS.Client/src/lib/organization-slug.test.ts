import { describe, expect, it } from "vitest";
import {
  ensureOrganizationSlug,
  isValidOrganizationSlugFormat,
  suggestOrganizationSlugFromDisplayName,
} from "@/lib/organization-slug";

describe("organization-slug", () => {
  it("suggests lowercase hyphenated slugs from display names", () => {
    expect(suggestOrganizationSlugFromDisplayName("Ana's Sari-Sari")).toBe("ana-s-sari-sari");
    expect(suggestOrganizationSlugFromDisplayName("  Cool Store  ")).toBe("cool-store");
  });

  it("validates slug format", () => {
    expect(isValidOrganizationSlugFormat("ab")).toBe(true);
    expect(isValidOrganizationSlugFormat("cool-store")).toBe(true);
    expect(isValidOrganizationSlugFormat("a")).toBe(false);
    expect(isValidOrganizationSlugFormat("-bad")).toBe(false);
    expect(isValidOrganizationSlugFormat("Bad")).toBe(false);
  });

  it("ensures a valid slug when suggestion is too short", () => {
    const slug = ensureOrganizationSlug("A");
    expect(isValidOrganizationSlugFormat(slug)).toBe(true);
  });
});
