import { createContext, useContext, useMemo, type ReactNode } from "react";
import { catalogs, type MessageKey } from "@/i18n/messages";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

type I18nContextValue = {
  locale: LocalePreference;
  t: (key: MessageKey) => string;
};

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({
  locale,
  children,
}: {
  locale: LocalePreference;
  children: ReactNode;
}) {
  const value = useMemo<I18nContextValue>(
    () => ({
      locale,
      t: (key) => catalogs[locale][key],
    }),
    [locale],
  );

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error("useI18n must be used within I18nProvider");
  }
  return context;
}
