import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import en from "./locales/en";

/**
 * Multilingual-ready architecture.
 * English only during V2 development.
 * Future locales (Tagalog, Cebuano, etc.) stay untranslated for now.
 * Language selection UI remains hidden.
 */
void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
  },
  lng: "en",
  fallbackLng: "en",
  supportedLngs: ["en"],
  interpolation: {
    escapeValue: false,
  },
  returnNull: false,
});

export default i18n;
