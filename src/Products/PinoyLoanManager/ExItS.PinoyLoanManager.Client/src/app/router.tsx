import { createBrowserRouter } from "react-router-dom";
import { RootLayout } from "@/app/RootLayout";
import { HomePage } from "@/features/home/HomePage";
import { SignInPage } from "@/features/sign-in/SignInPage";
import { AppShell } from "@/layouts/AppShell";
import { GuestOnly, RequireSession } from "@/session/SessionGuards";

export const router = createBrowserRouter([
  {
    element: <RootLayout />,
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
            <AppShell />
          </RequireSession>
        ),
        children: [{ index: true, element: <HomePage /> }],
      },
    ],
  },
]);
