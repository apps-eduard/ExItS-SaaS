import { createBrowserRouter } from "react-router-dom";
import { SessionWorkspaceRoot } from "@/app/SessionWorkspaceRoot";
import { RootLayout } from "@/app/RootLayout";
import { SignInPage } from "@/features/auth/SignInPage";
import { HomePage } from "@/features/home/HomePage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";
import { PersonalHomePage } from "@/features/personal/PersonalHomePage";
import { PreferencesPage } from "@/features/preferences/PreferencesPage";
import { NoAccessibleBranchPage } from "@/features/workspace/NoAccessibleBranchPage";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { GuestOnly, RequireSession, WorkspaceBootGate } from "@/session/SessionGuards";

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
          { path: "workspace", element: <WorkspaceChooserPage /> },
          { path: "personal", element: <PersonalHomePage /> },
          { path: "no-location", element: <NoAccessibleBranchPage /> },
          { path: "settings/preferences", element: <PreferencesPage /> },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
