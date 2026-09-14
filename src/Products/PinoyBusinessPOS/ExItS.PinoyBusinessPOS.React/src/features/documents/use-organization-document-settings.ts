import { useCallback, useEffect, useState } from "react";
import {
  readOrganizationDocumentSettings,
  writeOrganizationDocumentSettings,
  type OrganizationDocumentSettings,
} from "@/features/documents/document-settings";

const CHANGE_EVENT = "exits-document-settings-changed";

export function useOrganizationDocumentSettings(organizationId: string | null | undefined) {
  const [settings, setSettings] = useState<OrganizationDocumentSettings>(() =>
    readOrganizationDocumentSettings(organizationId ?? ""),
  );

  useEffect(() => {
    setSettings(readOrganizationDocumentSettings(organizationId ?? ""));
  }, [organizationId]);

  useEffect(() => {
    function onChange(event: Event) {
      const detail = (event as CustomEvent<{ organizationId?: string }>).detail;
      if (detail?.organizationId && detail.organizationId !== organizationId) {
        return;
      }
      setSettings(readOrganizationDocumentSettings(organizationId ?? ""));
    }
    window.addEventListener(CHANGE_EVENT, onChange);
    return () => window.removeEventListener(CHANGE_EVENT, onChange);
  }, [organizationId]);

  const save = useCallback(
    (next: OrganizationDocumentSettings) => {
      if (!organizationId) {
        return next;
      }
      const written = writeOrganizationDocumentSettings(organizationId, next);
      setSettings(written);
      window.dispatchEvent(
        new CustomEvent(CHANGE_EVENT, { detail: { organizationId } }),
      );
      return written;
    },
    [organizationId],
  );

  return { settings, save };
}
