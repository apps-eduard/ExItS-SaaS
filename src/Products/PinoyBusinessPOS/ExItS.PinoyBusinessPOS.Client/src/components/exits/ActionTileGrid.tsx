import type { LucideIcon } from "lucide-react";
import { RoleActionTile } from "@/components/exits/RoleActionTile";

export type ActionTileDef = {
  key: string;
  label: string;
  icon: LucideIcon;
  testId?: string;
  primary?: boolean;
  to?: string;
  onClick?: () => void;
};

export function ActionTileGrid({ tiles }: { tiles: ActionTileDef[] }) {
  if (tiles.length === 0) {
    return null;
  }

  return (
    <div className="grid min-w-0 grid-cols-2 gap-2" role="group">
      {tiles.map((tile, index) => {
        const fullWidth =
          tiles.length === 1 || (tiles.length % 2 === 1 && index === tiles.length - 1);
        const tileClassName = fullWidth ? "col-span-2" : undefined;

        return tile.onClick ? (
          <RoleActionTile
            key={tile.key}
            label={tile.label}
            icon={tile.icon}
            testId={tile.testId}
            primary={tile.primary}
            onClick={tile.onClick}
            className={tileClassName}
          />
        ) : (
          <RoleActionTile
            key={tile.key}
            label={tile.label}
            icon={tile.icon}
            testId={tile.testId}
            primary={tile.primary}
            to={tile.to!}
            className={tileClassName}
          />
        );
      })}
    </div>
  );
}
