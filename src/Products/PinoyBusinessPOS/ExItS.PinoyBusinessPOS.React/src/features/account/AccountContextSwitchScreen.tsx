import { AppBootLoader } from "@/components/exits/loading/AppBootLoader";

/** Full-viewport animated loader for account profile / workspace context switches. */
export function AccountContextSwitchScreen({ label }: { label: string }) {
  return <AppBootLoader label={label} defer={false} testId="account-context-switch" />;
}
