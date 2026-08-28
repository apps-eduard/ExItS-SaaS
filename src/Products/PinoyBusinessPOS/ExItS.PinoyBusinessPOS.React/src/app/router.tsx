import { createBrowserRouter, Navigate, Outlet } from "react-router-dom";
import { SessionWorkspaceRoot } from "@/app/SessionWorkspaceRoot";
import { RootLayout } from "@/app/RootLayout";
import { SignInPage } from "@/features/auth/SignInPage";
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage";
import { ActivateAccountPage } from "@/features/auth/ActivateAccountPage";
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage";
import { OfflinePinEnrollPage } from "@/features/offline/OfflinePinEnrollPage";
import { OfflinePinUnlockPage } from "@/features/offline/OfflinePinUnlockPage";
import { HomePage } from "@/features/home/HomePage";
import { NotFoundPage } from "@/features/not-found/NotFoundPage";
import { PersonalHomePage } from "@/features/personal/PersonalHomePage";
import { PersonalGuidePage } from "@/features/personal/guide/PersonalGuidePage";
import { PersonalMorePage, PersonalUtangHubPage } from "@/features/personal/PersonalHubPages";
import { PersonalOwnershipTransfersPage } from "@/features/personal/ownership/PersonalOwnershipTransfersPage";
import { PersonalStaffInvitationsPage } from "@/features/personal/staff/PersonalStaffInvitationsPage";
import { PersonalProfilePage } from "@/features/personal/PersonalProfilePage";
import { PersonalExplorePosPage } from "@/features/personal/start-business/PersonalExplorePosPage";
import { PersonalStartBusinessPage } from "@/features/personal/start-business/PersonalStartBusinessPage";
import { PersonalShell } from "@/features/personal/PersonalShell";
import { PostSubscriptionOnboardingPage } from "@/features/onboarding/PostSubscriptionOnboardingPage";
import { AccountContextSwitchPage } from "@/features/account/AccountContextSwitchPage";
import { OrgMorePage } from "@/features/shell/OrgMorePage";
import { AddLocalPersonPage } from "@/features/personal/AddLocalPersonPage";
import { AddPersonPage } from "@/features/personal/AddPersonPage";
import { PersonCreatePage } from "@/features/personal/PersonFormPage";
import { InvitationsPage } from "@/features/personal/InvitationsPage";
import { NotificationsPage } from "@/features/personal/NotificationsPage";
import { ArchivedNotificationsPage } from "@/features/personal/ArchivedNotificationsPage";
import { PeoplePage } from "@/features/personal/PeoplePage";
import { PersonDetailPage } from "@/features/personal/PersonDetailPage";
import {
  PersonalInvitationsPage,
  PersonalMyQrPage,
  PersonalUtangInviteAcceptPage,
} from "@/features/personal/social/PersonalSocialPages";
import {
  PersonalTodoDetailPage,
  PersonalTodoHubPage,
} from "@/features/personal/todo/PersonalTodoPages";
import {
  PersonalLentPage,
  PersonalOwePage,
  PersonalRelationshipDetailPage,
} from "@/features/personal/utang/PersonalUtangPages";
import { PersonalCustomerLinksPage } from "@/features/personal/customer-links/PersonalCustomerLinksPage";
import { PersonalBlockedBusinessesPage } from "@/features/personal/customer-links/PersonalBlockedBusinessesPage";
import { LinkedMerchantsPage } from "@/features/customer-ordering/LinkedMerchantsPage";
import { LinkedMerchantReceiptPage } from "@/features/personal/linked-merchants/LinkedMerchantReceiptPage";
import { LinkedMerchantStatementPage } from "@/features/personal/linked-merchants/LinkedMerchantStatementPage";
import { PersonalRewardsPage } from "@/features/personal/linked-merchants/PersonalRewardsPage";
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
import { OrgBusinessQrPage } from "@/features/org/OrgBusinessQrPage";
import { OrgNotificationsPage } from "@/features/org/OrgNotificationsPage";
import { OrgOwnershipTransferPage } from "@/features/org/ownership/OrgOwnershipTransferPage";
import { PublicStoreLandingPage } from "@/features/store/PublicStoreLandingPage";
import { CatalogCategoriesPage } from "@/features/catalog/CatalogCategoriesPage";
import { CatalogBrandsPage } from "@/features/catalog/CatalogBrandsPage";
import { CatalogGlobalBrowsePage } from "@/features/catalog/CatalogGlobalBrowsePage";
import { CatalogImportJobPage } from "@/features/catalog/CatalogImportJobPage";
import {
  CatalogProductCreatePage,
  CatalogProductEditPage,
} from "@/features/catalog/CatalogProductFormPage";
import { CatalogProductsPage } from "@/features/catalog/CatalogProductsPage";
import { CatalogTemplateImportPage } from "@/features/catalog/CatalogTemplateImportPage";
import { TodaysPricesPage } from "@/features/catalog/TodaysPricesPage";
import { CustomerDetailPage } from "@/features/customers/CustomerDetailPage";
import { CustomerCreatePage, CustomerEditPage } from "@/features/customers/CustomerFormPage";
import { CustomerRepayPage } from "@/features/customers/CustomerRepayPage";
import { CustomerStatementPage } from "@/features/customers/CustomerStatementPage";
import { CustomersListPage } from "@/features/customers/CustomersListPage";
import { ExpirationSettingsPage } from "@/features/inventory/ExpirationSettingsPage";
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
import { OfflineSaleQueuedPage } from "@/features/sell/OfflineSaleQueuedPage";
import { SellReadinessGate } from "@/features/sell/SellReadinessGate";
import { ShiftDetailPage } from "@/features/shifts/ShiftDetailPage";
import { ShiftOpenPage } from "@/features/shifts/ShiftOpenPage";
import { ShiftsHubPage } from "@/features/shifts/ShiftsHubPage";
import { SupplierDetailPage } from "@/features/suppliers/SupplierDetailPage";
import { SupplierCreatePage, SupplierEditPage } from "@/features/suppliers/SupplierFormPage";
import { SupplierAddChooserPage } from "@/features/suppliers/SupplierAddChooserPage";
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
import { OrgStaffAssignPage } from "@/features/staff/OrgStaffAssignPage";
import { OrgStaffInvitePage } from "@/features/staff/OrgStaffInvitePage";
import { OrgStaffPage } from "@/features/staff/OrgStaffPage";
import { StaffInvitationAcceptPage } from "@/features/staff/StaffInvitationAcceptPage";
import { NoAccessibleBranchPage } from "@/features/workspace/NoAccessibleBranchPage";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { AppShell } from "@/layouts/AppShell";
import {
  AllowInvitationAccept,
  GuestOnly,
  RequireOfflinePinFlow,
  RequireOnlineSession,
  RequireAdminExperience,
  RequireCashierRoleHome,
  RequireCreateCustomer,
  RequireCreateSale,
  RequireEditCustomer,
  RequireInviteStaff,
  RequireOrganizationOwnerMembership,
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
  RequireBranchBound,
  RequireOrganizationBound,
  WorkspaceBootGate,
} from "@/session/SessionGuards";
import { RouteErrorPage } from "@/diagnostics/RouteErrorPage";

export const appRoutes = [
  {
    element: <SessionWorkspaceRoot />,
    errorElement: <RouteErrorPage />,
    children: [
      {
        path: "/store/:publicOrganizationId",
        element: <PublicStoreLandingPage />,
      },
      {
        path: "/sign-in",
        element: (
          <GuestOnly>
            <SignInPage />
          </GuestOnly>
        ),
      },
      {
        path: "/forgot-password",
        element: (
          <GuestOnly>
            <ForgotPasswordPage />
          </GuestOnly>
        ),
      },
      {
        path: "/activate-account",
        element: <ActivateAccountPage />,
      },
      {
        path: "/reset-password",
        element: <ResetPasswordPage />,
      },
      {
        path: "/offline-pin-setup",
        element: (
          <RequireOnlineSession>
            <OfflinePinEnrollPage />
          </RequireOnlineSession>
        ),
      },
      {
        path: "/offline-pin",
        element: (
          <RequireOfflinePinFlow>
            <OfflinePinUnlockPage />
          </RequireOfflinePinFlow>
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
        path: "/personal/utang/invitations/accept",
        element: (
          <AllowInvitationAccept>
            <PersonalUtangInviteAcceptPage />
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
            path: "switching-context",
            element: <AccountContextSwitchPage />,
          },
          {
            path: "onboarding",
            element: (
              <RequireOrganizationSession>
                <PostSubscriptionOnboardingPage />
              </RequireOrganizationSession>
            ),
          },
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
                <PersonalShell />
              </RequirePersonalSession>
            ),
            children: [
              { index: true, element: <PersonalHomePage /> },
              { path: "utang", element: <PersonalUtangHubPage /> },
              { path: "utang/people", element: <Navigate to="/personal/people" replace /> },
              { path: "people", element: <PeoplePage /> },
              { path: "people/new", element: <PersonCreatePage /> },
              { path: "people/add/local", element: <AddLocalPersonPage /> },
              { path: "people/add", element: <AddPersonPage /> },
              { path: "people/:contactId", element: <PersonDetailPage /> },
              { path: "invitations", element: <InvitationsPage /> },
              { path: "utang/lent", element: <PersonalLentPage /> },
              { path: "utang/owe", element: <PersonalOwePage /> },
              {
                path: "utang/relationships/:relationshipId",
                element: <PersonalRelationshipDetailPage />,
              },
              { path: "utang/invitations", element: <PersonalInvitationsPage /> },
              { path: "notifications", element: <NotificationsPage /> },
              { path: "notifications/archived", element: <ArchivedNotificationsPage /> },
              { path: "my-qr", element: <PersonalMyQrPage /> },
              { path: "todo", element: <PersonalTodoHubPage /> },
              { path: "todo/:todoId", element: <PersonalTodoDetailPage /> },
              { path: "more", element: <PersonalMorePage /> },
              { path: "guide", element: <PersonalGuidePage /> },
              {
                path: "ownership-transfers",
                element: <PersonalOwnershipTransfersPage />,
              },
              {
                path: "staff-invitations",
                element: <PersonalStaffInvitationsPage />,
              },
              { path: "profile", element: <PersonalProfilePage /> },
              { path: "explore-pos", element: <PersonalExplorePosPage /> },
              {
                path: "start-business",
                element: <PersonalStartBusinessPage />,
              },
              { path: "customer-links", element: <PersonalCustomerLinksPage /> },
              { path: "blocked-businesses", element: <PersonalBlockedBusinessesPage /> },
              { path: "linked-merchants", element: <LinkedMerchantsPage /> },
              {
                path: "linked-merchants/:organizationId/:businessCustomerId",
                element: <LinkedMerchantStatementPage />,
              },
              {
                path: "linked-merchants/:organizationId/:businessCustomerId/receipts/:saleId",
                element: <LinkedMerchantReceiptPage />,
              },
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
              { path: "rewards", element: <PersonalRewardsPage /> },
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
            path: "more",
            element: (
              <RequireOrganizationSession>
                <RequireWorkspaceBound>
                  <OrgMorePage />
                </RequireWorkspaceBound>
              </RequireOrganizationSession>
            ),
          },
          {
            path: "sell",
            element: (
              <RequireOrganizationSession>
                <RequireBranchBound>
                  <RequireCreateSale>
                    <Outlet />
                  </RequireCreateSale>
                </RequireBranchBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <SellReadinessGate /> },
              { path: "checkout", element: <CheckoutCashPage /> },
              { path: "offline-queued/:saleId", element: <OfflineSaleQueuedPage /> },
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
            path: "org/notifications",
            element: (
              <RequireOrganizationSession>
                <RequireOrganizationBound>
                  <OrgNotificationsPage />
                </RequireOrganizationBound>
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
              { path: "business-qr", element: <OrgBusinessQrPage /> },
              {
                path: "ownership-transfer",
                element: (
                  <RequireOrganizationOwnerMembership>
                    <OrgOwnershipTransferPage />
                  </RequireOrganizationOwnerMembership>
                ),
              },
              {
                path: "staff",
                element: (
                  <RequireInviteStaff>
                    <OrgStaffPage />
                  </RequireInviteStaff>
                ),
              },
              {
                path: "staff/invite",
                element: (
                  <RequireInviteStaff>
                    <OrgStaffInvitePage />
                  </RequireInviteStaff>
                ),
              },
              {
                path: "staff/assign",
                element: (
                  <RequireInviteStaff>
                    <OrgStaffAssignPage />
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
              // Stale path used by older CTAs; product list lives at /catalog.
              { path: "products", element: <Navigate to="/catalog" replace /> },
              { path: "categories", element: <CatalogCategoriesPage /> },
              { path: "brands", element: <CatalogBrandsPage /> },
              { path: "todays-prices", element: <TodaysPricesPage /> },
              { path: "templates", element: <CatalogTemplateImportPage /> },
              { path: "global-catalog", element: <CatalogGlobalBrowsePage /> },
              { path: "import-jobs/:jobId", element: <CatalogImportJobPage /> },
              { path: "products/new", element: <CatalogProductCreatePage /> },
              { path: "products/:productId/edit", element: <CatalogProductEditPage /> },
            ],
          },
          {
            path: "products/templates",
            element: <Navigate to="/catalog/templates" replace />,
          },
          {
            path: "products/global-catalog",
            element: <Navigate to="/catalog/global-catalog" replace />,
          },
          {
            path: "inventory",
            element: (
              <RequireOrganizationSession>
                <RequireBranchBound>
                  <RequireViewInventory>
                    <Outlet />
                  </RequireViewInventory>
                </RequireBranchBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <InventoryListPage /> },
              { path: "expiration", element: <InventoryExpirationPage /> },
              { path: ":productId", element: <InventoryDetailPage /> },
              { path: ":productId/expiration", element: <ExpirationSettingsPage /> },
            ],
          },
          {
            path: "shifts",
            element: (
              <RequireOrganizationSession>
                <RequireBranchBound>
                  <RequireViewShifts>
                    <Outlet />
                  </RequireViewShifts>
                </RequireBranchBound>
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
                <RequireBranchBound>
                  <RequireViewSuppliers>
                    <Outlet />
                  </RequireViewSuppliers>
                </RequireBranchBound>
              </RequireOrganizationSession>
            ),
            children: [
              { index: true, element: <SuppliersListPage /> },
              {
                path: "new",
                element: (
                  <RequireManageSuppliers>
                    <SupplierAddChooserPage />
                  </RequireManageSuppliers>
                ),
              },
              {
                path: "new/manual",
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
                <RequireBranchBound>
                  <RequireViewReturns>
                    <Outlet />
                  </RequireViewReturns>
                </RequireBranchBound>
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
