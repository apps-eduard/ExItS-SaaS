import { createContext, useContext, type ReactNode } from "react";
import { catalogs, type MessageKey } from "@/i18n/messages";
import { usePreferences } from "@/hooks/usePreferences";

const I18nContext = createContext<(key: MessageKey) => string>((key) => catalogs.en[key]);

export function I18nProvider({ children }: { children: ReactNode }) {
  const { preferences } = usePreferences();
  const catalog = catalogs[preferences.locale];
  return <I18nContext.Provider value={(key) => catalog[key]}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const t = useContext(I18nContext);
  return { t };
}
