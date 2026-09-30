import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  setCustomerOnlineOrderingAccess,
  type CustomerOnlineOrderingAccess,
  type PosCustomerListItem,
} from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

type Props = {
  workspace: PosWorkspaceScope;
  customer: PosCustomerListItem;
  canEdit: boolean;
  online: boolean;
};

const OPTIONS: { value: CustomerOnlineOrderingAccess; labelKey: MessageKey }[] = [
  { value: "Default", labelKey: "customers.onlineOrdering.useStoreDefault" },
  { value: "Allowed", labelKey: "customers.onlineOrdering.allowed" },
  { value: "Blocked", labelKey: "customers.onlineOrdering.blocked" },
];

export function CustomerOnlineOrderingAccessSection({
  workspace,
  customer,
  canEdit,
  online,
}: Props) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const access = (customer.onlineOrderingAccess ?? "Default") as CustomerOnlineOrderingAccess;
  const effectiveAllowed = access !== "Blocked";

  const mutation = useMutation({
    mutationFn: (next: CustomerOnlineOrderingAccess) =>
      setCustomerOnlineOrderingAccess(workspace, customer.customerId, next),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });

  return (
    <Card className="flex flex-col gap-3 p-4" data-testid="customer-online-ordering-access">
      <div>
        <h2 className="text-base font-semibold">{t("customers.onlineOrdering.title")}</h2>
        <p className="text-sm text-muted-foreground" data-testid="customer-online-ordering-effective">
          {effectiveAllowed
            ? t("customers.onlineOrdering.effectiveAllowed")
            : t("customers.onlineOrdering.effectiveBlocked")}
        </p>
      </div>
      <fieldset disabled={!canEdit || !online || mutation.isPending} className="flex flex-col gap-2">
        <legend className="sr-only">{t("customers.onlineOrdering.title")}</legend>
        {OPTIONS.map((option) => (
          <label key={option.value} className="flex items-center gap-2 text-sm">
            <input
              type="radio"
              name="online-ordering-access"
              value={option.value}
              checked={access === option.value}
              data-testid={`customer-online-ordering-${option.value.toLowerCase()}`}
              onChange={() => mutation.mutate(option.value)}
            />
            {t(option.labelKey)}
          </label>
        ))}
      </fieldset>
      {mutation.isError ? (
        <p className="text-sm text-destructive" data-testid="customer-online-ordering-error">
          {mutation.error instanceof Error ? mutation.error.message : t("error.detail")}
        </p>
      ) : null}
    </Card>
  );
}
