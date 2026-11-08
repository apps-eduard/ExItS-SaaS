import type { ReactNode } from "react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";

/**
 * Visual stand-in for V1 Personal routes that are not yet wired in V2.
 * Preserves V1 link styling while documenting the functional gap.
 */
export function DeferredNav({
  className,
  children,
  testId,
  title,
  "data-shell": dataShell,
  "data-recipe": dataRecipe,
}: {
  className?: string;
  children: ReactNode;
  testId?: string;
  title?: string;
  "data-shell"?: string;
  "data-recipe"?: string;
}) {
  const { t } = useTranslation();
  return (
    <button
      type="button"
      className={cn(className)}
      data-testid={testId}
      data-shell={dataShell}
      data-recipe={dataRecipe}
      title={title ?? t("homeData.quickActionUnavailable")}
      onClick={() =>
        toast.message(t("home.comingSoon"), {
          description: t("homeData.quickActionUnavailable"),
        })
      }
    >
      {children}
    </button>
  );
}
