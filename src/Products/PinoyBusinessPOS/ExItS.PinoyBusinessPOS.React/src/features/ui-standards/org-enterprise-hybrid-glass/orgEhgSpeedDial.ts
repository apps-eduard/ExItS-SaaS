/**
 * Org Enterprise Hybrid Glass — Floating Action speed dial label types (UI Standards).
 */
export type OrgEhgSpeedDialLabelType =
  | "icon-only"
  | "label-tooltip"
  | "rounded-label"
  | "flat-label"
  | "extended";

export const ORG_EHG_SPEED_DIAL_LABEL_TYPES: ReadonlyArray<{
  label: string;
  value: OrgEhgSpeedDialLabelType;
}> = [
  { label: "Icon only", value: "icon-only" },
  { label: "Label tooltip", value: "label-tooltip" },
  { label: "Rounded label", value: "rounded-label" },
  { label: "Flat label", value: "flat-label" },
  { label: "Extended", value: "extended" },
];

export const ORG_EHG_SPEED_DIAL_LABEL_DEFAULT: OrgEhgSpeedDialLabelType = "label-tooltip";

/** Open / close motion for the speed dial action stack. */
export type OrgEhgSpeedDialTransition =
  | "none"
  | "fade"
  | "scale"
  | "slide"
  | "stagger"
  | "bounce";

export const ORG_EHG_SPEED_DIAL_TRANSITIONS: ReadonlyArray<{
  label: string;
  value: OrgEhgSpeedDialTransition;
}> = [
  { label: "None", value: "none" },
  { label: "Fade", value: "fade" },
  { label: "Scale", value: "scale" },
  { label: "Slide", value: "slide" },
  { label: "Stagger", value: "stagger" },
  { label: "Bounce", value: "bounce" },
];

export const ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT: OrgEhgSpeedDialTransition = "stagger";

/** Where child actions open relative to the trigger. */
export type OrgEhgSpeedDialShow =
  | "top"
  | "bottom"
  | "left"
  | "right";

export const ORG_EHG_SPEED_DIAL_SHOW_OPTIONS: ReadonlyArray<{
  label: string;
  value: OrgEhgSpeedDialShow;
}> = [
  { label: "Top", value: "top" },
  { label: "Bottom", value: "bottom" },
  { label: "Left", value: "left" },
  { label: "Right", value: "right" },
];

export const ORG_EHG_SPEED_DIAL_SHOW_DEFAULT: OrgEhgSpeedDialShow = "top";
