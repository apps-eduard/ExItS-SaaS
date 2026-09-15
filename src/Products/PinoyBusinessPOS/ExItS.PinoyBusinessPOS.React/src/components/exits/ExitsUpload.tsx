import {
  useId,
  useRef,
  useState,
  type ChangeEvent,
  type DragEvent,
  type KeyboardEvent,
} from "react";
import { CloudUpload, Trash2, Upload, X } from "lucide-react";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export type ExitsUploadVariant = "dropzone" | "tile" | "button";

/** Local or remote preview item — pages own persistence / upload APIs. */
export type ExitsUploadItem = {
  id: string;
  name: string;
  previewUrl?: string;
};

export type ExitsUploadProps = {
  /**
   * - dropzone: large cover / primary image area
   * - tile: compact gallery slot
   * - button: toolbar / form compact trigger (+ optional multi file list)
   */
  variant?: ExitsUploadVariant;
  /** Dropzone hint (default: Drop or select a cover image). */
  label?: string;
  /** Visible Upload action text (default: Upload). */
  uploadLabel?: string;
  accept?: string;
  /** Allow picking more than one file (especially for button compact). */
  multiple?: boolean;
  disabled?: boolean;
  /** Single filled preview for dropzone / tile (omit when empty). */
  value?: ExitsUploadItem | null;
  /** Multi file list for button compact (preferred over `value` when multiple). */
  values?: readonly ExitsUploadItem[];
  onSelectFiles?: (files: File[]) => void;
  /** Clear single dropzone/tile value. */
  onClear?: () => void;
  /** Remove one item from a multi list (button compact). */
  onRemove?: (id: string) => void;
  clearLabel?: string;
  removeLabel?: string;
  testId?: string;
  className?: string;
};

/**
 * Canonical ExItS file / image upload control.
 * Presentation only — no network. Pages wire `onSelectFiles` to their upload API.
 */
export function ExitsUpload({
  variant = "dropzone",
  label = "Drop or select a cover image",
  uploadLabel = "Upload",
  accept = "image/jpeg,image/png,image/webp",
  multiple = false,
  disabled = false,
  value = null,
  values,
  onSelectFiles,
  onClear,
  onRemove,
  clearLabel = "Remove image",
  removeLabel = "Remove file",
  testId = "exits-upload",
  className,
}: ExitsUploadProps) {
  const inputId = useId();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [dragging, setDragging] = useState(false);

  function openPicker() {
    if (disabled) {
      return;
    }
    inputRef.current?.click();
  }

  function emitFiles(list: FileList | File[] | null) {
    if (!list || disabled) {
      return;
    }
    const files = Array.from(list);
    if (files.length === 0) {
      return;
    }
    onSelectFiles?.(multiple ? files : files.slice(0, 1));
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  }

  function onInputChange(event: ChangeEvent<HTMLInputElement>) {
    emitFiles(event.target.files);
  }

  function onDragOver(event: DragEvent) {
    event.preventDefault();
    if (disabled) {
      return;
    }
    setDragging(true);
  }

  function onDragLeave(event: DragEvent) {
    event.preventDefault();
    setDragging(false);
  }

  function onDrop(event: DragEvent) {
    event.preventDefault();
    setDragging(false);
    if (disabled) {
      return;
    }
    emitFiles(event.dataTransfer.files);
  }

  function onZoneKeyDown(event: KeyboardEvent) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      openPicker();
    }
  }

  const fileInput = (
    <input
      ref={inputRef}
      id={inputId}
      type="file"
      className="sr-only"
      accept={accept}
      multiple={multiple}
      disabled={disabled}
      data-testid={`${testId}-input`}
      onChange={onInputChange}
    />
  );

  if (variant === "button") {
    const items =
      values !== undefined
        ? values
        : value
          ? [value]
          : [];

    return (
      <div
        className={cn("exits-upload exits-upload--button", className)}
        data-testid={testId}
        data-variant="button"
        data-multiple={multiple ? "true" : undefined}
      >
        {fileInput}
        <Button
          type="button"
          intent="neutral"
          appearance="outline"
          shape="soft"
          disabled={disabled}
          aria-label={uploadLabel}
          data-testid={`${testId}-trigger`}
          onClick={openPicker}
        >
          <Upload className={`size-4 ${buttonIconMotion.open}`} aria-hidden />
          {uploadLabel}
        </Button>
        {items.length > 0 ? (
          <ul className="exits-upload__file-list" data-testid={`${testId}-files`}>
            {items.map((item) => (
              <li key={item.id} className="exits-upload__file-chip" data-testid={`${testId}-file-${item.id}`}>
                <span className="exits-upload__file-name" title={item.name}>
                  {item.name}
                </span>
                {onRemove ? (
                  <button
                    type="button"
                    className="exits-upload__file-remove"
                    disabled={disabled}
                    aria-label={`${removeLabel}: ${item.name}`}
                    title={removeLabel}
                    data-testid={`${testId}-remove-${item.id}`}
                    onClick={() => onRemove(item.id)}
                  >
                    <X className="size-3.5" aria-hidden />
                  </button>
                ) : onClear && items.length === 1 ? (
                  <button
                    type="button"
                    className="exits-upload__file-remove"
                    disabled={disabled}
                    aria-label={clearLabel}
                    title={clearLabel}
                    data-testid={`${testId}-clear`}
                    onClick={() => onClear()}
                  >
                    <X className="size-3.5" aria-hidden />
                  </button>
                ) : null}
              </li>
            ))}
          </ul>
        ) : null}
      </div>
    );
  }

  const filled = Boolean(value?.previewUrl || value?.name);
  const zoneClass =
    variant === "tile" ? "exits-upload exits-upload--tile" : "exits-upload exits-upload--dropzone";

  return (
    <div
      className={cn(
        zoneClass,
        filled && "exits-upload--filled",
        dragging && "exits-upload--dragging",
        disabled && "exits-upload--disabled",
        className,
      )}
      data-testid={testId}
      data-variant={variant}
      data-filled={filled ? "true" : undefined}
    >
      {fileInput}
      {filled && value?.previewUrl ? (
        <>
          <img
            src={value.previewUrl}
            alt=""
            className="exits-upload__preview"
            data-testid={`${testId}-preview`}
          />
          {onClear ? (
            <Button
              type="button"
              intent="danger"
              appearance="solid"
              size="icon"
              shape="round"
              className="exits-upload__clear"
              disabled={disabled}
              aria-label={clearLabel}
              title={clearLabel}
              data-testid={`${testId}-clear`}
              onClick={(event) => {
                event.stopPropagation();
                onClear();
              }}
            >
              <Trash2 className={`size-3.5 ${buttonIconMotion.delete}`} aria-hidden />
            </Button>
          ) : null}
          <button
            type="button"
            className="exits-upload__replace"
            disabled={disabled}
            data-testid={`${testId}-replace`}
            onClick={openPicker}
          >
            Replace
          </button>
        </>
      ) : (
        <div
          role="button"
          tabIndex={disabled ? -1 : 0}
          aria-disabled={disabled || undefined}
          aria-label={label}
          className="exits-upload__empty"
          data-testid={`${testId}-zone`}
          onClick={openPicker}
          onKeyDown={onZoneKeyDown}
          onDragOver={onDragOver}
          onDragLeave={onDragLeave}
          onDrop={onDrop}
        >
          <CloudUpload className="exits-upload__icon" aria-hidden strokeWidth={1.5} />
          {variant === "dropzone" ? (
            <p className="exits-upload__hint">{label}</p>
          ) : null}
          <span className="exits-upload__action" data-testid={`${testId}-action`}>
            {uploadLabel}
          </span>
        </div>
      )}
    </div>
  );
}
