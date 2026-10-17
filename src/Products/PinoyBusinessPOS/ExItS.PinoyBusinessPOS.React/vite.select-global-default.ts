import fs from "node:fs";
import path from "node:path";
import type { Plugin } from "vite";

const SELECT_GLOBAL_INTENTS = new Set([
  "standard",
  "primary",
  "secondary",
  "success",
  "info",
  "amber",
  "danger",
  "warn",
  "help",
  "black",
]);

const SELECT_GLOBAL_APPEARANCES = new Set([
  "standard",
  "soft",
  "soft-outline",
  "filled",
  "outlined",
  "standout",
]);

export function selectGlobalDefaultSource(intent: string, appearance: string): string | null {
  if (!SELECT_GLOBAL_INTENTS.has(intent) || !SELECT_GLOBAL_APPEARANCES.has(appearance)) return null;
  const tokens = ["ext dropdown"];
  if (intent !== "standard") tokens.push(intent);
  if (appearance !== "standard") tokens.push(appearance);
  return `/** Locked by the Select playground "default global" control. Command: ${tokens.join(" ")} */\nexport const SELECT_GLOBAL_DEFAULT_INTENT = "${intent}" as const;\nexport const SELECT_GLOBAL_DEFAULT_APPEARANCE = "${appearance}" as const;\n`;
}

/** Dev-only. The Select playground lock writes the appearance Cursor reads. */
export function createSelectGlobalDefaultPlugin(rootDir: string): Plugin {
  const file = path.resolve(rootDir, "src/features/ui-standards/select-global-default.ts");
  return {
    name: "exits-select-global-default",
    configureServer(server) {
      server.middlewares.use("/__exits/select-default", (req, res) => {
        if (req.method !== "POST") {
          res.statusCode = 405;
          res.end();
          return;
        }
        const chunks: Buffer[] = [];
        req.on("data", (chunk: Buffer) => {
          chunks.push(chunk);
        });
        req.on("end", () => {
          try {
            const body = JSON.parse(Buffer.concat(chunks).toString("utf8")) as {
              intent?: string;
              appearance?: string;
            };
            const source = selectGlobalDefaultSource(String(body.intent ?? ""), String(body.appearance ?? ""));
            if (!source) {
              res.statusCode = 400;
              res.end();
              return;
            }
            fs.writeFileSync(file, source);
            res.statusCode = 204;
            res.end();
          } catch {
            res.statusCode = 400;
            res.end();
          }
        });
      });
    },
  };
}
