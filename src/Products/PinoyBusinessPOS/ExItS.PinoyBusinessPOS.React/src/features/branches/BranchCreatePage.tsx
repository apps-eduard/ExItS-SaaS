import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { canInviteOrganizationStaff, canManageBranchFulfillment, canUseWarehouseBranches } from "@/access/pos-capabilities";
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
      <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3" data-testid="branch-create-denied">
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
    <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3" data-testid="branch-create-page">
      <PageHeader
        title={t("branches.create.title")}
        description={t("branches.create.lede")}
        backTo="/org/branches"
        backLabel={t("branches.backList")}
        backTestId="page-header-back-branches"
      />

      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setFormError(null);
          createMutation.mutate();
        }}
      >
        <section className="catalog-form-section exits-animate-panel gap-3">
          <h2 className="catalog-form-section__title">{t("branches.create.title")}</h2>
          <div className="catalog-form-section__grid">
            <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.create.name")}
              <input
                className="catalog-form-select font-normal"
                value={name}
                onChange={(e) => setName(e.target.value)}
                data-testid="branch-create-name"
                required
              />
            </label>
            <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.create.code")}
              <input
                className="catalog-form-select font-normal uppercase"
                value={code}
                onChange={(e) => {
                  setCodeTouched(true);
                  setCode(e.target.value.toUpperCase());
                }}
                data-testid="branch-create-code"
                required
              />
              <span className="font-normal text-muted">{t("branches.create.codeHelper")}</span>
            </label>
            <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.type")}
              <select
                className="catalog-form-select font-normal"
                value={branchType}
                onChange={(e) => setBranchType(normalizeBranchType(e.target.value))}
                data-testid="branch-create-type"
              >
                <option value="Retail">{t("branches.type.retail")}</option>
                {warehouseAllowed ? (
                  <option value="Warehouse">{t("branches.type.warehouse")}</option>
                ) : null}
              </select>
              <span className="font-normal text-muted">
                {!warehouseAllowed
                  ? t("branches.type.warehouseLocked")
                  : branchType === "Warehouse"
                    ? t("branches.type.warehouseHelp")
                    : t("branches.type.retailHelp")}
              </span>
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.contactPhone")}
              <input
                className="catalog-form-select font-normal"
                value={contactPhone}
                onChange={(e) => setContactPhone(e.target.value)}
                data-testid="branch-create-phone"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.timeZone")}
              <input
                className="catalog-form-select bg-[var(--exits-surface-muted)] font-normal"
                value={BRANCH_DEFAULT_TIME_ZONE}
                readOnly
                aria-readonly="true"
                data-testid="branch-create-timezone"
              />
            </label>
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="branch-create-address">
          <h2 className="catalog-form-section__title">{t("branches.addressTitle")}</h2>
          <div className="catalog-form-section__grid">
            <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.addressLine1")}
              <input
                className="catalog-form-select font-normal"
                value={addressLine1}
                onChange={(e) => setAddressLine1(e.target.value)}
                data-testid="branch-create-address1"
              />
            </label>
            <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.addressLine2")}
              <input
                className="catalog-form-select font-normal"
                value={addressLine2}
                onChange={(e) => setAddressLine2(e.target.value)}
                data-testid="branch-create-address2"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.city")}
              <input
                className="catalog-form-select font-normal"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                data-testid="branch-create-city"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.region")}
              <input
                className="catalog-form-select font-normal"
                value={region}
                onChange={(e) => setRegion(e.target.value)}
                data-testid="branch-create-region"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.postalCode")}
              <input
                className="catalog-form-select font-normal"
                value={postalCode}
                onChange={(e) => setPostalCode(e.target.value)}
                data-testid="branch-create-postal"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.countryCode")}
              <input
                className="catalog-form-select bg-[var(--exits-surface-muted)] font-normal"
                value={BRANCH_DEFAULT_COUNTRY_CODE}
                readOnly
                aria-readonly="true"
                data-testid="branch-create-country"
              />
            </label>
          </div>
        </section>

        {formError ? (
          <div className="exits-alert exits-alert--error" role="alert" data-testid="branch-create-error">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{formError}</p>
          </div>
        ) : null}

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            onClick={() => navigate("/org/branches")}
          >
            {t("branches.cancel")}
          </Button>
          <Button
            type="submit"
            className="min-h-11"
            disabled={createMutation.isPending}
            data-testid="branch-create-submit"
          >
            {createMutation.isPending ? t("branches.create.creating") : t("branches.create.submit")}
          </Button>
        </div>
      </form>
    </div>
  );
}
