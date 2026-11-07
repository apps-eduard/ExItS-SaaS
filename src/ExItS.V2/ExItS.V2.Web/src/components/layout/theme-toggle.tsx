import { Moon, Sun } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useTheme } from "@/theme/theme-provider";

function ThemeToggle() {
  const { t } = useTranslation();
  const { appearance, toggleAppearance } = useTheme();
  const isDark = appearance === "dark";
  const label = isDark
    ? t("appearance.switchToLight")
    : t("appearance.switchToDark");

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="size-9 shrink-0 text-muted-foreground hover:text-foreground"
          aria-label={label}
          aria-pressed={isDark}
          onClick={toggleAppearance}
        >
          {isDark ? (
            <Sun className="size-4" aria-hidden />
          ) : (
            <Moon className="size-4" aria-hidden />
          )}
        </Button>
      </TooltipTrigger>
      <TooltipContent side="bottom">{label}</TooltipContent>
    </Tooltip>
  );
}

export { ThemeToggle };
