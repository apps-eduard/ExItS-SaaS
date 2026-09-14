import { useEffect, useId, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, RefreshCw, Search } from "lucide-react";
import {
  listBusinessCustomerOrganizationContacts,
  updateBusinessCustomerRelationshipContact,
  type BusinessCustomer,
  type BuyerOrganizationBusinessContact,
} from "@/api/pos/pos-connected-suppliers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { StatusChip } from "@/components/ui/badge";
import { SegmentedControl, SegmentedOption } from "@/components/ui/segmented-control";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type ContactSourceMode = "OrganizationMember" | "Custom";

type BusinessRelationshipContactEditDrawerProps = {
  open: boolean;
  onClose: () => void;
  workspace: PosWorkspaceScope;
  customer: BusinessCustomer;
};

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }
  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase();
  }
  return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
}

function hasCustomContactContent(customer: BusinessCustomer): boolean {
  return Boolean(
    customer.contactPersonName?.trim() ||
      customer.contactDepartment?.trim() ||
      customer.contactRole?.trim() ||
      customer.contactPhone?.trim() ||
      customer.contactEmail?.trim() ||
      customer.preferredContactMethod?.trim() ||
      customer.deliveryInstructions?.trim() ||
      customer.billingContactNotes?.trim() ||
      customer.internalNotes?.trim(),
  );
}

/** Connected + empty → Organization staff; honor saved source otherwise. Pending → Custom only. */
export function resolveInitialContactSource(
  customer: BusinessCustomer,
  canUseOrganizationContact: boolean,
): ContactSourceMode {
  if (!canUseOrganizationContact) {
    return "Custom";
  }
  if (customer.contactSource === "OrganizationMember" || customer.organizationMemberId) {
    return "OrganizationMember";
  }
  if (customer.contactSource === "Custom" && hasCustomContactContent(customer)) {
    return "Custom";
  }
  return "OrganizationMember";
}

function matchesStaffSearch(contact: BuyerOrganizationBusinessContact, term: string): boolean {
  if (!term) {
    return true;
  }
  const haystack = [contact.displayName, contact.roleTitle, contact.department ?? "", contact.email ?? ""]
    .join(" ")
    .toLowerCase();
  return haystack.includes(term.toLowerCase());
}

export function BusinessRelationshipContactEditDrawer({
  open,
  onClose,
  workspace,
  customer,
}: BusinessRelationshipContactEditDrawerProps) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const listboxId = useId();
  const searchInputRef = useRef<HTMLInputElement | null>(null);

  const isConnected = customer.relationshipStatus === "Active";
  const canUseOrganizationContact = isConnected;

  const [source, setSource] = useState<ContactSourceMode>("Custom");
  const [selectedMemberId, setSelectedMemberId] = useState<string | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [contactSearch, setContactSearch] = useState("");
  const [highlight, setHighlight] = useState(0);
  const [contactPerson, setContactPerson] = useState("");
  const [department, setDepartment] = useState("");
  const [role, setRole] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [preferredMethod, setPreferredMethod] = useState("");
  const [deliveryInstructions, setDeliveryInstructions] = useState("");
  const [billingNotes, setBillingNotes] = useState("");
  const [internalNotes, setInternalNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    const initialSource = resolveInitialContactSource(customer, canUseOrganizationContact);
    setSource(initialSource);
    setSelectedMemberId(customer.organizationMemberId ?? null);
    setPickerOpen(
      initialSource === "OrganizationMember" && !customer.organizationMemberId,
    );
    setContactSearch("");
    setHighlight(0);
    setContactPerson(customer.contactPersonName ?? "");
    setDepartment(customer.contactDepartment ?? "");
    setRole(customer.contactRole ?? "");
    setPhone(customer.contactPhone ?? "");
    setEmail(customer.contactEmail ?? "");
    setPreferredMethod(customer.preferredContactMethod ?? "");
    setDeliveryInstructions(customer.deliveryInstructions ?? "");
    setBillingNotes(customer.billingContactNotes ?? "");
    setInternalNotes(customer.internalNotes ?? "");
    setFormError(null);
  }, [open, customer, canUseOrganizationContact]);

  const buyerOrgName = customer.organizationDisplayName?.trim() || t("customers.business.relationshipContact.organization");

  const contactsQuery = useQuery({
    queryKey: [
      "business-customers",
      "organization-contacts",
      workspace.organizationId,
      customer.connectionId,
    ],
    enabled: open && source === "OrganizationMember" && canUseOrganizationContact,
    queryFn: ({ signal }) =>
      listBusinessCustomerOrganizationContacts(workspace, customer.connectionId, undefined, signal),
  });

  const filteredContacts = useMemo(() => {
    const items = contactsQuery.data ?? [];
    return items.filter((c) => matchesStaffSearch(c, contactSearch.trim()));
  }, [contactsQuery.data, contactSearch]);

  const directoryCountLabel =
    contactsQuery.isSuccess
      ? t("customers.business.relationshipContact.directoryCount")
          .replace("{count}", String(contactsQuery.data?.length ?? 0))
          .replace("{name}", buyerOrgName)
      : null;

  const selectedContact = useMemo(() => {
    if (!selectedMemberId) {
      return null;
    }
    const fromList = (contactsQuery.data ?? []).find(
      (c) => c.organizationMemberId === selectedMemberId,
    );
    if (fromList) {
      return fromList;
    }
    // Snapshot fallback while list loads / member removed
    if (customer.organizationMemberId === selectedMemberId && customer.contactPersonName) {
      const roleTitle = customer.contactRole ?? "";
      return {
        organizationMemberId: selectedMemberId,
        userId: selectedMemberId,
        displayName: customer.contactPersonName,
        roleTitle,
        isOwner: /\bowner\b/i.test(roleTitle),
        department: customer.contactDepartment,
        phone: customer.contactPhone,
        email: customer.contactEmail,
        employeeCode: null,
      } satisfies BuyerOrganizationBusinessContact;
    }
    return null;
  }, [contactsQuery.data, selectedMemberId, customer]);

  const memberUnavailable =
    source === "OrganizationMember"
    && Boolean(selectedMemberId)
    && !pickerOpen
    && customer.organizationMemberAvailable === false
    && !(contactsQuery.data ?? []).some((c) => c.organizationMemberId === selectedMemberId)
    && !contactsQuery.isLoading;

  useEffect(() => {
    if (!pickerOpen) {
      return;
    }
    const frame = window.requestAnimationFrame(() => searchInputRef.current?.focus());
    return () => window.cancelAnimationFrame(frame);
  }, [pickerOpen]);

  useEffect(() => {
    setHighlight(0);
  }, [contactSearch, filteredContacts.length]);

  const selectContact = (contact: BuyerOrganizationBusinessContact) => {
    setSelectedMemberId(contact.organizationMemberId);
    setContactPerson(contact.displayName);
    setDepartment(contact.department ?? "");
    setRole(contact.roleTitle);
    setPhone(contact.phone ?? "");
    setEmail(contact.email ?? "");
    setPickerOpen(false);
    setContactSearch("");
    setFormError(null);
  };

  const switchToOrganizationStaff = () => {
    if (!canUseOrganizationContact) {
      return;
    }
    setSource("OrganizationMember");
    setFormError(null);
    if (!selectedMemberId) {
      setPickerOpen(true);
    }
  };

  const switchToCustom = () => {
    setSource("Custom");
    setSelectedMemberId(null);
    setPickerOpen(false);
    setContactSearch("");
    setFormError(null);
  };

  const saveMutation = useMutation({
    mutationFn: () =>
      updateBusinessCustomerRelationshipContact(workspace, customer.connectionId, {
        contactSource: source,
        organizationMemberId: source === "OrganizationMember" ? selectedMemberId : null,
        contactPersonName: contactPerson.trim() || null,
        contactDepartment: department.trim() || null,
        contactRole: role.trim() || null,
        contactPhone: phone.trim() || null,
        contactEmail: email.trim() || null,
        preferredContactMethod: preferredMethod.trim() || null,
        deliveryInstructions: deliveryInstructions.trim() || null,
        billingContactNotes: billingNotes.trim() || null,
        internalNotes: internalNotes.trim() || null,
        expectedUpdatedAtUtc: customer.updatedAtUtc,
      }),
    onSuccess: async () => {
      showToast(t("customers.business.relationshipContact.saved"), "success");
      await queryClient.invalidateQueries({
        queryKey: ["business-customers", "detail", workspace.organizationId, customer.connectionId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["business-customers", "list", workspace.organizationId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["business-customers", "organization-contacts", workspace.organizationId, customer.connectionId],
      });
      onClose();
    },
    onError: (error) => {
      setFormError(
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("customers.business.relationshipContact.saveFailed"),
      );
    },
  });

  const canSave =
    !saveMutation.isPending
    && (source === "Custom"
      || (Boolean(selectedMemberId) && !memberUnavailable && !pickerOpen));

  const sellerOwnedFields = (
    <>
      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-preferred-method">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.business.relationshipContact.preferredMethod")}
        </span>
        <Input
          id="b2b-preferred-method"
          data-testid="b2b-preferred-method"
          value={preferredMethod}
          onChange={(event) => setPreferredMethod(event.target.value)}
          placeholder={t("customers.business.relationshipContact.preferredMethodHint")}
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-delivery-instructions">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.business.relationshipContact.deliveryInstructions")}
        </span>
        <textarea
          id="b2b-delivery-instructions"
          data-testid="b2b-delivery-instructions"
          className="customer-form-notes min-h-[3.5rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={deliveryInstructions}
          onChange={(event) => setDeliveryInstructions(event.target.value)}
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-billing-notes">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.business.relationshipContact.billingNotes")}
        </span>
        <textarea
          id="b2b-billing-notes"
          data-testid="b2b-billing-notes"
          className="customer-form-notes min-h-[3.5rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={billingNotes}
          onChange={(event) => setBillingNotes(event.target.value)}
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-internal-notes">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.business.relationshipContact.internalNotes")}
        </span>
        <textarea
          id="b2b-internal-notes"
          data-testid="b2b-internal-notes"
          className="customer-form-notes min-h-[4.25rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={internalNotes}
          onChange={(event) => setInternalNotes(event.target.value)}
        />
      </label>
    </>
  );

  return (
    <FormDrawer
      open={open}
      onOpenChange={(next) => {
        if (!next && !saveMutation.isPending) {
          onClose();
        }
      }}
      title={t("customers.business.relationshipContact.editTitle")}
      description={customer.organizationDisplayName}
      testId="business-relationship-contact-drawer"
      closeLabel={t("customers.business.relationshipContact.cancel")}
      cancelLabel={t("customers.business.relationshipContact.cancel")}
      cancelTestId="business-relationship-contact-cancel"
      saveTestId="business-relationship-contact-save"
      saveLabel={
        saveMutation.isPending
          ? t("customers.business.relationshipContact.saving")
          : t("customers.business.relationshipContact.save")
      }
      saving={saveMutation.isPending}
      saveDisabled={!canSave}
      onSave={() => {
        if (!canSave) return;
        setFormError(null);
        saveMutation.mutate();
      }}
    >
          {canUseOrganizationContact ? (
            <SegmentedControl label={t("customers.business.relationshipContact.contactSource")}>
              <SegmentedOption
                selected={source === "OrganizationMember"}
                onSelect={switchToOrganizationStaff}
              >
                <span data-testid="b2b-contact-source-organization">
                  {t("customers.business.relationshipContact.sourceOrganization")}
                </span>
              </SegmentedOption>
              <SegmentedOption selected={source === "Custom"} onSelect={switchToCustom}>
                <span data-testid="b2b-contact-source-custom">
                  {t("customers.business.relationshipContact.sourceCustom")}
                </span>
              </SegmentedOption>
            </SegmentedControl>
          ) : (
            <p
              className="m-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="b2b-contact-pending-custom-only"
            >
              {t("customers.business.relationshipContact.pendingCustomOnly")}
            </p>
          )}

          {source === "OrganizationMember" ? (
            <div className="flex min-w-0 flex-col gap-3" data-testid="b2b-organization-contact-mode">
              {contactsQuery.isError ? (
                <div
                  className="rounded-[var(--exits-radius-md)] border border-[var(--exits-danger)]/40 bg-[var(--exits-surface-muted)] px-3 py-2"
                  data-testid="b2b-org-contacts-error"
                  role="alert"
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                    {contactsQuery.error instanceof PosApiError
                      ? (contactsQuery.error.problem.detail ?? contactsQuery.error.message)
                      : t("customers.business.relationshipContact.contactsLoadFailed")}
                  </p>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="mt-2"
                    data-testid="b2b-org-contacts-retry"
                    onClick={() => void contactsQuery.refetch()}
                  >
                    {t("customers.business.relationshipContact.retry")}
                  </Button>
                </div>
              ) : null}

              {!contactsQuery.isError && contactsQuery.isSuccess && (contactsQuery.data?.length ?? 0) === 0 ? (
                <div
                  className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-3"
                  data-testid="b2b-org-contacts-empty"
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-foreground">
                    {t("customers.business.relationshipContact.noContactsAvailable").replace(
                      "{name}",
                      buyerOrgName,
                    )}
                  </p>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="mt-2"
                    data-testid="b2b-use-custom-from-empty"
                    onClick={switchToCustom}
                  >
                    {t("customers.business.relationshipContact.useCustomContact")}
                  </Button>
                </div>
              ) : null}

              {!contactsQuery.isError
              && ((contactsQuery.data?.length ?? 0) > 0 || contactsQuery.isLoading || pickerOpen)
              && (pickerOpen || !selectedContact) ? (
                <div className="b2b-staff-combobox" data-testid="b2b-organization-contact-picker">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <span className="text-[length:var(--exits-text-sm)] font-medium">
                      {t("customers.business.relationshipContact.organizationContact").replace(
                        "{name}",
                        buyerOrgName,
                      )}
                    </span>
                    <p
                      className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                      data-testid="b2b-org-contact-directory-help"
                    >
                      {t("customers.business.relationshipContact.directoryHelp").replace(
                        "{name}",
                        buyerOrgName,
                      )}
                    </p>
                    {directoryCountLabel ? (
                      <p
                        className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid="b2b-org-contact-directory-count"
                      >
                        {directoryCountLabel}
                      </p>
                    ) : null}
                  </div>

                  {!pickerOpen ? (
                    <button
                      type="button"
                      className="b2b-staff-combobox__trigger"
                      data-testid="b2b-staff-combobox-trigger"
                      aria-expanded={false}
                      aria-haspopup="listbox"
                      disabled={saveMutation.isPending || contactsQuery.isLoading}
                      onClick={() => setPickerOpen(true)}
                    >
                      <span className="b2b-staff-combobox__trigger-label text-muted">
                        {t("customers.business.relationshipContact.selectStaffPlaceholder")}
                      </span>
                      <ChevronDown className="size-4 shrink-0 text-muted" aria-hidden />
                    </button>
                  ) : (
                    <>
                      <div className="b2b-staff-combobox__search-wrap">
                        <Search
                          className="b2b-staff-combobox__search-icon"
                          aria-hidden
                        />
                        <input
                          id="b2b-staff-combobox-search"
                          ref={searchInputRef}
                          className="b2b-staff-combobox__search"
                          value={contactSearch}
                          onChange={(event) => {
                            setContactSearch(event.target.value);
                            setPickerOpen(true);
                          }}
                          onKeyDown={(event) => {
                            if (event.key === "Escape") {
                              event.preventDefault();
                              setPickerOpen(false);
                              setContactSearch("");
                              return;
                            }
                            if (event.key === "ArrowDown") {
                              event.preventDefault();
                              setHighlight((h) =>
                                filteredContacts.length === 0
                                  ? 0
                                  : Math.min(h + 1, filteredContacts.length - 1),
                              );
                              return;
                            }
                            if (event.key === "ArrowUp") {
                              event.preventDefault();
                              setHighlight((h) => Math.max(h - 1, 0));
                              return;
                            }
                            if (event.key === "Enter" && filteredContacts[highlight]) {
                              event.preventDefault();
                              selectContact(filteredContacts[highlight]!);
                            }
                          }}
                          role="combobox"
                          aria-expanded={true}
                          aria-controls={listboxId}
                          aria-autocomplete="list"
                          aria-activedescendant={
                            filteredContacts[highlight]
                              ? `${listboxId}-opt-${highlight}`
                              : undefined
                          }
                          autoComplete="off"
                          placeholder={t("customers.business.relationshipContact.selectStaffPlaceholder")}
                          data-testid="b2b-staff-combobox-search"
                        />
                        <ChevronDown
                          className="b2b-staff-combobox__search-chevron"
                          aria-hidden
                        />
                      </div>

                      <div className="b2b-staff-combobox__panel" data-testid="b2b-staff-combobox-panel">
                        {contactsQuery.isLoading ? (
                          <p className="b2b-staff-combobox__hint m-0">
                            {t("customers.business.relationshipContact.loadingContacts")}
                          </p>
                        ) : filteredContacts.length === 0 ? (
                          <p className="b2b-staff-combobox__hint m-0">
                            {t("customers.business.relationshipContact.noContacts").replace(
                              "{name}",
                              buyerOrgName,
                            )}
                          </p>
                        ) : (
                          <ul
                            id={listboxId}
                            role="listbox"
                            className="b2b-staff-combobox__list m-0 list-none p-0"
                            data-testid="b2b-org-contact-list"
                          >
                            {filteredContacts.map((contact, index) => (
                              <li key={contact.organizationMemberId} role="presentation">
                                <button
                                  type="button"
                                  id={`${listboxId}-opt-${index}`}
                                  role="option"
                                  aria-selected={highlight === index}
                                  className={cn(
                                    "b2b-staff-combobox__option",
                                    highlight === index && "is-active",
                                  )}
                                  data-testid={`b2b-org-contact-${contact.organizationMemberId}`}
                                  onMouseDown={(event) => event.preventDefault()}
                                  onMouseEnter={() => setHighlight(index)}
                                  onClick={() => selectContact(contact)}
                                >
                                  <span className="b2b-staff-combobox__avatar" aria-hidden>
                                    {initials(contact.displayName)}
                                  </span>
                                  <span className="b2b-staff-combobox__option-text min-w-0">
                                    <span className="flex flex-wrap items-center gap-1.5">
                                      <span className="truncate font-medium text-foreground">
                                        {contact.displayName}
                                      </span>
                                      {contact.isOwner ? (
                                        <StatusChip
                                          tone="info"
                                          className="b2b-staff-combobox__owner-badge"
                                        >
                                          {t("customers.business.relationshipContact.ownerBadge")}
                                        </StatusChip>
                                      ) : null}
                                    </span>
                                    <span className="mt-0.5 block truncate text-[length:var(--exits-text-xs)] text-muted">
                                      {[contact.roleTitle, contact.department]
                                        .filter(Boolean)
                                        .join(" · ")}
                                    </span>
                                  </span>
                                </button>
                              </li>
                            ))}
                          </ul>
                        )}
                      </div>
                    </>
                  )}
                </div>
              ) : null}

              {!pickerOpen && selectedContact ? (
                <div
                  className="b2b-staff-selected"
                  data-testid="b2b-selected-organization-contact"
                >
                  {memberUnavailable ? (
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                      {t("customers.business.relationshipContact.contactUnavailable")}
                    </p>
                  ) : null}
                  <div className="b2b-staff-selected__header">
                    <span className="b2b-staff-combobox__avatar" aria-hidden>
                      {initials(selectedContact.displayName)}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-1.5">
                        <p className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground">
                          {selectedContact.displayName}
                        </p>
                        {selectedContact.isOwner ? (
                          <StatusChip
                            tone="info"
                            className="b2b-staff-combobox__owner-badge"
                            data-testid="b2b-selected-owner-badge"
                          >
                            {t("customers.business.relationshipContact.ownerBadge")}
                          </StatusChip>
                        ) : null}
                      </div>
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                        {[selectedContact.roleTitle, selectedContact.department]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                    </div>
                  </div>
                  <dl className="customer-ownership-dl mt-2">
                    {selectedContact.phone?.trim() ? (
                      <div>
                        <dt>{t("customers.business.relationshipContact.phone")}</dt>
                        <dd data-testid="b2b-selected-phone">{selectedContact.phone}</dd>
                      </div>
                    ) : null}
                    {selectedContact.email?.trim() ? (
                      <div>
                        <dt>{t("customers.business.relationshipContact.email")}</dt>
                        <dd data-testid="b2b-selected-email">{selectedContact.email}</dd>
                      </div>
                    ) : null}
                  </dl>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
                    {t("customers.business.relationshipContact.organizationManagedReadonly").replace(
                      "{name}",
                      buyerOrgName,
                    )}
                  </p>
                  <button
                    type="button"
                    className="b2b-staff-selected__change"
                    data-testid="b2b-change-organization-contact"
                    disabled={saveMutation.isPending}
                    onClick={() => {
                      setPickerOpen(true);
                      setContactSearch("");
                    }}
                  >
                    <RefreshCw className="size-3.5 shrink-0" aria-hidden />
                    {t("customers.business.relationshipContact.changeContact")}
                  </button>
                </div>
              ) : null}

              {selectedContact && !pickerOpen && !memberUnavailable ? sellerOwnedFields : null}
            </div>
          ) : (
            <div className="flex min-w-0 flex-col gap-3" data-testid="b2b-custom-contact-mode">
              <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-contact-person">
                <span className="text-[length:var(--exits-text-sm)] font-medium">
                  {t("customers.business.relationshipContact.contactPerson")}
                </span>
                <Input
                  id="b2b-contact-person"
                  data-testid="b2b-contact-person"
                  value={contactPerson}
                  onChange={(event) => setContactPerson(event.target.value)}
                  autoComplete="name"
                />
              </label>

              <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-contact-department">
                <span className="text-[length:var(--exits-text-sm)] font-medium">
                  {t("customers.business.relationshipContact.department")}
                </span>
                <Input
                  id="b2b-contact-department"
                  data-testid="b2b-contact-department"
                  value={department}
                  onChange={(event) => setDepartment(event.target.value)}
                  autoComplete="organization"
                />
              </label>

              <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-contact-role">
                <span className="text-[length:var(--exits-text-sm)] font-medium">
                  {t("customers.business.relationshipContact.role")}
                </span>
                <Input
                  id="b2b-contact-role"
                  data-testid="b2b-contact-role"
                  value={role}
                  onChange={(event) => setRole(event.target.value)}
                  autoComplete="organization-title"
                />
              </label>

              <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-contact-phone">
                <span className="text-[length:var(--exits-text-sm)] font-medium">
                  {t("customers.business.relationshipContact.phone")}
                </span>
                <Input
                  id="b2b-contact-phone"
                  data-testid="b2b-contact-phone"
                  value={phone}
                  onChange={(event) => setPhone(event.target.value)}
                  inputMode="tel"
                  autoComplete="tel"
                />
              </label>

              <label className="flex min-w-0 flex-col gap-1.5" htmlFor="b2b-contact-email">
                <span className="text-[length:var(--exits-text-sm)] font-medium">
                  {t("customers.business.relationshipContact.email")}
                </span>
                <Input
                  id="b2b-contact-email"
                  data-testid="b2b-contact-email"
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  autoComplete="email"
                />
              </label>

              {sellerOwnedFields}
            </div>
          )}

          {formError ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
              data-testid="business-relationship-contact-error"
            >
              {formError}
            </p>
          ) : null}
    </FormDrawer>
  );
}
