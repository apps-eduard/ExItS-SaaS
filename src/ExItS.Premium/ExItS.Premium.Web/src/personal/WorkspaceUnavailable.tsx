import { useEffect, useState } from "react";
import { listEligibleOrganizations, selectOrganization, type EligibleOrganization, type PlatformSession } from "../auth/platform-client";
import { Button } from "../components/button/Button";
import { friendlyLoadError } from "./home-data";
import styles from "./WorkspaceUnavailable.module.scss";

export function WorkspaceUnavailable({ session, onLogout }: { session: PlatformSession; onLogout: () => void }) {
  const [orgs, setOrgs] = useState<EligibleOrganization[] | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void listEligibleOrganizations().then((next) => {
      if (active) setOrgs(next);
    }).catch((error: unknown) => {
      if (!active) return;
      setOrgs([]);
      setProblem(friendlyLoadError(error));
    });
    return () => {
      active = false;
    };
  }, [session.selectedOrganizationId]);

  const selected = session.selectedOrganizationDisplayName;
  const several = (orgs?.length ?? session.activeOrganizationCount) > 1;

  return (
    <main className={styles.page}>
      <section className={styles.card}>
        <p className={styles.kicker}>Organization</p>
        <h1>Workspace unavailable</h1>
        <p>
          {selected ? `${selected} is signed in.` : "This account is signed in."}
          {" "}ExItS Premium does not include an organization workspace yet.
        </p>
        {several ? <p>This account can use more than one organization. Choosing one updates the session only.</p> : null}
        {problem ? <p role="alert">{problem}</p> : null}
        {orgs && orgs.length > 0 ? (
          <ul className={styles.list}>
            {orgs.map((org) => (
              <li key={org.organizationId}>
                <span>{org.displayName}</span>
                <Button
                  type="button"
                  variant="secondary"
                  appearance="outline"
                  loading={busy === org.organizationId}
                  disabled={busy !== null}
                  onClick={() => {
                    setBusy(org.organizationId);
                    void selectOrganization(org.organizationId).then(() => window.location.reload()).catch((error: unknown) => {
                      setBusy(null);
                      setProblem(friendlyLoadError(error));
                    });
                  }}
                >
                  {session.selectedOrganizationId === org.organizationId ? "Current" : "Use"}
                </Button>
              </li>
            ))}
          </ul>
        ) : null}
        <Button type="button" variant="danger" appearance="outline" onClick={onLogout}>Log out</Button>
      </section>
    </main>
  );
}
