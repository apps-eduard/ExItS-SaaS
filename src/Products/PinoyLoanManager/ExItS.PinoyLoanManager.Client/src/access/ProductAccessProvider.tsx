import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { accessKeys } from "@/access/access-keys";
import {
  ACCOUNT_SCOPE_DENIED_ERROR_CODE,
  PLM_PRODUCT_CODE,
} from "@/api/platform-auth/browser-session";
import {
  evaluateCurrentSessionProductAccess,
  listAccountProfiles,
  listEligibleOrganizations,
  selectAccountProfile,
  setOrganizationContext,
  type EffectiveProductAccess,
  type EligibleOrganization,
} from "@/api/platform-auth/platform-auth-client";
import { useSession } from "@/session/SessionProvider";

export type ProductAccessPhase =
  | "loading"
  | "account-scope"
  | "zero-organizations"
  | "select-organization"
  | "denied"
  | "subscription-inactive"
  | "error"
  | "allowed";

type ProductAccessContextValue = {
  phase: ProductAccessPhase;
  organizations: EligibleOrganization[];
  selectedOrganization: EligibleOrganization | null;
  access: EffectiveProductAccess | null;
  errorDetail: string | null;
  switching: boolean;
  retry: () => void;
  selectOrganization: (organizationId: string) => Promise<void>;
};

const ProductAccessContext = createContext<ProductAccessContextValue | null>(null);

function isOrganizationClass(accountClass?: string | null) {
  return (accountClass ?? "").toLowerCase() === "organization";
}

function isPersonalClass(accountClass?: string | null) {
  return (accountClass ?? "").toLowerCase() === "personal";
}

function isPlatformClass(accountClass?: string | null) {
  return (accountClass ?? "").toLowerCase() === "platform";
}

export function ProductAccessProvider({ children }: { children: ReactNode }) {
  const { session, refreshSession } = useSession();
  const queryClient = useQueryClient();
  const [switching, setSwitching] = useState(false);
  const autoSelectLock = useRef(false);
  const profileSwitchLock = useRef(false);

  const organizationsQuery = useQuery({
    queryKey: accessKeys.organizations,
    enabled: Boolean(session) && !isPlatformClass(session?.accountClass),
    queryFn: async () => {
      const result = await listEligibleOrganizations();
      if (!result.ok) {
        throw Object.assign(new Error(result.body?.errorCode ?? "organizations"), {
          status: result.status,
          errorCode: result.body?.errorCode,
          detail: result.body?.detail,
        });
      }
      return result.organizations;
    },
  });

  const organizations = organizationsQuery.data ?? [];
  const selectedId = session?.selectedOrganizationId ?? null;
  const selectedOrganization =
    organizations.find((organization) => organization.organizationId === selectedId) ?? null;

  const selectOrganization = useCallback(
    async (organizationId: string) => {
      setSwitching(true);
      queryClient.removeQueries({ queryKey: ["plm", "product-access"] });
      try {
        const result = await setOrganizationContext(organizationId);
        if (!result.ok) {
          throw Object.assign(new Error(result.body?.errorCode ?? "organization-context"), {
            status: result.status,
            errorCode: result.body?.errorCode,
            detail: result.body?.detail,
          });
        }
        await refreshSession();
      } finally {
        setSwitching(false);
      }
    },
    [queryClient, refreshSession],
  );

  useEffect(() => {
    if (!session || !isPersonalClass(session.accountClass) || profileSwitchLock.current) {
      return;
    }
    if (!organizationsQuery.isSuccess || organizations.length === 0) {
      return;
    }
    profileSwitchLock.current = true;
    void (async () => {
      try {
        const profiles = await listAccountProfiles();
        const organizationProfile = profiles.find(
          (profile) => (profile.accountClass ?? "").toLowerCase() === "organization",
        );
        if (organizationProfile) {
          const switched = await selectAccountProfile(organizationProfile.id);
          if (switched.ok) {
            await refreshSession();
          }
        }
      } finally {
        profileSwitchLock.current = false;
      }
    })();
  }, [organizations, organizationsQuery.isSuccess, refreshSession, session]);

  useEffect(() => {
    if (!session || !isOrganizationClass(session.accountClass) || autoSelectLock.current) {
      return;
    }
    if (!organizationsQuery.isSuccess || switching) {
      return;
    }
    if (organizations.length === 1 && !selectedOrganization) {
      autoSelectLock.current = true;
      void selectOrganization(organizations[0].organizationId).finally(() => {
        autoSelectLock.current = false;
      });
    }
  }, [
    organizations,
    organizationsQuery.isSuccess,
    selectOrganization,
    selectedOrganization,
    session,
    switching,
  ]);

  const accessQuery = useQuery({
    queryKey: accessKeys.effective(PLM_PRODUCT_CODE, selectedOrganization?.organizationId ?? null),
    enabled:
      Boolean(session) &&
      isOrganizationClass(session?.accountClass) &&
      Boolean(selectedOrganization) &&
      !switching,
    queryFn: async () => {
      const result = await evaluateCurrentSessionProductAccess(PLM_PRODUCT_CODE);
      if (!result.ok) {
        throw Object.assign(new Error(result.body?.errorCode ?? "product-access"), {
          status: result.status,
          errorCode: result.body?.errorCode,
          detail: result.body?.detail,
        });
      }
      return result.access;
    },
  });

  const retry = useCallback(() => {
    void organizationsQuery.refetch();
    void accessQuery.refetch();
  }, [accessQuery, organizationsQuery]);

  const phase = useMemo<ProductAccessPhase>(() => {
    if (!session || switching) {
      return "loading";
    }
    if (isPlatformClass(session.accountClass)) {
      return "account-scope";
    }
    if (organizationsQuery.isPending) {
      return "loading";
    }
    const orgError = organizationsQuery.error as { errorCode?: string; status?: number } | null;
    if (organizationsQuery.isError) {
      if (orgError?.errorCode === ACCOUNT_SCOPE_DENIED_ERROR_CODE || orgError?.status === 403) {
        return "account-scope";
      }
      return "error";
    }
    if (organizations.length === 0) {
      return "zero-organizations";
    }
    if (isPersonalClass(session.accountClass)) {
      return "loading";
    }
    if (!selectedOrganization) {
      return organizations.length > 1 ? "select-organization" : "loading";
    }
    if (accessQuery.isPending) {
      return "loading";
    }
    if (accessQuery.isError) {
      const accessError = accessQuery.error as { errorCode?: string; status?: number } | null;
      if (
        accessError?.errorCode === ACCOUNT_SCOPE_DENIED_ERROR_CODE ||
        accessError?.status === 403
      ) {
        return "account-scope";
      }
      return "error";
    }
    const access = accessQuery.data;
    if (!access) {
      return "loading";
    }
    if (access.allowed) {
      return "allowed";
    }
    if (access.reasonCode === "subscription_ineligible") {
      return "subscription-inactive";
    }
    return "denied";
  }, [
    accessQuery.data,
    accessQuery.error,
    accessQuery.isError,
    accessQuery.isPending,
    organizations.length,
    organizationsQuery.error,
    organizationsQuery.isError,
    organizationsQuery.isPending,
    selectedOrganization,
    session,
    switching,
  ]);

  const value = useMemo(
    () => ({
      phase,
      organizations,
      selectedOrganization,
      access: accessQuery.data ?? null,
      errorDetail:
        (organizationsQuery.error as { detail?: string } | null)?.detail ??
        (accessQuery.error as { detail?: string } | null)?.detail ??
        null,
      switching,
      retry,
      selectOrganization,
    }),
    [
      accessQuery.data,
      accessQuery.error,
      organizations,
      organizationsQuery.error,
      phase,
      retry,
      selectOrganization,
      selectedOrganization,
      switching,
    ],
  );

  return <ProductAccessContext.Provider value={value}>{children}</ProductAccessContext.Provider>;
}

export function useProductAccess() {
  const context = useContext(ProductAccessContext);
  if (!context) {
    throw new Error("useProductAccess must be used within ProductAccessProvider");
  }
  return context;
}
