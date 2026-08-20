import { useProductAccess } from "@/access/ProductAccessProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function OrganizationSelectPage() {
  const { t } = useI18n();
  const { organizations, selectOrganization, switching } = useProductAccess();
  const { signOut } = useSession();

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 pt-6">
      <PageHeader title={t("access.selectTitle")} description={t("access.selectDescription")} />
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {organizations.map((organization) => (
          <li key={organization.organizationId}>
            <Card>
              <button
                type="button"
                className="flex min-h-[var(--exits-touch-target-min)] w-full flex-col items-start gap-1 text-left"
                disabled={switching}
                onClick={() => void selectOrganization(organization.organizationId)}
              >
                <span className="font-semibold">{organization.displayName}</span>
                {organization.slug ? (
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {organization.slug}
                  </span>
                ) : null}
              </button>
            </Card>
          </li>
        ))}
      </ul>
      <Button type="button" variant="ghost" onClick={() => void signOut()}>
        {t("auth.signOut")}
      </Button>
    </section>
  );
}
