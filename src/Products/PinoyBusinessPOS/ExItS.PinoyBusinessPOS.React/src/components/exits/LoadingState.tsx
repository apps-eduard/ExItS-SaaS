import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";

/**
 * Page-level loading affordance. Prefer PageSkeleton / AppBootLoader /
 * WorkspaceTransitionOverlay / ActionButtonLoading by lifecycle intent.
 * Kept as a thin alias so existing page imports upgrade from bare text.
 */
export function LoadingState({ label }: { label: string }) {
  return <PageSkeleton label={label} variant="list" />;
}
