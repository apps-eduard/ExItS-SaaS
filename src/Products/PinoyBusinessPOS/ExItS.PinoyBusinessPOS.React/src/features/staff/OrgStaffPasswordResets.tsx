import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  decideOrganizationStaffPasswordReset,
  listOrganizationStaffPasswordResets,
} from "@/api/platform/staff-password-reset-client";
import { Button } from "@/components/ui/button";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";

export function OrgStaffPasswordResets({ organizationId }: { organizationId: string }) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["org-staff-password-resets", organizationId],
    queryFn: ({ signal }) => listOrganizationStaffPasswordResets(organizationId, signal),
    meta: { suppressGlobalError: true, operation: "list staff password resets" },
  });
  const decide = useMutation({
    mutationFn: (input: { requestId: string; approve: boolean }) =>
      decideOrganizationStaffPasswordReset(organizationId, input.requestId, input.approve),
    onSuccess: async (result) => {
      if (result.ok) {
        await queryClient.invalidateQueries({
          queryKey: ["org-staff-password-resets", organizationId],
        });
      }
    },
  });

  const requests = (query.data ?? []).filter((item) => item.status === "Pending");
  if (query.isError || (query.isSuccess && requests.length === 0)) {
    return null;
  }
  if (query.isLoading || requests.length === 0) {
    return null;
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel flex flex-col gap-2"
      data-testid="org-staff-password-resets"
    >
      <h2 className="catalog-form-section__title">{t("staffManage.passwordResetsTitle")}</h2>
      {requests.map((request) => (
        <article
          key={request.id}
          className="exits-list__card flex flex-col gap-2"
          data-testid={`org-staff-password-reset-${request.id}`}
        >
          <p className="m-0 font-semibold">{request.staffDisplayName}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{request.staffLogin}</p>
          <div className="flex gap-2">
            <Button
              type="button"
              className="flex-1"
              disabled={!online || decide.isPending}
              data-testid={`org-staff-password-reset-approve-${request.id}`}
              onClick={() => decide.mutate({ requestId: request.id, approve: true })}
            >
              {t("staffManage.passwordResetApprove")}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="flex-1"
              disabled={!online || decide.isPending}
              data-testid={`org-staff-password-reset-deny-${request.id}`}
              onClick={() => decide.mutate({ requestId: request.id, approve: false })}
            >
              {t("staffManage.passwordResetDeny")}
            </Button>
          </div>
        </article>
      ))}
    </section>
  );
}
