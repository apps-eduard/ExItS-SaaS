import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useCreateContactMutation } from "@/features/personal/people-queries";
import { useI18n } from "@/i18n/I18nProvider";

export function AddLocalPersonPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const createMutation = useCreateContactMutation();
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    const name = displayName.trim();
    if (!name) {
      setError(t("people.localAdd.nameRequired"));
      return;
    }

    try {
      const contact = await createMutation.mutateAsync({
        displayName: name,
        phone: phone.trim() || null,
        email: email.trim() || null,
      });
      void navigate(`/personal/people/${contact.id}`, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  return (
    <section className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <PageHeader title={t("people.localAdd.title")} subtitle={t("people.localAdd.subtitle")} />

      <Card>
        <form className="flex flex-col gap-4" onSubmit={(event) => void onSubmit(event)}>
          <label className="flex flex-col gap-1">
            <span className="font-semibold">{t("people.localAdd.name")}</span>
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              required
              autoComplete="name"
              className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="font-semibold">{t("people.localAdd.phone")}</span>
            <input
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              inputMode="tel"
              autoComplete="tel"
              className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="font-semibold">{t("people.localAdd.email")}</span>
            <input
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              inputMode="email"
              autoComplete="email"
              className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            />
          </label>

          {error ? (
            <p className="m-0 text-destructive" role="alert">
              {error}
            </p>
          ) : null}

          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="secondary" asChild>
              <Link to="/personal/people">{t("people.add.cancel")}</Link>
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? t("loading.label") : t("people.add.confirm")}
            </Button>
          </div>
        </form>
      </Card>
    </section>
  );
}
