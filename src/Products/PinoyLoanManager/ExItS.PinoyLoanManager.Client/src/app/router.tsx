import { createBrowserRouter } from "react-router-dom";
import { HomePage } from "@/features/home/HomePage";
import { AppShell } from "@/layouts/AppShell";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppShell />,
    children: [{ index: true, element: <HomePage /> }],
  },
]);
