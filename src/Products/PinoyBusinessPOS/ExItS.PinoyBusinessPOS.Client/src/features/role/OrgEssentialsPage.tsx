import {
  ArrowLeftRight,
  Banknote,
  MapPin,
  MonitorSmartphone,
  Package,
  ShoppingCart,
  UserPlus,
} from "lucide-react";
import {
  canCreateSale,
  canInviteOrganizationStaff,
  canManageCatalog,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { ActionCard } from "@/components/exits/ActionCard";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgEssentialsPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const canInvite = canInviteOrganizationStaff(sessionGrant);
  const canSell = canCreateSale(sessionGrant);
  const canCatalog = canManageCatalog(sessionGrant);
  const canDevices = hasOrganizationManagementAuthority(sessionGrant);

  const operations = [
    canSell
      ? {
          to: "/sell",
          title: t("experience.startSelling"),
          subtitle: t("org.action.startSellingDetail"),
          icon: ShoppingCart,
          testId: "open-start-selling",
        }
      : null,
    canDevices
      ? {
          to: "/org/cash-handling",
          title: t("org.cashHandlingLink"),
          subtitle: t("org.cashHandlingLinkDetail"),
          icon: Banknote,
          testId: "open-cash-handling",
        }
      : null,
    canDevices
      ? {
          to: "/org/branches",
          title: t("org.branchesLink"),
          subtitle: t("org.action.branchesDetail"),
          icon: MapPin,
          testId: "open-branch-fulfillment",
        }
      : null,
    canCatalog
      ? {
          to: "/catalog",
          title: t("catalog.openCatalog"),
          subtitle: t("org.action.catalogDetail"),
          icon: Package,
          testId: "open-catalog",
        }
      : null,
  ].filter((item): item is NonNullable<typeof item> => item != null);

  const administration = [
    canInvite
      ? {
          to: "/org/staff/invite",
          title: t("staffInvite.title"),
          subtitle: t("org.action.inviteDetail"),
          icon: UserPlus,
          testId: "open-staff-invite",
        }
      : null,
    canDevices
      ? {
          to: "/org/devices",
          title: t("devices.listTitle"),
          subtitle: t("org.action.devicesDetail"),
          icon: MonitorSmartphone,
          testId: "open-org-devices",
        }
      : null,
  ].filter((item): item is NonNullable<typeof item> => item != null);

  const workspace = [
    {
      to: "/workspace",
      title: t("workspace.switch"),
      subtitle: t("org.action.workspaceDetail"),
      icon: ArrowLeftRight,
      testId: "open-switch-workspace",
    },
  ];

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="org-essentials-page">
      <PageHeader title={t("org.title")} description={t("org.lede")} />
      <StatusChip tone="warning">{t("org.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("org.body")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("org.noPayChrome")}
        </p>
      </Card>
      <EmptyState title={t("org.emptyTitle")} detail={t("org.emptyDetail")} />

      {operations.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-group-operations">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
            {t("org.group.operations")}
          </h2>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-2 lg:grid-cols-3">
            {operations.map((action) => (
              <ActionCard key={action.to} {...action} />
            ))}
          </div>
        </section>
      ) : null}

      {administration.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-group-administration">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
            {t("org.group.administration")}
          </h2>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-2 lg:grid-cols-3">
            {administration.map((action) => (
              <ActionCard key={action.to} {...action} />
            ))}
          </div>
        </section>
      ) : null}

      <section className="flex flex-col gap-2" data-testid="org-group-workspace">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
          {t("org.group.workspace")}
        </h2>
        <div className="grid grid-cols-2 gap-3 md:grid-cols-2 lg:grid-cols-3">
          {workspace.map((action) => (
            <ActionCard key={action.to} {...action} />
          ))}
        </div>
      </section>
    </div>
  );
}
