import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";

function useUnavailableNotice() {
  const { t } = useTranslation();
  return () => {
    toast.message(t("home.comingSoon"), {
      description: t("homeData.quickActionUnavailable"),
    });
  };
}

function UnavailableButton({
  className,
  children,
  title,
}: {
  className?: string;
  children: React.ReactNode;
  title?: string;
}) {
  const { t } = useTranslation();
  const notify = useUnavailableNotice();
  return (
    <button
      type="button"
      className={cn(className)}
      title={title ?? t("homeData.quickActionUnavailable")}
      onClick={notify}
    >
      {children}
    </button>
  );
}

export { UnavailableButton, useUnavailableNotice };
