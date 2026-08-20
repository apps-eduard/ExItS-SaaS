import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, type RenderOptions } from "@testing-library/react";
import type { ReactElement, ReactNode } from "react";
import { AppProviders } from "@/app/providers";
import { RootLayout } from "@/app/RootLayout";
import { FoundationPage } from "@/features/foundation/FoundationPage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";

function FoundationRoutes({ children }: { children?: ReactNode }) {
  return (
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route path="/" element={<RootLayout />}>
          <Route index element={children ?? <FoundationPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

export function renderApp(ui?: ReactElement, options?: Omit<RenderOptions, "wrapper">) {
  return render(ui ?? <FoundationPage />, {
    wrapper: ({ children }) => (
      <AppProviders>
        <FoundationRoutes>{children}</FoundationRoutes>
      </AppProviders>
    ),
    ...options,
  });
}

export function renderAt(path: string) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/" element={<RootLayout />}>
            <Route index element={<FoundationPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}
