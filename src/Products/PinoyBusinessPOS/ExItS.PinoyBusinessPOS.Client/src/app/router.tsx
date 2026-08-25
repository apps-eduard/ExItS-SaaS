import { Navigate, createBrowserRouter } from "react-router-dom";
import { SessionGate } from "@/auth/SessionGate";
import { SignInPage } from "@/auth/SignInPage";
import { AppearancePage } from "@/features/appearance/AppearancePage";
import { AddLocalPersonPage } from "@/features/personal/AddLocalPersonPage";
import { AddPersonPage } from "@/features/personal/AddPersonPage";
import { HomePage } from "@/features/home/HomePage";
import { InvitationsPage } from "@/features/personal/InvitationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { PeoplePage } from "@/features/personal/PeoplePage";
import { PersonDetailPage } from "@/features/personal/PersonDetailPage";
import { PersonalShell } from "@/features/personal/PersonalShell";
import { AppShell } from "@/layouts/AppShell";

export const router = createBrowserRouter([
  {
    element: <SessionGate />,
    children: [
      { path: "/sign-in", element: <SignInPage /> },
      {
        path: "/",
        element: <AppShell />,
        children: [
          { index: true, element: <HomePage /> },
          { path: "appearance", element: <AppearancePage /> },
          { path: "*", element: <Navigate to="/" replace /> },
        ],
      },
      {
        path: "/personal",
        element: <PersonalShell />,
        children: [
          { index: true, element: <Navigate to="people" replace /> },
          { path: "people", element: <PeoplePage /> },
          { path: "people/add/local", element: <AddLocalPersonPage /> },
          { path: "people/add", element: <AddPersonPage /> },
          { path: "people/:contactId", element: <PersonDetailPage /> },
          { path: "invitations", element: <InvitationsPage /> },
          { path: "notifications", element: <NotificationsPage /> },
          { path: "*", element: <Navigate to="/personal/people" replace /> },
        ],
      },
    ],
  },
]);
