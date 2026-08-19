import { Navigate, createBrowserRouter } from "react-router-dom";
import { SessionGate } from "@/auth/SessionGate";
import { SignInPage } from "@/auth/SignInPage";
import { AppearancePage } from "@/features/appearance/AppearancePage";
import { HomePage } from "@/features/home/HomePage";
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
    ],
  },
]);
