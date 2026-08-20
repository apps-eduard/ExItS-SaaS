import { createBrowserRouter, Outlet } from "react-router-dom";
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
import { OrgStaffInvitePage } from "@/features/staff/OrgStaffInvitePage";
import { StaffInvitationAcceptPage } from "@/features/staff/StaffInvitationAcceptPage";
import { NoAccessibleBranchPage } from "@/features/workspace/NoAccessibleBranchPage";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { AppShell } from "@/layouts/AppShell";
import {
  AllowInvitationAccept,
  GuestOnly,
  RequireAdminExperience,
  RequireCashierRoleHome,
  RequireCreateSale,
  RequireInviteStaff,
  RequireManagerRoleHome,
  RequireOrganizationSession,
  RequireOwnerRoleHome,
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
        path: "/personal/invitations/accept",
        element: (
          <AllowInvitationAccept>
            <AppShell>
              <StaffInvitationAcceptPage />
            </AppShell>
          </AllowInvitationAccept>
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
                  <RequireOwnerRoleHome>
                    <OwnerRoleHomePage />
                  </RequireOwnerRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/manager",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireManagerRoleHome>
                    <ManagerRoleHomePage />
                  </RequireManagerRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/cashier",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireCashierRoleHome>
                    <CashierRoleHomePage />
                  </RequireCashierRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "org",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireAdminExperience>
                    <Outlet />
                  </RequireAdminExperience>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <OrgEssentialsPage /> },
              {
                path: "staff/invite",
                element: (
                  <RequireInviteStaff>
                    <OrgStaffInvitePage />
                  </RequireInviteStaff>
                ),
              },
            ],
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
