import { useCallback, useEffect, useMemo, useState } from "react";
import { PERSONAL_GUIDE_FEATURES } from "@/features/personal/guide/personal-guide-features";
import {
  knownLearnedCodes,
  loadPersonalGuideProgress,
  markPersonalGuideFeatureLearned,
  savePersonalGuideProgress,
  setPersonalGuideHomeCardDismissed,
  type PersonalGuideProgress,
} from "@/features/personal/guide/personal-guide-storage";

export function usePersonalGuideProgress(accountKey: string | null | undefined) {
  const [progress, setProgress] = useState<PersonalGuideProgress>(() =>
    loadPersonalGuideProgress(accountKey),
  );

  useEffect(() => {
    setProgress(loadPersonalGuideProgress(accountKey));
  }, [accountKey]);

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
    setLearned,
    setHomeCardDismissed,
    isLearned: (code: string) => learnedCodes.includes(code),
  };
}
