import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { FileDown, Printer, Plus } from "lucide-react";
import { searchCheckoutCustomers } from "@/api/pos/pos-customers-client";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import {
  createQuotation,
  getQuotation,
  issueQuotation,
  updateQuotation,
  writePendingQuotationConvert,
  type CreateQuotationLineInput,
} from "@/api/pos/pos-quotations-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";
import {
  exportBusinessDocumentPdf,
  printBusinessDocument,
} from "@/features/documents/print-business-document";
import { useBusinessDocumentIdentity } from "@/features/documents/use-business-document-identity";
import { useOrganizationDocumentSettings } from "@/features/documents/use-organization-document-settings";
import { QuotationBusinessDocument } from "@/features/quotations/QuotationBusinessDocument";
import { QuotationManualCustomerDrawer } from "@/features/quotations/QuotationManualCustomerDrawer";
import { formatPeso } from "@/lib/format-money";
import { useI18n } from "@/i18n/I18nProvider";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DraftLine = CreateQuotationLineInput & { name: string };

export function QuotationEditorPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { quotationId } = useParams<{ quotationId?: string }>();
  const isNew = !quotationId || quotationId === "new";
  const online = useBrowserOnline();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  const { settings } = useOrganizationDocumentSettings(organizationId);
  const { identity, headerVisibility } = useBusinessDocumentIdentity(organizationId);
  const smartBack = usePageSmartBack({
    fallback: "quotations",
    backLabel: t("quotations.title"),
  });

  const workspace = organizationId
    ? { organizationId, branchId: branchId ?? undefined }
    : null;

  const existingQuery = useQuery({
    queryKey: ["quotation", organizationId, quotationId],
    enabled: Boolean(workspace) && !isNew && online,
    queryFn: ({ signal }) => getQuotation(workspace!, quotationId!, signal),
  });

  const [customerId, setCustomerId] = useState<string | null>(null);
  const [customerName, setCustomerName] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [productSearch, setProductSearch] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [terms, setTerms] = useState("");
  const [saving, setSaving] = useState(false);
  const [customerDrawerOpen, setCustomerDrawerOpen] = useState(false);

  useEffect(() => {
    const q = existingQuery.data;
    if (!q) return;
    setCustomerId(q.customerId);
    setCustomerName(q.customerDisplayName);
    setLines(
      q.lines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        discountAmount: l.discountAmount,
        name: l.nameSnapshot ?? "Product",
      })),
    );
    setValidUntil(q.validUntil ?? "");
    setReference(q.reference ?? "");
    setNotes(q.notes ?? "");
    setTerms(q.terms ?? "");
  }, [existingQuery.data]);

  const customerResults = useQuery({
    queryKey: ["quotation-customer-search", organizationId, customerSearch],
    enabled: Boolean(workspace) && online && customerSearch.trim().length >= 1,
    queryFn: ({ signal }) =>
      searchCheckoutCustomers(workspace!, { search: customerSearch.trim(), pageSize: 20 }, signal),
  });

  const productResults = useQuery({
    queryKey: ["quotation-product-search", organizationId, productSearch],
    enabled: Boolean(workspace) && online && productSearch.trim().length >= 1,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: productSearch.trim(), status: "Active", canBeSold: true, pageSize: 20 },
        signal,
      ),
  });

  const quotation = existingQuery.data;
  const isDraft = isNew || quotation?.status === "Draft";
  const issued = quotation && quotation.status !== "Draft";

  const subtotal = useMemo(
    () =>
      lines.reduce(
        (sum, line) => sum + Math.max(0, line.quantity * line.unitPrice - (line.discountAmount ?? 0)),
        0,
      ),
    [lines],
  );

  const documentSettings = settings ?? DEFAULT_DOCUMENT_SETTINGS;
  const docIdentity = {
    ...identity,
    branchName: boundWorkspace?.branchName ?? identity.branchName,
  };

  async function saveDraft() {
    if (!workspace || !branchId || !customerId || lines.length === 0) {
      showToast(t("quotations.saveRequirements"), "error");
      return;
    }
    setSaving(true);
    try {
      if (isNew) {
        const created = await createQuotation(workspace, {
          customerId,
          branchId,
          lines: lines.map(({ productId, quantity, unitPrice, discountAmount }) => ({
            productId,
            quantity,
            unitPrice,
            discountAmount,
          })),
          validUntil: validUntil || null,
          reference: reference || null,
          notes: notes || null,
          terms: terms || null,
        });
        showToast(t("quotations.saved"), "success");
        await queryClient.invalidateQueries({ queryKey: ["quotations"] });
        navigate(`/quotations/${created.quotationId}`, { replace: true });
      } else if (quotation) {
        await updateQuotation(workspace, quotation.quotationId, {
          customerId,
          branchId,
          lines: lines.map(({ productId, quantity, unitPrice, discountAmount }) => ({
            productId,
            quantity,
            unitPrice,
            discountAmount,
          })),
          expectedUpdatedAtUtc: quotation.updatedAtUtc,
          validUntil: validUntil || null,
          reference: reference || null,
          notes: notes || null,
          terms: terms || null,
        });
        showToast(t("quotations.saved"), "success");
        await queryClient.invalidateQueries({ queryKey: ["quotation", organizationId, quotationId] });
      }
    } catch (err) {
      showToast(err instanceof Error ? err.message : t("quotations.saveFailed"), "error");
    } finally {
      setSaving(false);
    }
  }

  async function issue() {
    if (!workspace || !quotationId || isNew) return;
    setSaving(true);
    try {
      await issueQuotation(workspace, quotationId);
      showToast(t("quotations.issued"), "success");
      await queryClient.invalidateQueries({ queryKey: ["quotation", organizationId, quotationId] });
    } catch (err) {
      showToast(err instanceof Error ? err.message : t("quotations.issueFailed"), "error");
    } finally {
      setSaving(false);
    }
  }

  function convertToSale() {
    if (!quotation || !customerId) return;
    writePendingQuotationConvert({
      quotationId: quotation.quotationId,
      customerId: quotation.customerId,
      customerDisplayName: quotation.customerDisplayName,
      lines: quotation.lines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        name: l.nameSnapshot ?? undefined,
      })),
    });
    navigate("/sell", { replace: false });
    showToast(t("quotations.convertSeeded"), "success");
  }

  if (!workspace || !organizationId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className="exits-page p-4" data-testid="quotation-editor-page">
        <PageHeader title={t("quotations.title")} {...smartBack} />
        <ErrorState title={t("offline.internetRequiredTitle")} detail={t("quotations.offline")} />
      </div>
    );
  }

  if (!isNew && existingQuery.isLoading) {
    return <LoadingState label={t("quotations.loading")} />;
  }

  if (!isNew && (existingQuery.isError || !quotation)) {
    return (
      <div className="exits-page p-4" data-testid="quotation-editor-page">
        <PageHeader title={t("quotations.title")} {...smartBack} />
        <ErrorState title={t("quotations.missing")} detail={t("quotations.missingDetail")} />
      </div>
    );
  }

  return (
    <div className="exits-page flex flex-col gap-4 p-4" data-testid="quotation-editor-page">
      <PageHeader
        title={isNew ? t("quotations.new") : quotation?.quotationNumber ?? t("quotations.draftLabel")}
        description={quotation?.status}
        {...smartBack}
        trailing={
          issued ? (
            <div className="flex flex-wrap gap-2 print:hidden">
              <Button type="button" variant="outline" onClick={() => printBusinessDocument()}>
                <Printer className="size-4" aria-hidden />
                {t("summary.print")}
              </Button>
              <Button type="button" variant="outline" onClick={() => exportBusinessDocumentPdf()}>
                <FileDown className="size-4" aria-hidden />
                {t("summary.exportPdf")}
              </Button>
            </div>
          ) : null
        }
      />

      {isDraft ? (
        <section className="flex flex-col gap-3" data-testid="quotation-editor-form">
          <div className="flex flex-col gap-2">
            <h2 className="m-0 text-base font-semibold">{t("quotations.customer")}</h2>
            {customerId ? (
              <p className="m-0 text-sm" data-testid="quotation-selected-customer">
                {customerName}
              </p>
            ) : null}
            <Input
              value={customerSearch}
              placeholder={t("quotations.customerSearchPlaceholder")}
              data-testid="quotation-customer-search"
              onChange={(e) => setCustomerSearch(e.target.value)}
            />
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                data-testid="quotation-add-customer"
                onClick={() => setCustomerDrawerOpen(true)}
              >
                <Plus className="size-4" aria-hidden />
                {t("quotations.addCustomer")}
              </Button>
            </div>
            {(customerResults.data?.items ?? [])
              .filter((c) => c.kind === "Customer" && c.customerId)
              .map((c) => (
              <button
                key={c.customerId!}
                type="button"
                className="rounded-md border border-border px-3 py-2 text-left text-sm"
                data-testid={`quotation-pick-customer-${c.customerId}`}
                onClick={() => {
                  setCustomerId(c.customerId!);
                  setCustomerName(c.displayName);
                  setCustomerSearch("");
                }}
              >
                {c.displayName}
              </button>
            ))}
          </div>

          <div className="flex flex-col gap-2">
            <h2 className="m-0 text-base font-semibold">{t("quotations.lines")}</h2>
            <Input
              value={productSearch}
              placeholder={t("quotations.productSearchPlaceholder")}
              data-testid="quotation-product-search"
              onChange={(e) => setProductSearch(e.target.value)}
            />
            {(productResults.data?.items ?? []).map((p) => (
              <button
                key={p.productId}
                type="button"
                className="rounded-md border border-border px-3 py-2 text-left text-sm"
                data-testid={`quotation-pick-product-${p.productId}`}
                onClick={() => {
                  setLines((prev) => [
                    ...prev,
                    {
                      productId: p.productId,
                      quantity: 1,
                      unitPrice: p.sellingPrice,
                      name: p.name,
                    },
                  ]);
                  setProductSearch("");
                }}
              >
                {p.name} · {formatPeso(p.sellingPrice)}
              </button>
            ))}
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {lines.map((line, index) => (
                <li
                  key={`${line.productId}-${index}`}
                  className="flex flex-wrap items-center gap-2 rounded-md border border-border px-3 py-2"
                >
                  <span className="min-w-0 flex-1 text-sm">{line.name}</span>
                  <Input
                    className="w-20"
                    type="number"
                    min={0.001}
                    step="any"
                    value={line.quantity}
                    onChange={(e) => {
                      const quantity = Number(e.target.value);
                      setLines((prev) =>
                        prev.map((l, i) => (i === index ? { ...l, quantity } : l)),
                      );
                    }}
                  />
                  <Input
                    className="w-28"
                    type="number"
                    min={0}
                    step="any"
                    value={line.unitPrice}
                    onChange={(e) => {
                      const unitPrice = Number(e.target.value);
                      setLines((prev) =>
                        prev.map((l, i) => (i === index ? { ...l, unitPrice } : l)),
                      );
                    }}
                  />
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setLines((prev) => prev.filter((_, i) => i !== index))}
                  >
                    {t("quotations.removeLine")}
                  </Button>
                </li>
              ))}
            </ul>
            <p className="m-0 text-sm font-medium">{t("quotations.subtotal")}: {formatPeso(subtotal)}</p>
          </div>

          <label className="flex flex-col gap-1 text-sm">
            <span>{t("quotations.validUntil")}</span>
            <Input type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>{t("quotations.reference")}</span>
            <Input value={reference} onChange={(e) => setReference(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>{t("quotations.notes")}</span>
            <textarea
              className="min-h-16 rounded-md border border-border bg-background px-3 py-2 text-sm"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>{t("quotations.terms")}</span>
            <textarea
              className="min-h-16 rounded-md border border-border bg-background px-3 py-2 text-sm"
              value={terms}
              onChange={(e) => setTerms(e.target.value)}
            />
          </label>

          <div className="flex flex-wrap gap-2">
            <Button type="button" disabled={saving} onClick={() => void saveDraft()} data-testid="quotation-save">
              {t("quotations.saveDraft")}
            </Button>
            {!isNew ? (
              <Button
                type="button"
                variant="outline"
                disabled={saving}
                onClick={() => void issue()}
                data-testid="quotation-issue"
              >
                {t("quotations.issue")}
              </Button>
            ) : null}
          </div>
        </section>
      ) : null}

      {quotation ? (
        <div className="mx-auto w-full max-w-[52rem]" data-testid="quotation-document-preview">
          <QuotationBusinessDocument
            quotation={quotation}
            settings={documentSettings}
            identity={docIdentity}
            headerVisibility={headerVisibility(documentSettings.header)}
            branchName={boundWorkspace?.branchName}
            preview
          />
          {quotation.status === "Sent" || quotation.status === "Accepted" ? (
            <div className="mt-3 flex flex-wrap gap-2 print:hidden">
              <Button type="button" data-testid="quotation-convert" onClick={convertToSale}>
                {t("quotations.convertToSale")}
              </Button>
              {quotation.convertedSaleId ? (
                <Button asChild variant="outline">
                  <AppLinkWithReturn to={`/sell/sales/${quotation.convertedSaleId}`}>
                    {t("quotations.viewSale")}
                  </AppLinkWithReturn>
                </Button>
              ) : null}
            </div>
          ) : null}
        </div>
      ) : null}

      <QuotationManualCustomerDrawer
        open={customerDrawerOpen}
        onOpenChange={setCustomerDrawerOpen}
        workspace={workspace}
        onCreated={(c) => {
          setCustomerId(c.customerId);
          setCustomerName(c.displayName);
          showToast(t("quotations.customerSelected"), "success");
        }}
      />
    </div>
  );
}
