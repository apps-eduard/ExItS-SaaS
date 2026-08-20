import { createBrowserRouter } from "react-router-dom";
import { RootLayout } from "@/app/RootLayout";
import { FoundationPage } from "@/features/foundation/FoundationPage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    children: [
      { index: true, element: <FoundationPage /> },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
