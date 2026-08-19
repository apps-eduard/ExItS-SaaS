import { Navigate, createBrowserRouter } from "react-router-dom";
import { AppearancePage } from "@/features/foundation/AppearancePage";
import { FoundationHomePage } from "@/features/foundation/FoundationHomePage";
import { AppShell } from "@/layouts/AppShell";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppShell />,
    children: [
      { index: true, element: <FoundationHomePage /> },
      { path: "appearance", element: <AppearancePage /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
