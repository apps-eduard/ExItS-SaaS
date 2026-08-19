import { Component, type ReactNode } from "react";
import { ErrorState } from "@/components/exits/ErrorState";
import { Button } from "@/components/ui/button";
import { catalogs, type MessageKey } from "@/i18n/messages";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import {
  DEFAULT_UI_PREFERENCES,
  readUiPreferences,
  type LocalePreference,
} from "@/lib/preferences/ui-preferences";

type Props = { children: ReactNode };
type State = { record: DiagnosticRecord | null };

function t(locale: LocalePreference, key: MessageKey): string {
  return catalogs[locale][key];
}

export class AppErrorBoundary extends Component<Props, State> {
  override state: State = { record: null };

  static getDerivedStateFromError(error: Error): State {
    const preferences =
      typeof window === "undefined" ? DEFAULT_UI_PREFERENCES : readUiPreferences();
    return {
      record: normalizeDiagnosticError(error, {
        locale: preferences.locale,
        theme: preferences.theme,
      }),
    };
  }

  override componentDidCatch(): void {
    // Intentionally no stack logging. Diagnostics are allowlisted.
  }

  override render(): ReactNode {
    if (!this.state.record) {
      return this.props.children;
    }
    const locale =
      typeof window === "undefined" ? DEFAULT_UI_PREFERENCES.locale : readUiPreferences().locale;
    return (
      <div className="mx-auto max-w-xl px-[var(--exits-page-padding)] py-10">
        <ErrorState
          title={t(locale, "error.title")}
          body={t(locale, "error.body")}
          record={this.state.record}
          action={
            <Button type="button" onClick={() => this.setState({ record: null })}>
              {t(locale, "error.reset")}
            </Button>
          }
        />
      </div>
    );
  }
}
