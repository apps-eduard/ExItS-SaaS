import { Navigate, createBrowserRouter } from "react-router-dom";
import { SessionGate } from "@/auth/SessionGate";
import { SignInPage } from "@/auth/SignInPage";
import { AppearancePage } from "@/features/appearance/AppearancePage";
import { AddPersonPage } from "@/features/personal/AddPersonPage";
import { HomePage } from "@/features/home/HomePage";
import { InvitationsPage } from "@/features/personal/InvitationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { PeoplePage } from "@/features/personal/PeoplePage";
import { PersonDetailPage } from "@/features/personal/PersonDetailPage";
import { PersonalShell } from "@/features/personal/PersonalShell";

export const router = createBrowserRouter([
  {
    element: <SessionGate />,
    children: [
      { path: "/sign-in", element: <SignInPage /> },
      {
        path: "/",
        element: <PersonalShell />,
        children: [
          { index: true, element: <HomePage /> },
          { path: "appearance", element: <AppearancePage /> },
          { path: "personal/people", element: <PeoplePage /> },
          { path: "personal/people/add", element: <AddPersonPage /> },
          { path: "personal/people/:contactId", element: <PersonDetailPage /> },
          { path: "personal/invitations", element: <InvitationsPage /> },
          { path: "personal/notifications", element: <NotificationsPage /> },
          { path: "*", element: <Navigate to="/" replace /> },
        ],
      },
    ],
  },
]);
