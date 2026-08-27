import {
  PERSONAL_GUIDE_FEATURE_CODES,
  PERSONAL_GUIDE_SCHEMA_VERSION,
} from "@/features/personal/guide/personal-guide-features";

export const PERSONAL_GUIDE_STORAGE_PREFIX = "exits.personal.guide.v1:";

export type PersonalGuideProgress = {
  version: number;
  learned: string[];
  homeCardDismissed: boolean;
};

export const EMPTY_PERSONAL_GUIDE_PROGRESS: PersonalGuideProgress = {
  version: PERSONAL_GUIDE_SCHEMA_VERSION,
  learned: [],
  homeCardDismissed: false,
};

export function personalGuideStorageKey(accountKey: string): string {
  return `${PERSONAL_GUIDE_STORAGE_PREFIX}${accountKey}`;
}

function uniqueCodes(codes: string[]): string[] {
  const seen = new Set<string>();
  const result: string[] = [];
  for (const code of codes) {
    if (seen.has(code)) {
      continue;
    }
    seen.add(code);
    result.push(code);
  }
  return result;
}

/** Keep unknown codes in storage for forward compatibility; ignore them in UI. */
export function knownLearnedCodes(learned: readonly string[]): string[] {
  return uniqueCodes(learned.filter((code) => PERSONAL_GUIDE_FEATURE_CODES.has(code)));
}

export function parsePersonalGuideProgress(raw: unknown): PersonalGuideProgress {
  if (!raw || typeof raw !== "object") {
    return { ...EMPTY_PERSONAL_GUIDE_PROGRESS };
  }
  const doc = raw as Record<string, unknown>;
  if (doc.version !== PERSONAL_GUIDE_SCHEMA_VERSION) {
    return { ...EMPTY_PERSONAL_GUIDE_PROGRESS };
  }

  const learnedRaw = Array.isArray(doc.learned) ? doc.learned : [];
  const learned = uniqueCodes(
    learnedRaw.filter((code): code is string => typeof code === "string" && code.trim().length > 0),
  );

  return {
    version: PERSONAL_GUIDE_SCHEMA_VERSION,
    learned,
    homeCardDismissed: doc.homeCardDismissed === true,
  };
}

export function loadPersonalGuideProgress(accountKey: string | null | undefined): PersonalGuideProgress {
  if (!accountKey || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return { ...EMPTY_PERSONAL_GUIDE_PROGRESS };
  }
  try {
    const raw = window.localStorage.getItem(personalGuideStorageKey(accountKey));
    if (!raw) {
      return { ...EMPTY_PERSONAL_GUIDE_PROGRESS };
    }
    return parsePersonalGuideProgress(JSON.parse(raw) as unknown);
  } catch {
    return { ...EMPTY_PERSONAL_GUIDE_PROGRESS };
  }
}

export function savePersonalGuideProgress(
  accountKey: string | null | undefined,
  progress: PersonalGuideProgress,
): void {
  if (!accountKey || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    const payload: PersonalGuideProgress = {
      version: PERSONAL_GUIDE_SCHEMA_VERSION,
      learned: uniqueCodes(progress.learned),
      homeCardDismissed: progress.homeCardDismissed === true,
    };
    window.localStorage.setItem(personalGuideStorageKey(accountKey), JSON.stringify(payload));
  } catch {
    // Quota / private mode — fail soft.
  }
}

export function markPersonalGuideFeatureLearned(
  progress: PersonalGuideProgress,
  code: string,
  learned: boolean,
): PersonalGuideProgress {
  const next = new Set(progress.learned);
  if (learned) {
    next.add(code);
  } else {
    next.delete(code);
  }
  return {
    version: PERSONAL_GUIDE_SCHEMA_VERSION,
    learned: [...next],
    homeCardDismissed: progress.homeCardDismissed,
  };
}

export function setPersonalGuideHomeCardDismissed(
  progress: PersonalGuideProgress,
  dismissed: boolean,
): PersonalGuideProgress {
  return {
    version: PERSONAL_GUIDE_SCHEMA_VERSION,
    learned: [...progress.learned],
    homeCardDismissed: dismissed,
  };
}
