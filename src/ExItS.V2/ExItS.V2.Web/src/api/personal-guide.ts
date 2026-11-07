const GUIDE_KEY_PREFIX = "exits.v2.personal.guide.v1:";

type GuideState = {
  homeCardDismissed: boolean;
};

function storageKey(userId: string): string {
  return `${GUIDE_KEY_PREFIX}${userId}`;
}

export function readGuideHomeDismissed(userId: string | undefined): boolean {
  if (!userId || typeof window === "undefined") return false;
  try {
    const raw = window.localStorage.getItem(storageKey(userId));
    if (!raw) return false;
    const parsed = JSON.parse(raw) as GuideState;
    return parsed.homeCardDismissed === true;
  } catch {
    return false;
  }
}

export function dismissGuideHomeCard(userId: string | undefined): void {
  if (!userId || typeof window === "undefined") return;
  try {
    const next: GuideState = { homeCardDismissed: true };
    window.localStorage.setItem(storageKey(userId), JSON.stringify(next));
  } catch {
    // ignore
  }
}
