import { type ReactNode } from "react";
import { Link, useLocation } from "react-router-dom";
import {
  ORGANIZATIONS_LIST_STATE_KEY,
  type OrganizationsLocationState,
} from "@/api/organizations/organization-id";
import { cn } from "@/lib/utils";

export function OrganizationWorkspaceLink({
  organizationId,
  className,
  children,
}: {
  organizationId: string;
  className?: string;
  children: ReactNode;
}) {
  const location = useLocation();
  const state: OrganizationsLocationState = {
    [ORGANIZATIONS_LIST_STATE_KEY]: location.search,
  };

  return (
    <Link
      className={cn("text-primary hover:underline focus-visible:outline-none", className)}
      to={`/admin/organizations/${organizationId}`}
      state={state}
    >
      {children}
    </Link>
  );
}
