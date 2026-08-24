import type { LucideIcon } from "lucide-react";
import { RoleActionTile } from "@/components/exits/RoleActionTile";
import { cn } from "@/lib/cn";

export type ActionTileDef = {
  key: string;
  label: string;
  icon: LucideIcon;
  testId?: string;
  primary?: boolean;
  disabled?: boolean;
  to?: string;
  onClick?: () => void;
};

export function ActionTileGrid({
  tiles,
  emphasizePrimary = false,
}: {
  tiles: ActionTileDef[];
  /** Primary tiles (e.g. Start Selling) span the full row and use hero sizing. */
  emphasizePrimary?: boolean;
}) {
  if (tiles.length === 0) {
    return null;
  }

  return (
    <div className="role-action-tile-grid grid min-w-0 grid-cols-2 gap-2" role="group">
      {tiles.map((tile, index) => {
        const isPrimaryHero = Boolean(emphasizePrimary && tile.primary);
        const fullWidth =
          isPrimaryHero ||
          tiles.length === 1 ||
          (tiles.length % 2 === 1 && index === tiles.length - 1);
        const tileClassName = cn(
          fullWidth && "col-span-2 role-action-tile--center",
          isPrimaryHero && "role-action-tile--hero",
        );

        return tile.onClick ? (
          <RoleActionTile
            key={tile.key}
            label={tile.label}
            icon={tile.icon}
            testId={tile.testId}
            primary={tile.primary}
            disabled={tile.disabled}
            onClick={tile.onClick}
            className={tileClassName}
            style={{ animationDelay: `${40 + index * 35}ms` }}
          />
        ) : (
          <RoleActionTile
            key={tile.key}
            label={tile.label}
            icon={tile.icon}
            testId={tile.testId}
            primary={tile.primary}
            disabled={tile.disabled}
            to={tile.to!}
            className={tileClassName}
            style={{ animationDelay: `${40 + index * 35}ms` }}
          />
        );
      })}
    </div>
  );
}
