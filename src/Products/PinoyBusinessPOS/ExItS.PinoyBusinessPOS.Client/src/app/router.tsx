import { createBrowserRouter, Outlet } from "react-router-dom";
import { SessionWorkspaceRoot } from "@/app/SessionWorkspaceRoot";
import { RootLayout } from "@/app/RootLayout";
import { SignInPage } from "@/features/auth/SignInPage";
import { HomePage } from "@/features/home/HomePage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";
import { PersonalHomePage } from "@/features/personal/PersonalHomePage";
import { PreferencesPage } from "@/features/preferences/PreferencesPage";
import { CashHandlingSettingsPage } from "@/features/settings/CashHandlingSettingsPage";
import { OrgEssentialsPage } from "@/features/role/OrgEssentialsPage";
import { CatalogCategoriesPage } from "@/features/catalog/CatalogCategoriesPage";
import {
  CatalogProductCreatePage,
  CatalogProductEditPage,
} from "@/features/catalog/CatalogProductFormPage";
import { CatalogProductsPage } from "@/features/catalog/CatalogProductsPage";
import { TodaysPricesPage } from "@/features/catalog/TodaysPricesPage";
import { CustomerDetailPage } from "@/features/customers/CustomerDetailPage";
import { CustomerCreatePage, CustomerEditPage } from "@/features/customers/CustomerFormPage";
import { CustomerRepayPage } from "@/features/customers/CustomerRepayPage";
import { CustomerStatementPage } from "@/features/customers/CustomerStatementPage";
import { CustomersListPage } from "@/features/customers/CustomersListPage";
import { InventoryDetailPage } from "@/features/inventory/InventoryDetailPage";
import { InventoryExpirationPage } from "@/features/inventory/InventoryExpirationPage";
import { InventoryListPage } from "@/features/inventory/InventoryListPage";
import {
  CashierRoleHomePage,
  ManagerRoleHomePage,
  OwnerRoleHomePage,
} from "@/features/role/RoleHomePages";
import { RegistersListPage } from "@/features/registers/RegistersListPage";
import { DeviceRegisterPage } from "@/features/devices/DeviceRegisterPage";
import { OrgPosDevicesPage } from "@/features/devices/OrgPosDevicesPage";
import { CheckoutCashPage } from "@/features/checkout/CheckoutCashPage";
import { TransactionSummaryPage } from "@/features/checkout/TransactionSummaryPage";
import { ProcessReturnPage } from "@/features/returns/ProcessReturnPage";
import { ReturnDetailPage } from "@/features/returns/ReturnDetailPage";
import { ReturnsHubPage } from "@/features/returns/ReturnsHubPage";
import { SellFloorPage } from "@/features/sell/SellFloorPage";
import { ShiftDetailPage } from "@/features/shifts/ShiftDetailPage";
import { ShiftOpenPage } from "@/features/shifts/ShiftOpenPage";
import { ShiftsHubPage } from "@/features/shifts/ShiftsHubPage";
import { OrgStaffInvitePage } from "@/features/staff/OrgStaffInvitePage";
import { StaffInvitationAcceptPage } from "@/features/staff/StaffInvitationAcceptPage";
import { NoAccessibleBranchPage } from "@/features/workspace/NoAccessibleBranchPage";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { AppShell } from "@/layouts/AppShell";
import {
  AllowInvitationAccept,
  GuestOnly,
  RequireAdminExperience,
  RequireCashierRoleHome,
  RequireCreateCustomer,
  RequireCreateSale,
  RequireEditCustomer,
  RequireInviteStaff,
  RequireManageCatalog,
  RequireManageShifts,
  RequireManagerRoleHome,
  RequireOrganizationSession,
  RequireOwnerRoleHome,
  RequirePersonalSession,
  RequireProcessReturn,
  RequireRecordRepayment,
  RequireSession,
  RequireViewCustomers,
  RequireViewInventory,
  RequireViewRegisters,
  RequireViewReturns,
  RequireViewShifts,
  RequireViewStatement,
  RequireWorkspaceBound,
  RequireOrganizationBound,
  WorkspaceBootGate,
} from "@/session/SessionGuards";

export const appRoutes = [
  {
    element: <SessionWorkspaceRoot />,
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
        path: "/personal/invitations/accept",
        element: (
          <AllowInvitationAccept>
            <AppShell>
              <StaffInvitationAcceptPage />
            </AppShell>
          </AllowInvitationAccept>
        ),
      },
      {
        path: "/",
        element: (
          <RequireSession>
            <WorkspaceBootGate>
              <RootLayout />
            </WorkspaceBootGate>
          </RequireSession>
        ),
        children: [
          { index: true, element: <HomePage /> },
          {
            path: "workspace",
            element: (
              <RequireOrganizationSession>
                <WorkspaceChooserPage />
              </RequireOrganizationSession>
            ),
          },
          {
            path: "personal",
            element: (
              <RequirePersonalSession>
                <PersonalHomePage />
              </RequirePersonalSession>
            ),
          },
          {
            path: "no-location",
            element: (
              <RequireOrganizationSession>
                <NoAccessibleBranchPage />
              </RequireOrganizationSession>
            ),
          },
          { path: "settings/preferences", element: <PreferencesPage /> },
          {
            path: "sell",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireCreateSale>
                    <Outlet />
                  </RequireCreateSale>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <SellFloorPage /> },
              { path: "checkout", element: <CheckoutCashPage /> },
              { path: "sales/:saleId/summary", element: <TransactionSummaryPage /> },
            ],
          },
          {
            path: "role/owner",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireOwnerRoleHome>
                    <OwnerRoleHomePage />
                  </RequireOwnerRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/manager",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireManagerRoleHome>
                    <ManagerRoleHomePage />
                  </RequireManagerRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "role/cashier",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireCashierRoleHome>
                    <CashierRoleHomePage />
                  </RequireCashierRoleHome>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "org",
            element: (
              <RequireOrganizationSession>
                <RequireOrganizationBound>
                  <RequireAdminExperience>
                    <Outlet />
                  </RequireAdminExperience>
                </RequireOrganizationBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <OrgEssentialsPage /> },
              {
                path: "staff/invite",
                element: (
                  <RequireInviteStaff>
                    <OrgStaffInvitePage />
                  </RequireInviteStaff>
                ),
              },
              { path: "devices", element: <OrgPosDevicesPage /> },
              { path: "cash-handling", element: <CashHandlingSettingsPage /> },
            ],
          },
          {
            path: "devices/register",
            element: (
              <RequireOrganizationSession>
                <RequireOrganizationBound>
                  <DeviceRegisterPage />
                </RequireOrganizationBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "catalog",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireManageCatalog>
                    <Outlet />
                  </RequireManageCatalog>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <CatalogProductsPage /> },
              { path: "categories", element: <CatalogCategoriesPage /> },
              { path: "todays-prices", element: <TodaysPricesPage /> },
              { path: "products/new", element: <CatalogProductCreatePage /> },
              { path: "products/:productId/edit", element: <CatalogProductEditPage /> },
            ],
          },
          {
            path: "inventory",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewInventory>
                    <Outlet />
                  </RequireViewInventory>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <InventoryListPage /> },
              { path: "expiration", element: <InventoryExpirationPage /> },
              { path: ":productId", element: <InventoryDetailPage /> },
            ],
          },
          {
            path: "shifts",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewShifts>
                    <Outlet />
                  </RequireViewShifts>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <ShiftsHubPage /> },
              {
                path: "open",
                element: (
                  <RequireManageShifts>
                    <ShiftOpenPage />
                  </RequireManageShifts>
                ),
              },
              { path: ":shiftId", element: <ShiftDetailPage /> },
            ],
          },
          {
            path: "registers",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewRegisters>
                    <RegistersListPage />
                  </RequireViewRegisters>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "customers",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewCustomers>
                    <Outlet />
                  </RequireViewCustomers>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <CustomersListPage /> },
              {
                path: "new",
                element: (
                  <RequireCreateCustomer>
                    <CustomerCreatePage />
                  </RequireCreateCustomer>
                ),
              },
              { path: ":customerId", element: <CustomerDetailPage /> },
              {
                path: ":customerId/edit",
                element: (
                  <RequireEditCustomer>
                    <CustomerEditPage />
                  </RequireEditCustomer>
                ),
              },
              {
                path: ":customerId/repay",
                element: (
                  <RequireRecordRepayment>
                    <CustomerRepayPage />
                  </RequireRecordRepayment>
                ),
              },
              {
                path: ":customerId/statement",
                element: (
                  <RequireViewStatement>
                    <CustomerStatementPage />
                  </RequireViewStatement>
                ),
              },
            ],
          },
          {
            path: "returns",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewReturns>
                    <Outlet />
                  </RequireViewReturns>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <ReturnsHubPage /> },
              {
                path: "sale/:saleId",
                element: (
                  <RequireProcessReturn>
                    <ProcessReturnPage />
                  </RequireProcessReturn>
                ),
              },
              { path: ":returnId", element: <ReturnDetailPage /> },
            ],
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
