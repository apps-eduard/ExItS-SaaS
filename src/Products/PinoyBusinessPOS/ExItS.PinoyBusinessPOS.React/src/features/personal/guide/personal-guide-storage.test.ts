import { afterEach, describe, expect, it } from "vitest";
import { PERSONAL_GUIDE_SCHEMA_VERSION } from "@/features/personal/guide/personal-guide-features";
import {
  EMPTY_PERSONAL_GUIDE_PROGRESS,
  knownLearnedCodes,
  loadPersonalGuideProgress,
  markPersonalGuideFeatureLearned,
  parsePersonalGuideProgress,
  personalGuideStorageKey,
  savePersonalGuideProgress,
  setPersonalGuideHomeCardDismissed,
} from "@/features/personal/guide/personal-guide-storage";

const USER_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

describe("personal-guide-storage", () => {
  afterEach(() => {
    window.localStorage.clear();
  });

  it("returns empty progress when storage is empty", () => {
    expect(loadPersonalGuideProgress(USER_A)).toEqual(EMPTY_PERSONAL_GUIDE_PROGRESS);
  });

  it("restores valid progress and marks learned", () => {
    let progress = markPersonalGuideFeatureLearned(EMPTY_PERSONAL_GUIDE_PROGRESS, "stores", true);
    savePersonalGuideProgress(USER_A, progress);
    const restored = loadPersonalGuideProgress(USER_A);
    expect(restored.learned).toEqual(["stores"]);
    progress = markPersonalGuideFeatureLearned(restored, "stores", false);
    savePersonalGuideProgress(USER_A, progress);
    expect(loadPersonalGuideProgress(USER_A).learned).toEqual([]);
  });

  it("fails safe on malformed JSON and wrong schema version", () => {
    window.localStorage.setItem(personalGuideStorageKey(USER_A), "{not-json");
    expect(loadPersonalGuideProgress(USER_A)).toEqual(EMPTY_PERSONAL_GUIDE_PROGRESS);
    expect(parsePersonalGuideProgress({ version: 99, learned: ["stores"] })).toEqual(
      EMPTY_PERSONAL_GUIDE_PROGRESS,
    );
    expect(parsePersonalGuideProgress(null)).toEqual(EMPTY_PERSONAL_GUIDE_PROGRESS);
    expect(parsePersonalGuideProgress({ version: PERSONAL_GUIDE_SCHEMA_VERSION, learned: "stores" })).toEqual(
      EMPTY_PERSONAL_GUIDE_PROGRESS,
    );
  });

  it("ignores unknown feature codes for progress and normalizes duplicates", () => {
    const parsed = parsePersonalGuideProgress({
      version: 1,
      learned: ["stores", "stores", "bnpl-future", "people"],
      homeCardDismissed: true,
    });
    expect(parsed.learned).toEqual(["stores", "bnpl-future", "people"]);
    expect(knownLearnedCodes(parsed.learned)).toEqual(["stores", "people"]);
    expect(parsed.homeCardDismissed).toBe(true);
  });

  it("isolates guide progress across accounts", () => {
    savePersonalGuideProgress(USER_A, {
      version: 1,
      learned: ["stores"],
      homeCardDismissed: false,
    });
    expect(loadPersonalGuideProgress(USER_B).learned).toEqual([]);
    expect(loadPersonalGuideProgress(USER_A).learned).toEqual(["stores"]);
  });

  it("persists home card dismissal and restore", () => {
    const dismissed = setPersonalGuideHomeCardDismissed(EMPTY_PERSONAL_GUIDE_PROGRESS, true);
    savePersonalGuideProgress(USER_A, dismissed);
    expect(loadPersonalGuideProgress(USER_A).homeCardDismissed).toBe(true);
    savePersonalGuideProgress(
      USER_A,
      setPersonalGuideHomeCardDismissed(loadPersonalGuideProgress(USER_A), false),
    );
    expect(loadPersonalGuideProgress(USER_A).homeCardDismissed).toBe(false);
  });

  it("does not persist without an account key", () => {
    savePersonalGuideProgress(null, {
      version: 1,
      learned: ["stores"],
      homeCardDismissed: true,
    });
    expect(window.localStorage.length).toBe(0);
  });
});
