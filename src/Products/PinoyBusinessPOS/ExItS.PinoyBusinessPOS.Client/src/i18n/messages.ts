import { en } from "./locales/en";
import { filPH } from "./locales/fil-PH";
import { cebPH } from "./locales/ceb-PH";
import { iloPH } from "./locales/ilo-PH";
import { hilPH } from "./locales/hil-PH";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

export { en, filPH, cebPH, iloPH, hilPH };

export type MessageKey = keyof typeof en;

export const catalogs: Record<LocalePreference, Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
  "ceb-PH": cebPH,
  "ilo-PH": iloPH,
  "hil-PH": hilPH,
};

export const supportedLocales: LocalePreference[] = ["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"];
