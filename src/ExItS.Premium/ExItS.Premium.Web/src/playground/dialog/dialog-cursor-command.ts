import type { DialogSize, DialogTransition } from "../../components/dialog/Dialog";
import { DIALOG_TRANSITION_DEFAULT } from "../../components/dialog/dialog-transition";

export function dialogCursorCommand(options: {
  size: DialogSize;
  transition?: DialogTransition;
  description: boolean;
  footer: boolean;
  dismissible: boolean;
  icon?: boolean;
  lineTop?: boolean;
  lineBottom?: boolean;
  grayFooter?: boolean;
}): string {
  const parts = ["ext dialog premium"];
  if (options.size !== "md") parts.push(options.size);
  if (options.transition && options.transition !== DIALOG_TRANSITION_DEFAULT) {
    parts.push(options.transition);
  }
  if (!options.description) parts.push("no-description");
  if (options.icon === false) parts.push("no-icon");
  if (!options.footer) parts.push("no-footer");
  if (options.lineTop === false) parts.push("no-line-top");
  if (options.lineBottom === false) parts.push("no-line-bottom");
  if (options.grayFooter === false) parts.push("no-gray-footer");
  if (!options.dismissible) parts.push("persistent");
  return parts.join(" ");
}
