import type { CSSProperties, ReactNode } from "react";

/**
 * Quasar QBreadcrumbs samples: Basic and Custom separators only.
 * Showcase only — not a product breadcrumb.
 * @see https://quasar.dev/vue-components/breadcrumbs
 */
type CrumbIconName = "home" | "widgets" | "navigation";
type Crumb = { label?: string; icon?: CrumbIconName };
type ActiveColor = "primary" | "secondary" | "purple";
type Tone = "orange" | "grey" | "brown";

const ICON_PATHS = {
  home: "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z",
  widgets: "M13 13v8h8v-8h-8zM3 21h8v-8H3v8zM3 3v8h8V3H3zm13.66-1.31L11 7.34 16.66 13l5.66-5.66-5.66-5.65z",
  navigation: "M12 2 4.5 20.29l.71.71L12 18l6.79 3 .71-.71z",
  arrow: "M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z",
  chevron: "M10 6 8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z",
} as const;

const ICON_LABELS: Record<CrumbIconName, string> = {
  home: "Home",
  widgets: "Widgets",
  navigation: "Navigation",
};

const ACTIVE_COLORS: Record<ActiveColor, string> = {
  primary: "#1976d2",
  secondary: "#26a69a",
  purple: "#9c27b0",
};

function MaterialIcon({
  name,
  className,
}: {
  name: keyof typeof ICON_PATHS;
  className: string;
}) {
  return (
    <svg className={className} viewBox="0 0 24 24" aria-hidden="true">
      <path d={ICON_PATHS[name]} />
    </svg>
  );
}

function CrumbIcon({ name, withLabel }: { name: CrumbIconName; withLabel: boolean }) {
  return (
    <MaterialIcon
      name={name}
      className={"ui-standards-qbreadcrumbs__icon" + (withLabel ? " ui-standards-qbreadcrumbs__icon--label" : "")}
    />
  );
}

function Trail({
  items,
  separator = "/",
  separatorNode,
  tone,
  active = "primary",
  testId,
}: {
  items: readonly Crumb[];
  separator?: string;
  separatorNode?: ReactNode;
  tone?: Tone;
  active?: ActiveColor;
  testId: string;
}) {
  const style = { "--qbreadcrumb-active": ACTIVE_COLORS[active] } as CSSProperties;
  return (
    <nav aria-label="Breadcrumb" data-testid={testId}>
      <ol
        className={"ui-standards-qbreadcrumbs" + (tone ? ` ui-standards-qbreadcrumbs--${tone}` : "")}
        style={style}
      >
        {items.flatMap((item, index) => {
          const last = index === items.length - 1;
          const nodes = [];
          if (index > 0) {
            nodes.push(
              <li key={`${testId}-sep-${index}`} className="ui-standards-qbreadcrumbs__separator" aria-hidden="true">
                {separatorNode ?? separator}
              </li>,
            );
          }
          nodes.push(
            <li
              key={`${testId}-item-${index}`}
              className={"ui-standards-qbreadcrumbs__el" + (last ? "" : " ui-standards-qbreadcrumbs__el--link")}
            >
              <span aria-current={last ? "page" : undefined} aria-label={item.label ? undefined : item.icon ? ICON_LABELS[item.icon] : undefined}>
                {item.icon ? <CrumbIcon name={item.icon} withLabel={Boolean(item.label)} /> : null}
                {item.label}
              </span>
            </li>,
          );
          return nodes;
        })}
      </ol>
    </nav>
  );
}

const BASIC_TEXT: readonly Crumb[] = [
  { label: "Home" },
  { label: "Components" },
  { label: "Breadcrumbs" },
];

const BASIC_ICONS: readonly Crumb[] = [
  { label: "Home", icon: "home" },
  { label: "Components", icon: "widgets" },
  { label: "Breadcrumbs" },
];

const BASIC_GLYPHS: readonly Crumb[] = [{ icon: "home" }, { icon: "widgets" }, { icon: "navigation" }];

const SEPARATOR_GLYPHS: readonly Crumb[] = [
  { icon: "home" },
  { label: "Components", icon: "widgets" },
  { label: "Breadcrumbs", icon: "navigation" },
];

const SEPARATOR_LABELED: readonly Crumb[] = [
  { label: "Home", icon: "home" },
  { label: "Components", icon: "widgets" },
  { label: "Breadcrumbs", icon: "navigation" },
];

export function UiStandardsBreadcrumbGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-breadcrumb-gallery">
      <div className="ui-standards-q-example" data-testid="ui-standard-breadcrumb-basic">
        <div className="ui-standards-q-example__label">Basic</div>
        <div className="ui-standards-breadcrumb-stack">
          <Trail items={BASIC_TEXT} testId="ui-standard-breadcrumb-basic-text" />
          <Trail items={BASIC_ICONS} testId="ui-standard-breadcrumb-basic-icons" />
          <Trail items={BASIC_GLYPHS} tone="grey" testId="ui-standard-breadcrumb-basic-glyphs" />
        </div>
      </div>
      <div className="ui-standards-q-example" data-testid="ui-standard-breadcrumb-separator">
        <div className="ui-standards-q-example__label">Custom separators</div>
        <div className="ui-standards-breadcrumb-stack">
          <Trail
            items={SEPARATOR_GLYPHS}
            separator="---"
            tone="orange"
            active="secondary"
            testId="ui-standard-breadcrumb-separator-dashes"
          />
          <Trail
            items={SEPARATOR_LABELED}
            tone="grey"
            active="purple"
            separatorNode={<MaterialIcon name="arrow" className="ui-standards-qbreadcrumbs__sep-icon" />}
            testId="ui-standard-breadcrumb-separator-arrow"
          />
          <Trail
            items={SEPARATOR_LABELED}
            tone="brown"
            separatorNode={<MaterialIcon name="chevron" className="ui-standards-qbreadcrumbs__sep-icon ui-standards-qbreadcrumbs__sep-icon--lg" />}
            testId="ui-standard-breadcrumb-separator-chevron"
          />
        </div>
      </div>
    </div>
  );
}
