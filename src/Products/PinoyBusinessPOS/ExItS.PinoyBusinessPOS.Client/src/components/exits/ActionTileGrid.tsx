import type { LucideIcon } from "lucide-react";
import { RoleActionTile } from "@/components/exits/RoleActionTile";
import { cn } from "@/lib/cn";

export type ActionTileDef = {
  key: string;
  label: string;
  icon: LucideIcon;
  testId?: string;
  primary?: boolean;
  /** Current page — primary styling without disabled fade. */
  current?: boolean;
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

  const nonHeroIndexes = tiles
    .map((tile, index) => ({ tile, index }))
    .filter(({ tile }) => !(emphasizePrimary && tile.primary))
    .map(({ index }) => index);
  const lastNonHeroIndex = nonHeroIndexes[nonHeroIndexes.length - 1];
  const stretchLastNonHero = nonHeroIndexes.length % 2 === 1;

  return (
    <div className="role-action-tile-grid grid min-w-0 grid-cols-2 gap-2" role="group">
      {tiles.map((tile, index) => {
        const isPrimaryHero = Boolean(emphasizePrimary && tile.primary);
        const isPrimary = Boolean(tile.primary || tile.current);
        const fullWidth =
          isPrimaryHero ||
          tiles.length === 1 ||
          (stretchLastNonHero && index === lastNonHeroIndex);
        const tileClassName = cn(
          fullWidth && "col-span-2 role-action-tile--center",
          isPrimaryHero && "role-action-tile--hero",
        );

        if (tile.current) {
          return (
            <RoleActionTile
              key={tile.key}
              label={tile.label}
              icon={tile.icon}
              testId={tile.testId}
              primary={isPrimary}
              current
              onClick={() => {}}
              className={tileClassName}
              style={{ animationDelay: `${40 + index * 35}ms` }}
            />
          );
        }

        return tile.onClick ? (
          <RoleActionTile
            key={tile.key}
            label={tile.label}
            icon={tile.icon}
            testId={tile.testId}
            primary={isPrimary}
            current={tile.current}
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
            primary={isPrimary}
            current={tile.current}
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
