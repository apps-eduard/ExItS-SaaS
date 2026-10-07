import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Check } from "lucide-react";
import {
  setCustomerOnlineOrderingAccess,
  type CustomerOnlineOrderingAccess,
  type PosCustomerListItem,
} from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { StatusChip } from "@/components/exits/StatusChip";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/cn";
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
  const selectedLabel = t(
    OPTIONS.find((option) => option.value === access)?.labelKey ??
      "customers.onlineOrdering.useStoreDefault",
  );

  const mutation = useMutation({
    mutationFn: (next: CustomerOnlineOrderingAccess) =>
      setCustomerOnlineOrderingAccess(workspace, customer.customerId, next),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });
  const locked = !canEdit || !online || mutation.isPending;

  return (
    <Card className="flex flex-col gap-3 p-4" data-testid="customer-online-ordering-access">
      <div className="flex min-w-0 flex-col gap-1">
        <h2 className="text-base font-semibold">{t("customers.onlineOrdering.title")}</h2>
        {access === "Default" ? (
          <p className="text-sm" data-testid="customer-online-ordering-value">
            {selectedLabel}
          </p>
        ) : null}
        <StatusChip
          tone={effectiveAllowed ? "success" : "danger"}
          appearance="emphasis"
          shape="square"
          className="w-fit self-start"
          data-testid="customer-online-ordering-effective"
        >
          {effectiveAllowed
            ? t("customers.onlineOrdering.allowed")
            : t("customers.onlineOrdering.blocked")}
        </StatusChip>
      </div>
      <div
        role="radiogroup"
        aria-label={t("customers.onlineOrdering.title")}
        className="grid grid-cols-3 gap-2"
        data-testid="customer-online-ordering-choices"
      >
        {OPTIONS.map((option) => {
          const selected = access === option.value;
          return (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={selected}
              disabled={locked}
              data-testid={`customer-online-ordering-${option.value.toLowerCase()}`}
              className={cn(
                "flex min-h-[var(--exits-row-min-height)] min-w-0 items-center gap-2 rounded-[var(--exits-control-radius)] border px-3 py-2.5 text-left text-[length:var(--exits-text-sm)] font-medium transition-[background-color,border-color,color,box-shadow] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60",
                selected
                  ? "border-primary bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] font-semibold text-foreground shadow-[inset_0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_35%,transparent)]"
                  : "border-border bg-background text-foreground hover:bg-[var(--exits-surface-muted)]",
              )}
              onClick={() => {
                if (!selected) mutation.mutate(option.value);
              }}
            >
              <span className="min-w-0 flex-1 wrap-break-word">{t(option.labelKey)}</span>
              {selected ? (
                <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
              ) : (
                <span className="size-4 shrink-0" aria-hidden="true" />
              )}
            </button>
          );
        })}
      </div>
      {mutation.isError ? (
        <p className="text-sm text-destructive" data-testid="customer-online-ordering-error">
          {mutation.error instanceof Error ? mutation.error.message : t("error.detail")}
        </p>
      ) : null}
    </Card>
  );
}
