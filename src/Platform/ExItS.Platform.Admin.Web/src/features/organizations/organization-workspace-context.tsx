import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

export type OrganizationWorkspaceIdentity = {
  id: string;
  displayName: string;
};

type OrganizationWorkspaceContextValue = {
  identity: OrganizationWorkspaceIdentity | null;
  setIdentity: (identity: OrganizationWorkspaceIdentity | null) => void;
};

const OrganizationWorkspaceContext = createContext<OrganizationWorkspaceContextValue | null>(null);

export function OrganizationWorkspaceProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentity] = useState<OrganizationWorkspaceIdentity | null>(null);
  const value = useMemo(() => ({ identity, setIdentity }), [identity]);
  return (
    <OrganizationWorkspaceContext.Provider value={value}>
      {children}
    </OrganizationWorkspaceContext.Provider>
  );
}

export function useOrganizationWorkspaceIdentity() {
  return useContext(OrganizationWorkspaceContext);
}
