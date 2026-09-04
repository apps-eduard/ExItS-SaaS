import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { LockKeyhole, Plus, RotateCcw, X } from "lucide-react";
import {
  canInviteOrganizationStaff,
  canManageBranchFulfillment,
  canUseWarehouseBranches,
} from "@/access/pos-capabilities";
import { createOrganizationBranch } from "@/api/platform/organization-branches-client";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { suggestBranchCode } from "@/features/branches/branch-code";
import {
  BRANCH_DEFAULT_COUNTRY_CODE,
  BRANCH_DEFAULT_TIME_ZONE,
} from "@/features/branches/branch-defaults";
import {
  normalizeBranchType,
  type OrganizationBranchType,
} from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function BranchCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const canCreate = canInviteOrganizationStaff(sessionGrant);
  const warehouseAllowed = canUseWarehouseBranches(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [codeTouched, setCodeTouched] = useState(false);
  const [branchType, setBranchType] = useState<OrganizationBranchType>("Retail");
  const [contactPhone, setContactPhone] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [addressLine2, setAddressLine2] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!codeTouched) {
      setCode(suggestBranchCode(name));
    }
  }, [name, codeTouched]);

  function resetForm() {
    setName("");
    setCode("");
    setCodeTouched(false);
    setBranchType("Retail");
    setContactPhone("");
    setAddressLine1("");
    setAddressLine2("");
    setCity("");
    setRegion("");
    setPostalCode("");
    setFormError(null);
  }

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId) {
        throw new Error(t("branches.create.failed"));
      }
      const trimmedName = name.trim();
      const trimmedCode = code.trim().toUpperCase();
      if (!trimmedName) {
        throw new Error(t("branches.nameRequired"));
      }
      if (!trimmedCode) {
        throw new Error(t("branches.create.codeRequired"));
      }
      const result = await createOrganizationBranch(organizationId, {
        name: trimmedName,
        code: trimmedCode,
        branchType,
        contactPhone: contactPhone.trim() || null,
        addressLine1: addressLine1.trim() || null,
        addressLine2: addressLine2.trim() || null,
        city: city.trim() || null,
        region: region.trim() || null,
        postalCode: postalCode.trim() || null,
        countryCode: BRANCH_DEFAULT_COUNTRY_CODE,
        timeZoneId: BRANCH_DEFAULT_TIME_ZONE,
        pickupEnabled: false,
        deliveryEnabled: false,
        customerOrderingEnabled: false,
      });
      if (!result.ok) {
        const codeHint = (result.errorCode ?? "").toLowerCase();
        if (codeHint.includes("code_conflict") || result.status === 409) {
          throw new Error(t("branches.create.codeConflict"));
        }
        if (codeHint.includes("capacity_exceeded")) {
          throw new Error(t("branches.create.capacityExceeded"));
        }
        if (codeHint.includes("warehouse_entitlement")) {
          throw new Error(t("branches.create.warehouseEntitlement"));
        }
        throw new Error(result.body?.detail ?? t("branches.create.failed"));
      }
      return result.value;
    },
    onSuccess: (branch) => {
      navigate(`/org/branches/${branch.id}`, { replace: true });
    },
    onError: (error) => {
      setFormError(error instanceof Error ? error.message : t("branches.create.failed"));
    },
  });

  if (!canManage || !canCreate) {
    return (
      <div
        className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="branch-create-denied"
      >
        <PageHeader
          title={t("branches.create.title")}
          description={t("branches.mgmt.denied")}
          backTo="/org/branches"
          backLabel={t("branches.backList")}
          backTestId="page-header-back-branches"
        />
      </div>
    );
  }

  return (
    <div
      className="branch-mgmt-page branch-create-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="branch-create-page"
    >
      <PageHeader
        title={t("branches.create.title")}
        description={t("branches.create.lede")}
        backTo="/org/branches"
        backLabel={t("branches.backList")}
        backTestId="page-header-back-branches"
      />

      <form
        className="branch-create-form flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setFormError(null);
          createMutation.mutate();
        }}
      >
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="branch-create-details"
        >
          <h2 className="catalog-form-section__title exits-type-section-title">
            {t("branches.detailsTitle")}
          </h2>
          <div className="catalog-form-section__grid">
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.create.name")}
              <input
                className="catalog-form-select"
                value={name}
                onChange={(e) => setName(e.target.value)}
                data-testid="branch-create-name"
                required
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.create.code")}
              <input
                className="catalog-form-select uppercase"
                value={code}
                onChange={(e) => {
                  setCodeTouched(true);
                  setCode(e.target.value.toUpperCase());
                }}
                data-testid="branch-create-code"
                required
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.type")}
              <select
                className="catalog-form-select"
                value={branchType}
                onChange={(e) => setBranchType(normalizeBranchType(e.target.value))}
                data-testid="branch-create-type"
              >
                <option value="Retail">{t("branches.type.retail")}</option>
                {warehouseAllowed ? (
                  <option value="Warehouse">{t("branches.type.warehouse")}</option>
                ) : null}
              </select>
              {warehouseAllowed ? (
                <span className="branch-create-field-helper">
                  {branchType === "Warehouse"
                    ? t("branches.type.warehouseHelp")
                    : t("branches.type.retailHelp")}
                </span>
              ) : (
                <p
                  className="branch-create-entitlement m-0"
                  data-testid="branch-create-warehouse-locked"
                >
                  <LockKeyhole className="size-3.5 shrink-0" aria-hidden />
                  <span>{t("branches.type.warehouseLocked")}</span>
                </p>
              )}
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.timeZone")}
              <input
                className="catalog-form-select catalog-form-select--readonly"
                value={BRANCH_DEFAULT_TIME_ZONE}
                readOnly
                aria-readonly="true"
                data-testid="branch-create-timezone"
              />
            </label>
            <label className="catalog-form-field--full exits-type-label flex flex-col gap-1.5">
              {t("branches.contactPhone")}
              <input
                className="catalog-form-select"
                value={contactPhone}
                onChange={(e) => setContactPhone(e.target.value)}
                data-testid="branch-create-phone"
              />
            </label>
          </div>
        </section>

        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="branch-create-address"
        >
          <h2 className="catalog-form-section__title exits-type-section-title">
            {t("branches.addressTitle")}
          </h2>
          <div className="catalog-form-section__grid">
            <label className="catalog-form-field--full exits-type-label flex flex-col gap-1.5">
              {t("branches.addressLine1")}
              <input
                className="catalog-form-select"
                value={addressLine1}
                onChange={(e) => setAddressLine1(e.target.value)}
                data-testid="branch-create-address1"
              />
            </label>
            <label className="catalog-form-field--full exits-type-label flex flex-col gap-1.5">
              {t("branches.addressLine2")}
              <input
                className="catalog-form-select"
                value={addressLine2}
                onChange={(e) => setAddressLine2(e.target.value)}
                data-testid="branch-create-address2"
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.city")}
              <input
                className="catalog-form-select"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                data-testid="branch-create-city"
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.region")}
              <input
                className="catalog-form-select"
                value={region}
                onChange={(e) => setRegion(e.target.value)}
                data-testid="branch-create-region"
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.postalCode")}
              <input
                className="catalog-form-select"
                value={postalCode}
                onChange={(e) => setPostalCode(e.target.value)}
                data-testid="branch-create-postal"
              />
            </label>
            <label className="exits-type-label flex flex-col gap-1.5">
              {t("branches.countryCode")}
              <input
                className="catalog-form-select catalog-form-select--readonly"
                value={BRANCH_DEFAULT_COUNTRY_CODE}
                readOnly
                aria-readonly="true"
                data-testid="branch-create-country"
              />
            </label>
          </div>
        </section>

        {formError ? (
          <div
            className="exits-alert exits-alert--error"
            role="alert"
            data-testid="branch-create-error"
          >
            <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">{formError}</p>
          </div>
        ) : null}

        <div className="branch-create-actions gap-3">
          <Button
            type="submit"
            disabled={createMutation.isPending}
            data-testid="branch-create-submit"
          >
            <Plus className="size-4 shrink-0" aria-hidden />
            {createMutation.isPending
              ? t("branches.create.creating")
              : t("branches.create.submit")}
          </Button>
          <Button
            type="button"
            variant="outline"
            data-testid="branch-create-cancel"
            onClick={() => navigate("/org/branches")}
          >
            <X className="size-4 shrink-0" aria-hidden />
            {t("branches.cancel")}
          </Button>
          <Button
            type="button"
            variant="outline"
            data-testid="branch-create-reset"
            disabled={createMutation.isPending}
            onClick={resetForm}
          >
            <RotateCcw className="size-4 shrink-0" aria-hidden />
            {t("branches.create.reset")}
          </Button>
        </div>
      </form>
    </div>
  );
}
