import { useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router-dom";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";
import { RedirectIfAuthenticated } from "@/app/RedirectIfAuthenticated";
import { RequireSession } from "@/app/RequireSession";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AuthPlaceholderPage } from "@/features/auth/AuthPlaceholderPage";
import { SignInPage } from "@/features/auth/SignInPage";
import { OrganizationBranchesPage } from "@/features/organizations/OrganizationBranchesPage";
import { OrganizationPeoplePage } from "@/features/organizations/OrganizationPeoplePage";
import { OrganizationProductsPage } from "@/features/organizations/OrganizationProductsPage";
import { OrganizationBillingPage } from "@/features/organizations/OrganizationBillingPage";
import { OrganizationActivityPage } from "@/features/organizations/OrganizationActivityPage";
import { OrganizationEntitlementsPage } from "@/features/organizations/OrganizationEntitlementsPage";
import { OrganizationSubscriptionsPage } from "@/features/organizations/OrganizationSubscriptionsPage";
import { OrganizationOverviewPage } from "@/features/organizations/OrganizationOverviewPage";
import { OrganizationsPage } from "@/features/organizations/OrganizationsPage";
import { OrganizationWorkspaceLayout } from "@/features/organizations/OrganizationWorkspaceLayout";
import { ProductsPage } from "@/features/products/ProductsPage";
import { ProductDetailPage } from "@/features/products/ProductDetailPage";
import { PlansPage } from "@/features/plans/PlansPage";
import { PlanDetailPage } from "@/features/plans/PlanDetailPage";
import { SubscriptionsPage } from "@/features/subscriptions/SubscriptionsPage";
import { SubscriptionDetailPage } from "@/features/subscriptions/SubscriptionDetailPage";
import { PaymentsPage } from "@/features/payments/PaymentsPage";
import { PaymentDetailPage } from "@/features/payments/PaymentDetailPage";
import { EntitlementsPortfolioPage } from "@/features/entitlements/EntitlementsPortfolioPage";
import { OverviewPage } from "@/features/overview/OverviewPage";
import { UsersPage } from "@/features/users/UsersPage";
import { UserDetailPage } from "@/features/users/UserDetailPage";
import { ShellCatchAllPage } from "@/features/overview/ShellCatchAllPage";
import { AuthorizationProvider } from "@/hooks/use-authorization";
import { DiagnosticsProvider } from "@/hooks/use-diagnostics";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { SessionProvider, useSession } from "@/hooks/use-session";
import { AppShell } from "@/layouts/AppShell";
import { AuthLayout } from "@/layouts/AuthLayout";

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

function ProtectedShell() {
  return (
    <RequireSession>
      <AuthorizedAppShell />
    </RequireSession>
  );
}

function AuthorizedAppShell() {
  const { session } = useSession();
  return (
    <AuthorizationProvider key={session?.userId ?? "session"}>
      <AppShell />
    </AuthorizationProvider>
  );
}

export function App() {
  const [queryClient] = useState(createQueryClient);

  return (
    <PreferencesProvider>
      <AppErrorBoundary>
        <TooltipProvider>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <DiagnosticsProvider>
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
                    <Route element={<ProtectedShell />}>
                      <Route path="/" element={<Navigate to="/admin" replace />} />
                      <Route path="/admin" element={<Outlet />}>
                        <Route index element={<OverviewPage />} />
                        <Route path="organizations">
                          <Route index element={<OrganizationsPage />} />
                          <Route path=":organizationId" element={<OrganizationWorkspaceLayout />}>
                            <Route index element={<OrganizationOverviewPage />} />
                            <Route path="branches" element={<OrganizationBranchesPage />} />
                            <Route path="people" element={<OrganizationPeoplePage />} />
                            <Route path="products" element={<OrganizationProductsPage />} />
                            <Route
                              path="subscription"
                              element={<OrganizationSubscriptionsPage />}
                            />
                            <Route path="entitlements" element={<OrganizationEntitlementsPage />} />
                            <Route path="billing" element={<OrganizationBillingPage />} />
                            <Route path="activity" element={<OrganizationActivityPage />} />
                            <Route path="*" element={<ShellCatchAllPage />} />
                          </Route>
                        </Route>
                        <Route path="products">
                          <Route index element={<ProductsPage />} />
                          <Route path=":productId" element={<ProductDetailPage />} />
                        </Route>
                        <Route path="plans">
                          <Route index element={<PlansPage />} />
                          <Route path=":planId" element={<PlanDetailPage />} />
                        </Route>
                        <Route path="subscriptions">
                          <Route index element={<SubscriptionsPage />} />
                          <Route path=":subscriptionId" element={<SubscriptionDetailPage />} />
                        </Route>
                        <Route path="payments">
                          <Route index element={<PaymentsPage />} />
                          <Route path=":paymentId" element={<PaymentDetailPage />} />
                        </Route>
                        <Route path="entitlements" element={<EntitlementsPortfolioPage />} />
                        <Route path="users">
                          <Route index element={<UsersPage />} />
                          <Route path=":userId" element={<UserDetailPage />} />
                        </Route>
                        <Route path="*" element={<ShellCatchAllPage />} />
                      </Route>
                    </Route>
                  </Routes>
                </SessionProvider>
              </DiagnosticsProvider>
            </BrowserRouter>
          </QueryClientProvider>
        </TooltipProvider>
      </AppErrorBoundary>
    </PreferencesProvider>
  );
}
