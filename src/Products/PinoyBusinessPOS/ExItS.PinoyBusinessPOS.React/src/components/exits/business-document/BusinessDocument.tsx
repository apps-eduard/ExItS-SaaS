import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type BusinessIdentity = {
  businessName: string;
  logoUrl?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  branchName?: string | null;
  branchAddress?: string | null;
};

export type DocumentHeaderVisibility = {
  showLogo: boolean;
  showBusinessName: boolean;
  showBusinessAddress: boolean;
  showBusinessPhone: boolean;
  showBusinessEmail: boolean;
  showWebsite: boolean;
  showBranchName: boolean;
  showBranchAddress: boolean;
};

export function DocumentHeader({
  identity,
  visibility,
  title,
  referenceNumber,
  dateLabel,
  statusLabel,
}: {
  identity: BusinessIdentity;
  visibility: DocumentHeaderVisibility;
  title: string;
  referenceNumber?: string | null;
  dateLabel?: string | null;
  statusLabel?: string | null;
}) {
  const showLogo = visibility.showLogo && Boolean(identity.logoUrl?.trim());
  const addressLines = visibility.showBusinessAddress
    ? (identity.address?.split("\n").map((l) => l.trim()).filter(Boolean) ?? [])
    : [];

  return (
    <header className="exits-bizdoc__header" data-testid="business-document-header">
      <div className="exits-bizdoc__header-left">
        {showLogo ? (
          <img
            className="exits-bizdoc__logo"
            src={identity.logoUrl!}
            alt=""
            data-testid="business-document-logo"
          />
        ) : null}
        <div className="exits-bizdoc__identity">
          {visibility.showBusinessName ? (
            <p className="exits-bizdoc__business-name" data-testid="business-document-business-name">
              {identity.businessName}
            </p>
          ) : null}
          {visibility.showBranchName && identity.branchName?.trim() ? (
            <p className="exits-bizdoc__branch-name" data-testid="business-document-branch-name">
              {identity.branchName.trim()}
            </p>
          ) : null}
          {addressLines.map((line) => (
            <p key={line} className="exits-bizdoc__identity-line">
              {line}
            </p>
          ))}
          {visibility.showBranchAddress && identity.branchAddress?.trim() ? (
            <p className="exits-bizdoc__identity-line" data-testid="business-document-branch-address">
              {identity.branchAddress.trim()}
            </p>
          ) : null}
          {visibility.showBusinessPhone && identity.phone?.trim() ? (
            <p className="exits-bizdoc__identity-line" data-testid="business-document-phone">
              {identity.phone.trim()}
            </p>
          ) : null}
          {visibility.showBusinessEmail && identity.email?.trim() ? (
            <p className="exits-bizdoc__identity-line" data-testid="business-document-email">
              {identity.email.trim()}
            </p>
          ) : null}
          {visibility.showWebsite && identity.website?.trim() ? (
            <p className="exits-bizdoc__identity-line" data-testid="business-document-website">
              {identity.website.trim()}
            </p>
          ) : null}
        </div>
      </div>
      <div className="exits-bizdoc__header-right">
        <h1 className="exits-bizdoc__doc-title" data-testid="business-document-title">
          {title}
        </h1>
        {referenceNumber?.trim() ? (
          <p className="exits-bizdoc__doc-ref" data-testid="business-document-reference">
            {referenceNumber.trim()}
          </p>
        ) : null}
        {dateLabel?.trim() ? (
          <p className="exits-bizdoc__doc-date" data-testid="business-document-date">
            {dateLabel.trim()}
          </p>
        ) : null}
        {statusLabel?.trim() ? (
          <p className="exits-bizdoc__doc-status" data-testid="business-document-status">
            {statusLabel.trim()}
          </p>
        ) : null}
      </div>
    </header>
  );
}

export function DocumentPartyBlock({
  label,
  name,
  publicId,
  contactPerson,
  phone,
  email,
  address,
}: {
  label: string;
  name?: string | null;
  publicId?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
}) {
  if (!name?.trim() && !publicId?.trim() && !address?.trim()) {
    return null;
  }
  return (
    <section className="exits-bizdoc__party" data-testid="business-document-party">
      <h2 className="exits-bizdoc__party-label">{label}</h2>
      {name?.trim() ? (
        <p className="exits-bizdoc__party-name" data-testid="business-document-party-name">
          {name.trim()}
        </p>
      ) : null}
      {publicId?.trim() ? (
        <p className="exits-bizdoc__party-line">{publicId.trim()}</p>
      ) : null}
      {contactPerson?.trim() ? (
        <p className="exits-bizdoc__party-line">{contactPerson.trim()}</p>
      ) : null}
      {phone?.trim() ? <p className="exits-bizdoc__party-line">{phone.trim()}</p> : null}
      {email?.trim() ? <p className="exits-bizdoc__party-line">{email.trim()}</p> : null}
      {address?.trim()
        ? address
            .split("\n")
            .map((line) => line.trim())
            .filter(Boolean)
            .map((line) => (
              <p key={line} className="exits-bizdoc__party-line" data-testid="business-document-party-address">
                {line}
              </p>
            ))
        : null}
    </section>
  );
}

export function DocumentMetadata({
  items,
}: {
  items: Array<{ key: string; label: string; value: string | null | undefined }>;
}) {
  const visible = items.filter((item) => Boolean(item.value?.trim()));
  if (visible.length === 0) {
    return null;
  }
  return (
    <dl className="exits-bizdoc__meta" data-testid="business-document-metadata">
      {visible.map((item) => (
        <div key={item.key} className="exits-bizdoc__meta-row">
          <dt>{item.label}</dt>
          <dd data-testid={`business-document-meta-${item.key}`}>{item.value!.trim()}</dd>
        </div>
      ))}
    </dl>
  );
}

export type DocumentColumn = {
  key: string;
  header: string;
  align?: "left" | "right" | "center";
  width?: string;
};

export function DocumentLineTable({
  columns,
  rows,
  emptyLabel = "No lines",
}: {
  columns: DocumentColumn[];
  rows: Array<Record<string, ReactNode>>;
  emptyLabel?: string;
}) {
  return (
    <table className="exits-bizdoc__table" data-testid="business-document-line-table">
      <thead>
        <tr>
          {columns.map((col) => (
            <th
              key={col.key}
              className={cn(
                col.align === "right" && "is-right",
                col.align === "center" && "is-center",
              )}
              style={col.width ? { width: col.width } : undefined}
            >
              {col.header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.length === 0 ? (
          <tr>
            <td colSpan={columns.length}>{emptyLabel}</td>
          </tr>
        ) : (
          rows.map((row, index) => (
            <tr key={index} className="exits-bizdoc__table-row">
              {columns.map((col) => (
                <td
                  key={col.key}
                  className={cn(
                    col.align === "right" && "is-right",
                    col.align === "center" && "is-center",
                  )}
                >
                  {row[col.key] ?? ""}
                </td>
              ))}
            </tr>
          ))
        )}
      </tbody>
    </table>
  );
}

export function DocumentTotals({
  rows,
}: {
  rows: Array<{ key: string; label: string; value: ReactNode; emphasize?: boolean }>;
}) {
  const visible = rows.filter((row) => row.value != null && row.value !== "");
  if (visible.length === 0) {
    return null;
  }
  return (
    <div className="exits-bizdoc__totals" data-testid="business-document-totals">
      {visible.map((row) => (
        <div
          key={row.key}
          className={cn("exits-bizdoc__totals-row", row.emphasize && "is-emphasize")}
          data-testid={`business-document-total-${row.key}`}
        >
          <span>{row.label}</span>
          <span>{row.value}</span>
        </div>
      ))}
    </div>
  );
}

export function DocumentNotes({ title, text }: { title?: string; text?: string | null }) {
  if (!text?.trim()) {
    return null;
  }
  return (
    <section className="exits-bizdoc__notes" data-testid="business-document-notes">
      {title ? <h2 className="exits-bizdoc__section-title">{title}</h2> : null}
      <p className="exits-bizdoc__notes-body">{text.trim()}</p>
    </section>
  );
}

export function DocumentSignatures({
  slots,
}: {
  slots: Array<{ key: string; label: string; name?: string | null }>;
}) {
  if (slots.length === 0) {
    return null;
  }
  return (
    <div className="exits-bizdoc__signatures" data-testid="business-document-signatures">
      {slots.map((slot) => (
        <div key={slot.key} className="exits-bizdoc__signature">
          <div className="exits-bizdoc__signature-line" />
          <p className="exits-bizdoc__signature-label">{slot.label}</p>
          {slot.name?.trim() ? (
            <p className="exits-bizdoc__signature-name">{slot.name.trim()}</p>
          ) : null}
        </div>
      ))}
    </div>
  );
}

export function DocumentFooter({
  customText,
  showCustomFooter,
  contactLine,
  showBusinessContact,
  disclaimer,
  extendedDisclaimer,
  showPageNumber,
}: {
  customText?: string | null;
  showCustomFooter: boolean;
  contactLine?: string | null;
  showBusinessContact: boolean;
  /** Primary legal line (e.g. BIR-safe sales disclaimer). */
  disclaimer?: string | null;
  /** Optional longer record-purpose disclaimer under the primary line. */
  extendedDisclaimer?: string | null;
  showPageNumber: boolean;
}) {
  return (
    <footer className="exits-bizdoc__footer" data-testid="business-document-footer">
      {showCustomFooter && customText?.trim() ? (
        <p className="exits-bizdoc__footer-thanks" data-testid="business-document-footer-thanks">
          {customText.trim()}
        </p>
      ) : null}
      {showBusinessContact && contactLine?.trim() ? (
        <p className="exits-bizdoc__footer-contact" data-testid="business-document-footer-contact">
          {contactLine.trim()}
        </p>
      ) : null}
      {disclaimer?.trim() ? (
        <p className="exits-bizdoc__footer-disclaimer" data-testid="business-document-disclaimer">
          {disclaimer.trim()}
        </p>
      ) : null}
      {extendedDisclaimer?.trim() ? (
        <p
          className="exits-bizdoc__footer-extended"
          data-testid="business-document-extended-disclaimer"
        >
          {extendedDisclaimer.trim()}
        </p>
      ) : null}
      {showPageNumber ? (
        <p className="exits-bizdoc__page-number" data-testid="business-document-page-number">
          <span className="exits-bizdoc__page-number-label">Page </span>
          <span className="exits-bizdoc__page-number-current" />
        </p>
      ) : null}
    </footer>
  );
}

/**
 * Canonical printable business document shell.
 * Domain documents supply data only; this owns A4/print presentation.
 */
export function BusinessDocument({
  children,
  className,
  testId = "business-document",
  preview = false,
}: {
  children: ReactNode;
  className?: string;
  testId?: string;
  /** When true, shown on screen as preview; print CSS still targets the same root. */
  preview?: boolean;
}) {
  return (
    <article
      className={cn("exits-bizdoc", preview && "exits-bizdoc--preview", className)}
      data-testid={testId}
      data-business-document-root
    >
      {children}
    </article>
  );
}
