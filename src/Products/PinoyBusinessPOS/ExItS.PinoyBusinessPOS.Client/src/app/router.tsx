import { createBrowserRouter } from "react-router-dom";
import { SessionWorkspaceRoot } from "@/app/SessionWorkspaceRoot";
import { RootLayout } from "@/app/RootLayout";
import { SignInPage } from "@/features/auth/SignInPage";
import { HomePage } from "@/features/home/HomePage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";
import { PersonalHomePage } from "@/features/personal/PersonalHomePage";
import { PreferencesPage } from "@/features/preferences/PreferencesPage";
import { OrgEssentialsPage } from "@/features/role/OrgEssentialsPage";
import {
  CashierRoleHomePage,
  ManagerRoleHomePage,
  OwnerRoleHomePage,
} from "@/features/role/RoleHomePages";
import { SellFloorPage } from "@/features/sell/SellFloorPage";
import { NoAccessibleBranchPage } from "@/features/workspace/NoAccessibleBranchPage";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import {
  GuestOnly,
  RequireCreateSale,
  RequireOrganizationSession,
  RequirePersonalSession,
  RequireSession,
  RequireWorkspaceBound,
  WorkspaceBootGate,
} from "@/session/SessionGuards";

export const appRoutes = [
  {
    element: <SessionWorkspaceRoot />,
    children: [
      {
        path: "/sign-in",
        element: (
          <GuestOnly>
            <SignInPage />
          </GuestOnly>
        ),
      },
      {
        path: "/",
        element: (
          <RequireSession>
            <WorkspaceBootGate>
              <RootLayout />
            </WorkspaceBootGate>
          </RequireSession>
        ),
        children: [
          { index: true, element: <HomePage /> },
          {
            path: "workspace",
            element: (
              <RequireOrganizationSession>
                <WorkspaceChooserPage />
              </RequireOrganizationSession>
            ),
          },
          {
            path: "personal",
            element: (
              <RequirePersonalSession>
                <PersonalHomePage />
              </RequirePersonalSession>
            ),
          },
          {
            path: "no-location",
            element: (
              <RequireOrganizationSession>
                <NoAccessibleBranchPage />
              </RequireOrganizationSession>
            ),
          },
          { path: "settings/preferences", element: <PreferencesPage /> },
          {
            path: "sell",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireCreateSale>
                    <SellFloorPage />
                  </RequireCreateSale>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/owner",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <OwnerRoleHomePage />
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/manager",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <ManagerRoleHomePage />
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/cashier",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <CashierRoleHomePage />
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "org",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <OrgEssentialsPage />
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
