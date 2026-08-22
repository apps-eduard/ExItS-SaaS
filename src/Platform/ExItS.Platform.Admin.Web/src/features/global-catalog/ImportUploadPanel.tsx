import { useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Download, Upload } from "lucide-react";

import {
  GLOBAL_CATALOG_IMPORT_MAX_FILE_BYTES,
  GLOBAL_CATALOG_IMPORT_TEMPLATE_FILENAME,
} from "@/api/global-catalog/global-catalog-types";
import { downloadGlobalCatalogImportTemplate } from "@/api/global-catalog/global-catalog-client";
import { PlatformApiError } from "@/api/platform-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { globalCatalogControlClass } from "@/features/global-catalog/global-catalog-presentation";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";

const ACCEPTED_EXTENSIONS = [".csv", ".xlsx"];

function isAcceptedImportFile(file: File): boolean {
  const lowerName = file.name.toLowerCase();
  return ACCEPTED_EXTENSIONS.some((ext) => lowerName.endsWith(ext));
}

function triggerBrowserDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

export function ImportUploadPanel({ enabled }: { enabled: boolean }) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { uploadImport } = useGlobalCatalogMutations();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [idempotencyKey, setIdempotencyKey] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);
  const [downloadBusy, setDownloadBusy] = useState(false);

  function onFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    setClientError(null);
    if (file && !isAcceptedImportFile(file)) {
      setClientError(t("globalCatalog.imports.unsupportedFile"));
      setSelectedFile(null);
      return;
    }
    setSelectedFile(file);
  }

  async function onDownloadTemplate() {
    setClientError(null);
    setDownloadBusy(true);
    try {
      const { blob, fileName } = await downloadGlobalCatalogImportTemplate(env.platformApiBaseUrl);
      triggerBrowserDownload(blob, fileName || GLOBAL_CATALOG_IMPORT_TEMPLATE_FILENAME);
    } catch (error) {
      setClientError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : t("globalCatalog.imports.templateDownloadError"),
      );
    } finally {
      setDownloadBusy(false);
    }
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setClientError(null);
    if (!selectedFile) {
      setClientError(t("globalCatalog.imports.fileRequired"));
      return;
    }
    if (selectedFile.size <= 0) {
      setClientError(t("globalCatalog.imports.emptyFile"));
      return;
    }
    if (selectedFile.size > GLOBAL_CATALOG_IMPORT_MAX_FILE_BYTES) {
      setClientError(t("globalCatalog.imports.fileTooLarge"));
      return;
    }
    if (!isAcceptedImportFile(selectedFile)) {
      setClientError(t("globalCatalog.imports.unsupportedFile"));
      return;
    }

    try {
      const job = await uploadImport.mutateAsync({
        file: selectedFile,
        idempotencyKey: idempotencyKey.trim() || undefined,
      });
      navigate(`/admin/global-catalog/imports/${job.id}`);
    } catch (error) {
      setClientError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : t("globalCatalog.imports.uploadError"),
      );
    }
  }

  return (
    <section className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
      <div>
        <h2 className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("globalCatalog.imports.uploadTitle")}
        </h2>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("globalCatalog.imports.uploadDescription")}
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={!enabled || downloadBusy}
          onClick={() => void onDownloadTemplate()}
        >
          <Download aria-hidden="true" className="mr-1.5 size-4" />
          {downloadBusy
            ? t("globalCatalog.imports.downloadingTemplate")
            : t("globalCatalog.imports.downloadTemplate")}
        </Button>
      </div>

      <form className="grid gap-3" onSubmit={(event) => void onSubmit(event)}>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="gc-import-file"
        >
          {t("globalCatalog.imports.fileLabel")}
          <input
            ref={fileInputRef}
            id="gc-import-file"
            type="file"
            accept=".csv,.xlsx"
            className={globalCatalogControlClass}
            disabled={!enabled || uploadImport.isPending}
            onChange={onFileChange}
          />
        </label>
        {selectedFile ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{selectedFile.name}</p>
        ) : null}
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="gc-import-idempotency"
        >
          {t("globalCatalog.imports.idempotencyKey")}
          <Input
            id="gc-import-idempotency"
            value={idempotencyKey}
            onChange={(event) => setIdempotencyKey(event.target.value)}
            placeholder={t("globalCatalog.imports.idempotencyKeyPlaceholder")}
            autoComplete="off"
            disabled={!enabled || uploadImport.isPending}
          />
        </label>
        {clientError ? (
          <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {clientError}
          </p>
        ) : null}
        <div>
          <Button
            type="submit"
            size="sm"
            disabled={!enabled || !selectedFile || uploadImport.isPending}
          >
            <Upload aria-hidden="true" className="mr-1.5 size-4" />
            {uploadImport.isPending
              ? t("globalCatalog.imports.uploading")
              : t("globalCatalog.imports.upload")}
          </Button>
        </div>
      </form>
    </section>
  );
}
