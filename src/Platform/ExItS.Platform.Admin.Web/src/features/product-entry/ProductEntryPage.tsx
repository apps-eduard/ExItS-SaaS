import { useState } from "react";
import { issueProductEntryToken } from "@/api/auth/auth-client";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import { decideProductEntry } from "@/lib/auth/cutover-routing";
import { env } from "@/lib/env";

export function ProductEntryPage() {
  const { t } = usePreferences();
  const { session } = useSession();
  const [productCode, setProductCode] = useState("pinoy-business-pos");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [issued, setIssued] = useState<{ accessToken: string; expiresAtUtc: string; productCode: string | null } | null>(
    null,
  );

  const organizationId = session?.selectedOrganizationId ?? null;

  async function onIssue() {
    setError(null);
    setIssued(null);
    if (!organizationId) {
      setError(t("productEntry.noOrganization"));
      return;
    }
    if (productCode.trim().length === 0) {
      setError(t("productEntry.codeRequired"));
      return;
    }
    setBusy(true);
    try {
      const result = await issueProductEntryToken(env.platformApiBaseUrl, organizationId, productCode.trim());
      const decision = decideProductEntry(result);
      if (decision.status === "denied") {
        setError(
          decision.reasonCode
            ? t("productEntry.deniedWithReason").replace("{reason}", decision.reasonCode)
            : t("productEntry.denied"),
        );
        return;
      }
      setIssued({
        accessToken: decision.accessToken,
        expiresAtUtc: decision.expiresAtUtc,
        productCode: decision.productCode,
      });
    } catch (reason) {
      setError(
        reason instanceof PlatformApiError && reason.problem.detail
          ? reason.problem.detail
          : t("productEntry.denied"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function copyToken() {
    if (!issued) {
      return;
    }
    try {
      await navigator.clipboard.writeText(issued.accessToken);
    } catch {
      // The token stays visible for manual copy.
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("productEntry.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("productEntry.hint")}</p>
      <dl className="mt-4 grid gap-2 text-[length:var(--exits-text-sm)]">
        <div>
          <dt className="text-muted">{t("productEntry.sessionUser")}</dt>
          <dd>
            {session?.displayName} ({session?.username})
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("productEntry.organization")}</dt>
          <dd>{session?.selectedOrganizationDisplayName || organizationId || t("productEntry.noOrganization")}</dd>
        </div>
      </dl>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {organizationId ? (
        <div className="mt-4 grid gap-3">
          <div className="grid gap-1.5">
            <Label htmlFor="product-code">{t("productEntry.code")}</Label>
            <Input id="product-code" value={productCode} autoComplete="off" onChange={(event) => setProductCode(event.target.value)} />
          </div>
          <Button type="button" disabled={busy} onClick={() => void onIssue()}>
            {busy ? t("productEntry.issuing") : t("productEntry.issue")}
          </Button>
        </div>
      ) : null}
      {issued ? (
        <div className="mt-4 grid gap-2">
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("productEntry.once")}</p>
          <Label htmlFor="access-token">{t("productEntry.token")}</Label>
          <textarea id="access-token" className="min-h-24 rounded-md border p-2" readOnly value={issued.accessToken} />
          <Button type="button" variant="secondary" onClick={() => void copyToken()}>
            {t("productEntry.copy")}
          </Button>
          <p className="text-[length:var(--exits-text-sm)]">
            {t("productEntry.expires")}: {issued.expiresAtUtc || "—"}
          </p>
        </div>
      ) : null}
    </Card>
  );
}
