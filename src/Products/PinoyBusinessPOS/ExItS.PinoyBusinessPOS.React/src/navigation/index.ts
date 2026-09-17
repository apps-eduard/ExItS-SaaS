export {
  buildReturnTo,
  canUseSafeHistoryBack,
  clearSmartBackReturnTo,
  isSafeAppReturnPath,
  linkStateWithReturn,
  navigateWithReturn,
  rememberSmartBackReturnTo,
  resolveSmartBackTo,
  smartBackFallbacks,
  smartBackNavigationState,
  takeSmartBackTo,
  type NavigateLike,
  type SmartBackFallbackKey,
  type SmartBackLocationState,
} from "@/navigation/smart-back";
export { AppBackButton, type AppBackButtonProps } from "@/navigation/AppBackButton";
export { AppLinkWithReturn, type AppLinkWithReturnProps } from "@/navigation/AppLinkWithReturn";
export {
  usePageSmartBack,
  useSmartBack,
  type UseSmartBackOptions,
} from "@/navigation/useSmartBack";
