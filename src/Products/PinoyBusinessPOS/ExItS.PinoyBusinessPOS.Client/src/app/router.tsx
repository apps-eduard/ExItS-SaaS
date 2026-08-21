import { createBrowserRouter, Outlet } from "react-router-dom";
import { SessionWorkspaceRoot } from "@/app/SessionWorkspaceRoot";
import { RootLayout } from "@/app/RootLayout";
import { SignInPage } from "@/features/auth/SignInPage";
import { HomePage } from "@/features/home/HomePage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";
import { PersonalHomePage } from "@/features/personal/PersonalHomePage";
import { LinkedMerchantsPage } from "@/features/customer-ordering/LinkedMerchantsPage";
import { MerchantShopPage } from "@/features/customer-ordering/MerchantShopPage";
import { MerchantCheckoutPage } from "@/features/customer-ordering/MerchantCheckoutPage";
import { MyOrdersPage } from "@/features/customer-ordering/MyOrdersPage";
import { MyOrderDetailPage } from "@/features/customer-ordering/MyOrderDetailPage";
import { SellerOrdersPage } from "@/features/customer-ordering/SellerOrdersPage";
import { SellerOrderDetailPage } from "@/features/customer-ordering/SellerOrderDetailPage";
import { ClassicReportPage } from "@/features/reports/ClassicReportPage";
import { ManagementDashboardPage } from "@/features/reports/ManagementDashboardPage";
import { OperationalReportPage } from "@/features/reports/OperationalReportPage";
import { ReportsHubPage } from "@/features/reports/ReportsHubPage";
import { PreferencesPage } from "@/features/preferences/PreferencesPage";
import { CashHandlingSettingsPage } from "@/features/settings/CashHandlingSettingsPage";
import { BranchFulfillmentEditPage } from "@/features/branches/BranchFulfillmentEditPage";
import { BranchFulfillmentListPage } from "@/features/branches/BranchFulfillmentListPage";
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
import { SupplierDetailPage } from "@/features/suppliers/SupplierDetailPage";
import { SupplierCreatePage, SupplierEditPage } from "@/features/suppliers/SupplierFormPage";
import { SuppliersListPage } from "@/features/suppliers/SuppliersListPage";
import { ConnectedRequestPage } from "@/features/suppliers/ConnectedRequestPage";
import { ConnectedIncomingRequestsPage } from "@/features/suppliers/ConnectedIncomingRequestsPage";
import { ConnectedBuyersPage } from "@/features/suppliers/ConnectedBuyersPage";
import { ConnectedSharedProductsPage } from "@/features/suppliers/ConnectedSharedProductsPage";
import { ConnectedCatalogPage } from "@/features/suppliers/ConnectedCatalogPage";
import { LinkedProductsPage } from "@/features/suppliers/LinkedProductsPage";
import { PurchasingHubPage } from "@/features/purchasing/PurchasingHubPage";
import { PurchaseOrdersListPage } from "@/features/purchasing/PurchaseOrdersListPage";
import { PurchaseOrderCreatePage } from "@/features/purchasing/PurchaseOrderCreatePage";
import { PurchaseOrderDetailPage } from "@/features/purchasing/PurchaseOrderDetailPage";
import { PurchaseOrderReceivePage } from "@/features/purchasing/PurchaseOrderReceivePage";
import { ReceivableOrdersPage } from "@/features/purchasing/ReceivableOrdersPage";
import { ReceiveStockPage } from "@/features/purchasing/ReceiveStockPage";
import { DirectPurchasesListPage } from "@/features/purchasing/DirectPurchasesListPage";
import { DirectPurchaseDetailPage } from "@/features/purchasing/DirectPurchaseDetailPage";
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
  RequireManageInventory,
  RequireManagePurchasing,
  RequireManageShifts,
  RequireManageSuppliers,
  RequireManagerRoleHome,
  RequireOrganizationSession,
  RequireOwnerRoleHome,
  RequirePersonalSession,
  RequireProcessReturn,
  RequirePurchasingHubAccess,
  RequireRecordRepayment,
  RequireSession,
  RequireViewCustomers,
  RequireAccessReportsHub,
  RequireClassicExpensesReport,
  RequireClassicInventoryReport,
  RequireClassicSalesReport,
  RequireClassicUtangReport,
  RequireViewCustomerOrders,
  RequireViewDashboard,
  RequireViewInventory,
  RequireViewPurchasing,
  RequireViewRegisters,
  RequireViewReturns,
  RequireViewShifts,
  RequireViewStatement,
  RequireViewSuppliers,
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
                <Outlet />
              </RequirePersonalSession>
            ),
            children: [
              { index: true, element: <PersonalHomePage /> },
              { path: "linked-merchants", element: <LinkedMerchantsPage /> },
              {
                path: "linked-merchants/:organizationId/shop",
                element: <MerchantShopPage />,
              },
              {
                path: "linked-merchants/:organizationId/shop/checkout",
                element: <MerchantCheckoutPage />,
              },
              { path: "orders", element: <MyOrdersPage /> },
              { path: "orders/:orderId", element: <MyOrderDetailPage /> },
            ],
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
              { path: "branches", element: <BranchFulfillmentListPage /> },
              { path: "branches/:branchId", element: <BranchFulfillmentEditPage /> },
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
            path: "suppliers",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewSuppliers>
                    <Outlet />
                  </RequireViewSuppliers>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <SuppliersListPage /> },
              {
                path: "new",
                element: (
                  <RequireManageSuppliers>
                    <SupplierCreatePage />
                  </RequireManageSuppliers>
                ),
              },
              {
                path: "connected/request",
                element: (
                  <RequireManageSuppliers>
                    <ConnectedRequestPage />
                  </RequireManageSuppliers>
                ),
              },
              {
                path: "connected/requests",
                element: <ConnectedIncomingRequestsPage />,
              },
              {
                path: "connected/buyers",
                element: <ConnectedBuyersPage />,
              },
              {
                path: "connected/buyers/:relationshipId",
                element: <ConnectedBuyersPage />,
              },
              {
                path: "connected/buyers/:relationshipId/shared-products",
                element: (
                  <RequireManageSuppliers>
                    <ConnectedSharedProductsPage />
                  </RequireManageSuppliers>
                ),
              },
              {
                path: "connected/buyers/:relationshipId/share-products",
                element: (
                  <RequireManageSuppliers>
                    <ConnectedSharedProductsPage />
                  </RequireManageSuppliers>
                ),
              },
              {
                path: ":supplierId/connected-catalog",
                element: (
                  <RequireViewPurchasing>
                    <ConnectedCatalogPage />
                  </RequireViewPurchasing>
                ),
              },
              {
                path: ":supplierId/linked-products",
                element: (
                  <RequireViewPurchasing>
                    <LinkedProductsPage />
                  </RequireViewPurchasing>
                ),
              },
              { path: ":supplierId", element: <SupplierDetailPage /> },
              {
                path: ":supplierId/edit",
                element: (
                  <RequireManageSuppliers>
                    <SupplierEditPage />
                  </RequireManageSuppliers>
                ),
              },
            ],
          },
          {
            path: "purchasing",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequirePurchasingHubAccess>
                    <Outlet />
                  </RequirePurchasingHubAccess>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <PurchasingHubPage /> },
              {
                path: "orders",
                element: (
                  <RequireViewPurchasing>
                    <PurchaseOrdersListPage />
                  </RequireViewPurchasing>
                ),
              },
              {
                path: "new",
                element: (
                  <RequireManagePurchasing>
                    <PurchaseOrderCreatePage />
                  </RequireManagePurchasing>
                ),
              },
              {
                path: "receipts",
                element: (
                  <RequireViewPurchasing>
                    <ReceivableOrdersPage />
                  </RequireViewPurchasing>
                ),
              },
              {
                path: "receive-stock",
                element: (
                  <RequireManageInventory>
                    <ReceiveStockPage />
                  </RequireManageInventory>
                ),
              },
              {
                path: "direct-purchases",
                element: (
                  <RequireViewInventory>
                    <DirectPurchasesListPage />
                  </RequireViewInventory>
                ),
              },
              {
                path: "direct-purchases/:receiptId",
                element: (
                  <RequireViewInventory>
                    <DirectPurchaseDetailPage />
                  </RequireViewInventory>
                ),
              },
              {
                path: ":purchaseOrderId",
                element: (
                  <RequireViewPurchasing>
                    <PurchaseOrderDetailPage />
                  </RequireViewPurchasing>
                ),
              },
              {
                path: ":purchaseOrderId/receive",
                element: (
                  <RequireManagePurchasing>
                    <PurchaseOrderReceivePage />
                  </RequireManagePurchasing>
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
          {
            path: "orders",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewCustomerOrders>
                    <Outlet />
                  </RequireViewCustomerOrders>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <SellerOrdersPage /> },
              { path: ":orderId", element: <SellerOrderDetailPage /> },
            ],
          },
          {
            path: "dashboard",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireViewDashboard>
                    <ManagementDashboardPage />
                  </RequireViewDashboard>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "reports",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <RequireAccessReportsHub>
                    <Outlet />
                  </RequireAccessReportsHub>
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <ReportsHubPage /> },
              { path: "operational/:kind", element: <OperationalReportPage /> },
              {
                path: "sales",
                element: (
                  <RequireClassicSalesReport>
                    <ClassicReportPage />
                  </RequireClassicSalesReport>
                ),
              },
              {
                path: "utang",
                element: (
                  <RequireClassicUtangReport>
                    <ClassicReportPage />
                  </RequireClassicUtangReport>
                ),
              },
              {
                path: "inventory",
                element: (
                  <RequireClassicInventoryReport>
                    <ClassicReportPage />
                  </RequireClassicInventoryReport>
                ),
              },
              {
                path: "expenses",
                element: (
                  <RequireClassicExpensesReport>
                    <ClassicReportPage />
                  </RequireClassicExpensesReport>
                ),
              },
            ],
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
