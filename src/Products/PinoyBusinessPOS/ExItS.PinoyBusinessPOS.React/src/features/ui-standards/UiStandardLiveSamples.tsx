import { useMemo, useState, type ReactNode } from "react";
import {
  Building2,
  ClipboardList,
  Coins,
  Inbox,
  LayoutDashboard,
  Loader2,
  MoreHorizontal,
  PackageCheck,
  Receipt,
  ShoppingCart,
  Truck,
  UserRound,
  Users,
  Wallet,
} from "lucide-react";
import {
  getActionButtonStyle,
  getActionIcon,
} from "@/components/exits/action-semantics";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsMultiSelect } from "@/components/exits/ExitsMultiSelect";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { ModuleSubnav } from "@/components/exits/ModuleSubnav";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { TableActionButton } from "@/components/exits/TableActionButton";
import { useExitsToast } from "@/components/exits/ToastProvider";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { CreatableCombobox } from "@/components/exits/CreatableCombobox";
import { Button, buttonIconMotion } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { SettingsSelect } from "@/components/ui/settings-select";
import { Switch } from "@/components/ui/switch";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { FilterChip } from "@/components/exits/FilterChip";
import type { PosSaleDto } from "@/api/pos/pos-sales-client";
import { CustomerPurchaseSummaryDocument } from "@/features/documents/SaleBusinessDocument";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";
import type { UiStandardLiveCardId } from "@/features/ui-standards/ui-standard-catalog";

const DEMO_INVOICE_IDENTITY = {
  businessName: "Paul Coffee",
  address: "Iloilo City",
  phone: "0917 000 0000",
  email: "hello@paulcoffee.demo",
  branchName: "Main branch",
};

const DEMO_INVOICE_VISIBILITY = {
  showLogo: false,
  showBusinessName: true,
  showBusinessAddress: true,
  showBusinessPhone: true,
  showBusinessEmail: true,
  showWebsite: false,
  showBranchName: true,
  showBranchAddress: false,
};

const DEMO_INVOICE_SALE = {
  saleId: "11111111-1111-4111-8111-111111111111",
  organizationId: "22222222-2222-4222-8222-222222222222",
  saleNumber: "S-20260915-000042",
  status: "Completed",
  paymentMethod: "Cash",
  subtotal: 250,
  total: 230,
  taxAmount: 0,
  discountTotal: 20,
  recordedAtUtc: "2026-09-15T10:00:00Z",
  recordedBy: "33333333-3333-4333-8333-333333333333",
  lines: [
    {
      saleLineId: "44444444-4444-4444-8444-444444444444",
      productId: "55555555-5555-4555-8555-555555555555",
      lineNumber: 1,
      name: "Espresso",
      sku: "ESP",
      unitOfMeasure: "pc",
      sellingMode: "Unit",
      unitPrice: 50,
      quantity: 2,
      lineTotal: 100,
      lineDiscountAmount: 0,
    },
    {
      saleLineId: "66666666-6666-4666-8666-666666666666",
      productId: "77777777-7777-4777-8777-777777777777",
      lineNumber: 2,
      name: "Croissant",
      sku: "CRO",
      unitOfMeasure: "pc",
      sellingMode: "Unit",
      unitPrice: 75,
      quantity: 2,
      lineTotal: 150,
      lineDiscountAmount: 20,
    },
  ],
  customerDisplayName: "Ada Reyes",
} as PosSaleDto;

function SectionLabel({ children }: { children: ReactNode }) {
  return (
    <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
      {children}
    </p>
  );
}

export type UiStandardLiveSamplesProps = {
  /** When omitted, all live cards render. */
  visibleCardIds?: ReadonlySet<UiStandardLiveCardId>;
};

/**
 * TASK-66A live production-component samples — ONE CARD per category.
 * Demo-only: no backend calls.
 */
export function UiStandardLiveSamples({ visibleCardIds }: UiStandardLiveSamplesProps) {
  const toast = useExitsToast();
  const [confirmVariant, setConfirmVariant] = useState<"default" | "warning" | "danger" | null>(
    null,
  );
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [contentTab, setContentTab] = useState("overview");
  const [kindTab, setKindTab] = useState("personal");
  const [filterSegment, setFilterSegment] = useState("all");
  const [purchasingDest, setPurchasingDest] = useState("incoming");
  const [selectableWarehouse, setSelectableWarehouse] = useState("main");
  const [search, setSearch] = useState("");
  const [notify, setNotify] = useState(true);
  const [allowAccess, setAllowAccess] = useState(true);
  const [paymentTerms, setPaymentTerms] = useState("30");
  const [paymentMethod, setPaymentMethod] = useState("Cash");
  const [amount, setAmount] = useState("1,250.00");
  const [selectedRow, setSelectedRow] = useState("1");
  const [standardSelect, setStandardSelect] = useState("Cash");
  const [searchSelect, setSearchSelect] = useState("iloilo");
  const [multiSelect, setMultiSelect] = useState<string[]>(["Cash", "ManualGCash"]);
  const [branchFilter, setBranchFilter] = useState("all");
  const [comboboxValue, setComboboxValue] = useState("Sales");
  const [comboboxOptions, setComboboxOptions] = useState(["Sales", "Kitchen", "Warehouse"]);
  const [densitySelect, setDensitySelect] = useState<"compact" | "balance" | "comfort">("balance");
  const [themeSelect, setThemeSelect] = useState<"system" | "light" | "dark">("system");
  const [selectSearch, setSelectSearch] = useState("");
  const [selectSwitch, setSelectSwitch] = useState(true);
  const [sizeSingle, setSizeSingle] = useState("M");
  const [sizeMulti, setSizeMulti] = useState<string[]>(["S", "L", "XL"]);

  const show = (id: UiStandardLiveCardId) => !visibleCardIds || visibleCardIds.has(id);

  const SaveIcon = getActionIcon("save");
  const CreateIcon = getActionIcon("create");
  const AddIcon = getActionIcon("add");
  const EditIcon = getActionIcon("edit");
  const ActivateIcon = getActionIcon("activate");
  const DeactivateIcon = getActionIcon("deactivate");
  const DeleteIcon = getActionIcon("delete");
  const RetryIcon = getActionIcon("retry");

  const tableRows = useMemo(
    () => [
      {
        id: "1",
        customer: "Juan Dela Cruz",
        type: "Personal",
        balance: "₱1,250.00",
        status: "Active" as const,
        statusTone: "success" as const,
      },
      {
        id: "2",
        customer: "Paul Coffee",
        type: "Business",
        balance: "₱8,500.00",
        status: "Pending" as const,
        statusTone: "warning" as const,
      },
      {
        id: "3",
        customer: "Ana Santos",
        type: "Personal",
        balance: "₱0.00",
        status: "Active" as const,
        statusTone: "success" as const,
      },
    ],
    [],
  );

  if (visibleCardIds && visibleCardIds.size === 0) {
    return null;
  }

  return (
    <>
      <div className="grid min-w-0 gap-3 lg:grid-cols-2" data-testid="ui-standard-cards">
        {show("buttons") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-buttons">
            <CardTitle>Buttons</CardTitle>
            <div className="flex flex-col gap-2">
              <SectionLabel>Common actions</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <Button type="button" {...getActionButtonStyle("save")}>
                  {SaveIcon ? <SaveIcon className="size-4" aria-hidden /> : null}
                  Save
                </Button>
                <Button type="button" {...getActionButtonStyle("create")}>
                  {CreateIcon ? (
                    <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Create
                </Button>
                <Button type="button" {...getActionButtonStyle("add")}>
                  {AddIcon ? (
                    <AddIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Add
                </Button>
                <Button type="button" {...getActionButtonStyle("edit")}>
                  {EditIcon ? <EditIcon className="size-4" aria-hidden /> : null}
                  Edit
                </Button>
                <Button type="button" {...getActionButtonStyle("cancel")}>
                  Cancel
                </Button>
                <Button type="button" {...getActionButtonStyle("activate")}>
                  {ActivateIcon ? <ActivateIcon className="size-4" aria-hidden /> : null}
                  Activate
                </Button>
                <Button type="button" {...getActionButtonStyle("deactivate")}>
                  {DeactivateIcon ? <DeactivateIcon className="size-4" aria-hidden /> : null}
                  Deactivate
                </Button>
                <Button type="button" {...getActionButtonStyle("delete")}>
                  {DeleteIcon ? (
                    <DeleteIcon className={`size-4 ${buttonIconMotion.delete}`} aria-hidden />
                  ) : null}
                  Delete
                </Button>
              </div>
            </div>
            <div
              className="flex flex-col gap-2 border-t border-border pt-3"
              data-testid="ui-standard-hierarchy-good"
            >
              <SectionLabel>One Primary per group</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <Button type="button" {...getActionButtonStyle("create")}>
                  {CreateIcon ? (
                    <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Create customer
                </Button>
                <Button type="button" {...getActionButtonStyle("add", { isPrimaryInGroup: false })}>
                  {AddIcon ? <AddIcon className="size-4" aria-hidden /> : null}
                  Add existing
                </Button>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Intent / Tone</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {(
                  [
                    ["Primary", "primary"],
                    ["Neutral", "neutral"],
                    ["Success", "success"],
                    ["Info", "info"],
                    ["Warning", "warning"],
                    ["Danger", "danger"],
                  ] as const
                ).map(([label, intent]) => (
                  <Button
                    key={intent}
                    type="button"
                    intent={intent}
                    appearance="solid"
                    data-testid={`ui-standard-intent-${intent}`}
                  >
                    {label}
                  </Button>
                ))}
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Intent = semantic meaning. All samples use Solid appearance.
              </p>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Appearance / Treatment</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {(
                  [
                    ["Solid", "solid"],
                    ["Outline", "outline"],
                    ["Ghost", "ghost"],
                    ["Elevated", "elevated"],
                    ["Gradient", "gradient"],
                  ] as const
                ).map(([label, appearance]) => (
                  <Button
                    key={appearance}
                    type="button"
                    intent="primary"
                    appearance={appearance}
                    data-testid={`ui-standard-appearance-${appearance}`}
                  >
                    {label}
                  </Button>
                ))}
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Appearance = how it is drawn. All samples use Primary intent. Gradient is
                brand-primary preferred.
              </p>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Shape</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {(
                  [
                    ["Standard", "standard"],
                    ["Soft", "soft"],
                    ["Pill", "pill"],
                  ] as const
                ).map(([label, shape]) => (
                  <Button
                    key={shape}
                    type="button"
                    intent="primary"
                    appearance="solid"
                    shape={shape}
                    data-testid={`ui-standard-shape-${shape}`}
                  >
                    {label}
                  </Button>
                ))}
              </div>
              <div className="flex flex-wrap gap-2">
                {(
                  [
                    ["standard", "Save"],
                    ["soft", "Create"],
                    ["pill", "Add"],
                  ] as const
                ).map(([shape, label]) => (
                  <Button
                    key={`shape-icon-${shape}`}
                    type="button"
                    intent="primary"
                    appearance="solid"
                    shape={shape}
                    data-testid={`ui-standard-shape-icon-${shape}`}
                  >
                    {CreateIcon ? (
                      <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                    ) : null}
                    {label}
                  </Button>
                ))}
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Shape = geometry. All samples use Primary + Solid. Default product controls use
                Auto (follows Control Shape preference). Second row shows with-icon variety.
              </p>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>States</SectionLabel>
              <div className="flex flex-wrap items-center gap-2">
                <Button type="button" intent="primary" appearance="solid" disabled>
                  {SaveIcon ? <SaveIcon className="size-4" aria-hidden /> : null}
                  Save
                </Button>
                <Button type="button" intent="primary" appearance="solid" disabled aria-busy="true">
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                  Saving…
                </Button>
                <TableActionButton action="edit" label="Edit" testId="ui-standard-table-action-edit" />
                <TableActionButton
                  action="delete"
                  label="Delete"
                  testId="ui-standard-table-action-delete"
                />
              </div>
            </div>
          </Card>
        ) : null}

        {show("toasts") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-toasts">
            <CardTitle>Toasts</CardTitle>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="success"
                data-testid="ui-standard-toast-success"
                onClick={() =>
                  toast.success("Changes saved", "Your changes were saved successfully.")
                }
              >
                Success
              </Button>
              <Button
                type="button"
                variant="info"
                data-testid="ui-standard-toast-info"
                onClick={() => toast.info("Quotation saved", "Quotation was saved as a draft.")}
              >
                Info
              </Button>
              <Button
                type="button"
                variant="warning"
                data-testid="ui-standard-toast-warning"
                onClick={() =>
                  toast.warning("Check pending", "The check is waiting for clearing.")
                }
              >
                Warning
              </Button>
              <Button
                type="button"
                variant="destructive"
                data-testid="ui-standard-toast-error"
                onClick={() =>
                  toast.error("Payment failed", "The payment could not be recorded.")
                }
              >
                Error
              </Button>
            </div>
          </Card>
        ) : null}

        {show("confirm") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-confirm">
            <CardTitle>Confirm Dialog</CardTitle>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                data-testid="ui-standard-confirm-default"
                onClick={() => setConfirmVariant("default")}
              >
                Confirm
              </Button>
              <Button
                type="button"
                variant="warning"
                data-testid="ui-standard-confirm-deactivate"
                onClick={() => setConfirmVariant("warning")}
              >
                {DeactivateIcon ? <DeactivateIcon className="size-4" aria-hidden /> : null}
                Deactivate
              </Button>
              <Button
                type="button"
                variant="destructive"
                data-testid="ui-standard-confirm-delete"
                onClick={() => setConfirmVariant("danger")}
              >
                {DeleteIcon ? <DeleteIcon className="size-4" aria-hidden /> : null}
                Delete
              </Button>
            </div>
          </Card>
        ) : null}

        {show("drawer") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-drawer">
            <CardTitle>Form Drawer</CardTitle>
            <Button
              type="button"
              variant="outline"
              data-testid="ui-standard-open-drawer"
              onClick={() => setDrawerOpen(true)}
            >
              {EditIcon ? <EditIcon className="size-4" aria-hidden /> : null}
              Open edit drawer
            </Button>
          </Card>
        ) : null}

        {show("modal") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-modal">
            <CardTitle>Modal</CardTitle>
            <Button
              type="button"
              variant="outline"
              data-testid="ui-standard-open-modal"
              onClick={() => {
                setAmount("1,250.00");
                setPaymentMethod("Cash");
                setModalOpen(true);
              }}
            >
              Open modal
            </Button>
          </Card>
        ) : null}

        {show("status") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-status">
            <CardTitle>Status & Chips</CardTitle>
            <div className="flex flex-col gap-2">
              <SectionLabel>Tone</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="neutral">Draft</StatusChip>
                <StatusChip tone="info">Processing</StatusChip>
                <StatusChip tone="success">Active</StatusChip>
                <StatusChip tone="warning">Pending</StatusChip>
                <StatusChip tone="danger">Declined</StatusChip>
                <StatusChip tone="primary">Preferred</StatusChip>
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Tone = semantic meaning. All samples use Soft appearance (default).
              </p>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Appearance</SectionLabel>
              <div className="flex flex-col gap-2.5">
                {(
                  [
                    ["Soft", "soft"],
                    ["Outline", "outline"],
                    ["Solid", "solid"],
                  ] as const
                ).map(([label, appearance]) => (
                  <div key={appearance} className="flex min-w-0 flex-col gap-1.5">
                    <span className="text-[length:var(--exits-text-xs)] text-muted">{label}</span>
                    <div className="flex flex-wrap gap-2">
                      <StatusChip appearance={appearance} tone="neutral">
                        Draft
                      </StatusChip>
                      <StatusChip appearance={appearance} tone="info">
                        Processing
                      </StatusChip>
                      <StatusChip appearance={appearance} tone="success">
                        Active
                      </StatusChip>
                      <StatusChip appearance={appearance} tone="warning">
                        Pending
                      </StatusChip>
                      <StatusChip appearance={appearance} tone="danger">
                        Declined
                      </StatusChip>
                      <StatusChip appearance={appearance} tone="primary">
                        Preferred
                      </StatusChip>
                    </div>
                  </div>
                ))}
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Appearance = how it is drawn. Soft is default; Outline / Solid are optional.
                Independent from shape (pill / soft / square).
              </p>
            </div>
          </Card>
        ) : null}

        {show("forms") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-forms">
            <CardTitle>Form Controls</CardTitle>
            <Input
              label="Customer name"
              name="ui-std-customer-name"
              defaultValue="Juan Dela Cruz"
              required
            />
            <SearchField
              label="Search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onClear={() => setSearch("")}
              placeholder="Search customers..."
              testId="ui-standard-search"
            />
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
              <span className="exits-type-label">Payment terms</span>
              <ExitsSelect
                value={paymentTerms}
                options={[
                  { value: "15", label: "Net 15" },
                  { value: "30", label: "Net 30" },
                  { value: "60", label: "Net 60" },
                ]}
                onChange={setPaymentTerms}
                menuLabel="Payment terms"
                testId="ui-standard-payment-terms"
              />
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                Default credit term in days.
              </span>
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
              <span className="exits-type-label">Notes</span>
              <textarea
                className="exits-input min-h-20 rounded-[var(--exits-field-radius)] border border-border bg-surface px-[var(--exits-control-padding-x)] py-2"
                defaultValue="Preferred morning delivery"
                data-testid="ui-standard-notes"
              />
            </label>
            <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <input
                type="checkbox"
                className="size-4"
                checked={notify}
                onChange={(e) => setNotify(e.target.checked)}
                data-testid="ui-standard-checkbox-notify"
              />
              Send notification
            </label>
            <div className="flex items-center gap-2">
              <Switch
                aria-label="Allow customer access"
                checked={allowAccess}
                onCheckedChange={setAllowAccess}
                showStateLabel={false}
                data-testid="ui-standard-switch-access"
              />
              <span className="text-[length:var(--exits-text-sm)]">Allow customer access</span>
            </div>
            <Input label="Organization ID" name="ui-std-org-id" disabled defaultValue="ORG436352" />
            <div className="flex flex-col gap-1.5">
              <Input
                label="Email"
                name="ui-std-email"
                aria-invalid
                defaultValue="not-an-email"
                data-testid="ui-standard-email-invalid"
              />
              <span className="text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]">
                Enter a valid email address.
              </span>
            </div>
          </Card>
        ) : null}

        {show("selects") ? (
          <Card
            className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2"
            data-testid="ui-standard-card-selects"
          >
            <CardTitle>Selects</CardTitle>
            <div className="grid min-w-0 gap-4 md:grid-cols-2">
              <div className="flex min-w-0 flex-col gap-3">
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Standard — ExitsSelect</SectionLabel>
                  <ExitsSelect
                    value={standardSelect}
                    options={[
                      { value: "Cash", label: "Cash" },
                      { value: "ManualGCash", label: "Manual GCash" },
                      { value: "Check", label: "Check" },
                    ]}
                    onChange={setStandardSelect}
                    menuLabel="Payment method"
                    testId="ui-standard-select-standard"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Searchable — long lists</SectionLabel>
                  <ExitsSelect
                    value={searchSelect}
                    searchable
                    searchPlaceholder="Search branch…"
                    options={[
                      { value: "iloilo", label: "Iloilo Main" },
                      { value: "mandurriao", label: "Mandurriao" },
                      { value: "jaro", label: "Jaro" },
                      { value: "molo", label: "Molo" },
                      { value: "arevalo", label: "Arevalo" },
                      { value: "lapaz", label: "La Paz" },
                    ]}
                    onChange={setSearchSelect}
                    menuLabel="Branch"
                    testId="ui-standard-select-searchable"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Multi select — ExitsMultiSelect</SectionLabel>
                  <ExitsMultiSelect
                    value={multiSelect}
                    searchable
                    options={[
                      { value: "Cash", label: "Cash" },
                      { value: "ManualGCash", label: "Manual GCash" },
                      { value: "Check", label: "Check" },
                      { value: "BankTransfer", label: "Bank transfer" },
                      { value: "Card", label: "Card" },
                    ]}
                    onChange={setMultiSelect}
                    placeholder="Payment methods…"
                    menuLabel="Payment methods"
                    testId="ui-standard-select-multi"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Pill select — attribute (single)</SectionLabel>
                  <ExitsPillSelect
                    aria-label="Size"
                    value={sizeSingle}
                    onChange={setSizeSingle}
                    options={[
                      { value: "XS", label: "XS" },
                      { value: "S", label: "S" },
                      { value: "M", label: "M" },
                      { value: "L", label: "L" },
                      { value: "XL", label: "XL" },
                      { value: "XXL", label: "XXL" },
                    ]}
                    testId="ui-standard-select-pill-single"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Pill select — multi</SectionLabel>
                  <ExitsPillSelect
                    mode="multi"
                    aria-label="Available sizes"
                    value={sizeMulti}
                    onChange={setSizeMulti}
                    options={[
                      { value: "XS", label: "XS" },
                      { value: "S", label: "S" },
                      { value: "M", label: "M" },
                      { value: "L", label: "L" },
                      { value: "XL", label: "XL" },
                      { value: "XXL", label: "XXL" },
                    ]}
                    testId="ui-standard-select-pill-multi"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Creatable combobox</SectionLabel>
                  <CreatableCombobox
                    value={comboboxValue}
                    options={comboboxOptions}
                    onChange={(next) => {
                      setComboboxValue(next);
                      if (!comboboxOptions.includes(next)) {
                        setComboboxOptions((prev) => [...prev, next]);
                      }
                    }}
                    placeholder="Department…"
                    testId="ui-standard-select-combobox"
                  />
                </div>
              </div>

              <div className="flex min-w-0 flex-col gap-3">
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Search field</SectionLabel>
                  <SearchField
                    label="Filter options"
                    value={selectSearch}
                    onChange={(e) => setSelectSearch(e.target.value)}
                    onClear={() => setSelectSearch("")}
                    placeholder="Search…"
                    testId="ui-standard-select-search-field"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Switch — boolean select</SectionLabel>
                  <div className="flex items-center gap-2">
                    <Switch
                      aria-label="Online payments"
                      checked={selectSwitch}
                      onCheckedChange={setSelectSwitch}
                      data-testid="ui-standard-select-switch"
                    />
                    <span className="text-[length:var(--exits-text-sm)]">Online payments</span>
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Filter chips — exclusive</SectionLabel>
                  <div className="flex flex-wrap gap-2" role="group" aria-label="Branch filter">
                    {(
                      [
                        ["all", "All"],
                        ["active", "Active"],
                        ["inactive", "Inactive"],
                      ] as const
                    ).map(([key, label]) => (
                      <FilterChip
                        key={key}
                        selected={branchFilter === key}
                        onClick={() => setBranchFilter(key)}
                        data-testid={`ui-standard-select-filter-${key}`}
                      >
                        {label}
                      </FilterChip>
                    ))}
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Settings — segmented</SectionLabel>
                  <SettingsSelect
                    label="Density"
                    variant="segmented"
                    value={densitySelect}
                    options={[
                      { value: "compact", label: "Compact" },
                      { value: "balance", label: "Balance" },
                      { value: "comfort", label: "Comfort" },
                    ]}
                    onChange={setDensitySelect}
                    testId="ui-standard-select-segmented"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Settings — cards</SectionLabel>
                  <SettingsSelect
                    label="Appearance"
                    variant="cards"
                    value={themeSelect}
                    options={[
                      { value: "system", label: "System" },
                      { value: "light", label: "Light" },
                      { value: "dark", label: "Dark" },
                    ]}
                    onChange={setThemeSelect}
                    testId="ui-standard-select-cards"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <SectionLabel>Disabled / invalid</SectionLabel>
                  <div className="grid gap-2">
                    <ExitsSelect
                      value="Cash"
                      options={[{ value: "Cash", label: "Cash" }]}
                      onChange={() => undefined}
                      disabled
                      testId="ui-standard-select-disabled"
                    />
                    <ExitsSelect
                      value="Cash"
                      options={[
                        { value: "Cash", label: "Cash" },
                        { value: "Check", label: "Check" },
                      ]}
                      onChange={setStandardSelect}
                      invalid
                      testId="ui-standard-select-invalid"
                    />
                  </div>
                </div>
              </div>
            </div>
          </Card>
        ) : null}

        {show("nav") ? (
          <Card
            className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2"
            data-testid="ui-standard-card-nav"
          >
            <CardTitle>Navigation & Selection</CardTitle>
            <div className="flex flex-col gap-2">
              <SectionLabel>Tabs — same-view sections</SectionLabel>
              <UnderlineTabBar
                ariaLabel="Demo content tabs"
                activeKey={contentTab}
                onChange={setContentTab}
                items={[
                  { key: "overview", label: "Overview", icon: LayoutDashboard },
                  { key: "transactions", label: "Transactions", icon: Receipt, count: 12 },
                  { key: "payments", label: "Payments", icon: Wallet, count: 3 },
                ]}
              />
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Soft / Pill tabs</SectionLabel>
              <ExitsTabs
                variant="soft"
                ariaLabel="Demo kind soft tabs"
                value={kindTab}
                onValueChange={setKindTab}
                items={[
                  { key: "personal", label: "Personal" },
                  { key: "business", label: "Business" },
                ]}
              />
              <ExitsTabs
                variant="pill"
                ariaLabel="Demo kind pill tabs"
                value={kindTab}
                onValueChange={setKindTab}
                items={[
                  { key: "personal", label: "Personal" },
                  { key: "business", label: "Business" },
                ]}
              />
              <SectionLabel>With icon</SectionLabel>
              <ExitsTabs
                variant="soft"
                ariaLabel="Demo soft tabs with icons"
                value={kindTab}
                onValueChange={setKindTab}
                testId="ui-standard-tabs-soft-icons"
                items={[
                  { key: "personal", label: "Personal", icon: UserRound },
                  { key: "business", label: "Business", icon: Building2 },
                ]}
              />
              <ExitsTabs
                variant="pill"
                ariaLabel="Demo pill tabs with icons"
                value={kindTab}
                onValueChange={setKindTab}
                testId="ui-standard-tabs-pill-icons"
                items={[
                  { key: "personal", label: "Personal", icon: UserRound },
                  { key: "business", label: "Business", icon: Building2 },
                ]}
              />
              <SectionLabel>With count</SectionLabel>
              <ExitsTabs
                variant="soft"
                ariaLabel="Demo soft tabs with counts"
                value={kindTab}
                onValueChange={setKindTab}
                testId="ui-standard-tabs-soft-counts"
                items={[
                  {
                    key: "personal",
                    label: "Personal",
                    icon: UserRound,
                    count: 8,
                    countTone: kindTab === "personal" ? "primary" : "neutral",
                  },
                  {
                    key: "business",
                    label: "Business",
                    icon: Building2,
                    count: 24,
                    countTone: kindTab === "business" ? "primary" : "neutral",
                  },
                ]}
              />
              <ExitsTabs
                variant="pill"
                ariaLabel="Demo pill tabs with counts"
                value={kindTab}
                onValueChange={setKindTab}
                testId="ui-standard-tabs-pill-counts"
                items={[
                  {
                    key: "personal",
                    label: "Personal",
                    icon: UserRound,
                    count: 8,
                    countTone: kindTab === "personal" ? "primary" : "neutral",
                  },
                  {
                    key: "business",
                    label: "Business",
                    icon: Building2,
                    count: 24,
                    countTone: kindTab === "business" ? "primary" : "neutral",
                  },
                ]}
              />
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Segmented — filter / exclusive selection</SectionLabel>
              <ExitsTabs
                variant="segmented"
                ariaLabel="Demo status filter"
                value={filterSegment}
                onValueChange={setFilterSegment}
                items={[
                  { key: "all", label: "All", count: 42 },
                  { key: "active", label: "Active", count: 31 },
                  { key: "inactive", label: "Inactive", count: 11 },
                ]}
              />
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Pill bar — module destinations</SectionLabel>
              <ModuleSubnav
                variant="pillBar"
                activeTreatment="solid"
                ariaLabel="Purchasing destinations pill bar"
                testId="ui-standard-subnav-pillbar-text"
                value={purchasingDest}
                onValueChange={setPurchasingDest}
                items={[
                  { key: "po", label: "Purchase orders", to: "/demo/po" },
                  { key: "incoming", label: "Incoming orders", to: "/demo/incoming" },
                  { key: "receive", label: "Ready to receive", to: "/demo/receive" },
                  { key: "direct", label: "Direct purchases", to: "/demo/direct" },
                  { key: "suppliers", label: "Suppliers", to: "/demo/suppliers" },
                ]}
              />
              <SectionLabel>Pill bar — with icon + count</SectionLabel>
              <ModuleSubnav
                variant="pillBar"
                activeTreatment="solid"
                scrollable
                ariaLabel="Purchasing destinations pill bar with icons"
                testId="ui-standard-subnav-pillbar-icons"
                value={purchasingDest}
                onValueChange={setPurchasingDest}
                items={[
                  {
                    key: "po",
                    label: "Purchase orders",
                    to: "/demo/po",
                    icon: ClipboardList,
                    count: 0,
                  },
                  {
                    key: "incoming",
                    label: "Incoming orders",
                    to: "/demo/incoming",
                    icon: Inbox,
                    count: 2,
                  },
                  {
                    key: "receive",
                    label: "Ready to receive",
                    to: "/demo/receive",
                    icon: Truck,
                    count: 0,
                  },
                  {
                    key: "direct",
                    label: "Direct purchases",
                    to: "/demo/direct",
                    icon: PackageCheck,
                    count: 0,
                  },
                  {
                    key: "suppliers",
                    label: "Suppliers",
                    to: "/demo/suppliers",
                    icon: Users,
                    count: 0,
                  },
                ]}
              />
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Vertical — with icon + count</SectionLabel>
              <ModuleSubnav
                variant="vertical"
                ariaLabel="Purchasing destinations vertical"
                testId="ui-standard-subnav-vertical"
                value={purchasingDest}
                onValueChange={setPurchasingDest}
                items={[
                  {
                    key: "po",
                    label: "Purchase orders",
                    to: "/demo/po",
                    icon: ClipboardList,
                    count: 0,
                  },
                  {
                    key: "incoming",
                    label: "Incoming orders",
                    to: "/demo/incoming",
                    icon: Inbox,
                    count: 2,
                  },
                  {
                    key: "receive",
                    label: "Ready to receive",
                    to: "/demo/receive",
                    icon: Truck,
                    count: 0,
                  },
                  {
                    key: "direct",
                    label: "Direct purchases",
                    to: "/demo/direct",
                    icon: PackageCheck,
                    count: 0,
                  },
                  {
                    key: "suppliers",
                    label: "Suppliers",
                    to: "/demo/suppliers",
                    icon: Users,
                    count: 0,
                  },
                ]}
              />
            </div>
          </Card>
        ) : null}

        {show("table") ? (
          <Card
            className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2"
            data-testid="ui-standard-card-table"
          >
            <CardTitle>Table</CardTitle>
            <ExitsTableContainer>
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>Customer</ExitsTableHead>
                    <ExitsTableHead>Type</ExitsTableHead>
                    <ExitsTableHead cellAlign="money">Balance</ExitsTableHead>
                    <ExitsTableHead>Status</ExitsTableHead>
                    <ExitsTableHead cellAlign="actions" className="w-28">
                      Actions
                    </ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {tableRows.map((row) => (
                    <ExitsTableRow
                      key={row.id}
                      data-selected={selectedRow === row.id || undefined}
                      className={
                        selectedRow === row.id ? "bg-[var(--exits-surface-muted)]" : undefined
                      }
                      onClick={() => setSelectedRow(row.id)}
                    >
                      <ExitsTableCell>{row.customer}</ExitsTableCell>
                      <ExitsTableCell>{row.type}</ExitsTableCell>
                      <ExitsTableCell cellAlign="money" className="tabular-nums">
                        {row.balance}
                      </ExitsTableCell>
                      <ExitsTableCell>
                        <StatusChip tone={row.statusTone}>{row.status}</StatusChip>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <div className="inline-flex gap-1" onClick={(e) => e.stopPropagation()}>
                          <TableActionButton
                            action="edit"
                            label={`Edit ${row.customer}`}
                            testId={`ui-standard-row-edit-${row.id}`}
                          />
                          <TableActionButton
                            action="close"
                            label={`More for ${row.customer}`}
                            variant="ghost"
                            icon={<MoreHorizontal className="size-4" aria-hidden />}
                            testId={`ui-standard-row-more-${row.id}`}
                          />
                        </div>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          </Card>
        ) : null}

        {show("cards") ? (
          <Card
            className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2"
            data-testid="ui-standard-card-cards"
          >
            <CardTitle>Cards</CardTitle>
            <div className="flex flex-col gap-2">
              <SectionLabel>Types</SectionLabel>
              <div className="grid min-w-0 gap-3 sm:grid-cols-2 xl:grid-cols-3">
                <Card treatment="bordered" data-testid="ui-standard-card-type-basic">
                  <CardHeader>
                    <div className="min-w-0">
                      <CardTitle as="h4">Basic</CardTitle>
                      <CardDescription>Grouped content / store information</CardDescription>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="m-0 text-[length:var(--exits-text-sm)]">
                      <span className="text-muted">Business hours</span>
                      <br />
                      8:00 AM – 8:00 PM
                    </p>
                  </CardContent>
                  <CardFooter className="justify-end border-t-0 pt-0">
                    <Button type="button" intent="neutral" appearance="outline" shape="soft">
                      Edit
                    </Button>
                  </CardFooter>
                </Card>

                <Card treatment="bordered" data-testid="ui-standard-card-type-kpi">
                  <CardDescription className="uppercase tracking-wide">Today&apos;s sales</CardDescription>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    ₱24,850.00
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">
                    +8.4% vs yesterday
                  </p>
                </Card>

                <Card treatment="bordered" interactive data-testid="ui-standard-card-type-action">
                  <CardHeader>
                    <ShoppingCart className="size-4 text-[var(--exits-primary)]" aria-hidden />
                    <div className="min-w-0">
                      <CardTitle as="h4">Action</CardTitle>
                      <CardDescription>Start a new customer transaction.</CardDescription>
                    </div>
                  </CardHeader>
                </Card>

                <Card treatment="bordered" interactive data-testid="ui-standard-card-type-entity">
                  <CardHeader>
                    <div
                      className="flex size-8 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] text-[length:var(--exits-text-xs)] font-semibold"
                      aria-hidden
                    >
                      KF
                    </div>
                    <div className="min-w-0">
                      <CardTitle as="h4">Entity</CardTitle>
                      <CardDescription>Kizy Fruits · Business customer</CardDescription>
                    </div>
                    <StatusChip tone="success">Active</StatusChip>
                  </CardHeader>
                </Card>

                <Card
                  treatment="accent"
                  accentTone="warning"
                  accentPosition="start"
                  data-testid="ui-standard-card-type-status"
                >
                  <CardTitle as="h4">Status</CardTitle>
                  <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
                    18 products
                  </p>
                  <StatusChip tone="warning">Needs attention</StatusChip>
                </Card>

                <Card treatment="bordered" padding="compact" data-testid="ui-standard-card-type-compact">
                  <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">
                    Compact
                  </CardTitle>
                  <CardDescription>Dense business information</CardDescription>
                </Card>

                <Card treatment="featured" data-testid="ui-standard-card-type-featured">
                  <CardTitle as="h4">Featured</CardTitle>
                  <CardDescription>Recommended / promoted option</CardDescription>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    ₱499
                    <span className="text-[length:var(--exits-text-sm)] font-normal text-muted">
                      {" "}
                      / month
                    </span>
                  </p>
                </Card>

                <Card treatment="bordered" className="sm:col-span-2" data-testid="ui-standard-card-type-kpi-icon">
                  <CardHeader className="items-center gap-2">
                    <Coins className="size-4 shrink-0 text-muted" aria-hidden />
                    <CardDescription className="uppercase tracking-wide">Cash in drawer</CardDescription>
                  </CardHeader>
                  <CardContent className="grid gap-0.5 pt-0">
                    <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                      ₱8,420.00
                    </p>
                  </CardContent>
                </Card>
              </div>
            </div>

            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Selectable</SectionLabel>
              <div
                className="grid gap-2 sm:grid-cols-2"
                role="radiogroup"
                aria-label="Warehouse"
                data-testid="ui-standard-card-selectable-group"
              >
                {(
                  [
                    ["main", "Main warehouse"],
                    ["iloilo", "Iloilo warehouse"],
                  ] as const
                ).map(([key, label]) => (
                  <Card
                    key={key}
                    as="button"
                    type="button"
                    role="radio"
                    aria-checked={selectableWarehouse === key}
                    treatment={selectableWarehouse === key ? "selected" : "bordered"}
                    selected={selectableWarehouse === key}
                    onClick={() => setSelectableWarehouse(key)}
                    data-testid={`ui-standard-card-selectable-${key}`}
                  >
                    <CardTitle as="h4">{label}</CardTitle>
                    <CardDescription>Selectable option</CardDescription>
                  </Card>
                ))}
              </div>
            </div>

            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Treatments</SectionLabel>
              <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-4">
                {(
                  [
                    ["Surface", "surface"],
                    ["Bordered", "bordered"],
                    ["Elevated", "elevated"],
                    ["Interactive", "interactive"],
                  ] as const
                ).map(([label, treatment]) => (
                  <Card
                    key={treatment}
                    treatment={treatment}
                    interactive={treatment === "interactive"}
                    data-testid={`ui-standard-card-treatment-${treatment}`}
                  >
                    <CardTitle as="h4">{label}</CardTitle>
                    <CardDescription>treatment=&quot;{treatment}&quot;</CardDescription>
                  </Card>
                ))}
              </div>
            </div>

            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Summary / invoice — Customer Purchase Summary</SectionLabel>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                Same production BusinessDocument used for sale receipts (not a BIR invoice).
              </p>
              <div
                className="max-h-[32rem] overflow-auto rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/20 p-2"
                data-testid="ui-standard-card-invoice-sample"
              >
                <CustomerPurchaseSummaryDocument
                  sale={DEMO_INVOICE_SALE}
                  settings={DEFAULT_DOCUMENT_SETTINGS}
                  identity={DEMO_INVOICE_IDENTITY}
                  headerVisibility={DEMO_INVOICE_VISIBILITY}
                  paymentLabel="Cash"
                  cashierLabel="Cashier One"
                  audience="Seller"
                  preview
                />
              </div>
            </div>
          </Card>
        ) : null}

        {show("states") ? (
          <Card
            className="flex min-w-0 flex-col gap-3 p-3 lg:col-span-2"
            data-testid="ui-standard-card-states"
          >
            <CardTitle>States</CardTitle>
            <div className="grid min-w-0 gap-3 md:grid-cols-3">
              <div className="flex min-w-0 flex-col gap-2">
                <SectionLabel>Empty</SectionLabel>
                <EmptyState
                  title="No customers yet"
                  detail="Add your first customer to begin."
                  size="compact"
                  action={
                    <Button type="button" variant="default">
                      {CreateIcon ? (
                        <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                      ) : null}
                      Create customer
                    </Button>
                  }
                />
              </div>
              <div className="flex min-w-0 flex-col gap-2">
                <SectionLabel>Loading</SectionLabel>
                <LoadingState label="Loading…" />
              </div>
              <div className="flex min-w-0 flex-col gap-2">
                <SectionLabel>Error</SectionLabel>
                <ErrorState title="Unable to load customers." detail="Something went wrong." />
                <Button
                  type="button"
                  variant="outline"
                  data-testid="ui-standard-retry"
                  onClick={() => toast.info("Retry", "Demo retry — no backend request.")}
                >
                  {RetryIcon ? (
                    <RetryIcon className={`size-4 ${buttonIconMotion.refresh}`} aria-hidden />
                  ) : null}
                  Retry
                </Button>
              </div>
            </div>
          </Card>
        ) : null}
      </div>

      <ConfirmActionDialog
        open={confirmVariant === "default"}
        variant="default"
        title="Confirm action?"
        description="Continue with this action?"
        confirmLabel="Continue"
        onCancel={() => setConfirmVariant(null)}
        onConfirm={() => {
          toast.success("Confirmed", "Demo only — no data changed.");
          setConfirmVariant(null);
        }}
        testId="ui-standard-confirm-dialog-default"
      />
      <ConfirmActionDialog
        open={confirmVariant === "warning"}
        variant="warning"
        title="Deactivate customer?"
        description={
          "This customer will no longer be available for new transactions.\nExisting transaction history will remain available."
        }
        confirmLabel="Deactivate"
        onCancel={() => setConfirmVariant(null)}
        onConfirm={() => {
          toast.warning("Deactivated", "Demo only — no customer changed.");
          setConfirmVariant(null);
        }}
        testId="ui-standard-confirm-dialog-warning"
      />
      <ConfirmActionDialog
        open={confirmVariant === "danger"}
        variant="danger"
        title="Delete draft quotation?"
        description={"This draft will be permanently deleted.\nThis action cannot be undone."}
        confirmLabel="Delete"
        onCancel={() => setConfirmVariant(null)}
        onConfirm={() => {
          toast.error("Deleted", "Demo only — no quotation deleted.");
          setConfirmVariant(null);
        }}
        testId="ui-standard-confirm-dialog-danger"
      />

      <FormDrawer
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        title="Edit customer"
        saveLabel="Save"
        cancelLabel="Cancel"
        onSave={() => {
          toast.success("Changes saved", "Your changes were saved successfully.");
          setDrawerOpen(false);
        }}
        testId="ui-standard-form-drawer"
      >
        <div className="flex flex-col gap-3">
          <Input label="Customer name" name="drawer-name" defaultValue="Juan Dela Cruz" />
          <Input label="Phone" name="drawer-phone" defaultValue="09171234567" />
          <Input label="Address" name="drawer-address" defaultValue="Iloilo City" />
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
            <span className="exits-type-label">Notes</span>
            <textarea
              className="exits-input min-h-20 rounded-[var(--exits-field-radius)] border border-border bg-surface px-[var(--exits-control-padding-x)] py-2"
              defaultValue="Preferred morning delivery"
              name="drawer-notes"
            />
          </label>
        </div>
      </FormDrawer>

      <ExitsModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        title="Record payment"
        description="Juan Dela Cruz"
        testId="ui-standard-modal"
        footer={
          <>
            <Button
              type="button"
              variant="outline"
              className={EXITS_CANCEL_BUTTON_CLASS}
              onClick={() => setModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="default"
              data-testid="ui-standard-modal-record"
              onClick={() => {
                toast.success("Payment recorded", "Demo only — no payment API called.");
                setModalOpen(false);
              }}
            >
              Record payment
            </Button>
          </>
        }
      >
        <div className="grid gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            Outstanding balance:{" "}
            <span className="font-medium text-foreground tabular-nums">₱1,250.00</span>
          </p>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            Amount
            <div className="exits-currency-field">
              <span className="exits-currency-field__prefix" aria-hidden>
                ₱
              </span>
              <input
                type="text"
                inputMode="decimal"
                className="exits-currency-field__input"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                data-testid="ui-standard-modal-amount"
              />
            </div>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            Payment method
            <ExitsSelect
              value={paymentMethod}
              options={[
                { value: "Cash", label: "Cash" },
                { value: "ManualGCash", label: "Manual GCash" },
                { value: "Check", label: "Check" },
              ]}
              onChange={setPaymentMethod}
              menuLabel="Payment method"
              testId="ui-standard-modal-method"
            />
          </label>
        </div>
      </ExitsModal>
    </>
  );
}
