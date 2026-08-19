import { RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { router } from "@/app/router";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";

export function App() {
  return (
    <AppProviders>
      <PwaUpdateHost />
      <RouterProvider router={router} />
    </AppProviders>
  );
}
