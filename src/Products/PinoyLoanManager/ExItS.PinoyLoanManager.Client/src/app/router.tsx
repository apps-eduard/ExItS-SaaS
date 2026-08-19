import { createBrowserRouter } from "react-router-dom";
import { RootLayout } from "@/app/RootLayout";
import { ActivateAccountPage } from "@/features/auth/ActivateAccountPage";
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage";
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage";
import { SignUpPage } from "@/features/auth/SignUpPage";
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
        path: "/sign-up",
        element: (
          <GuestOnly>
            <SignUpPage />
          </GuestOnly>
        ),
      },
      {
        path: "/forgot-password",
        element: (
          <GuestOnly>
            <ForgotPasswordPage />
          </GuestOnly>
        ),
      },
      {
        path: "/activate-account",
        element: <ActivateAccountPage />,
      },
      {
        path: "/reset-password",
        element: <ResetPasswordPage />,
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
