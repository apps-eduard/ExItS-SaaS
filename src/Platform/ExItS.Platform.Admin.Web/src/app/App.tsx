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
import { OverviewPage } from "@/features/overview/OverviewPage";
import { UsersPage } from "@/features/users/UsersPage";
import { UserDetailPage } from "@/features/users/UserDetailPage";
import { CategoriesPage } from "@/features/global-catalog/CategoriesPage";
import {
  CategoryDetailPage,
  CategoryFormPage,
} from "@/features/global-catalog/CategoryDetailPage";
import { BusinessTypesPage } from "@/features/global-catalog/BusinessTypesPage";
import {
  BusinessTypeDetailPage,
  BusinessTypeFormPage,
} from "@/features/global-catalog/BusinessTypeDetailPage";
import { GlobalProductsPage } from "@/features/global-catalog/GlobalProductsPage";
import {
  ProductDetailPage as GlobalCatalogProductDetailPage,
  ProductFormPage as GlobalCatalogProductFormPage,
} from "@/features/global-catalog/ProductDetailPage";
import { ImportsPage } from "@/features/global-catalog/ImportsPage";
import { ImportDetailPage } from "@/features/global-catalog/ImportDetailPage";
import { TemplatesPage } from "@/features/global-catalog/TemplatesPage";
import {
  TemplateDetailPage,
  TemplateFormPage,
} from "@/features/global-catalog/TemplateDetailPage";
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
                        <Route path="users">
                          <Route index element={<UsersPage />} />
                          <Route path=":userId" element={<UserDetailPage />} />
                        </Route>
                        <Route path="global-catalog">
                          <Route path="business-types">
                            <Route index element={<BusinessTypesPage />} />
                            <Route path="new" element={<BusinessTypeFormPage mode="create" />} />
                            <Route path=":businessTypeId">
                              <Route index element={<BusinessTypeDetailPage />} />
                              <Route path="edit" element={<BusinessTypeFormPage mode="edit" />} />
                            </Route>
                          </Route>
                          <Route path="categories">
                            <Route index element={<CategoriesPage />} />
                            <Route path="new" element={<CategoryFormPage mode="create" />} />
                            <Route path=":categoryId">
                              <Route index element={<CategoryDetailPage />} />
                              <Route path="edit" element={<CategoryFormPage mode="edit" />} />
                            </Route>
                          </Route>
                          <Route path="products">
                            <Route index element={<GlobalProductsPage />} />
                            <Route path="new" element={<GlobalCatalogProductFormPage mode="create" />} />
                            <Route path=":productId">
                              <Route index element={<GlobalCatalogProductDetailPage />} />
                              <Route path="edit" element={<GlobalCatalogProductFormPage mode="edit" />} />
                            </Route>
                          </Route>
                          <Route path="imports">
                            <Route index element={<ImportsPage />} />
                            <Route path=":jobId" element={<ImportDetailPage />} />
                          </Route>
                          <Route path="templates">
                            <Route index element={<TemplatesPage />} />
                            <Route path="new" element={<TemplateFormPage mode="create" />} />
                            <Route path=":templateId">
                              <Route index element={<TemplateDetailPage />} />
                              <Route path="edit" element={<TemplateFormPage mode="edit" />} />
                            </Route>
                          </Route>
                        </Route>
                        <Route
                          path="catalog/business-types"
                          element={<Navigate to="/admin/global-catalog/business-types" replace />}
                        />
                        <Route
                          path="catalog/templates"
                          element={<Navigate to="/admin/global-catalog/templates" replace />}
                        />
                        <Route
                          path="catalog/imports"
                          element={<Navigate to="/admin/global-catalog/imports" replace />}
                        />
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
