import { useCallback, useMemo } from "react";
import {
  useLocation,
  useNavigate,
  useSearchParams,
} from "react-router-dom";
import {
  buildReturnTo,
  canUseSafeHistoryBack,
  resolveSmartBackTo,
  takeSmartBackTo,
  type SmartBackFallbackKey,
  smartBackFallbacks,
} from "@/navigation/smart-back";

export type UseSmartBackOptions = {
  /** Canonical parent when no safe returnTo exists (bookmark / refresh cold). */
  fallback: string | SmartBackFallbackKey;
  backLabel: string;
  backTestId?: string;
  /**
   * When true (default), Back clears stored returnTo for this detail path.
   * Use false if the page only peeks at return without navigating.
   */
  clearOnNavigate?: boolean;
};

/**
 * Resolve PageHeader Back props for detail/edit pages.
 * Priority: explicit returnTo → safe history → canonical fallback.
 */
export function useSmartBack(options: UseSmartBackOptions): {
  backTo: string;
  backLabel: string;
  backTestId: string;
  goBack: () => void;
  hasExplicitReturnTo: boolean;
} {
  const location = useLocation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const fallback =
    options.fallback in smartBackFallbacks
      ? smartBackFallbacks[options.fallback as SmartBackFallbackKey]
      : options.fallback;

  const currentLocation = buildReturnTo(
    location.pathname,
    location.search,
    location.hash,
  );

  const resolved = useMemo(
    () =>
      resolveSmartBackTo({
        locationState: location.state,
        currentPathname: location.pathname,
        currentSearch: location.search,
        currentHash: location.hash,
        searchParams,
        fallback,
      }),
    [
      location.state,
      location.pathname,
      location.search,
      location.hash,
      searchParams,
      fallback,
    ],
  );

  const hasExplicitReturnTo = resolved !== fallback;

  const goBack = useCallback(() => {
    const clear = options.clearOnNavigate !== false;
    const target = clear
      ? takeSmartBackTo({
          locationState: location.state,
          currentPathname: location.pathname,
          currentSearch: location.search,
          currentHash: location.hash,
          searchParams,
          fallback,
        })
      : resolved;

    const useHistory = canUseSafeHistoryBack({
      hasExplicitReturnTo: target !== fallback,
      historyLength: typeof window !== "undefined" ? window.history.length : 0,
      referrer: typeof document !== "undefined" ? document.referrer : "",
      currentLocation,
    });

    if (target === fallback && useHistory) {
      navigate(-1);
      return;
    }

    navigate(target);
  }, [
    options.clearOnNavigate,
    location.state,
    location.pathname,
    location.search,
    location.hash,
    searchParams,
    fallback,
    resolved,
    currentLocation,
    hasExplicitReturnTo,
    navigate,
  ]);

  return {
    backTo: resolved,
    backLabel: options.backLabel,
    backTestId: options.backTestId ?? "page-header-back",
    goBack,
    hasExplicitReturnTo,
  };
}

/** Props to spread onto PageHeader for Smart Back. */
export function usePageSmartBack(options: UseSmartBackOptions): {
  backTo: string;
  backLabel: string;
  backTestId: string;
  onBack: () => void;
} {
  const smart = useSmartBack(options);
  return {
    backTo: smart.backTo,
    backLabel: smart.backLabel,
    backTestId: smart.backTestId,
    onBack: smart.goBack,
  };
}
