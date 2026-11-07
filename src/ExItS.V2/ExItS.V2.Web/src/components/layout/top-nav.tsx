import { useTranslation } from "react-i18next";
import { ExitsLogo } from "@/components/brand/exits-logo";
import { SearchField } from "@/components/ui/search-field";
import { ProfileMenu } from "./profile-menu";
import { ThemeToggle } from "./theme-toggle";

function TopNav() {
  const { t } = useTranslation();

  return (
    <header className="sticky top-0 z-40 border-b border-border bg-card/95 pt-[env(safe-area-inset-top)] backdrop-blur-sm supports-[backdrop-filter]:bg-card/85">
      <div className="flex h-14 items-center gap-2.5 px-4 md:gap-3 md:px-6">
        <div className="min-w-0 lg:hidden">
          <ExitsLogo compact showWordmark />
        </div>
        <div className="hidden min-w-0 flex-1 md:block md:max-w-md">
          <SearchField
            placeholder={t("designSystem.sampleSearch")}
            aria-label={t("common.search")}
            defaultValue=""
            readOnly
            className="[&_input]:h-9"
          />
        </div>
        <div className="ml-auto flex shrink-0 items-center gap-0.5">
          <ThemeToggle />
          <ProfileMenu />
        </div>
      </div>
    </header>
  );
}

export { TopNav };
