import { useState } from "react";
import { Settings2, ShieldCheck } from "lucide-react";
import { ExitsCheckbox } from "@/components/exits/ExitsCheckbox";
import { Button } from "@/components/ui/button";
import { Notice } from "@/components/exits/Notice";
import {
  useBlockContactMutation,
  usePersonalSharedUtangPreferenceQuery,
  useUnlinkContactMutation,
  useUpdatePersonalSharedUtangPreferenceMutation,
} from "@/features/personal/people-queries";
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";

type SharedUtangSettingPanelProps = {
  contactId: string;
  displayName: string;
  connectedAtUtc?: string | null;
  createdAtUtc: string;
  counterpartyUserIdentityId: string;
};

/** Shared Utang choices and connection safety, shared by the person page and the relationship dialog. */
export function SharedUtangSettingPanel({
  contactId,
  displayName,
  connectedAtUtc,
  createdAtUtc,
  counterpartyUserIdentityId,
}: SharedUtangSettingPanelProps) {
  const { t } = useI18n();
  const sharedPrefsQuery = usePersonalSharedUtangPreferenceQuery(counterpartyUserIdentityId);
  const updateSharedPrefs = useUpdatePersonalSharedUtangPreferenceMutation(counterpartyUserIdentityId);
  const unlinkContact = useUnlinkContactMutation();
  const blockContact = useBlockContactMutation();
  const [confirmUnlink, setConfirmUnlink] = useState(false);
  const [confirmBlock, setConfirmBlock] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  return (
    <div className="person-shared-card__columns">
      <div className="person-shared-card__col">
        <h3 className="person-ribbon-card__section-title" data-testid="person-detail-shared-settings-heading">
          <Settings2 className="size-4 shrink-0 text-primary" aria-hidden />
          {t("personal.utang.prefsSectionTitle")}
        </h3>
        {sharedPrefsQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
        ) : sharedPrefsQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.utang.prefsRequiresOnline")}
          </p>
        ) : (
          <div className="person-ribbon-card__checks">
            <ExitsCheckbox
              label={t("personal.utang.prefsReceive")}
              testId="person-detail-prefs-receive"
              checked={sharedPrefsQuery.data?.receiveSharedUtang ?? true}
              disabled={updateSharedPrefs.isPending}
              onCheckedChange={(receive) => {
                void updateSharedPrefs.mutateAsync({
                  receiveSharedUtang: receive,
                  autoAcceptSharedUtang: false,
                  sharedUtangNotifications: sharedPrefsQuery.data?.sharedUtangNotifications ?? true,
                  expectedVersion: sharedPrefsQuery.data?.version,
                });
              }}
            />
            <ExitsCheckbox
              label={t("personal.utang.prefsNotifications")}
              testId="person-detail-prefs-notifications"
              checked={sharedPrefsQuery.data?.sharedUtangNotifications ?? true}
              disabled={updateSharedPrefs.isPending}
              onCheckedChange={(notify) => {
                void updateSharedPrefs.mutateAsync({
                  receiveSharedUtang: sharedPrefsQuery.data?.receiveSharedUtang ?? true,
                  autoAcceptSharedUtang: false,
                  sharedUtangNotifications: notify,
                  expectedVersion: sharedPrefsQuery.data?.version,
                });
              }}
            />
          </div>
        )}
      </div>
      <div className="person-shared-card__divider" role="separator" aria-orientation="vertical" />
      <div className="person-shared-card__col">
        <h3 className="person-ribbon-card__section-title" data-testid="person-detail-connection-heading">
          <ShieldCheck className="size-4 shrink-0 text-primary" aria-hidden />
          {t("people.detail.connectionAndSafety")}
        </h3>
        <p className="m-0 text-muted">
          {t("people.detail.connectedSince").replace(
            "{date}",
            formatShortDate(connectedAtUtc ?? createdAtUtc),
          )}
        </p>
        {!confirmUnlink && !confirmBlock ? (
          <div className="flex min-w-0 flex-row flex-wrap gap-2">
            <Button
              type="button"
              intent="primary"
              appearance="solid"
              emphasis="soft"
              className="min-w-0 flex-1"
              data-testid="person-detail-unlink"
              onClick={() => {
                setConfirmBlock(false);
                setConfirmUnlink(true);
              }}
            >
              {t("people.detail.unlink")}
            </Button>
            <Button
              type="button"
              intent="danger"
              appearance="solid"
              emphasis="soft"
              className="min-w-0 flex-1"
              data-testid="person-detail-block"
              onClick={() => {
                setConfirmUnlink(false);
                setConfirmBlock(true);
              }}
            >
              {t("people.detail.block")}
            </Button>
          </div>
        ) : null}
        {confirmUnlink ? (
          <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">
            <p className="m-0 font-semibold">
              {t("people.detail.unlinkConfirmTitle").replace("{name}", displayName)}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("people.detail.unlinkConfirmBody")}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" onClick={() => setConfirmUnlink(false)}>
                {t("people.add.cancel")}
              </Button>
              <Button
                type="button"
                intent="primary"
                appearance="solid"
                emphasis="soft"
                disabled={unlinkContact.isPending}
                onClick={() => {
                  setActionError(null);
                  void unlinkContact
                    .mutateAsync(contactId)
                    .then(() => setConfirmUnlink(false))
                    .catch((err) => {
                      setActionError(err instanceof Error ? err.message : t("error.body"));
                    });
                }}
              >
                {t("people.detail.unlinkConfirmAction")}
              </Button>
            </div>
          </div>
        ) : null}
        {confirmBlock ? (
          <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("people.detail.blockConfirmBody")}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" onClick={() => setConfirmBlock(false)}>
                {t("people.add.cancel")}
              </Button>
              <Button
                type="button"
                intent="danger"
                appearance="solid"
                emphasis="soft"
                disabled={blockContact.isPending}
                onClick={() => {
                  setActionError(null);
                  void blockContact
                    .mutateAsync(contactId)
                    .then(() => setConfirmBlock(false))
                    .catch((err) => {
                      setActionError(err instanceof Error ? err.message : t("error.body"));
                    });
                }}
              >
                {t("people.detail.block")}
              </Button>
            </div>
          </div>
        ) : null}
        {actionError ? (
          <Notice tone="danger" testId="person-detail-shared-action-error">
            {actionError}
          </Notice>
        ) : null}
      </div>
    </div>
  );
}
