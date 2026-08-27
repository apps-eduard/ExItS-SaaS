import type { ReactNode } from "react";
import { Link2 } from "lucide-react";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import {
  MerchantOrderingBadge,
  storeDisplayInitial,
} from "@/features/customer-ordering/personal-commerce-ui";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type HeadingLevel = "h1" | "h2" | "h3";

export type PersonalStoreIdentityProps = {
  storeName: string;
  relationshipLabel?: string | null;
  canCustomerOrder: boolean;
  orderingPending?: boolean;
  headingLevel?: HeadingLevel;
  headingId?: string;
  connectionTestId?: string;
  extra?: ReactNode;
};

export function PersonalStoreIdentity({
  storeName,
  relationshipLabel,
  canCustomerOrder,
  orderingPending = false,
  headingLevel = "h2",
  headingId,
  connectionTestId = "personal-store-identity-connection",
  extra,
}: PersonalStoreIdentityProps) {
  const { t } = useI18n();
  const Heading = headingLevel;
  const name = storeName.trim() || "?";

  return (
    <div className="pc-store-card__top" data-testid="personal-store-identity">
      <span className="pc-store-card__avatar" aria-hidden>
        {storeDisplayInitial(name)}
      </span>
      <div className="pc-store-card__body">
        <Heading id={headingId} className="pc-store-card__name">
          {name}
        </Heading>
        {relationshipLabel ? (
          <p className="pc-store-card__linked-as m-0">
            <Link2 className="pc-store-card__link-icon size-3.5 shrink-0" aria-hidden />
            <span>{t("personal.merchants.linkedAs").replace("{name}", relationshipLabel)}</span>
          </p>
        ) : null}
        <div className="pc-store-card__badge-row">
          <ConnectionStatusChip
            state="Linked"
            audience="personal"
            className="pc-store-card__status"
            testId={connectionTestId}
          />
          <MerchantOrderingBadge available={canCustomerOrder} pending={orderingPending} />
        </div>
        {extra}
      </div>
    </div>
  );
}

export function PersonalStoreIdentityCard({
  className,
  children,
  testId,
  ...identity
}: PersonalStoreIdentityProps & {
  className?: string;
  children?: ReactNode;
  testId?: string;
}) {
  return (
    <section
      className={cn("pc-store-card pc-store-card--static exits-animate-panel", className)}
      data-testid={testId}
    >
      <PersonalStoreIdentity {...identity} />
      {children}
    </section>
  );
}
