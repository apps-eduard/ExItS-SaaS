import { useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Outlet, Route, Routes } from "react-router-dom";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";
import { RedirectIfAuthenticated } from "@/app/RedirectIfAuthenticated";
import { RequireSession } from "@/app/RequireSession";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AuthPlaceholderPage } from "@/features/auth/AuthPlaceholderPage";
import { SignInPage } from "@/features/auth/SignInPage";
import { ScaffoldPage } from "@/features/scaffold/ScaffoldPage";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { SessionProvider } from "@/hooks/use-session";
import { AuthLayout } from "@/layouts/AuthLayout";
import { RootLayout } from "@/layouts/RootLayout";

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        refetchOnWindowFocus: false,
      },
    },
  });
}

function AuthRoutes() {
  return (
    <AuthLayout>
      <Outlet />
    </AuthLayout>
  );
}

export function App() {
  const [queryClient] = useState(createQueryClient);

  return (
    <AppErrorBoundary>
      <PreferencesProvider>
        <TooltipProvider>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <SessionProvider>
                <Routes>
                  <Route element={<AuthRoutes />}>
                    <Route
                      path="/admin/login"
                      element={
                        <RedirectIfAuthenticated>
                          <SignInPage />
                        </RedirectIfAuthenticated>
                      }
                    />
                    <Route
                      path="/admin/forgot-password"
                      element={<AuthPlaceholderPage titleKey="auth.forgotPassword.title" />}
                    />
                    <Route
                      path="/admin/register"
                      element={<AuthPlaceholderPage titleKey="auth.createAccount.title" />}
                    />
                  </Route>
                  <Route element={<RootLayout />}>
                    <Route
                      path="/"
                      element={
                        <RequireSession>
                          <ScaffoldPage />
                        </RequireSession>
                      }
                    />
                  </Route>
                </Routes>
              </SessionProvider>
            </BrowserRouter>
          </QueryClientProvider>
        </TooltipProvider>
      </PreferencesProvider>
    </AppErrorBoundary>
  );
}
