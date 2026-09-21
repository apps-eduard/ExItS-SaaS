import { useEffect, useMemo, useRef, useState, type ReactNode, Fragment } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, RotateCcw, X } from "lucide-react";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getGoodsReceipt,
  getPurchaseOrder,
  isPurchaseOrderReceivable,
  listGoodsReceiptsForPurchaseOrder,
  receivePurchaseOrder,
  type PosGoodsReceiptDto,
} from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableEditMenu,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableInlineEditor,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTablePagination,
  ExitsTableRow,
  type ExitsTableEditableField,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { QuantityInput } from "@/components/exits/MoneyQuantityInputs";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import {
  GoodsReceiptBusinessDocument,
  PurchaseOrderBusinessDocument,
} from "@/features/documents/PurchasingBusinessDocuments";
import { useBusinessDocumentIdentity } from "@/features/documents/use-business-document-identity";
import { useOrganizationDocumentSettings } from "@/features/documents/use-organization-document-settings";
import { buildPurchaseOrderActivityEvents } from "@/features/purchasing/purchase-order-activity";
import { PoProcessHeaderActions } from "@/features/purchasing/PoProcessHeaderActions";
import { PurchaseOrderTimelineDrawer } from "@/features/purchasing/PurchaseOrderTimelineDrawer";
import { ReceivePaymentSection } from "@/features/purchasing/ReceivePaymentSection";
import { ReceiveRemainingQuantityTable } from "@/features/purchasing/ReceiveRemainingQuantityTable";
import {
  buildRemainingDecisionRows,
  countUnresolvedRemaining,
  sumRemainingUnits,
  type RemainingDecisionAction,
} from "@/features/purchasing/receive-remaining-decision";
import {
  buildReceiveSettlementPayload,
  EMPTY_RECEIVE_SETTLEMENT,
  formatMoneyInput,
  parseMoneyInput,
  resolveLockedReceivePaymentFromPo,
  roundMoney,
  shouldSkipReceiveSettlementPayload,
  validateLockedReceivePaymentMatrix,
  type ReceivePaymentMethodCode,
  type ReceivePaymentMode,
  type ReceiveSettlementFields,
} from "@/features/purchasing/receive-payment";
import {
  buildPurchaseOrderReceiveExportModel,
  downloadPurchaseOrderReceiveCsv,
  downloadPurchaseOrderReceivePdf,
  downloadPurchaseOrderReceiveXlsx,
  printPurchaseOrderReceiveDocument,
} from "@/features/purchasing/purchase-order-receive-output";
import {
  buildReceivePlan,
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import {
  formatReceiveDiscrepancySummary,
  isReceiveDiscrepancyClassified,
  lineHasReceiveDiscrepancy,
  lineNeedsReceiveDiscrepancyClassification,
} from "@/features/purchasing/receive-discrepancy-display";
import { ReceiveDiscrepancyDialog } from "@/features/purchasing/ReceiveDiscrepancyDialog";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import { selectUntrackedReceivingLines } from "@/features/purchasing/receive-tracking";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

function receiveStatusTone(
  status: string,
  displayStatus: string,
): "success" | "warning" | "info" | "danger" {
  const key = displayStatus || status;
  switch (key) {
    case "Ordered":
    case "Ready":
    case "Shipped":
    case "AwaitingBuyerReceipt":
      return "success";
    case "PartiallyReceived":
    case "ReceivedWithIssues":
    case "ReceivedAwaitingPayment":
      return "warning";
    case "Cancelled":
    case "Declined":
      return "danger";
    default:
      return "info";
  }
}

function receiveStatusLabel(
  t: (key: MessageKey) => string,
  status: string,
  displayStatus: string,
): string {
  const key = displayStatus || status;
  switch (key) {
    case "PartiallyReceived":
      return "Partially received";
    case "Shipped":
    case "AwaitingBuyerReceipt":
      return "Shipped — awaiting receipt";
    case "Ready":
      return t("purchasing.readyForReceipt");
    case "ReceivedAwaitingPayment":
      return t("incomingOrders.statusReceivedAwaitingPayment");
    default:
      return key || status;
  }
}

type ReceiveEditFieldKey = "receiveNow";

type ReceiveEditBaseline = {
  goodText: string;
};

type ReceiveEditErrors = {
  receiveNow?: string;
};

function ReceiveIconAction({
  label,
  variant = "ghost",
  children,
  onClick,
  className,
  title,
  "data-testid": testId,
}: {
  label: string;
  variant?: "ghost" | "destructive" | "success" | "info";
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  title?: string;
  "data-testid"?: string;
}) {
  return (
    <Button
      type="button"
      variant={variant}
      size="icon"
      shape="round"
      title={title ?? label}
      aria-label={label}
      className={className}
      data-testid={testId}
      onClick={onClick}
    >
      {children}
    </Button>
  );
}

type LineEdit = {
  productId: string;
  name: string;
  sku: string;
  uom: string;
  orderedQty: number;
  receivedQty: number;
  outstandingQty: number;
  unitPurchaseCost: number;
  tracksExpiration: boolean;
  isInventoryTracked: boolean;
  goodText: string;
  damagedText: string;
  notDeliveredText: string;
  remarksText: string;
  /** null = unresolved; replace_later keeps outstanding; cancel_remaining → shortClosedQty. */
  remainingAction: RemainingDecisionAction | null;
  expiryDate: string;
  lotNumber: string;
};

export function PurchaseOrderReceivePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const { settings: documentSettings } = useOrganizationDocumentSettings(organizationId);
  const { identity, headerVisibility } = useBusinessDocumentIdentity(organizationId);
  const allowManage = canManagePurchasing(sessionGrant);
  const [lines, setLines] = useState<LineEdit[] | null>(null);
  const [deliveryReference, setDeliveryReference] = useState("");
  const [notes, setNotes] = useState("");
  const [reviewing, setReviewing] = useState(false);
  const [trackingConfirm, setTrackingConfirm] = useState(false);
  const [completedReceipt, setCompletedReceipt] = useState<PosGoodsReceiptDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusLocked, setStatusLocked] = useState(false);
  const [paidNowText, setPaidNowText] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [paymentMode, setPaymentMode] = useState<ReceivePaymentMode>("paidInFull");
  const [paymentMethod, setPaymentMethod] = useState<ReceivePaymentMethodCode>("Cash");
  const [settlementFields, setSettlementFields] =
    useState<ReceiveSettlementFields>(EMPTY_RECEIVE_SETTLEMENT);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [editingProductId, setEditingProductId] = useState<string | null>(null);
  const [editingFields, setEditingFields] = useState<Set<ReceiveEditFieldKey>>(new Set());
  const [mobileEditingProductId, setMobileEditingProductId] = useState<string | null>(null);
  const [mobileEditingFields, setMobileEditingFields] = useState<Set<ReceiveEditFieldKey>>(
    new Set(),
  );
  const [editBaseline, setEditBaseline] = useState<ReceiveEditBaseline | null>(null);
  const [editErrors, setEditErrors] = useState<ReceiveEditErrors>({});
  const [discrepancyOpen, setDiscrepancyOpen] = useState(false);
  const [discrepancyTargetProductId, setDiscrepancyTargetProductId] = useState<string | null>(null);
  const [highlightUnclassified, setHighlightUnclassified] = useState(false);
  const [highlightUnresolvedRemaining, setHighlightUnresolvedRemaining] = useState(false);
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);
  const goodsReceiptIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const branchName = boundWorkspace?.branchName?.trim() || boundWorkspace?.branchId || "";

  useEffect(() => {
    setPage(1);
  }, [pageSize]);

  const query = useQuery({
    queryKey: ["purchase-order", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: async ({ signal }) => {
      const po = await getPurchaseOrder(workspace!, purchaseOrderId!, signal);
      setLines(
        po.lines.map((line) => ({
          productId: line.productId ?? "",
          name: line.nameSnapshot ?? line.productId ?? "",
          sku: line.skuSnapshot?.trim() || "",
          uom: line.uomSnapshot ?? "",
          orderedQty: line.orderedQty,
          receivedQty: line.receivedQty,
          outstandingQty: line.outstandingQty,
          unitPurchaseCost: line.unitPurchaseCost,
          tracksExpiration: line.tracksExpiration === true,
          // Only explicit false is untracked; undefined/true skip enable-tracking confirmation.
          isInventoryTracked: line.isInventoryTracked !== false,
          goodText: line.outstandingQty > 0 ? String(line.outstandingQty) : "",
          damagedText: "0",
          notDeliveredText: "0",
          remarksText: "",
          remainingAction: null,
          expiryDate: "",
          lotNumber: "",
        })),
      );
      return po;
    },
  });

  const receiptsQuery = useQuery({
    queryKey: ["purchase-order-receipts", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: ({ signal }) =>
      listGoodsReceiptsForPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const po = query.data;
  const receipts = receiptsQuery.data ?? [];
  const actors = useActorDirectory(workspace?.organizationId, [
    po?.orderedBy,
    po?.financiallySettledBy,
    po?.remainingClosedByUserId,
    ...receipts.flatMap((r) => [r.receivedBy, r.voidedByUserId]),
  ]);
  const hasTimeline = useMemo(
    () => (po ? buildPurchaseOrderActivityEvents({ po, receipts }).length > 0 : false),
    [po, receipts],
  );
  const statusLabel = po
    ? receiveStatusLabel(t, po.status, po.displayStatus || po.status)
    : "";
  const statusTone = po
    ? receiveStatusTone(po.status, po.displayStatus || po.status)
    : "info";

  function runReceiveOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    const model = buildExportModel();
    if (action === "csv") {
      downloadPurchaseOrderReceiveCsv(model);
      return;
    }
    if (action === "xlsx") {
      downloadPurchaseOrderReceiveXlsx(model);
      return;
    }
    if (action === "pdf") {
      downloadPurchaseOrderReceivePdf(model);
      return;
    }
    printPurchaseOrderReceiveDocument();
  }

  function renderPoUtilityActions(options?: { includePreview?: boolean }) {
    const includePreview = options?.includePreview !== false;
    return (
      <PoProcessHeaderActions
        statusLabel={statusLabel}
        statusTone={statusTone}
        timelineEnabled={hasTimeline}
        previewEnabled={includePreview}
        onTimeline={() => setTimelineOpen(true)}
        onPreview={() => setDocumentPreviewOpen(true)}
        previewTestId="receive-po-preview-open"
        onPrint={() => runReceiveOutput("print")}
        onCsv={() => runReceiveOutput("csv")}
        onXlsx={() => runReceiveOutput("xlsx")}
        onPdf={() => runReceiveOutput("pdf")}
      />
    );
  }

  const canReceive =
    allowManage &&
    online &&
    po != null &&
    isPurchaseOrderReceivable(po) &&
    (lines?.some((l) => l.outstandingQty > 0) ?? false);

  const hasMissingExpiryOnReceive =
    lines?.some((line) => {
      const good = parseNonNegativeQty(line.goodText) ?? 0;
      return line.tracksExpiration && good > 0 && !line.expiryDate.trim();
    }) ?? false;

  const untrackedReceivingLines = useMemo(
    () => (lines ? selectUntrackedReceivingLines(lines) : []),
    [lines],
  );

  const estimatedTotal = useMemo(() => {
    if (!lines) {
      return 0;
    }
    return roundMoney(
      lines.reduce((sum, line) => {
        const good = parseNonNegativeQty(line.goodText) ?? 0;
        return sum + good * line.unitPurchaseCost;
      }, 0),
    );
  }, [lines]);

  const orderValue = useMemo(() => {
    if (!lines) {
      return 0;
    }
    if (po?.confirmedTotalAmount != null && po.confirmedTotalAmount > 0) {
      return roundMoney(po.confirmedTotalAmount);
    }
    return roundMoney(
      lines.reduce((sum, line) => sum + line.orderedQty * line.unitPurchaseCost, 0),
    );
  }, [lines, po?.confirmedTotalAmount]);

  const previouslyReceivedValue = useMemo(() => {
    if (!lines) {
      return 0;
    }
    return roundMoney(
      lines.reduce((sum, line) => sum + line.receivedQty * line.unitPurchaseCost, 0),
    );
  }, [lines]);

  const remainingValue = useMemo(() => {
    return roundMoney(Math.max(0, orderValue - previouslyReceivedValue - estimatedTotal));
  }, [estimatedTotal, orderValue, previouslyReceivedValue]);

  const isPartialReceiptSummary =
    previouslyReceivedValue > 0.0000001 || remainingValue > 0.0000001;

  const lockedReceivePayment = useMemo(() => {
    if (!po?.paymentTerm) {
      return null;
    }
    return resolveLockedReceivePaymentFromPo({
      paymentTerm: po.paymentTerm,
      paymentTiming: po.paymentTiming,
      estimatedTotal,
      amountPaidSnapshot: po.amountPaidSnapshot,
      confirmedTotalAmount: po.confirmedTotalAmount,
      financialSettlementStatus: po.financialSettlementStatus,
      buyerPrepaymentReference: po.buyerPrepaymentReference,
      buyerPrepaymentSubmittedAtUtc: po.buyerPrepaymentSubmittedAtUtc,
    });
  }, [
    estimatedTotal,
    po?.amountPaidSnapshot,
    po?.buyerPrepaymentReference,
    po?.buyerPrepaymentSubmittedAtUtc,
    po?.confirmedTotalAmount,
    po?.financialSettlementStatus,
    po?.paymentTerm,
    po?.paymentTiming,
  ]);

  const filteredSortedLines = useMemo(() => lines ?? [], [lines]);

  const receiveEditableFields = useMemo<ReadonlyArray<ExitsTableEditableField>>(
    () => [{ key: "receiveNow", label: t("purchasing.receiveNow") }],
    [t],
  );

  const pagedLines = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredSortedLines.slice(start, start + pageSize);
  }, [filteredSortedLines, page, pageSize]);

  const remainingDecisionRows = useMemo(() => {
    if (!lines || !reviewing) {
      return [];
    }
    return buildRemainingDecisionRows(lines, {
      damaged: t("purchasing.damaged"),
      notDelivered: t("purchasing.notDelivered"),
    });
  }, [lines, reviewing, t]);

  const remainingDecisionSummary = useMemo(() => {
    const products = remainingDecisionRows.length;
    if (products === 0) {
      return "";
    }
    const units = formatStockQtyLabel(sumRemainingUnits(remainingDecisionRows));
    if (products === 1) {
      return t("purchasing.remainingDecisionSummaryOne").replace("{units}", units);
    }
    return t("purchasing.remainingDecisionSummary")
      .replace("{products}", String(products))
      .replace("{units}", units);
  }, [remainingDecisionRows, t]);

  const discrepancyLines = useMemo(() => {
    if (!lines) {
      return [];
    }
    return lines
      .map((line) => {
        const good = parseNonNegativeQty(line.goodText) ?? 0;
        const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
        return { line, good, discrepancy };
      })
      .filter((entry) => entry.discrepancy > 1e-9)
      .filter((entry) =>
        discrepancyTargetProductId
          ? entry.line.productId === discrepancyTargetProductId
          : true,
      )
      .map(({ line, good }) => ({
        productId: line.productId,
        name: line.name,
        uom: line.uom,
        outstandingQty: line.outstandingQty,
        goodQty: good,
        damagedText: line.damagedText,
        notDeliveredText: line.notDeliveredText,
        remarksText: line.remarksText,
      }));
  }, [lines, discrepancyTargetProductId]);

  const unclassifiedProductIds = useMemo(() => {
    if (!lines) {
      return new Set<string>();
    }
    return new Set(
      lines
        .filter((line) => lineNeedsReceiveDiscrepancyClassification(line))
        .map((line) => line.productId),
    );
  }, [lines]);

  function openDiscrepancyForProduct(productId: string) {
    setError(null);
    setTrackingConfirm(false);
    setHighlightUnclassified(false);
    setDiscrepancyTargetProductId(productId);
    setDiscrepancyOpen(true);
  }

  function closeDiscrepancyDialog() {
    setDiscrepancyOpen(false);
    setDiscrepancyTargetProductId(null);
  }

  useEffect(() => {
    if (!lockedReceivePayment) {
      return;
    }
    setPaymentMode(lockedReceivePayment.mode);
    if (lockedReceivePayment.paymentMethod) {
      setPaymentMethod(lockedReceivePayment.paymentMethod);
    }
    setPaidNowText(formatMoneyInput(lockedReceivePayment.paidNow));
    setDueDate("");
  }, [lockedReceivePayment]);

  const paidNowValue = lockedReceivePayment?.paidNow ?? parseMoneyInput(paidNowText);

  const paymentSectionTitle = useMemo(() => {
    const timing = lockedReceivePayment?.paymentTiming ?? po?.paymentTiming ?? "";
    if (timing === "SupplierCredit" || lockedReceivePayment?.mode === "supplierCredit") {
      return t("purchasing.supplierCredit");
    }
    if (timing === "PayBeforeFulfillment" || lockedReceivePayment?.prepaidSettled) {
      return t("purchasing.prepayment");
    }
    if (timing === "PayOnDeliveryOrReceipt") {
      return t("purchasing.paymentAtReceipt");
    }
    return t("purchasing.payment");
  }, [lockedReceivePayment, po?.paymentTiming, t]);

  const prepaidView = useMemo(() => {
    if (!po || !lockedReceivePayment) {
      return null;
    }
    const showPrepaid =
      lockedReceivePayment.prepaidSettled ||
      lockedReceivePayment.alreadySettledAtReceipt ||
      (lockedReceivePayment.paymentTiming === "PayBeforeFulfillment" &&
        !lockedReceivePayment.prepaidIntegrityMissing);
    if (!showPrepaid) {
      return null;
    }
    const method = lockedReceivePayment.paymentMethod;
    let referenceLabel: string | null = null;
    if (method === "GCash") {
      referenceLabel = t("purchasing.gcashReference");
    } else if (method === "BankTransfer" || method === "BankDeposit") {
      referenceLabel = t("purchasing.transferReference");
    } else if (method === "Check") {
      referenceLabel = t("purchasing.checkReference");
    }
    return {
      timingLabel:
        po.paymentTimingLabel?.trim() ||
        (lockedReceivePayment.paymentTiming === "PayBeforeFulfillment"
          ? t("connectedCommerce.timing.payBefore")
          : t("purchasing.paymentAtReceipt")),
      methodLabel: po.paymentTermLabel?.trim() || po.paymentTerm || "—",
      statusLabel: t("purchasing.paymentSettled"),
      paidAmount: roundMoney(po.amountPaidSnapshot ?? po.confirmedTotalAmount ?? orderValue),
      referenceLabel,
      referenceValue: po.buyerPrepaymentReference?.trim() || null,
      confirmedBy: null,
      confirmedAtUtc:
        po.financiallySettledAtUtc?.trim() ||
        po.buyerPrepaymentSubmittedAtUtc?.trim() ||
        null,
      notes:
        po.sellerSettlementRemarks?.trim() ||
        po.buyerPrepaymentDetails?.trim() ||
        null,
    };
  }, [lockedReceivePayment, orderValue, po, t]);

  function validateLockedReceiptPayment(): boolean {
    if (!lockedReceivePayment) {
      return true;
    }
    const settlementError = validateLockedReceivePaymentMatrix(
      lockedReceivePayment,
      settlementFields,
      estimatedTotal,
    );
    if (settlementError) {
      setError(t(settlementError));
      return false;
    }
    return true;
  }

  function buildExportModel() {
    return buildPurchaseOrderReceiveExportModel({
      poNumber: po?.poNumber ?? purchaseOrderId ?? "purchase-order",
      filterLabel: t("purchasing.receiveFilterAll"),
      rows: filteredSortedLines.map((line) => ({
        product: line.name,
        uom: line.uom,
        ordered: line.orderedQty,
        goodReceived: line.goodText || "0",
        damaged: line.damagedText || "0",
        expiry: line.expiryDate || "",
        lot: line.lotNumber || "",
      })),
    });
  }

  function updateLine(productId: string, patch: Partial<LineEdit>) {
    setLines((prev) =>
      (prev ?? []).map((line) => (line.productId === productId ? { ...line, ...patch } : line)),
    );
  }

  function loadReceiveEditBaseline(line: LineEdit) {
    setEditBaseline({ goodText: line.goodText });
    setEditErrors({});
  }

  function cancelReceiveRowEdit() {
    setEditingProductId(null);
    setEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  function cancelReceiveMobileEdit() {
    setMobileEditingProductId(null);
    setMobileEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  function validateReceiveLineFields(line: LineEdit): ReceiveEditErrors {
    const errors: ReceiveEditErrors = {};
    const good = parseNonNegativeQty(line.goodText);
    if (good === null) {
      errors.receiveNow = t("purchasing.invalidReceiveQty");
    } else if (good > line.outstandingQty + 1e-9) {
      errors.receiveNow = t("purchasing.overReceive");
    }
    return errors;
  }

  function receiveEditErrorMessages(errors: ReceiveEditErrors): string[] {
    const messages = [errors.receiveNow].filter(Boolean) as string[];
    return [...new Set(messages)];
  }

  function onReceiveQtyBlur(line: LineEdit) {
    setEditErrors(validateReceiveLineFields(line));
  }

  function dismissReceiveEditErrors() {
    setEditErrors({});
  }

  function startReceiveFieldEdit(line: LineEdit, fieldKey: string, mobile = false) {
    const key = fieldKey as ReceiveEditFieldKey;
    if (key !== "receiveNow") return;
    if (mobile) {
      setEditingProductId(null);
      setEditingFields(new Set());
      setMobileEditingProductId(line.productId);
      setMobileEditingFields(new Set([key]));
    } else {
      setMobileEditingProductId(null);
      setMobileEditingFields(new Set());
      setEditingProductId(line.productId);
      setEditingFields(new Set([key]));
    }
    loadReceiveEditBaseline(line);
  }

  function startReceiveEditAll(line: LineEdit, mobile = false) {
    const all = new Set<ReceiveEditFieldKey>(["receiveNow"]);
    if (mobile) {
      setEditingProductId(null);
      setEditingFields(new Set());
      setMobileEditingProductId(line.productId);
      setMobileEditingFields(all);
    } else {
      setMobileEditingProductId(null);
      setMobileEditingFields(new Set());
      setEditingProductId(line.productId);
      setEditingFields(all);
    }
    loadReceiveEditBaseline(line);
  }

  function finishReceiveEdit(line: LineEdit, mobile = false) {
    const current = (lines ?? []).find((entry) => entry.productId === line.productId) ?? line;
    const errors = validateReceiveLineFields(current);
    if (receiveEditErrorMessages(errors).length > 0) {
      setEditErrors(errors);
      return;
    }
    const good = parseNonNegativeQty(current.goodText) ?? 0;
    const hasDiscrepancy = receiveDiscrepancyQty(current.outstandingQty, good) > 1e-9;
    if (!hasDiscrepancy) {
      updateLine(current.productId, {
        damagedText: "0",
        notDeliveredText: "0",
        remarksText: "",
        remainingAction: null,
      });
    }
    if (mobile) cancelReceiveMobileEdit();
    else cancelReceiveRowEdit();
    if (hasDiscrepancy) {
      openDiscrepancyForProduct(current.productId);
    }
  }

  function resetReceiveEdit(line: LineEdit, mobile = false) {
    if (!editBaseline) {
      if (mobile) cancelReceiveMobileEdit();
      else cancelReceiveRowEdit();
      return;
    }
    const alreadyOriginal = line.goodText === editBaseline.goodText;
    if (alreadyOriginal) {
      if (mobile) cancelReceiveMobileEdit();
      else cancelReceiveRowEdit();
      return;
    }
    updateLine(line.productId, {
      goodText: editBaseline.goodText,
    });
    setEditErrors({});
  }

  useEffect(() => {
    if (!editingProductId && !mobileEditingProductId) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      const activeId = editingProductId ?? mobileEditingProductId;
      if (!activeId || !editBaseline) {
        cancelReceiveRowEdit();
        cancelReceiveMobileEdit();
        return;
      }
      updateLine(activeId, {
        goodText: editBaseline.goodText,
      });
      cancelReceiveRowEdit();
      cancelReceiveMobileEdit();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [editingProductId, mobileEditingProductId, editBaseline]);

  useEffect(() => {
    if (!editingProductId) return;
    const focusTestId = editingFields.has("receiveNow")
      ? `receive-good-${editingProductId}`
      : null;
    if (!focusTestId) return;
    const timer = window.setTimeout(() => {
      const el = document.querySelector<HTMLInputElement>(`[data-testid="${focusTestId}"]`);
      el?.focus();
      el?.select?.();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [editingProductId, editingFields]);

  useEffect(() => {
    if (!mobileEditingProductId) return;
    const focusTestId = mobileEditingFields.has("receiveNow")
      ? `receive-good-mobile-${mobileEditingProductId}`
      : null;
    if (!focusTestId) return;
    const timer = window.setTimeout(() => {
      const el = document.querySelector<HTMLInputElement>(`[data-testid="${focusTestId}"]`);
      el?.focus();
      el?.select?.();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [mobileEditingProductId, mobileEditingFields]);

  useEffect(() => {
    if (reviewing) {
      cancelReceiveRowEdit();
      cancelReceiveMobileEdit();
    }
  }, [reviewing]);

  function tryPlan(options?: { requireClassification?: boolean }) {
    if (!lines) {
      return null;
    }
    const requireClassification = options?.requireClassification !== false;
    const parsed = lines.map((line) => {
      const good = parseNonNegativeQty(line.goodText);
      const damaged = parseNonNegativeQty(line.damagedText);
      const notDelivered = parseNonNegativeQty(line.notDeliveredText);
      return { line, good, damaged, notDelivered };
    });
    if (parsed.some((p) => p.good === null || p.damaged === null || p.notDelivered === null)) {
      setError(t("purchasing.invalidReceiveQty"));
      return null;
    }
    const missingExpiry = parsed.find(
      ({ line, good }) => line.tracksExpiration && (good ?? 0) > 0 && !line.expiryDate.trim(),
    );
    if (missingExpiry) {
      setError(t("purchasing.expiryRequired"));
      return null;
    }

    if (requireClassification) {
      const needsClassification = parsed.some(
        ({ line, good }) => receiveDiscrepancyQty(line.outstandingQty, good ?? 0) > 1e-9,
      );
      if (needsClassification) {
        const incomplete = parsed.some(({ line, good, damaged, notDelivered }) => {
          const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good ?? 0);
          if (discrepancy <= 1e-9) {
            return false;
          }
          return Math.abs((damaged ?? 0) + (notDelivered ?? 0) - discrepancy) > 1e-9;
        });
        if (incomplete) {
          setError(t("purchasing.discrepancyClassificationRequired"));
          return null;
        }
        const missingNote = parsed.some(({ line, good }) => {
          const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good ?? 0);
          if (discrepancy <= 1e-9) {
            return false;
          }
          return !line.remarksText.trim();
        });
        if (missingNote) {
          setError(t("purchasing.discrepancyNoteRequired"));
          return null;
        }
      }
    }

    const result = buildReceivePlan(
      parsed.map(({ line, good, damaged, notDelivered }) => ({
        productId: line.productId,
        outstandingQty: line.outstandingQty,
        goodQty: good!,
        damagedQty: damaged!,
        notDeliveredQty: notDelivered!,
        cancelRemaining: line.remainingAction === "cancel_remaining",
      })),
    );
    if (!result.ok) {
      if (result.error === "over_receive") {
        setError(t("purchasing.overReceive"));
      } else if (result.error === "no_activity") {
        setError(t("purchasing.receiveRequiresLines"));
      } else if (
        result.error === "classification_incomplete" ||
        result.error === "classification_mismatch"
      ) {
        setError(t("purchasing.discrepancyClassificationRequired"));
      } else {
        setError(t("purchasing.invalidReceiveQty"));
      }
      return null;
    }
    setError(null);
    return result.lines;
  }

  function onReview() {
    if (!lines) {
      return;
    }
    const parsedOk = lines.every((line) => parseNonNegativeQty(line.goodText) !== null);
    if (!parsedOk) {
      setError(t("purchasing.invalidReceiveQty"));
      return;
    }
    const over = lines.some((line) => {
      const good = parseNonNegativeQty(line.goodText) ?? 0;
      return good > line.outstandingQty + 1e-9;
    });
    if (over) {
      setError(t("purchasing.overReceive"));
      return;
    }
    const missingExpiry = lines.find((line) => {
      const good = parseNonNegativeQty(line.goodText) ?? 0;
      return line.tracksExpiration && good > 0 && !line.expiryDate.trim();
    });
    if (missingExpiry) {
      setError(t("purchasing.expiryRequired"));
      return;
    }
    if (!validateLockedReceiptPayment()) {
      return;
    }

    if (unclassifiedProductIds.size > 0) {
      setHighlightUnclassified(true);
      setError(t("purchasing.classifyBeforeReview"));
      setTrackingConfirm(false);
      return;
    }

    if (!tryPlan({ requireClassification: true })) {
      return;
    }
    setError(null);
    setHighlightUnclassified(false);
    setHighlightUnresolvedRemaining(false);
    setTrackingConfirm(false);
    setReviewing(true);
  }

  function onDiscrepancyConfirmed() {
    if (!lines || !discrepancyTargetProductId) {
      return;
    }
    const target = lines.find((line) => line.productId === discrepancyTargetProductId);
    if (!target || !isReceiveDiscrepancyClassified(target)) {
      setError(t("purchasing.discrepancyClassificationRequired"));
      return;
    }
    if (!target.remarksText.trim()) {
      setError(t("purchasing.discrepancyNoteRequired"));
      return;
    }
    setError(null);
    setHighlightUnclassified(false);
    closeDiscrepancyDialog();
  }

  function onReviewConfirmClick() {
    if (!lines) {
      return;
    }
    const remainingRows = buildRemainingDecisionRows(lines, {
      damaged: t("purchasing.damaged"),
      notDelivered: t("purchasing.notDelivered"),
    });
    if (countUnresolvedRemaining(remainingRows) > 0) {
      setHighlightUnresolvedRemaining(true);
      setError(t("purchasing.remainingDecisionRequired"));
      setTrackingConfirm(false);
      return;
    }
    setHighlightUnresolvedRemaining(false);
    if (!tryPlan()) {
      return;
    }
    if (!validateLockedReceiptPayment()) {
      return;
    }
    const untracked = selectUntrackedReceivingLines(lines);
    if (untracked.length > 0) {
      setError(null);
      setTrackingConfirm(true);
      return;
    }
    void postReceive(false);
  }

  async function postReceive(enableTrackingIfNeeded: boolean) {
    if (!workspace || !purchaseOrderId || !canReceive || busy || statusLocked || !lines) {
      return;
    }
    const planned = tryPlan();
    if (!planned) {
      return;
    }
    if (!validateLockedReceiptPayment()) {
      return;
    }
    const paidNow = lockedReceivePayment?.paidNow ?? 0;
    // Always send the PO-locked method (never omit for prepaid) so server method-lock matches.
    // Utang/supplier-credit has no receipt payment method code.
    const methodAtReceipt =
      lockedReceivePayment?.mode === "supplierCredit" &&
      lockedReceivePayment.paymentMethod == null
        ? null
        : (lockedReceivePayment?.paymentMethod ?? paymentMethod);
    const settlementPayload = buildReceiveSettlementPayload(methodAtReceipt, settlementFields, {
      skipSettlement: shouldSkipReceiveSettlementPayload(lockedReceivePayment),
    });
    if (!goodsReceiptIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("purchasing.receiveFailed"));
        return;
      }
      goodsReceiptIdRef.current = generated.id;
    }
    const goodsReceiptId = goodsReceiptIdRef.current;
    setBusy(true);
    setError(null);
    try {
      const receipt = await receivePurchaseOrder(workspace, purchaseOrderId, {
        goodsReceiptId,
        deliveryReference: deliveryReference.trim() || null,
        notes: notes.trim() || null,
        paidNow,
        dueDate: null,
        paymentMethodAtReceipt: methodAtReceipt,
        ...settlementPayload,
        enableTrackingIfNeeded: enableTrackingIfNeeded ? true : undefined,
        lines: planned.map((line) => {
          const edit = lines.find((l) => l.productId === line.productId);
          const goodQty = line.receiveQty;
          const note = edit?.remarksText.trim() || null;
          return {
            productId: line.productId,
            receiveQty: line.receiveQty,
            damagedQty: line.damagedQty,
            rejectedQty: line.rejectedQty,
            shortClosedQty: line.shortClosedQty,
            discrepancyKind: line.discrepancyKind,
            discrepancyNote: line.discrepancyKind && note ? note : null,
            expiryDate:
              edit?.tracksExpiration && goodQty > 0 && edit.expiryDate.trim()
                ? edit.expiryDate.trim()
                : null,
            lotNumber:
              edit?.tracksExpiration && goodQty > 0 && edit.lotNumber.trim()
                ? edit.lotNumber.trim()
                : null,
          };
        }),
      });
      goodsReceiptIdRef.current = null;
      setCompletedReceipt(receipt);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["purchase-order"] }),
        queryClient.invalidateQueries({ queryKey: ["purchase-orders"] }),
        queryClient.invalidateQueries({ queryKey: ["supplier-payables"] }),
        queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] }),
        queryClient.invalidateQueries({ queryKey: ["business-customers"] }),
        queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] }),
        queryClient.invalidateQueries({
          queryKey: ["connected-suppliers", "buyer-credit-policy"],
        }),
      ]);
      setBusy(false);
    } catch (err) {
      setError(t("checkout.confirmingTransaction"));
      const outcome = await resolveAmbiguousMutationOutcome({
        error: err,
        lookup: () => getGoodsReceipt(workspace, goodsReceiptId),
      });
      if (outcome.kind === "confirmed") {
        goodsReceiptIdRef.current = null;
        setCompletedReceipt(outcome.value);
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: ["purchase-order"] }),
          queryClient.invalidateQueries({ queryKey: ["purchase-orders"] }),
          queryClient.invalidateQueries({ queryKey: ["supplier-payables"] }),
          queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] }),
          queryClient.invalidateQueries({ queryKey: ["business-customers"] }),
          queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] }),
          queryClient.invalidateQueries({
            queryKey: ["connected-suppliers", "buyer-credit-policy"],
          }),
        ]);
        setBusy(false);
        return;
      }
      if (outcome.kind === "still_unknown") {
        setStatusLocked(true);
        setError(t("checkout.transactionStatusUnknown"));
        return;
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.receiveFailed"))
          : t("purchasing.receiveFailed"),
      );
      setBusy(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!purchaseOrderId) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }
  if (query.isLoading || !lines) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !po) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }

  if (completedReceipt) {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-receive-page">
        <PageHeader
          title={t("purchasing.receiptCompleted")}
          description={po.poNumber ?? t("purchasing.receiveSubtitle")}
          backTo={`/purchasing/${purchaseOrderId}`}
          backLabel={t("purchasing.backDetail")}
          backTestId="page-header-back-purchasing"
          actions={renderPoUtilityActions({ includePreview: false })}
        />
        <GoodsReceiptBusinessDocument
          receipt={completedReceipt}
          po={po}
          poNumber={po.poNumber}
          supplierName={po.supplierName}
          settings={documentSettings}
          identity={identity}
          headerVisibility={headerVisibility(documentSettings.header)}
          preview
        />
        <div className="flex min-w-0 flex-col gap-4 print:hidden" data-testid="receive-completed-panel">
          <ExitsTableContainer data-testid="receive-completed-table">
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text">{t("purchasing.receiveProduct")}</ExitsTableHead>
                  <ExitsTableHead cellAlign="text">{t("purchasing.received")}</ExitsTableHead>
                  <ExitsTableHead cellAlign="money">{t("purchasing.purchaseAmount")}</ExitsTableHead>
                  <ExitsTableHead cellAlign="numeric">{t("purchasing.previousTrackedStock")}</ExitsTableHead>
                  <ExitsTableHead cellAlign="numeric">{t("purchasing.newTrackedStock")}</ExitsTableHead>
                  <ExitsTableHead cellAlign="text">{t("purchasing.inventoryTracking")}</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {completedReceipt.lines.map((line) => {
                  const qty = line.quantityReceived ?? line.receivedQty ?? 0;
                  return (
                    <ExitsTableRow key={line.lineId} data-testid={`receive-completed-row-${line.productId}`}>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        {line.nameSnapshot}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" className="tabular-nums">
                        {qty} {line.uomSnapshot}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <MoneyDisplay amount={line.lineTotalSnapshot} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                        {line.previousTrackedStock != null ? line.previousTrackedStock : "—"}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                        {line.newTrackedStock != null ? line.newTrackedStock : "—"}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text">
                        {line.inventoryTrackingEnabled ? t("purchasing.inventoryTrackingEnabled") : "—"}
                      </ExitsTableCell>
                    </ExitsTableRow>
                  );
                })}
              </ExitsTableBody>
            </ExitsTable>

            <ExitsTableMobile className="gap-3 p-3" data-testid="receive-completed-mobile">
              {completedReceipt.lines.map((line) => {
                const qty = line.quantityReceived ?? line.receivedQty ?? 0;
                const amount = line.lineTotalSnapshot;
                return (
                  <ExitsTableMobileRow
                    key={line.lineId}
                    className="rounded-md border border-border border-b p-3"
                    data-testid={`receive-completed-line-${line.productId}`}
                  >
                    <div className="exits-table-mobile__title-row">
                      <p className="exits-table-mobile__title">{line.nameSnapshot}</p>
                    </div>
                    <dl className="mt-2 mb-0 grid gap-1 text-[length:var(--exits-text-sm)]">
                      <div className="flex flex-wrap gap-x-2">
                        <dt className="text-muted">{t("purchasing.received")}:</dt>
                        <dd className="m-0">
                          {qty} {line.uomSnapshot}
                        </dd>
                      </div>
                      <div className="flex flex-wrap items-baseline gap-x-2">
                        <dt className="text-muted">{t("purchasing.purchaseAmount")}:</dt>
                        <dd className="m-0">
                          <MoneyDisplay amount={amount} />
                        </dd>
                      </div>
                      {line.inventoryTrackingEnabled ? (
                        <>
                          <div className="text-muted">{t("purchasing.inventoryTrackingEnabled")}</div>
                          {line.newTrackedStock != null ? (
                            <div className="flex flex-wrap gap-x-2">
                              <dt className="text-muted">{t("purchasing.newTrackedStock")}:</dt>
                              <dd className="m-0">{line.newTrackedStock}</dd>
                            </div>
                          ) : null}
                        </>
                      ) : line.previousTrackedStock != null || line.newTrackedStock != null ? (
                        <div className="flex flex-wrap gap-x-3 gap-y-1">
                          {line.previousTrackedStock != null ? (
                            <span>
                              <span className="text-muted">{t("purchasing.previousTrackedStock")}: </span>
                              {line.previousTrackedStock}
                            </span>
                          ) : null}
                          <span>
                            <span className="text-muted">{t("purchasing.received")}: </span>
                            {qty}
                          </span>
                          {line.newTrackedStock != null ? (
                            <span>
                              <span className="text-muted">{t("purchasing.newTrackedStock")}: </span>
                              {line.newTrackedStock}
                            </span>
                          ) : null}
                        </div>
                      ) : null}
                    </dl>
                  </ExitsTableMobileRow>
                );
              })}
            </ExitsTableMobile>
          </ExitsTableContainer>

          <div>
            <Button
              type="button"
              onClick={() => navigate(`/purchasing/${purchaseOrderId}`, { replace: true })}
              data-testid="receive-back-to-po"
            >
              {t("purchasing.backDetail")}
            </Button>
          </div>
        </div>

        <PurchaseOrderTimelineDrawer
          open={timelineOpen}
          onOpenChange={setTimelineOpen}
          po={po}
          receipts={receipts}
          resolveActor={actors.resolve}
          isResolving={actors.isResolving}
          receiptsLoading={receiptsQuery.isLoading}
        />
      </div>
    );
  }

  const exportModel = buildExportModel();

  return (
    <div
      className="purchase-order-receive-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="purchase-order-receive-page"
    >
      <div className="purchase-order-receive-print-root" aria-hidden>
        <h1>{t("purchasing.receiveTitle")}</h1>
        <p>{exportModel.poNumber}</p>
        <table>
          <thead>
            <tr>
              <th>{t("purchasing.receiveProduct")}</th>
              <th>{t("purchasing.ordered")}</th>
              <th>{t("purchasing.receiveNow")}</th>
              <th>{t("purchasing.unit")}</th>
              <th>{t("purchasing.damaged")}</th>
            </tr>
          </thead>
          <tbody>
            {exportModel.rows.map((row) => (
              <tr key={`${row.product}-${row.uom}-${row.ordered}`}>
                <td>{row.product}</td>
                <td>{row.ordered}</td>
                <td>{row.goodReceived}</td>
                <td>{row.uom}</td>
                <td>{row.damaged}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageHeader
        title={t("purchasing.receiveTitle")}
        subtitle={po.poNumber ?? undefined}
        description={t("purchasing.receiptsStockNote")}
        backTo={`/purchasing/${purchaseOrderId}`}
        backLabel={t("purchasing.backDetail")}
        backTestId="page-header-back-purchasing"
        actions={renderPoUtilityActions()}
      />
      {po.paymentTerm || po.paymentTiming ? (
        <Card
          className="po-document-summary po-document-summary--meta-cards po-document-summary--meta-cards-4 grid gap-3 p-3"
          data-testid="receive-po-context"
        >
          <h2
            className="po-document-summary__title m-0"
            data-testid="receive-po-context-title"
          >
            {t("incomingOrders.orderInfoTitle")}
          </h2>
          <dl className="po-document-summary__meta m-0">
            <div className="po-document-summary__field po-document-summary__field--paymentTiming min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("purchasing.paymentTiming")}
              </dt>
              <dd className="m-0 font-semibold" data-testid="receive-context-timing">
                {po.paymentTimingLabel?.trim() ||
                  (po.paymentTiming === "PayBeforeFulfillment"
                    ? t("connectedCommerce.timing.payBefore")
                    : po.paymentTiming === "PayOnDeliveryOrReceipt"
                      ? t("connectedCommerce.timing.payOnDelivery")
                      : po.paymentTiming === "SupplierCredit"
                        ? t("connectedCommerce.timing.supplierCredit")
                        : po.paymentTiming) ||
                  "—"}
              </dd>
            </div>
            <div className="po-document-summary__field po-document-summary__field--paymentStatus min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("purchasing.paymentStatus")}
              </dt>
              <dd className="m-0 font-semibold" data-testid="receive-context-payment-status">
                {lockedReceivePayment?.prepaidSettled
                  ? t("purchasing.paymentSettled")
                  : po.financialSettlementStatus === "Settled"
                    ? t("purchasing.paymentSettled")
                    : po.financialSettlementStatus === "AwaitingPayment"
                      ? t("purchasing.payBeforeDueChip")
                      : po.paymentTermLabel || po.paymentTerm || "—"}
              </dd>
            </div>
            <div className="po-document-summary__field po-document-summary__field--fulfillment min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("purchasing.fulfillmentMethod")}
              </dt>
              <dd className="m-0 font-semibold" data-testid="receive-context-fulfillment">
                {po.fulfillmentMethod === "Pickup"
                  ? t("purchasing.fulfillment.pickup")
                  : po.fulfillmentMethod === "Delivery"
                    ? t("purchasing.fulfillment.delivery")
                    : po.fulfillmentMethod?.trim() || "—"}
              </dd>
            </div>
            <div className="po-document-summary__field po-document-summary__field--receiving min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("purchasing.receivingStatus")}
              </dt>
              <dd className="m-0 font-semibold" data-testid="receive-context-receiving">
                {t("purchasing.awaitingGoodsReceipt")}
              </dd>
            </div>
          </dl>
        </Card>
      ) : null}
      {!online ? (
        <Notice tone="warning" testId="receive-offline">
          {t("purchasing.offline")}
        </Notice>
      ) : null}
      {po.canReceiveConnected === false ? (
        <Notice tone="warning" testId="receive-connected-gate">
          {t("purchasing.connectedReceiveBlocked")}
        </Notice>
      ) : null}
      {error ? (
        <Notice tone="danger" testId="receive-error">
          {error}
        </Notice>
      ) : null}

      {trackingConfirm ? (
        <Card data-testid="receive-tracking-confirm">
          <ul className="m-0 flex list-none flex-col gap-4 p-0">
            {untrackedReceivingLines.map((line) => (
              <li key={line.productId} data-testid={`receive-tracking-line-${line.productId}`}>
                <div className="font-medium">{line.name}</div>
                <p className="mt-1 mb-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.received")}: {line.receivedQty} {line.uom}
                </p>
                <p className="mt-0 mb-1 flex flex-wrap items-baseline gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="text-muted">{t("purchasing.purchasePrice")}:</span>
                  <MoneyDisplay amount={line.unitPurchaseCost} />
                </p>
                <p className="mt-0 mb-2 flex flex-wrap items-baseline gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="text-muted">{t("purchasing.purchaseAmount")}:</span>
                  <MoneyDisplay amount={line.purchaseAmount} />
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.inventoryNotCurrentlyTracked")}
                </p>
                <p className="mt-2 mb-1 text-[length:var(--exits-text-sm)] font-medium">
                  {t("purchasing.receivingWill")}
                </p>
                <ul className="m-0 list-disc pl-5 text-[length:var(--exits-text-sm)]">
                  <li>{t("purchasing.enableInventoryTracking")}</li>
                  <li>{t("purchasing.trackingStartsWithReceived")}</li>
                  <li>
                    {t("purchasing.addStockToBranch").replace("{name}", branchName || "—")}
                  </li>
                </ul>
              </li>
            ))}
          </ul>
          <div className="mt-4 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={busy || statusLocked}
              onClick={() => setTrackingConfirm(false)}
              data-testid="receive-tracking-back"
            >
              {t("purchasing.backToReceipt")}
            </Button>
            <Button
              type="button"
              disabled={!canReceive || busy || statusLocked}
              onClick={() => void postReceive(true)}
              data-testid="receive-confirm"
            >
              {busy ? t("purchasing.receiving") : t("purchasing.confirmReceipt")}
            </Button>
          </div>
        </Card>
      ) : (
        <>
          {!reviewing ? (
          <>
          <ExitsTableContainer data-testid="receive-lines-table">
            {filteredSortedLines.length === 0 ? (
              <EmptyState
                align="center"
                size="compact"
                title={t("purchasing.receiveLinesEmpty")}
                detail={t("purchasing.receiveLinesEmptyDetail")}
                testId="receive-lines-empty"
              />
            ) : (
              <>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text" colSize="flex">
                        {t("purchasing.receiveProduct")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text" colSize="sku">
                        {t("catalog.sku")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric" colSize="numeric">
                        {t("purchasing.ordered")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric" colSize="numeric">
                        {t("purchasing.receiveNow")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text" colSize="numeric">
                        {t("purchasing.unit")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        {t("purchasing.unitCost")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        {t("purchasing.lineTotal")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="actions" colSize="actions">
                        {t("purchasing.action")}
                      </ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {pagedLines.map((line) => {
                      const goodQty = parseNonNegativeQty(line.goodText) ?? 0;
                      const showExpiry = line.tracksExpiration && goodQty > 0;
                      const canEdit = !reviewing && canReceive && line.outstandingQty > 0;
                      const editing = editingProductId === line.productId;
                      const editLocked =
                        (editingProductId !== null && editingProductId !== line.productId) ||
                        (mobileEditingProductId !== null && mobileEditingProductId !== line.productId);
                      const editReceiveNow = editing && editingFields.has("receiveNow");
                      const needsClassification = unclassifiedProductIds.has(line.productId);
                      const discrepancySummary = formatReceiveDiscrepancySummary(line, {
                        damaged: t("purchasing.damaged"),
                        notDelivered: t("purchasing.notDelivered"),
                      });
                      const rowErrorMessages = editing
                        ? receiveEditErrorMessages(editErrors)
                        : [];
                      const showRowValidation = rowErrorMessages.length > 0;
                      const rowValidationId = `receive-line-validation-${line.productId}`;
                      return (
                        <Fragment key={line.productId}>
                        <ExitsTableRow
                          editing={editing}
                          error={highlightUnclassified && needsClassification}
                          data-testid={`receive-line-${line.productId}`}
                        >
                          <ExitsTableCell cellAlign="text" className="font-medium">
                            <div>{line.name}</div>
                            {!editing && lineHasReceiveDiscrepancy(line) ? (
                              <button
                                type="button"
                                className="mt-1 block max-w-full truncate border-0 bg-transparent p-0 text-left text-[length:var(--exits-text-xs)] font-normal underline-offset-2 hover:underline"
                                data-testid={`receive-discrepancy-summary-${line.productId}`}
                                disabled={!canEdit || editLocked}
                                onClick={() => openDiscrepancyForProduct(line.productId)}
                              >
                                {needsClassification ? (
                                  <span className="text-[var(--exits-warning)]">
                                    {t("purchasing.discrepancy")}: {t("purchasing.needsClassification")} ⚠
                                  </span>
                                ) : discrepancySummary ? (
                                  <span className="text-muted">
                                    {t("purchasing.discrepancy")}: {discrepancySummary} ✓
                                  </span>
                                ) : null}
                              </button>
                            ) : null}
                            {showExpiry ? (
                              <div className="text-[length:var(--exits-text-xs)] text-muted">
                                {t("purchasing.expiryDate")}
                              </div>
                            ) : null}
                            {editing && showExpiry ? (
                              <div className="mt-2 flex min-w-[12rem] flex-col gap-1.5">
                                <input
                                  type="date"
                                  className="exits-input"
                                  value={line.expiryDate}
                                  onChange={(e) =>
                                    updateLine(line.productId, { expiryDate: e.target.value })
                                  }
                                  aria-label={t("purchasing.expiryDate")}
                                  data-testid={`receive-expiry-${line.productId}`}
                                />
                                <input
                                  className="exits-input"
                                  value={line.lotNumber}
                                  onChange={(e) =>
                                    updateLine(line.productId, { lotNumber: e.target.value })
                                  }
                                  placeholder={t("purchasing.lotNumber")}
                                  aria-label={t("purchasing.lotNumber")}
                                  data-testid={`receive-lot-${line.productId}`}
                                />
                              </div>
                            ) : null}
                            {!editing && showExpiry && (line.expiryDate || line.lotNumber.trim()) ? (
                              <div className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                                {line.expiryDate || "—"}
                                {line.lotNumber.trim() ? ` · ${line.lotNumber}` : ""}
                              </div>
                            ) : null}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text" colSize="sku" className="tabular-nums text-muted">
                            {line.sku || "—"}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {line.orderedQty}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric">
                            {editReceiveNow ? (
                              <ExitsTableInlineEditor
                                invalid={Boolean(editErrors.receiveNow)}
                                errorId={
                                  editErrors.receiveNow ? rowValidationId : undefined
                                }
                              >
                                <QuantityInput
                                  label={t("purchasing.receiveNow")}
                                  value={line.goodText}
                                  onChange={(e) => {
                                    updateLine(line.productId, {
                                      goodText: e.target.value,
                                      // Reset classification when good qty changes.
                                      damagedText: "0",
                                      notDeliveredText: "0",
                                      remarksText: "",
                                    });
                                    setEditErrors((prev) => ({
                                      ...prev,
                                      receiveNow: undefined,
                                    }));
                                  }}
                                  onBlur={(e) =>
                                    onReceiveQtyBlur({
                                      ...line,
                                      goodText: e.currentTarget.value,
                                    })
                                  }
                                  aria-invalid={Boolean(editErrors.receiveNow)}
                                  aria-describedby={
                                    editErrors.receiveNow ? rowValidationId : undefined
                                  }
                                  data-testid={`receive-good-${line.productId}`}
                                />
                              </ExitsTableInlineEditor>
                            ) : (
                              <span className="tabular-nums">{line.goodText || "0"}</span>
                            )}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text" className="text-muted">
                            {line.uom || "—"}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="money" className="tabular-nums">
                            <MoneyDisplay amount={line.unitPurchaseCost} />
                          </ExitsTableCell>
                          <ExitsTableCell
                            cellAlign="money"
                            className="tabular-nums font-medium"
                            data-testid={`receive-line-total-${line.productId}`}
                          >
                            <MoneyDisplay
                              amount={roundMoney(goodQty * line.unitPurchaseCost)}
                            />
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="actions" colSize="actions">
                            {canEdit ? (
                              <ExitsTableActions
                                data-testid={`receive-line-actions-${line.productId}`}
                              >
                                {editing ? (
                                  <>
                                    <ReceiveIconAction
                                      label={`Save ${line.name} changes`}
                                      variant="success"
                                      data-testid={`receive-good-edit-save-${line.productId}`}
                                      onClick={() => finishReceiveEdit(line)}
                                    >
                                      <Check className="size-4" aria-hidden />
                                    </ReceiveIconAction>
                                    <ReceiveIconAction
                                      label={`Reset ${line.name} to original`}
                                      title="Reset"
                                      className="exits-table__action-reset"
                                      data-testid={`receive-good-edit-reset-${line.productId}`}
                                      onClick={() => resetReceiveEdit(line)}
                                    >
                                      <RotateCcw className="size-4" aria-hidden />
                                    </ReceiveIconAction>
                                  </>
                                ) : (
                                  <ExitsTableEditMenu
                                    fields={receiveEditableFields}
                                    ariaLabel={`Edit ${line.name}`}
                                    disabled={editLocked}
                                    data-testid={`receive-line-edit-menu-${line.productId}`}
                                    onSelectField={(key) => startReceiveFieldEdit(line, key)}
                                    onEditAll={() => startReceiveEditAll(line)}
                                  />
                                )}
                              </ExitsTableActions>
                            ) : null}
                          </ExitsTableCell>
                        </ExitsTableRow>
                        {showRowValidation ? (
                          <ExitsTableRow error>
                            <ExitsTableCell colSpan={8}>
                              <div
                                id={rowValidationId}
                                className="exits-table__row-validation"
                                role="alert"
                                data-testid={`receive-line-validation-${line.productId}`}
                              >
                                <span className="exits-table__row-validation-message">
                                  {rowErrorMessages.join(" ")}
                                </span>
                                <button
                                  type="button"
                                  className="exits-table__row-validation-close"
                                  aria-label={t("diagnostics.dismiss")}
                                  data-testid={`receive-line-validation-close-${line.productId}`}
                                  onClick={dismissReceiveEditErrors}
                                >
                                  <X className="size-3.5" aria-hidden strokeWidth={2} />
                                </button>
                              </div>
                            </ExitsTableCell>
                          </ExitsTableRow>
                        ) : null}
                        </Fragment>
                      );
                    })}
                  </ExitsTableBody>
                  <ExitsTableFooter>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text" colSpan={6}>
                        {t("purchasing.receiptTotal")}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <span className="font-semibold tabular-nums" data-testid="receive-estimated-total">
                          <MoneyDisplay amount={estimatedTotal} />
                        </span>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions" aria-hidden />
                    </ExitsTableRow>
                  </ExitsTableFooter>
                </ExitsTable>

                <ExitsTableMobile data-testid="receive-lines-mobile">
                  {pagedLines.map((line) => {
                    const goodQty = parseNonNegativeQty(line.goodText) ?? 0;
                    const showExpiry = line.tracksExpiration && goodQty > 0;
                    const canEdit = !reviewing && canReceive && line.outstandingQty > 0;
                    const mobileEditing = mobileEditingProductId === line.productId;
                    const editLocked =
                      (mobileEditingProductId !== null && mobileEditingProductId !== line.productId) ||
                      (editingProductId !== null && editingProductId !== line.productId);
                    const editReceiveNow = mobileEditing && mobileEditingFields.has("receiveNow");
                    return (
                      <ExitsTableMobileRow
                        key={line.productId}
                        editing={mobileEditing}
                        data-testid={`receive-line-mobile-${line.productId}`}
                      >
                        <div className="exits-table-mobile__title-row">
                          <div className="exits-table-mobile__lead-body min-w-0 flex-1">
                            <p className="exits-table-mobile__title">{line.name}</p>
                            <p className="exits-table-mobile__meta">
                              {[
                                line.sku ? `${t("catalog.sku")}: ${line.sku}` : null,
                                line.uom ? `${t("purchasing.unit")}: ${line.uom}` : null,
                                `${t("purchasing.ordered")}: ${line.orderedQty}`,
                              ]
                                .filter(Boolean)
                                .join(" · ")}
                            </p>
                          </div>
                          {canEdit ? (
                            <div className="exits-table-mobile__actions">
                              {mobileEditing ? (
                                <>
                                  <ReceiveIconAction
                                    label={`Save ${line.name} changes`}
                                    variant="success"
                                    data-testid={`receive-good-edit-save-mobile-${line.productId}`}
                                    onClick={() => finishReceiveEdit(line, true)}
                                  >
                                    <Check className="size-4" aria-hidden />
                                  </ReceiveIconAction>
                                  <ReceiveIconAction
                                    label={`Reset ${line.name} to original`}
                                    title="Reset"
                                    className="exits-table__action-reset"
                                    data-testid={`receive-good-edit-reset-mobile-${line.productId}`}
                                    onClick={() => resetReceiveEdit(line, true)}
                                  >
                                    <RotateCcw className="size-4" aria-hidden />
                                  </ReceiveIconAction>
                                </>
                              ) : (
                                <ExitsTableEditMenu
                                  fields={receiveEditableFields}
                                  ariaLabel={`Edit ${line.name}`}
                                  disabled={editLocked}
                                  data-testid={`receive-line-edit-menu-mobile-${line.productId}`}
                                  onSelectField={(key) => startReceiveFieldEdit(line, key, true)}
                                  onEditAll={() => startReceiveEditAll(line, true)}
                                />
                              )}
                            </div>
                          ) : null}
                        </div>
                        {(() => {
                          const needsClassification = unclassifiedProductIds.has(line.productId);
                          const discrepancySummary = formatReceiveDiscrepancySummary(line, {
                            damaged: t("purchasing.damaged"),
                            notDelivered: t("purchasing.notDelivered"),
                          });
                          if (!lineHasReceiveDiscrepancy(line) || mobileEditing) {
                            return null;
                          }
                          return (
                            <button
                              type="button"
                              className="mt-1 block max-w-full truncate border-0 bg-transparent p-0 text-left text-[length:var(--exits-text-xs)] underline-offset-2 hover:underline"
                              data-testid={`receive-discrepancy-summary-mobile-${line.productId}`}
                              disabled={!canEdit || editLocked}
                              onClick={() => openDiscrepancyForProduct(line.productId)}
                            >
                              {needsClassification ? (
                                <span className="text-[var(--exits-warning)]">
                                  {t("purchasing.discrepancy")}: {t("purchasing.needsClassification")} ⚠
                                </span>
                              ) : discrepancySummary ? (
                                <span className="text-muted">
                                  {t("purchasing.discrepancy")}: {discrepancySummary} ✓
                                </span>
                              ) : null}
                            </button>
                          );
                        })()}
                        <p className="exits-table-mobile__meta">
                          {t("purchasing.unitCost")}:{" "}
                          <MoneyDisplay amount={line.unitPurchaseCost} />
                        </p>
                        {editReceiveNow ? (
                          <div className="mt-2 grid gap-2">
                            <ExitsTableInlineEditor align="start">
                              <QuantityInput
                                label={t("purchasing.receiveNow")}
                                value={line.goodText}
                                onChange={(e) =>
                                  updateLine(line.productId, {
                                    goodText: e.target.value,
                                    damagedText: "0",
                                    notDeliveredText: "0",
                                    remarksText: "",
                                  })
                                }
                                onBlur={(e) =>
                                  onReceiveQtyBlur({
                                    ...line,
                                    goodText: e.currentTarget.value,
                                  })
                                }
                                data-testid={`receive-good-mobile-${line.productId}`}
                              />
                            </ExitsTableInlineEditor>
                            {showExpiry ? (
                              <>
                                <input
                                  type="date"
                                  className="exits-input"
                                  value={line.expiryDate}
                                  onChange={(e) =>
                                    updateLine(line.productId, { expiryDate: e.target.value })
                                  }
                                  aria-label={t("purchasing.expiryDate")}
                                  data-testid={`receive-expiry-mobile-${line.productId}`}
                                />
                                <input
                                  className="exits-input"
                                  value={line.lotNumber}
                                  onChange={(e) =>
                                    updateLine(line.productId, { lotNumber: e.target.value })
                                  }
                                  placeholder={t("purchasing.lotNumber")}
                                  aria-label={t("purchasing.lotNumber")}
                                  data-testid={`receive-lot-mobile-${line.productId}`}
                                />
                              </>
                            ) : null}
                          </div>
                        ) : (
                          <p className="exits-table-mobile__math mt-2">
                            {t("purchasing.receiveNow")}: {line.goodText || "0"}
                            {" · "}
                            {t("purchasing.lineTotal")}:{" "}
                            <MoneyDisplay
                              amount={roundMoney(goodQty * line.unitPurchaseCost)}
                            />
                          </p>
                        )}
                      </ExitsTableMobileRow>
                    );
                  })}
                </ExitsTableMobile>

                {filteredSortedLines.length > 10 ? (
                  <ExitsTablePagination
                    page={page}
                    pageSize={pageSize}
                    total={filteredSortedLines.length}
                    pageSizeOptions={PAGE_SIZE_OPTIONS}
                    onPageChange={setPage}
                    onPageSizeChange={(size) => {
                      setPageSize(size);
                      setPage(1);
                    }}
                    rowsPerPageLabel={t("exitsTable.rowsPerPage")}
                    previousLabel={t("exitsTable.previous")}
                    nextLabel={t("exitsTable.next")}
                    rangeLabel={t("exitsTable.range")}
                  />
                ) : null}
              </>
            )}
          </ExitsTableContainer>
          <Card data-testid="receive-value-summary" className="grid gap-3 p-3">
            <dl className="receive-value-summary__meta receive-payment-prepaid__meta m-0 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <div className="receive-payment-prepaid__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("purchasing.deliveryReference")}
                </dt>
                <dd className="m-0">
                  {canReceive ? (
                    <input
                      className="exits-input w-full min-w-0"
                      value={deliveryReference}
                      onChange={(e) => setDeliveryReference(e.target.value)}
                      data-testid="receive-delivery-reference"
                      aria-label={t("purchasing.deliveryReference")}
                    />
                  ) : (
                    <span className="font-semibold">{deliveryReference.trim() || "—"}</span>
                  )}
                </dd>
              </div>
              <div className="receive-payment-prepaid__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("purchasing.orderedValue")}
                </dt>
                <dd className="m-0 font-semibold tabular-nums" data-testid="receive-order-value">
                  <MoneyDisplay amount={orderValue} />
                </dd>
              </div>
              <div className="receive-payment-prepaid__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("purchasing.thisReceipt")}
                </dt>
                <dd className="m-0 font-semibold tabular-nums" data-testid="receive-this-receipt-value">
                  <MoneyDisplay amount={estimatedTotal} />
                </dd>
              </div>
              <div className="receive-payment-prepaid__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("purchasing.remainingValue")}
                </dt>
                <dd className="m-0 font-semibold tabular-nums" data-testid="receive-remaining-value">
                  <MoneyDisplay amount={remainingValue} />
                </dd>
              </div>
              {isPartialReceiptSummary && previouslyReceivedValue > 0.0000001 ? (
                <div className="receive-payment-prepaid__field min-w-0 sm:col-span-2 lg:col-span-4">
                  <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {t("purchasing.previouslyReceivedValue")}
                  </dt>
                  <dd
                    className="m-0 font-semibold tabular-nums"
                    data-testid="receive-previously-received-value"
                  >
                    <MoneyDisplay amount={previouslyReceivedValue} />
                  </dd>
                </div>
              ) : null}
            </dl>
          </Card>
          </>
          ) : (
            <div className="grid gap-3" data-testid="receive-review-summary">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
                {t("purchasing.reviewReceipt")}
              </h2>
              <ExitsTableContainer data-testid="receive-review-lines-table">
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text" colSize="flex">
                        {t("purchasing.receiveProduct")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text" colSize="sku">
                        {t("catalog.sku")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric" colSize="numeric">
                        {t("purchasing.ordered")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric" colSize="numeric">
                        {t("purchasing.goodReceived")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text" colSize="numeric">
                        {t("purchasing.unit")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        {t("purchasing.unitCost")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        {t("purchasing.lineTotal")}
                      </ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {(lines ?? []).map((line) => {
                      const good = parseNonNegativeQty(line.goodText) ?? 0;
                      const damaged = parseNonNegativeQty(line.damagedText) ?? 0;
                      const notDelivered = parseNonNegativeQty(line.notDeliveredText) ?? 0;
                      const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
                      const discrepancySummary = formatReceiveDiscrepancySummary(line, {
                        damaged: t("purchasing.damaged"),
                        notDelivered: t("purchasing.notDelivered"),
                      });
                      return (
                        <ExitsTableRow
                          key={line.productId}
                          data-testid={`receive-review-line-${line.productId}`}
                        >
                          <ExitsTableCell cellAlign="text" className="font-medium">
                            <div>{line.name}</div>
                            {discrepancy > 1e-9 ? (
                              <div className="mt-1 text-[length:var(--exits-text-xs)] font-normal text-muted">
                                {discrepancySummary
                                  ? `${t("purchasing.discrepancy")}: ${discrepancySummary}`
                                  : null}
                                {` · ${
                                  line.remainingAction === "cancel_remaining"
                                    ? t("purchasing.cancelRemaining")
                                    : line.remainingAction === "replace_later"
                                      ? t("purchasing.replaceLater")
                                      : t("purchasing.remainingDecisionCol")
                                }`}
                                {line.remarksText.trim()
                                  ? ` · ${line.remarksText.trim()}`
                                  : ""}
                                {damaged > 0
                                  ? ` · ${t("purchasing.damaged")}: ${formatStockQtyLabel(damaged, line.uom)}`
                                  : ""}
                                {notDelivered > 0
                                  ? ` · ${t("purchasing.notDelivered")}: ${formatStockQtyLabel(notDelivered, line.uom)}`
                                  : ""}
                              </div>
                            ) : null}
                          </ExitsTableCell>
                          <ExitsTableCell
                            cellAlign="text"
                            colSize="sku"
                            className="tabular-nums text-muted"
                          >
                            {line.sku || "—"}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {line.orderedQty}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {good}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text" className="text-muted">
                            {line.uom || "—"}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="money" className="tabular-nums">
                            <MoneyDisplay amount={line.unitPurchaseCost} />
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="money" className="tabular-nums font-medium">
                            <MoneyDisplay
                              amount={roundMoney(good * line.unitPurchaseCost)}
                            />
                          </ExitsTableCell>
                        </ExitsTableRow>
                      );
                    })}
                  </ExitsTableBody>
                  <ExitsTableFooter>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text" colSpan={6}>
                        {t("purchasing.receiptTotal")}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <span
                          className="font-semibold tabular-nums"
                          data-testid="receive-review-estimated-total"
                        >
                          <MoneyDisplay amount={estimatedTotal} />
                        </span>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableFooter>
                </ExitsTable>

                <ExitsTableMobile data-testid="receive-review-lines-mobile">
                  {(lines ?? []).map((line) => {
                    const good = parseNonNegativeQty(line.goodText) ?? 0;
                    const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
                    const discrepancySummary = formatReceiveDiscrepancySummary(line, {
                      damaged: t("purchasing.damaged"),
                      notDelivered: t("purchasing.notDelivered"),
                    });
                    return (
                      <ExitsTableMobileRow
                        key={line.productId}
                        data-testid={`receive-review-line-mobile-${line.productId}`}
                      >
                        <p className="exits-table-mobile__title">{line.name}</p>
                        <p className="exits-table-mobile__meta">
                          {[
                            line.sku ? `${t("catalog.sku")}: ${line.sku}` : null,
                            line.uom ? `${t("purchasing.unit")}: ${line.uom}` : null,
                            `${t("purchasing.ordered")}: ${line.orderedQty}`,
                          ]
                            .filter(Boolean)
                            .join(" · ")}
                        </p>
                        <p className="exits-table-mobile__math mt-1">
                          {t("purchasing.goodReceived")}: {good}
                          {" · "}
                          {t("purchasing.lineTotal")}:{" "}
                          <MoneyDisplay
                            amount={roundMoney(good * line.unitPurchaseCost)}
                          />
                        </p>
                        {discrepancy > 1e-9 && discrepancySummary ? (
                          <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                            {t("purchasing.discrepancy")}: {discrepancySummary}
                            {line.remarksText.trim()
                              ? ` · ${line.remarksText.trim()}`
                              : ""}
                          </p>
                        ) : null}
                      </ExitsTableMobileRow>
                    );
                  })}
                </ExitsTableMobile>
              </ExitsTableContainer>

              <Card
                className="po-document-summary po-document-summary--meta-cards po-document-summary--meta-cards-4 grid gap-3 p-3"
                data-testid="receive-review-meta"
              >
                <h2
                  className="po-document-summary__title m-0"
                  data-testid="receive-review-meta-title"
                >
                  {t("purchasing.receiptSummaryTitle")}
                </h2>
                <dl className="po-document-summary__meta m-0">
                  <div className="po-document-summary__field min-w-0">
                    <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("purchasing.deliveryReference")}
                    </dt>
                    <dd
                      className="m-0 truncate font-semibold"
                      title={deliveryReference.trim() || undefined}
                      data-testid="receive-review-delivery-reference"
                    >
                      {deliveryReference.trim() || "—"}
                    </dd>
                  </div>
                  <div className="po-document-summary__field min-w-0">
                    <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("purchasing.orderedValue")}
                    </dt>
                    <dd
                      className="m-0 font-semibold tabular-nums"
                      data-testid="receive-review-order-value"
                    >
                      <MoneyDisplay amount={orderValue} />
                    </dd>
                  </div>
                  <div className="po-document-summary__field min-w-0">
                    <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("purchasing.receiptTotal")}
                    </dt>
                    <dd
                      className="m-0 font-semibold tabular-nums"
                      data-testid="receive-review-receipt-total"
                    >
                      <MoneyDisplay amount={estimatedTotal} />
                    </dd>
                  </div>
                  <div className="po-document-summary__field min-w-0">
                    <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("purchasing.paymentMethod")}
                    </dt>
                    <dd className="m-0 font-semibold" data-testid="receive-review-payment-method">
                      {po.paymentTermLabel || po.paymentTerm || "—"}
                    </dd>
                  </div>
                </dl>
                {notes.trim() ? (
                  <div className="po-document-summary__field min-w-0">
                    <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("purchasing.receiveNotes")}
                    </dt>
                    <dd className="m-0 whitespace-pre-wrap font-semibold">{notes.trim()}</dd>
                  </div>
                ) : null}
              </Card>
            </div>
          )}

          {reviewing && remainingDecisionRows.length > 0 ? (
            <ReceiveRemainingQuantityTable
              title={t("purchasing.remainingDecisionTitle")}
              summaryText={remainingDecisionSummary}
              applyToAllLabel={t("purchasing.remainingApplyToAll")}
              productColLabel={t("purchasing.colProduct")}
              remainingColLabel={t("purchasing.remainingCol")}
              issueColLabel={t("purchasing.remainingIssueCol")}
              decisionColLabel={t("purchasing.remainingDecisionCol")}
              replaceLaterLabel={t("purchasing.replaceLater")}
              cancelRemainingLabel={t("purchasing.cancelRemaining")}
              rows={remainingDecisionRows}
              highlightUnresolved={highlightUnresolvedRemaining}
              onDecisionChange={(productId, action) => {
                setHighlightUnresolvedRemaining(false);
                setError(null);
                updateLine(productId, { remainingAction: action });
              }}
              onApplyToAll={(action) => {
                setHighlightUnresolvedRemaining(false);
                setError(null);
                setLines((prev) => {
                  if (!prev) {
                    return prev;
                  }
                  const remainingIds = new Set(remainingDecisionRows.map((row) => row.productId));
                  return prev.map((line) =>
                    remainingIds.has(line.productId) ? { ...line, remainingAction: action } : line,
                  );
                });
              }}
            />
          ) : null}

          {!reviewing && canReceive ? (
            <div className="grid gap-2">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.receiveNotes")}
                <textarea
                  className="exits-input receive-stock-notes"
                  rows={2}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
              </label>
              <ReceivePaymentSection
                estimatedTotal={estimatedTotal}
                mode={paymentMode}
                onModeChange={() => {}}
                lockedFromPo
                lockedPaymentMethodLabel={po?.paymentTermLabel}
                settlementFields={settlementFields}
                onSettlementFieldsChange={setSettlementFields}
                paidNowText={paidNowText}
                onPaidNowChange={setPaidNowText}
                paymentMethod={paymentMethod}
                onPaymentMethodChange={setPaymentMethod}
                dueDate={dueDate}
                onDueDateChange={setDueDate}
                paidNowValue={paidNowValue}
                sectionTitle={paymentSectionTitle}
                prepaidSettled={
                  lockedReceivePayment?.prepaidSettled === true ||
                  lockedReceivePayment?.alreadySettledAtReceipt === true ||
                  (lockedReceivePayment?.paymentTiming === "PayBeforeFulfillment" &&
                    lockedReceivePayment.prepaidIntegrityMissing !== true)
                }
                prepaidView={prepaidView}
                prepaidIntegrityMissing={
                  lockedReceivePayment?.prepaidIntegrityMissing === true
                }
                supplierCreditReadOnly={
                  lockedReceivePayment?.mode === "supplierCredit" &&
                  lockedReceivePayment.paymentMethod == null
                }
              />
            </div>
          ) : null}

          {reviewing ? (
            <ReceivePaymentSection
              estimatedTotal={estimatedTotal}
              mode={paymentMode}
              onModeChange={() => {}}
              lockedFromPo
              lockedPaymentMethodLabel={po?.paymentTermLabel}
              settlementFields={settlementFields}
              onSettlementFieldsChange={setSettlementFields}
              paidNowText={paidNowText}
              onPaidNowChange={setPaidNowText}
              paymentMethod={paymentMethod}
              onPaymentMethodChange={setPaymentMethod}
              dueDate={dueDate}
              onDueDateChange={setDueDate}
              paidNowValue={paidNowValue}
              disabled={busy || statusLocked}
              sectionTitle={paymentSectionTitle}
              prepaidSettled={
                lockedReceivePayment?.prepaidSettled === true ||
                lockedReceivePayment?.alreadySettledAtReceipt === true ||
                (lockedReceivePayment?.paymentTiming === "PayBeforeFulfillment" &&
                  lockedReceivePayment.prepaidIntegrityMissing !== true)
              }
              prepaidView={prepaidView}
              prepaidIntegrityMissing={
                lockedReceivePayment?.prepaidIntegrityMissing === true
              }
              supplierCreditReadOnly={
                lockedReceivePayment?.mode === "supplierCredit" &&
                lockedReceivePayment.paymentMethod == null
              }
            />
          ) : null}

          <div className="receive-stock-actions">
            {reviewing ? (
              <div className="receive-stock-actions__primary">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setReviewing(false)}
                  data-testid="receive-back-to-edit"
                >
                  <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
                  {t("purchasing.backToReceipt")}
                </Button>
                <Button
                  type="button"
                  disabled={!canReceive || busy || statusLocked}
                  onClick={onReviewConfirmClick}
                  data-testid="receive-confirm"
                >
                  {busy ? t("purchasing.receiving") : t("purchasing.confirmReceipt")}
                </Button>
              </div>
            ) : (
              <div className="receive-stock-actions__primary">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => navigate(`/purchasing/${purchaseOrderId}`)}
                  aria-label={t("purchasing.backDetail")}
                  data-testid="receive-footer-back"
                >
                  <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
                  {t("purchasing.backDetail")}
                </Button>
                <Button
                  type="button"
                  disabled={!canReceive || busy || hasMissingExpiryOnReceive}
                  onClick={onReview}
                  data-testid="receive-review"
                >
                  {t("purchasing.reviewReceipt")}
                </Button>
              </div>
            )}
          </div>
        </>
      )}

      <ReceiveDiscrepancyDialog
        open={discrepancyOpen}
        lines={discrepancyLines}
        onChangeLine={(productId, patch) => {
          const next: Partial<LineEdit> = {};
          if (patch.damagedText !== undefined) next.damagedText = patch.damagedText;
          if (patch.notDeliveredText !== undefined) next.notDeliveredText = patch.notDeliveredText;
          if (patch.remarksText !== undefined) next.remarksText = patch.remarksText;
          updateLine(productId, next);
        }}
        onCancel={closeDiscrepancyDialog}
        onConfirm={onDiscrepancyConfirmed}
        title={t("purchasing.discrepancyClassifyTitle")}
        classifyHint={t("purchasing.discrepancyClassifyHint")}
        allDamagedLabel={t("purchasing.allDamaged")}
        allNotDeliveredLabel={t("purchasing.allNotDelivered")}
        damagedLabel={t("purchasing.damaged")}
        notDeliveredLabel={t("purchasing.notDelivered")}
        remarksLabel={t("purchasing.discrepancyNote")}
        remarksRequiredLabel={t("checkout.fieldRequired")}
        remainingToClassifyLabel={t("purchasing.remainingToClassify")}
        cancelLabel={t("purchasing.cancel")}
        confirmLabel={t("purchasing.saveClassification")}
        notAcceptedTemplate={t("purchasing.notAcceptedQty")}
      />

      <PurchaseOrderTimelineDrawer
        open={timelineOpen}
        onOpenChange={setTimelineOpen}
        po={po}
        receipts={receipts}
        resolveActor={actors.resolve}
        isResolving={actors.isResolving}
        receiptsLoading={receiptsQuery.isLoading}
      />

      {documentPreviewOpen ? (
        <BusinessDocumentPreview
          open={documentPreviewOpen}
          onClose={() => setDocumentPreviewOpen(false)}
          title={po.poNumber ?? t("purchasing.receiveTitle")}
          closeLabel={t("summary.closePreview")}
          printLabel={t("exitsTable.print")}
          pdfLabel={t("exitsTable.exportPdf")}
          testId="receive-po-document-preview"
        >
          <PurchaseOrderBusinessDocument
            po={po}
            supplierName={po.supplierName}
            settings={documentSettings}
            identity={identity}
            headerVisibility={headerVisibility(documentSettings.header)}
            deliveryAddress={boundWorkspace?.branchName ?? null}
            preview
          />
        </BusinessDocumentPreview>
      ) : null}
    </div>
  );
}
