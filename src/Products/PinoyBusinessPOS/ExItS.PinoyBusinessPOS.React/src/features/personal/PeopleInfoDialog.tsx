import type { RefObject } from "react";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { useI18n } from "@/i18n/I18nProvider";

/** Anchored help panel under the People title. The arrow points at the info icon. */
export function PeopleInfoPopover({
  anchorRef,
}: {
  anchorRef: RefObject<HTMLElement | null>;
}) {
  const { t } = useI18n();

  return (
    <InfoPopover
      id="people-info-popover"
      titleId="people-info-title"
      title={t("people.info.title")}
      anchorRef={anchorRef}
    >
      <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("people.info.body1")}</p>
      <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("people.info.body2")}</p>
      <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("people.info.body3")}</p>
    </InfoPopover>
  );
}
