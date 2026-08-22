import {
  ArrowLeftRight,
  Banknote,
  MapPin,
  MonitorSmartphone,
  Package,
  QrCode,
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
import { PageHeader } from "@/components/exits/PageHeader";
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
    canDevices
      ? {
          to: "/org/business-qr",
          title: t("org.businessQr.title"),
          subtitle: t("org.action.businessQrDetail"),
          icon: QrCode,
          testId: "open-business-qr",
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

      {operations.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-group-operations">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
            {t("org.group.operations")}
          </h2>
          <div className="grid grid-cols-2 gap-3">
            {operations.map((action, index) => {
              const fullWidth =
                operations.length === 1 ||
                (operations.length % 2 === 1 && index === operations.length - 1);
              return (
                <ActionCard
                  key={action.to}
                  {...action}
                  className={fullWidth ? "col-span-2" : undefined}
                />
              );
            })}
          </div>
        </section>
      ) : null}

      {administration.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-group-administration">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
            {t("org.group.administration")}
          </h2>
          <div className="grid grid-cols-2 gap-3">
            {administration.map((action, index) => {
              const fullWidth =
                administration.length === 1 ||
                (administration.length % 2 === 1 && index === administration.length - 1);
              return (
                <ActionCard
                  key={action.to}
                  {...action}
                  className={fullWidth ? "col-span-2" : undefined}
                />
              );
            })}
          </div>
        </section>
      ) : null}

      <section className="flex flex-col gap-2" data-testid="org-group-workspace">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
          {t("org.group.workspace")}
        </h2>
        <div className="grid grid-cols-2 gap-3">
          {workspace.map((action, index) => {
            const fullWidth =
              workspace.length === 1 ||
              (workspace.length % 2 === 1 && index === workspace.length - 1);
            return (
              <ActionCard
                key={action.to}
                {...action}
                className={fullWidth ? "col-span-2" : undefined}
              />
            );
          })}
        </div>
      </section>
    </div>
  );
}
