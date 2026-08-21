import { Building2, ChevronDown, Home, LogOut, RefreshCw, Settings, User } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { DropdownMenu, MenuHeader, MenuItem, MenuSeparator } from "@/components/ui/dropdown-menu";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";
import { ensurePersonalSessionProfile } from "@/session/ensure-personal-profile";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { resolveEffectivePosRoleCode } from "@/access/pos-capabilities";
import {
  deriveUserInitials,
  resolveFriendlyPosRole,
  resolveUserDisplayName,
  resolveUserSecondaryIdentity,
} from "@/lib/user-display";
import { cn } from "@/lib/cn";

type AccountMenuProps = {
  signingOut: boolean;
  onSignOut: () => void;
  compact?: boolean;
};

function experienceLabel(
  experience: string | undefined,
  t: (
    key: "experience.manageBusiness" | "experience.operations" | "experience.startSelling",
  ) => string,
): string | null {
  if (experience === "manage_business") {
    return t("experience.manageBusiness");
  }
  if (experience === "operations") {
    return t("experience.operations");
  }
  if (experience === "start_selling") {
    return t("experience.startSelling");
  }
  return null;
}

export function AccountMenu({ signingOut, onSignOut, compact = false }: AccountMenuProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { session, refreshSession } = useSession();
  const { sessionGrant, boundWorkspace, clearBoundWorkspace } = useWorkspace();
  const [open, setOpen] = useState(false);
  const [switchingPersonal, setSwitchingPersonal] = useState(false);

  const displayName = resolveUserDisplayName(session) || t("account.signedIn");
  const secondary = resolveUserSecondaryIdentity(session);
  const initials = deriveUserInitials(session);
  const friendlyRole = resolveFriendlyPosRole(resolveEffectivePosRoleCode(sessionGrant));
  const roleLabel =
    friendlyRole === "owner"
      ? t("account.role.owner")
      : friendlyRole === "manager"
        ? t("account.role.manager")
        : friendlyRole === "cashier"
          ? t("account.role.cashier")
          : null;
  const currentExperience = experienceLabel(boundWorkspace?.experience, t);
  const canReturnToPersonal =
    sessionAccountClass(session) === "Organization" && !isOrganizationContextLocked(session);

  const accountLabel = `${t("account.menu")}: ${displayName}`;

  const switchToPersonal = async () => {
    if (switchingPersonal) return;
    setSwitchingPersonal(true);
    try {
      const result = await ensurePersonalSessionProfile({ session, refreshSession });
      if (!result.ok) {
        return;
      }
      clearBoundWorkspace();
      navigate("/personal", { replace: true });
    } finally {
      setSwitchingPersonal(false);
    }
  };

  return (
    <DropdownMenu
      align="end"
      open={open}
      onOpenChange={setOpen}
      menuLabel={accountLabel}
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <button
          id={id}
          type="button"
          data-testid="account-menu-trigger"
          className={cn(
            "inline-flex min-h-[var(--exits-touch-target-min)] items-center gap-2 rounded-full border border-border bg-surface px-1.5 py-1 text-foreground transition-colors duration-[var(--exits-motion-fast)] hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            expanded && "bg-[var(--exits-surface-muted)]",
            !compact && "sm:pr-2.5",
          )}
          aria-haspopup="menu"
          aria-expanded={expanded}
          aria-controls={controls}
          aria-label={accountLabel}
          title={displayName}
          onClick={onClick}
          onKeyDown={onKeyDown}
        >
          <span
            className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary text-[length:var(--exits-text-xs)] font-bold text-primary-foreground"
            aria-hidden="true"
          >
            {initials ? initials : <User className="size-4" aria-hidden="true" />}
          </span>
          {!compact ? (
            <>
              <span className="hidden max-w-[9rem] truncate text-[length:var(--exits-text-sm)] font-semibold lg:inline">
                {displayName}
              </span>
              <ChevronDown
                className="hidden size-3.5 shrink-0 text-muted sm:block"
                aria-hidden="true"
              />
            </>
          ) : null}
        </button>
      )}
    >
      <MenuHeader>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {displayName}
        </p>
        {secondary ? (
          <p className="m-0 mt-0.5 truncate text-[length:var(--exits-text-xs)] text-muted">
            {secondary}
          </p>
        ) : null}
        {roleLabel ? (
          <p className="m-0 mt-1 truncate text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {roleLabel}
          </p>
        ) : null}
        {boundWorkspace ? (
          <div className="mt-1 space-y-0.5 text-[length:var(--exits-text-xs)] text-muted">
            <p className="m-0 truncate">
              {boundWorkspace.organizationDisplayName}
              {boundWorkspace.branchName ? ` · ${boundWorkspace.branchName}` : ""}
            </p>
            {currentExperience ? (
              <p className="m-0 truncate">
                {t("experience.current")}: {currentExperience}
              </p>
            ) : null}
          </div>
        ) : null}
      </MenuHeader>
      {boundWorkspace ? (
        <>
          <MenuItem
            onSelect={() => {
              setOpen(false);
              clearBoundWorkspace();
              navigate("/workspace");
            }}
          >
            <Building2 className="size-4 shrink-0" aria-hidden="true" />
            {t("workspace.switch")}
          </MenuItem>
          <MenuItem
            onSelect={() => {
              setOpen(false);
              clearBoundWorkspace();
              navigate("/workspace");
            }}
          >
            <RefreshCw className="size-4 shrink-0" aria-hidden="true" />
            {t("workspace.switchExperience")}
          </MenuItem>
          <MenuSeparator />
        </>
      ) : null}
      {canReturnToPersonal ? (
        <>
          <MenuItem
            disabled={switchingPersonal}
            onSelect={() => {
              setOpen(false);
              void switchToPersonal();
            }}
          >
            <Home className="size-4 shrink-0" aria-hidden="true" />
            {switchingPersonal ? t("account.switchingPersonal") : t("account.switchToPersonal")}
          </MenuItem>
          <MenuSeparator />
        </>
      ) : null}
      <MenuItem
        onSelect={() => {
          setOpen(false);
          navigate("/settings/preferences");
        }}
      >
        <Settings className="size-4 shrink-0" aria-hidden="true" />
        {t("topbar.preferences")}
      </MenuItem>
      <MenuSeparator />
      <MenuItem
        destructive
        disabled={signingOut}
        onSelect={() => {
          setOpen(false);
          onSignOut();
        }}
      >
        <LogOut className="size-4 shrink-0" aria-hidden="true" />
        {signingOut ? t("topbar.signingOut") : t("topbar.signOut")}
      </MenuItem>
    </DropdownMenu>
  );
}
