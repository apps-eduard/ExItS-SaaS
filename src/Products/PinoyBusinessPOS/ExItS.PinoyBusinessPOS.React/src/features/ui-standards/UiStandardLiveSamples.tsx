import { useMemo, useState, type ReactNode } from "react";
import { Loader2, MoreHorizontal } from "lucide-react";
import {
  EXITS_ACTIONS,
  getActionIcon,
  getActionIntent,
} from "@/components/exits/action-semantics";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsMultiSelect } from "@/components/exits/ExitsMultiSelect";
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
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { TableActionButton } from "@/components/exits/TableActionButton";
import { useExitsToast } from "@/components/exits/ToastProvider";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { CreatableCombobox } from "@/components/exits/CreatableCombobox";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { Card, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { SettingsSelect } from "@/components/ui/settings-select";
import { Switch } from "@/components/ui/switch";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { FilterChip } from "@/components/exits/FilterChip";
import type { UiStandardLiveCardId } from "@/features/ui-standards/ui-standard-catalog";

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
                <Button type="button" variant={EXITS_ACTIONS.save.defaultIntent}>
                  {SaveIcon ? <SaveIcon className="size-4" aria-hidden /> : null}
                  Save
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.create.defaultIntent}>
                  {CreateIcon ? (
                    <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Create
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.add.defaultIntent}>
                  {AddIcon ? (
                    <AddIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Add
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.edit.defaultIntent}>
                  {EditIcon ? <EditIcon className="size-4" aria-hidden /> : null}
                  Edit
                </Button>
                <Button type="button" variant="ghost">
                  Cancel
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.activate.defaultIntent}>
                  {ActivateIcon ? <ActivateIcon className="size-4" aria-hidden /> : null}
                  Activate
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.deactivate.defaultIntent}>
                  {DeactivateIcon ? <DeactivateIcon className="size-4" aria-hidden /> : null}
                  Deactivate
                </Button>
                <Button type="button" variant={EXITS_ACTIONS.delete.defaultIntent}>
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
                <Button type="button" variant={getActionIntent("create")}>
                  {CreateIcon ? (
                    <CreateIcon className={`size-4 ${buttonIconMotion.add}`} aria-hidden />
                  ) : null}
                  Create customer
                </Button>
                <Button type="button" variant={getActionIntent("add", { isPrimaryInGroup: false })}>
                  {AddIcon ? <AddIcon className="size-4" aria-hidden /> : null}
                  Add existing
                </Button>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Intents</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {(
                  [
                    ["Primary", "default"],
                    ["Success", "success"],
                    ["Info", "info"],
                    ["Warning", "warning"],
                    ["Danger", "destructive"],
                    ["Outline", "outline"],
                    ["Ghost", "ghost"],
                    ["Muted", "secondary"],
                  ] as const
                ).map(([label, variant]) => (
                  <Button key={variant} type="button" variant={variant}>
                    {label}
                  </Button>
                ))}
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>States</SectionLabel>
              <div className="flex flex-wrap items-center gap-2">
                <Button type="button" variant="default" disabled>
                  {SaveIcon ? <SaveIcon className="size-4" aria-hidden /> : null}
                  Save
                </Button>
                <Button type="button" variant="default" disabled aria-busy="true">
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                  Saving…
                </Button>
                <TableActionButton action="edit" label="Edit" testId="ui-standard-table-action-edit" />
                <TableActionButton
                  action="delete"
                  label="Delete"
                  variant="ghost"
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
              <SectionLabel>Neutral</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="neutral">Draft</StatusChip>
                <StatusChip tone="neutral">Cancelled</StatusChip>
                <StatusChip tone="neutral">Disabled</StatusChip>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Info</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="info">Processing</StatusChip>
                <StatusChip tone="info">Connected</StatusChip>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Success</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="success">Active</StatusChip>
                <StatusChip tone="success">Cleared</StatusChip>
                <StatusChip tone="success">Completed</StatusChip>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Warning</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="warning">Pending</StatusChip>
                <StatusChip tone="warning">Warning</StatusChip>
              </div>
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Danger</SectionLabel>
              <div className="flex flex-wrap gap-2">
                <StatusChip tone="danger">Declined</StatusChip>
              </div>
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
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-nav">
            <CardTitle>Navigation & Selection</CardTitle>
            <div className="flex flex-col gap-2">
              <SectionLabel>Tabs — same-view sections</SectionLabel>
              <UnderlineTabBar
                ariaLabel="Demo content tabs"
                activeKey={contentTab}
                onChange={setContentTab}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "transactions", label: "Transactions" },
                  { key: "payments", label: "Payments" },
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
            </div>
            <div className="flex flex-col gap-2 border-t border-border pt-3">
              <SectionLabel>Segmented — filter / exclusive selection</SectionLabel>
              <ExitsTabs
                variant="segmented"
                ariaLabel="Demo status filter"
                value={filterSegment}
                onValueChange={setFilterSegment}
                items={[
                  { key: "all", label: "All" },
                  { key: "active", label: "Active" },
                  { key: "inactive", label: "Inactive" },
                ]}
              />
            </div>
          </Card>
        ) : null}

        {show("table") ? (
          <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="ui-standard-card-table">
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
