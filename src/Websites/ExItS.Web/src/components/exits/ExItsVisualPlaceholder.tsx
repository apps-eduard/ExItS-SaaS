import { ExItsAbstractVisual, type AbstractVisualVariant } from "./ExItsAbstractVisual";

/**
 * Compatibility wrapper — renders abstract product visuals (not wireframe captions).
 * `caption` is accepted for call-site compatibility but not shown publicly.
 */
export function ExItsVisualPlaceholder({
  title,
  caption: _caption,
  className,
  variant = "dashboard",
}: {
  title: string;
  caption?: string;
  className?: string;
  variant?: AbstractVisualVariant;
}) {
  void _caption;
  return <ExItsAbstractVisual title={title} variant={variant} className={className} />;
}
