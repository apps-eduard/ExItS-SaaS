import type { ReactNode } from "react";
import type { IconName } from "../../icons/registry";
import { Button } from "../button/Button";
import { Dialog, type DialogSize, type DialogTone } from "./Dialog";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  tone?: "primary" | "danger";
  loading?: boolean;
  error?: string;
  size?: DialogSize;
  dismissible?: boolean;
  icon?: IconName | ReactNode;
  iconTone?: DialogTone;
  onConfirm: () => void;
  onClose: () => void;
  children?: ReactNode;
};

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  tone = "primary",
  loading = false,
  error,
  size,
  dismissible,
  icon,
  iconTone,
  onConfirm,
  onClose,
  children,
}: ConfirmDialogProps) {
  const resolvedIcon = icon ?? (tone === "danger" ? "warning" : "check");
  const resolvedTone: DialogTone = iconTone ?? (tone === "danger" ? "danger" : "primary");

  return (
    <Dialog
      open={open}
      title={title}
      description={description}
      onClose={onClose}
      size={size}
      dismissible={dismissible}
      icon={resolvedIcon}
      iconTone={resolvedTone}
      mobilePresentation="center"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button variant={tone === "danger" ? "danger" : "primary"} loading={loading} data-autofocus onClick={onConfirm}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      {error ? (
        <p className="type-secondary" role="alert">
          {error}
        </p>
      ) : (
        children
      )}
    </Dialog>
  );
}
