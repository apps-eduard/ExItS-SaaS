import { useCallback, useEffect, useMemo, useState } from "react";
import { PERSONAL_GUIDE_FEATURES } from "@/features/personal/guide/personal-guide-features";
import {
  applyPersonalGuideSession,
  knownLearnedCodes,
  loadPersonalGuideProgress,
  markPersonalGuideFeatureLearned,
  savePersonalGuideProgress,
  setPersonalGuideHideOnNextLogin,
  setPersonalGuideHomeCardDismissed,
  type PersonalGuideProgress,
} from "@/features/personal/guide/personal-guide-storage";

export function usePersonalGuideProgress(
  accountKey: string | null | undefined,
  sessionId: string | null | undefined = null,
) {
  const [progress, setProgress] = useState<PersonalGuideProgress>(() =>
    applyPersonalGuideSession(loadPersonalGuideProgress(accountKey), sessionId),
  );

  useEffect(() => {
    const stored = loadPersonalGuideProgress(accountKey);
    const loaded = applyPersonalGuideSession(stored, sessionId);
    setProgress(loaded);
    if (
      loaded.hideGuideAfterSessionId !== stored.hideGuideAfterSessionId
      || loaded.homeCardDismissed !== stored.homeCardDismissed
    ) {
      savePersonalGuideProgress(accountKey, loaded);
    }
  }, [accountKey, sessionId]);

  const persist = useCallback(
    (next: PersonalGuideProgress) => {
      setProgress(next);
      savePersonalGuideProgress(accountKey, next);
    },
    [accountKey],
  );

  const setLearned = useCallback(
    (code: string, learned: boolean) => {
      persist(markPersonalGuideFeatureLearned(progress, code, learned));
    },
    [persist, progress],
  );

  const setHomeCardDismissed = useCallback(
    (dismissed: boolean) => {
      persist(setPersonalGuideHomeCardDismissed(progress, dismissed));
    },
    [persist, progress],
  );

  const setHideOnNextLogin = useCallback(
    (hide: boolean, currentSessionId: string) => {
      persist(setPersonalGuideHideOnNextLogin(progress, hide, currentSessionId));
    },
    [persist, progress],
  );

  const learnedCodes = useMemo(() => knownLearnedCodes(progress.learned), [progress.learned]);
  const total = PERSONAL_GUIDE_FEATURES.length;
  const explored = learnedCodes.length;
  const percent = total === 0 ? 0 : Math.round((explored / total) * 100);

  return {
    progress,
    learnedCodes,
    explored,
    total,
    percent,
    homeCardDismissed: progress.homeCardDismissed,
    hideOnNextLogin: progress.hideGuideAfterSessionId != null
      && progress.hideGuideAfterSessionId === (sessionId?.trim() || null),
    setLearned,
    setHomeCardDismissed,
    setHideOnNextLogin,
    isLearned: (code: string) => learnedCodes.includes(code),
  };
}
