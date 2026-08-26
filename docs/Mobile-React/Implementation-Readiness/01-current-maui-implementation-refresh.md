# POS-REACT-READINESS-01 — Current MAUI Implementation Refresh

**Package:** POS-REACT-READINESS-01  
**Status:** Documentation only. React implementation is **NOT AUTHORIZED**.  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`

This file refreshes the approved Mobile-React current-state audit against **current main**. It does not redesign accepted MOBILE-D decisions. It does not authorize a React client, PWA production, Capacitor, or MAUI retirement.

Approved planning baseline remains:

- [current-state-and-replacement-boundaries.md](../current-state-and-replacement-boundaries.md)
- [decisions.md](../decisions.md)
- remaining `docs/Mobile-React/` DOC-02 … DOC-08 and AMEND-01 … AMEND-03

---

## 0. How to read this refresh

| Statement type | Meaning |
|---|---|
| **VERIFIED** | Approved baseline still matches current source. |
| **DELTA** | Current source adds, clarifies, or dates the baseline. Do **not** treat a delta as a silent rewrite of MOBILE-D decisions. |
| **CURRENT** | What MAUI does today. |
| **PROPOSED** | Future React / PWA / Capacitor planning only. |

Do not equate route count with feature parity (MOBILE-D-046). Do not describe current MAUI as POS checkout only (MOBILE-D-005).

---

## 1. Host architecture

### 1.1 Process and Blazor Hybrid wiring

**CURRENT** host: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui`

| Layer | Evidence |
|---|---|
| SDK | `Microsoft.NET.Sdk.Razor` + `<UseMaui>true</UseMaui>` + `Microsoft.AspNetCore.Components.WebView.Maui` 10.0.0 |
| Composition | `MauiProgram.CreateMauiApp()` → `UseMauiApp<App>()` → `AddMauiBlazorWebView()` |
| Window | `App.xaml.cs` creates `Window(new MainPage())` |
| Native page | `MainPage.xaml` hosts one `BlazorWebView` (`HostPage="wwwroot/index.html"`, `SafeAreaEdges="Container"`) |
| Root component | `Routes` on `#app` |
| Router | `Components/Routes.razor` — `DefaultLayout = typeof(Layout.PosShell)` |
| HybridWebView | **Absent** |

`wwwroot/index.html` loads `blazor.webview.js` (`autostart="false"`), DesignSystem CSS, `app.css`, and `keyboard-inset.js`.

### 1.2 Android target and RuntimeIdentifiers

From `ExItS.PinoyBusinessPOS.Maui.csproj`:

| Property | Current value |
|---|---|
| `TargetFrameworks` | `net10.0-android` only |
| `SupportedOSPlatformVersion` | `24.0` (min API 24) |
| `RuntimeIdentifiers` | `android-arm64;android-x64` |
| `ApplicationTitle` | `ExItS POS` |
| `ApplicationId` | `com.exits.pinoybusinesspos` |
| `ApplicationDisplayVersion` | `0.5.0` |
| `ApplicationVersion` | `1` |
| Target SDK | **Not set in csproj** (workload default) |

csproj comment remains: Android-first; iOS/Windows/MacCatalyst are not required for the existing MAUI foundation.

### 1.3 Non-Android platform folders

**Built TFM:** Android only.

Scaffold folders still exist and are **not** in `TargetFrameworks`:

- `Platforms/iOS/`
- `Platforms/MacCatalyst/`
- `Platforms/Windows/`

`Properties/launchSettings.json` still contains a “Windows Machine” profile. That profile is not an active TFM.

### 1.4 Android configuration (proven)

| File | Current behavior |
|---|---|
| `Platforms/Android/AndroidManifest.xml` | `INTERNET`, `ACCESS_NETWORK_STATE`, `CAMERA` (optional hardware feature); `usesCleartextTraffic=true` constrained by network security config |
| `Resources/xml/network_security_config.xml` | Debug cleartext only for `10.0.2.2`, `localhost`, `127.0.0.1`, and Local Validation PublicHost |
| `Resources/xml/network_security_config.Release.xml` | Release swap: cleartext forbidden (wired by csproj) |
| `MainActivity.cs` | Soft keyboard `AdjustResize`; Debug WebView CDP |
| `WebAuthenticationCallbackActivity.cs` | OAuth callback `exitspos://auth/callback` |
| `MainPage.xaml.cs` | Debug Android mixed-content allow for Local Validation HTTP |

**VERIFIED** vs approved DOC-01: MAUI + BlazorWebView + Android-first + application id/title.  
**DELTA:** explicit RIDs, min API 24, display version `0.5.0`, OAuth callback activity, and Debug/Release network-security swap are now recorded from current source.

---

## 2. Experience shells

Exactly **three** layout shells. There is **no** fourth Owner shell.

| Shell | File | What it hosts | Chrome |
|---|---|---|---|
| **Auth** | `Components/Layout/AuthShell.razor` | Sign-in, registration, activation, recovery, onboarding, workspace/org select, reconnect, PIN, device register, access denied, Start a Business | Optional `StoreHeader` when authenticated; **no** bottom nav |
| **Personal** | `Components/Layout/PersonalShell.razor` | Personal Account experience | Personal identity header + bottom nav: Home, People, Lent, Borrowed, More |
| **POS / Owner host** | `Components/Layout/PosShell.razor` | Default layout for Organization Owner Mobile **and** POS Operations | Org header + bottom nav. Full ops nav when `HasPosAccess`; Home + More when owner essentials only |

Routing switch:

1. `Routes.razor` defaults to `PosShell`.
2. Pages opt into `AuthShell` or `PersonalShell` with `@layout`.
3. `NavigationGate.ResolveStartRouteAsync` chooses Personal `/personal`, org essentials `/org`, or role POS home (`/owner`, `/manager`, `/cashier`) via `RoleHomeResolver`.

Owner vs POS Operations is **session + capability**, not a separate host:

| Mode | Condition | Typical home | Shell |
|---|---|---|---|
| Personal Mobile | Personal default / no org | `/personal` | PersonalShell |
| Organization Owner Mobile | Org selected, `!HasPosAccess` | `/org` | PosShell (reduced nav) |
| POS Operations | `HasPosAccess` plus device/branch/PIN/setup gates | `/owner`, `/manager`, or `/cashier` | PosShell (full ops nav) |
| Owner selling overlay | `SellingModeService` temporary checkout | returns to preferred home | Still PosShell |

**VERIFIED:** current MAUI hosts AUTH + PERSONAL MOBILE + ORGANIZATION OWNER MOBILE + POS OPERATIONS in one BlazorWebView (MOBILE-D-005). First React selling slice must not be read as permission to drop Personal or Owner from eventual parity.

---

## 3. Route inventory

**171** `@page` templates in `Components/Pages/**` at this SHA.  
Do **not** treat 171 as a parity score. Duplicate aliases (`/org/devices` and `/organization/devices`; `/offline-pin-setup` and `/setup-pin`) are still one capability.

Shell column: explicit `@layout`, otherwise default `PosShell`.

### 3.1 Auth

| Path | Component | Shell | Purpose |
|---|---|---|---|
| `/` | `Boot.razor` | Auth | Cold start → `NavigationGate` |
| `/signin` | `SignIn.razor` | Auth | Password / Local Validation quick login |
| `/register` | `Register.razor` | Auth | Personal registration |
| `/activate` | `ActivateAccount.razor` | Auth | Account activation |
| `/forgot-password` | `ForgotPassword.razor` | Auth | Password recovery |
| `/welcome` | `Welcome.razor` | Auth | Welcome |
| `/onboarding/language` | `Onboarding/LanguageStep.razor` | Auth | Locale step |
| `/onboarding/theme` | `Onboarding/ThemeStep.razor` | Auth | Theme step |
| `/onboarding/density` | `Onboarding/DensityStep.razor` | Auth | Density step |
| `/onboarding/dev-confirm` | `Onboarding/DevConfirmStep.razor` | Auth | Dev environment confirm |
| `/onboarding/access-confirm` | `Onboarding/AccessConfirmStep.razor` | Auth | Access confirm |
| `/workspace-select` | `WorkspaceSelect.razor` | Auth | Personal vs org workspace |
| `/organization-select` | `OrganizationSelect.razor` | Auth | Bind organization |
| `/reconnect` | `Reconnect.razor` | Auth | Online reconnect wall |
| `/offline-pin` | `OfflinePinUnlock.razor` | Auth | Offline PIN unlock |
| `/offline-pin-setup`, `/setup-pin` | `OfflinePinEnrollment.razor` | Auth | PIN enrollment / change |
| `/access-denied` | `AccessDenied.razor` | Auth | POS role/access denied |
| `/personal/invitations/accept` | `Personal/PersonalInvitationAccept.razor` | Auth | Accept invite |
| `/start-business` | `Personal/StartBusiness.razor` | Auth | Create organization |
| `/onboarding/business-types` | `Personal/OnboardingActivateBusinessTypes.razor` | Auth | Post-create business-type activation |
| `/setup` | `OperationalSetup/OperationalSetupPage.razor` | Auth | First-time operational setup |
| `/devices/register` | `Devices/PosDeviceRegister.razor` | Auth | Register this POS device |
| `/catalog/import` | `Catalog/CatalogImport.razor` | Auth | Catalog template import entry |

### 3.2 Personal

| Path | Component | Purpose |
|---|---|---|
| `/personal` | `Personal/PersonalHome.razor` | Personal home |
| `/personal/utang/people` | `PersonalPeople.razor` | Contacts |
| `/personal/utang/people/{ContactId:guid}` | `PersonalPeopleDetail.razor` | Contact detail |
| `/personal/utang/lent` | `PersonalLent.razor` | Lent ledger |
| `/personal/utang/borrowed` | `PersonalBorrowed.razor` | Borrowed ledger |
| `/personal/utang/relationships/{RelationshipId:guid}` | `PersonalRelationshipDetail.razor` | Relationship |
| `/personal/utang/invitations` | `PersonalUtangInvitations.razor` | Utang invitations |
| `/personal/more` | `PersonalMore.razor` | More hub |
| `/personal/profile` | `PersonalProfile.razor` | Profile |
| `/personal/settings` | `PersonalSettings.razor` | Settings |
| `/personal/settings/support/diagnostics` | `PersonalSupportDiagnosticsPage.razor` | Diagnostics |
| `/personal/my-qr` | `PersonalMyQr.razor` | Show / share QR |
| `/personal/notifications` | `PersonalNotifications.razor` | Notifications |
| `/personal/orders` | `PersonalOrders.razor` | Customer orders |
| `/personal/orders/{OrderId:guid}` | `PersonalOrderDetail.razor` | Order detail |
| `/personal/linked-merchants` | `PersonalLinkedMerchants.razor` | Linked merchants |
| `/personal/linked-merchants/{OrganizationId:guid}/shop` | `PersonalMerchantShop.razor` | Merchant shop |
| `/personal/linked-merchants/{OrganizationId:guid}/shop/review` | `PersonalMerchantShopReview.razor` | Cart review |
| `/personal/linked-merchants/{OrganizationId:guid}/{PlatformBusinessCustomerId:guid}` | `PersonalLinkedMerchantStatement.razor` | Statement |
| `/personal/linked-merchants/.../receipts/{SaleId:guid}` | `PersonalLinkedMerchantReceipt.razor` | Receipt |
| `/personal/customer-link-requests` | `PersonalCustomerLinkRequests.razor` | Link requests |
| `/personal/ownership-transfers` | `PersonalOwnershipTransfers.razor` | Ownership transfers |
| `/personal/rewards` | `PersonalRewards.razor` | Rewards |
| `/personal/explore-pos` | `PersonalExplorePos.razor` | Explore POS |
| `/personal/resolve-user` | `PublicUserResolve.razor` | Resolve public user |

All Personal rows use `PersonalShell` except invitation accept / Start a Business / business-type onboarding (Auth, listed above).

### 3.3 Owner / governance

Default `PosShell` unless noted.

| Path | Component | Purpose |
|---|---|---|
| `/org` | `Organization/OrgSummary.razor` | Org essentials / enter POS |
| `/org/profile` | `OrgProfile.razor` | Organization profile |
| `/org/subscription` | `OrgSubscription.razor` | Subscription / entitlement |
| `/org/business-types` | `OrgBusinessTypes.razor` | Business types |
| `/org/business-qr` | `OrgBusinessQr.razor` | Business QR + share |
| `/org/staff` | `OrgStaff.razor` | Staff list |
| `/org/staff/invite` | `OrgStaffInvite.razor` | Invite staff |
| `/org/staff/assign` | `OrgStaffAssign.razor` | Assign roles |
| `/org/notifications`, `/org/customer-link-notifications` | `OrganizationNotifications.razor` | Org notifications |
| `/org/privacy` | `PrivacyDataProtection.razor` | Privacy readiness |
| `/org/devices`, `/organization/devices` | `OrgPosDevices.razor` | Device inventory |
| `/organization/branches` | `Branches.razor` | Branches |
| `/organization/branches/{BranchId:guid}` | `BranchEdit.razor` | Branch edit |
| `/organization/tax-compliance` | `TaxCompliance.razor` | Tax compliance |
| `/branch-settings` | `BranchSettings.razor` | Branch settings |
| `/manage-business` | `ManageBusiness.razor` | Manage-business hub |
| `/sales-document-education` | `SalesDocumentEducation.razor` | Sales-document education |
| `/permissions` | `Permissions/PermissionsHub.razor` | Permissions hub |
| `/permissions/my-access` | `MyAccess.razor` | Effective access |
| `/permissions/assignments/new` | `AssignmentCreate.razor` | Create assignment |
| `/permissions/assignments/{AssignmentId:guid}` | `AssignmentDetail.razor` | Assignment detail |
| `/owner` | `Dashboards/OwnerDashboard.razor` | Owner POS home |
| `/manager` | `ManagerDashboard.razor` | Manager home |
| `/cashier` | `CashierHome.razor` | Cashier home |
| `/home` | `Home.razor` | Redirect via `NavigationGate` |
| `/more` | `MoreHub.razor` | Org More hub |

### 3.4 POS selling

| Path | Component | Purpose |
|---|---|---|
| `/sales` | `Sales/SalesList.razor` | Sales history |
| `/sales/new` | `SaleCheckout.razor` | Checkout / cart |
| `/sales/{SaleId:guid}` | `SaleDetail.razor` | Sale detail |
| `/sales/{SaleId:guid}/receipt` | `SaleReceipt.razor` | Receipt |
| `/sales/local/{SaleId:guid}/receipt` | `LocalSaleReceipt.razor` | Offline local receipt |
| `/sales/{SaleId:guid}/return` | `Returns/SaleReturn.razor` | Return |
| `/orders` | `Orders/SellerOrders.razor` | Seller customer orders |
| `/orders/{OrderId:guid}` | `SellerOrderDetail.razor` | Order detail |

Non-routed helpers: `Components/Sales/` cart/weight/unit panels.

### 3.5 Catalog / inventory

| Path | Component | Purpose |
|---|---|---|
| `/catalog` | `Catalog/CatalogProductsList.razor` | Products |
| `/products` | `ProductsRedirect.razor` | Redirect → catalog |
| `/catalog/products/new` | `CatalogProductCreate.razor` | Create |
| `/catalog/products/{ProductId:guid}` | `CatalogProductDetail.razor` | Detail |
| `/catalog/products/{ProductId:guid}/edit` | `CatalogProductEdit.razor` | Edit |
| `/catalog/categories` | `CatalogCategories.razor` | Categories |
| `/catalog/barcode-lookup` | `CatalogBarcodeLookup.razor` | Manual barcode/SKU lookup |
| `/catalog/todays-prices` | `CatalogTodaysPrices.razor` | Today’s prices |
| `/catalog/global` | `CatalogGlobalBrowse.razor` | Global browse |
| `/catalog/connected-buyer-availability` | `CatalogConnectedBuyerAvailability.razor` | Buyer availability |
| `/catalog/import/jobs/{JobId:guid}` | `CatalogImportJob.razor` | Import job (default PosShell) |
| `/catalog/import/jobs/{JobId:guid}/review` | `CatalogImportReview.razor` | Import review |
| `/inventory` | `Inventory/InventoryList.razor` | Stock list |
| `/inventory/{ProductId:guid}` | `InventoryDetail.razor` | Stock detail |
| `/inventory/{ProductId:guid}/adjust` | `InventoryAdjust.razor` | Adjust |
| `/inventory/{ProductId:guid}/reorder` | `InventoryReorder.razor` | Reorder |
| `/inventory/low-stock` | `InventoryLowStock.razor` | Low stock |
| `/inventory/expiration` | `InventoryExpiration.razor` | Expiration |
| `/inventory/transfers` | `InventoryTransfers.razor` | Transfers |
| `/inventory/transfers/new` | `InventoryTransferCreate.razor` | New transfer |
| `/inventory/transfers/{TransferId:guid}` | `InventoryTransferDetail.razor` | Transfer detail |
| `/inventory/transfers/{TransferId:guid}/receive` | `InventoryTransferReceive.razor` | Receive |
| `/inventory/counts` | `StockCountsList.razor` | Counts |
| `/inventory/counts/new` | `StockCountCreate.razor` | New count |
| `/inventory/counts/{StockCountId:guid}` | `StockCountDetail.razor` | Count detail |

### 3.6 Customers / credit

| Path | Component | Purpose |
|---|---|---|
| `/customers` | `Customers/CustomersList.razor` | Customers |
| `/customers/new` | `CustomerCreate.razor` | Create |
| `/customers/{CustomerId:guid}` | `CustomerDetail.razor` | Detail |
| `/customers/{CustomerId:guid}/edit` | `CustomerEdit.razor` | Edit |
| `/customers/{CustomerId:guid}/ledger` | `CustomerLedger.razor` | Ledger |
| `/customers/{CustomerId:guid}/statement` | `CustomerStatement.razor` | Statement + share |
| `/customers/{CustomerId:guid}/overdue` | `CustomerOverdue.razor` | Customer overdue |
| `/customers/{CustomerId:guid}/credit/new` | `CreditCreate.razor` | New credit |
| `/customers/{CustomerId:guid}/credit/{CreditEntryId:guid}` | `CreditDetail.razor` | Credit detail |
| `/customers/{CustomerId:guid}/repayments/new` | `RepaymentCreate.razor` | New repayment |
| `/customers/{CustomerId:guid}/repayments/{RepaymentId:guid}` | `RepaymentDetail.razor` | Repayment detail |
| `/customers/.../receipt` | `RepaymentReceipt.razor` | Receipt + share |
| `/overdue` | `OverdueList.razor` | Org overdue |

### 3.7 Shifts / registers

| Path | Component | Purpose |
|---|---|---|
| `/shifts` | `Shifts/ShiftsList.razor` | Shifts |
| `/shifts/open` | `ShiftOpen.razor` | Open shift |
| `/shifts/{ShiftId:guid}` | `ShiftDetail.razor` | Shift detail / close |
| `/registers` | `Registers/RegistersList.razor` | Registers |
| `/registers/new` | `RegisterCreate.razor` | Create |
| `/registers/{RegisterId:guid}` | `RegisterDetail.razor` | Detail |
| `/registers/{RegisterId:guid}/edit` | `RegisterEdit.razor` | Edit |

### 3.8 Purchasing / suppliers

| Path | Component | Purpose |
|---|---|---|
| `/purchasing` | `Purchasing/PurchasingHub.razor` | Hub |
| `/purchasing/orders` | `PurchasingList.razor` | PO list |
| `/purchasing/new` | `PurchasingCreate.razor` | Create PO |
| `/purchasing/{PurchaseOrderId:guid}` | `PurchasingDetail.razor` | PO detail |
| `/purchasing/{PurchaseOrderId:guid}/receive` | `PurchasingReceive.razor` | Receive PO |
| `/purchasing/receive-stock` | `ReceiveStock.razor` | Receive stock |
| `/purchasing/receipts` | `GoodsReceipts.razor` | Goods receipts |
| `/purchasing/direct-purchases` | `DirectPurchasesList.razor` | Direct purchases |
| `/purchasing/direct-purchases/{ReceiptId:guid}` | `DirectPurchaseDetail.razor` | Direct detail |
| `/connected-suppliers/incoming` | `ConnectedSupplierIncomingOrders.razor` | Incoming orders |
| `/connected-suppliers/incoming/{OrderId:guid}` | `ConnectedSupplierIncomingOrderDetail.razor` | Incoming detail |
| `/suppliers` | `Suppliers/SuppliersList.razor` | Suppliers |
| `/suppliers/new` | `SupplierCreate.razor` | Create |
| `/suppliers/{SupplierId:guid}` | `SupplierDetail.razor` | Detail |
| `/suppliers/{SupplierId:guid}/edit` | `SupplierEdit.razor` | Edit |
| `/suppliers/{SupplierId:guid}/linked-products` | `LinkedSupplierProducts.razor` | Linked products |
| `/suppliers/{SupplierId:guid}/connected-catalog` | `ConnectedSupplierCatalog.razor` | Connected catalog |
| `/suppliers/connected/request` | `ConnectedSupplierRequest.razor` | Request (+ QR scan) |
| `/suppliers/connected/requests` | `ConnectedSupplierIncomingRequests.razor` | Incoming requests |
| `/suppliers/connected/buyers` (+ `/{RelationshipId}`) | `ConnectedBuyers.razor` | Connected buyers |
| `/suppliers/connected/buyers/{RelationshipId}/share-products` | `ConnectedBuyerSharePrompt.razor` | Share prompt |
| `/suppliers/connected/buyers/{RelationshipId}/shared-products` | `ConnectedBuyerSharedProducts.razor` | Shared products |

### 3.9 Reports

| Path | Component | Purpose |
|---|---|---|
| `/reports` | `Reporting/ReportsHub.razor` | Hub |
| `/reports/sales` | `SalesReportPage.razor` | Sales |
| `/reports/inventory` | `InventoryReportPage.razor` | Inventory |
| `/reports/expenses` | `ExpensesReportPage.razor` | Expenses |
| `/reports/utang` | `UtangReportPage.razor` | Utang |
| `/reports/operational/{Kind}` | `OperationalReportPage.razor` | Operational kinds |
| `/dashboard` | `DashboardPage.razor` | Dashboard |
| `/expenses`, `/expenses/new`, `/expenses/{ExpenseId:guid}`, `/expenses/categories`, `/expenses/summary` | `Expenses/*.razor` | Expense operations |

### 3.10 Devices

Covered under Auth (`/devices/register`) and Owner (`/org/devices`, `/organization/devices`). No additional device-hardware pages.

### 3.11 Settings / support

| Path | Component | Purpose |
|---|---|---|
| `/settings` | `Settings.razor` | App settings |
| `/settings/cash-handling` | `CashHandlingSettings.razor` | Denomination UI (**not** a hardware drawer) |
| `/settings/support/diagnostics` | `Support/OrgSupportDiagnosticsPage.razor` | Org diagnostics |
| `/not-found` | `NotFound.razor` | Router not-found page |

### 3.12 Development-only

| Path | Component | Gate | Purpose |
|---|---|---|---|
| `/dev/components` | `Dev/ComponentShowcase.razor` | `IAppInfoService` environment Development/Testing | DesignSystem showcase |
| `/dev/offline-foundation` | `Dev/OfflineFoundationDiagnosticsPage.razor` | Linked from Settings | Offline/SQLite diagnostics |

---

## 4. DI / composition audit

Root: `MauiProgram.RegisterApplicationServices` + `AddPosApiClient` + `AddPinoyBusinessPosLocalStore`.

**Complexity:** one large composition root (~90 Maui registrations), ~20 typed HttpClients, and a full LocalStore graph. `IOfflineReconnectAutoSync.Start()` runs at `CreateMauiApp()` after `Build()`.

React must **not** reproduce this as one giant provider tree. Future composition should split by concern (auth/session, workspace, HTTP, offline coordination, selling session, UI preferences, device adapters).

### 4.1 UI preferences

`MauiThemePreferenceStore`, `MauiDensityPreferenceStore`, `MauiCulturePreferenceStore`, `MauiOnboardingPreferenceStore`, `ThemeController`, `DensityController`, `CultureController`, `ApiStatusLocalizer`, `BuyerShareDraftState`, `PurchaseOrderDraftSession`

Startup cultures in `MauiProgram`: `en` and `fil-PH`, with `en` as the process default before preference restore (aligns with MOBILE-D-064 planning defaults; this is current MAUI evidence, not a new decision).

### 4.2 Auth / session

`MauiSecureTokenStore`, `SecureSessionStore`, `CurrentUserContext`, `LoggingAuthEventSink`, `ProductAccessResolver`, `AuthenticationService` (+ token recovery), `PostSignInReturnRoute`, `AuthShellIdentityState`, `ProtectedShellAccessPolicy`, offline grant store/service, `DeviceRecoveryCredentialStore`, `OfflineSessionUxState`, `LocalValidationClientOptions`

### 4.3 Workspace / context

`OwnerAccessibleBranchResolver`, `WorkspaceSelectionService`, `PlatformOrganizationOwnerProbe`, `WorkspaceGovernanceGate`, `StoreHeaderState`, `ShellNotificationUnreadState`, `RoleHomeResolver`, `NavigationGate`, `SellingModeService`, `OnlineRequiredGuard`, scoped `OfflineAwareNavigation`, `PosOfflineCapabilityPolicy`

### 4.4 API clients

`AddPosApiClient`: dual bases `PosApi:BaseUrl` (Platform) and `PosBusinessApi:BaseUrl` (POS). Typed clients for customers, catalog, import, sales, customer orders, payment attempts, inventory, expenses, suppliers, connected suppliers, registers, operational setup, privacy, POs, direct purchases, cashier shifts, branches, returns, permissions, reporting, offline probe, linked customers. Handlers: bearer, session, org/device/commercial, reachability, Dev user header, token recovery.

### 4.5 Offline / store

`MauiLocalStoreRootPathProvider`, LocalStore factory/migrator/context, encrypted queue, customer credit + personal utang stores, selling catalog/cash sale store, connected-supplier stores, queue processor/retry/revalidator, `OfflineFoundationDiagnostics`

### 4.6 Sync

Offline operation dispatchers (dev probe, customer/credit/repayment, sale checkout, catalog create, personal ops), customer/credit/personal/catalog/selling-catalog/linked-supplier sync services, `OfflineReconnectAutoSyncService`, `PosSyncStatusService`, `PosStatusState`, diagnostics sync retry

### 4.7 Selling

`SaleCartService` — **in-memory session cart**; MauiProgram comment: never persisted or queued; clears on sign-out or organization switch.  
`MauiPendingPaymentStore` — identifiers only.  
`PersonalMerchantCart`

### 4.8 Catalog

`MauiProductImageCacheRoot`, `ProductImageThumbnailCache`, `PendingProductImageStore`, `AdoptedTemplateThumbnailPrefetch`, `MauiProductImagePicker`

### 4.9 Customer / credit

Customer/credit/repayment dispatchers and sync services; `UtangCapabilityEvaluator`

### 4.10 Personal

`PersonalMerchantCart`, personal offline dispatchers/sync, Personal support diagnostics provider

### 4.11 Device adapters

`MauiConnectivityService`, `MauiQrCodeScanService`, `MauiDocumentHandoffService`, `MauiAppInfoService`, `MauiProductImagePicker`

### 4.12 Diagnostics / support

`SupportDiagnosticsService` + Personal/Org providers, `PosEffectiveRoleReader`, `OfflineFoundationDiagnostics`

### 4.13 Other

`TimeProvider.System`, `AddLocalization()`, fonts. Debug: BlazorWebView developer tools + debug logging.

---

## 5. Native / device dependencies (current source)

| Dependency | Status | Evidence |
|---|---|---|
| **SecureStorage** | Implemented | `MauiSecureTokenStore.cs` — session secrets, never passwords |
| **Connectivity** | Implemented | `MauiConnectivityService.cs` — OS network access, not API health |
| **Preferences** | Implemented | Theme/density/culture/onboarding; SignIn remember-me; `MauiPendingPaymentStore` identifiers |
| **MediaPicker** | Implemented | `MauiQrCodeScanService`, `MauiProductImagePicker` — capture/pick still image |
| **QR decode** | Implemented | ZXing `BarcodeFormat.QR_CODE` only; still-image; **no live camera view** |
| **QR generate** | Implemented | `LocalQrCodeRenderer.cs` (QRCoder) |
| **Share** | Implemented | `MauiDocumentHandoffService` — `Share.Default.RequestAsync`; reports initiated, never print/save success |
| **FileSystem** | Implemented | `FileSystem.AppDataDirectory` for LocalStore root and product image cache |
| **Camera permission** | Implemented | Android `CAMERA` + MAUI `Permissions.Camera` |
| **Keyboard inset** | Host chrome | `MainActivity` `AdjustResize` + `wwwroot/keyboard-inset.js` — not a MAUI Keyboard API |
| **WebAuthenticator** | Implemented callback | `exitspos://auth/callback` |
| **HID / scanner SDK** | **Absent** | Barcode is typed text (`CatalogBarcodeLookup` / checkout search) |
| **Printer SDK** | **Absent** | Share is not print |
| **Cash drawer kick** | **Absent** | Cash-handling settings are denomination UI |
| **NFC** | **Absent** | No NFC permission or API |
| **Payment terminal SDK** | **Absent** | `HandleTerminalAttemptAsync` is payment-attempt **status** UI, not a card terminal |

---

## 6. Local Validation / configuration

Debug strategy (current):

- `#if DEBUG` `MauiProgram.ConfigureAppConfiguration` injects in-memory `PosApi` / `PosBusinessApi` HTTP Local Validation hosts and enables `LocalValidation`.
- Default Debug target: `PosLocalValidationTarget=PhysicalDevice` (Tailscale/LAN PublicHost). Emulator loopback is opt-in (`-p:PosLocalValidationTarget=Emulator`).
- Embedded JSON: Debug `wwwroot/appsettings.json`; PhysicalDevice overlay `appsettings.LocalValidation.PhysicalDevice.json`; Release `appsettings.Release.json` HTTPS placeholders.
- Release: `Security:RequireHttpsApiUrls=true` and HTTPS-only network security config.
- Sign-in uses `IOptions<LocalValidationClientOptions>` for Debug quick login when enabled.
- Dev pages use `MauiAppInfoService` environment `"Development"` (DEBUG) / `"Production"` (Release).

**DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED**

A shared Development password is compiled into Debug `MauiProgram` (`LocalValidation:SharedPassword`) and is documented there as matching the local-validation env file. This documentation **does not** reproduce the value.

Recommendation for a later authorized package (not this queue): replace the embedded shared password with runtime / developer-secret injection. Do not ship that credential in Release.

---

## 7. Current hardware truth

| Capability | Implemented? | Notes |
|---|---|---|
| Barcode / HID | **Absent as hardware** | Keyboard/HID wedge can type into search; no scanner SDK |
| Product barcode camera | **Absent** | QR path is ExItS identity QR, not live product barcode |
| ExItS QR | **Implemented** | Still-image capture or gallery; generate PNG |
| Camera photos | **Implemented** | Product image pick/capture |
| Share | **Implemented** | OS share sheet |
| Thermal / Bluetooth / USB printer | **Absent** | |
| Physical cash drawer | **Absent** | Logical shift cash only |
| NFC | **Absent** | |
| Real payment terminal | **Absent** | Fake/manual electronic attempts are status UX; no acquirer SDK |

**VERIFIED** vs [device-and-payment-integration.md](../device-and-payment-integration.md) §0.

---

## 8. Current build complexity

| Topic | Current MAUI cost | React/PWA/Capacitor note |
|---|---|---|
| Workload | .NET MAUI 10 (`Microsoft.Maui.Controls` / `WebView.Maui` 10.0.0) | Browser/PWA would not need the MAUI Android workload |
| TFM | `net10.0-android` + explicit RIDs | Capacitor later uses its own Android toolchain |
| Debug APK | `EmbedAssembliesIntoApk=true` for sideload without Fast Deployment | Different packaging model |
| Release trim/AOT | Explicitly **off** (`AndroidLinkMode=None`, `RunAOTCompilation=false`) so plain Release build stays reliable | Do not claim performance wins without measurement |
| Signing | No keystore properties in csproj | Future Capacitor signing is a separate open decision |
| Extra packages | QRCoder, ZXing.Net, Localization, Http, Configuration.Json | Future adapters replace these; not selected here |
| Project refs | DesignSystem, ApiClient, Application, LocalStore | React must not reference Infrastructure/EF/Npgsql |

This section records **build-surface complexity**. It does **not** claim that React will be faster.

---

## 9. Status deltas vs approved `current-state-and-replacement-boundaries.md`

Approved file evidence baseline was `5a9be941…`. This refresh uses `5979a9ce…` (required main for this queue). Historical reports remain historical.

| Topic | Status | Notes |
|---|---|---|
| Host is MAUI Blazor Hybrid, Android-first | **VERIFIED** | Still `net10.0-android`, BlazorWebView, `com.exits.pinoybusinesspos` |
| Host is not POS-only | **VERIFIED** | Auth + Personal + Owner + POS Operations |
| Three shells; Owner is capability inside PosShell | **VERIFIED** | No fourth shell invented |
| Organization Web is not checkout | **VERIFIED** | Not re-audited as a rewrite; still a different host |
| Personal Web is not the Mobile Client | **VERIFIED** | |
| Platform Admin stays Web-only | **VERIFIED** | |
| LocalStore encrypted outbox consumed by MAUI | **VERIFIED** | |
| Auth is Platform-authoritative Bearer in SecureStorage | **VERIFIED** | |
| CURRENT_IMPLEMENTATION_REQUIREMENT (native CSS / Razor on MAUI) | **VERIFIED** | Unchanged for the current host |
| PROPOSED React path `ExItS.PinoyBusinessPOS.React/` | **VERIFIED** | Still **not created** |
| Representative route lists in DOC-01 | **DELTA (inventory, not architecture)** | DOC-01 listed examples. Current main has **171** `@page` templates including connected-supplier, expenses, permissions-assignment, catalog import jobs, local sale receipt. Architecture groups still match. |
| Min SDK / RIDs / display version | **DELTA** | Now recorded: API 24, `android-arm64;android-x64`, `0.5.0` |
| OAuth callback activity | **DELTA** | `exitspos://auth/callback` present |
| Debug shared Local Validation password in source | **DELTA** | Flag only: `DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED` |
| In-memory cart | **DELTA (clarification)** | `SaleCartService` is session memory, not LocalStore. Approved selling UX still wants a session-persistent cart in the **future** client (MOBILE-D-019). |
| Hardware matrix | **VERIFIED** | QR/share/camera present; printer/drawer/NFC/terminal absent |

No accepted MOBILE-D decision is changed by these deltas.

---

## 10. Authorization lock (repeat)

| Item | Status |
|---|---|
| React implementation | **NOT AUTHORIZED** |
| Create `ExItS.PinoyBusinessPOS.React` | **NOT AUTHORIZED** |
| Modify MAUI | **NOT AUTHORIZED** (this package did not) |
| PWA production | **NOT AUTHORIZED** |
| Capacitor production | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| API / database change | **NOT AUTHORIZED** |
