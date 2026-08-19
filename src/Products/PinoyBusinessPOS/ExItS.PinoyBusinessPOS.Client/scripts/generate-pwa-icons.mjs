import { createHash } from "node:crypto";
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";

const rootDir = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const outDir = path.join(rootDir, "public", "icons");

const GREEN = [0x16, 0x65, 0x34];
const WHITE = [0xff, 0xff, 0xff];

function crc32(buffer) {
  let crc = 0xffffffff;
  for (const byte of buffer) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit += 1) {
      const mask = -(crc & 1);
      crc = (crc >>> 1) ^ (0xedb88320 & mask);
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const typeBuffer = Buffer.from(type);
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);
  const crcBuffer = Buffer.alloc(4);
  crcBuffer.writeUInt32BE(crc32(Buffer.concat([typeBuffer, data])));
  return Buffer.concat([length, typeBuffer, data, crcBuffer]);
}

function inRect(x, y, left, top, width, height) {
  return x >= left && x < left + width && y >= top && y < top + height;
}

function paintIcon(size, maskable) {
  const inset = maskable ? 0.28 : 0.22;
  const left = Math.round(size * inset);
  const top = Math.round(size * inset);
  const glyph = Math.round(size * (1 - inset * 2));
  const thickness = Math.max(3, Math.round(glyph * 0.18));
  const midWidth = Math.round(glyph * 0.72);
  const midTop = top + Math.round(glyph / 2 - thickness / 2);

  const pixels = Buffer.alloc((size * 3 + 1) * size);
  for (let y = 0; y < size; y += 1) {
    const row = y * (size * 3 + 1);
    pixels[row] = 0;
    for (let x = 0; x < size; x += 1) {
      const onGlyph =
        inRect(x, y, left, top, thickness, glyph) ||
        inRect(x, y, left, top, glyph, thickness) ||
        inRect(x, y, left, midTop, midWidth, thickness) ||
        inRect(x, y, left, top + glyph - thickness, glyph, thickness);
      const [r, g, b] = onGlyph ? WHITE : GREEN;
      const index = row + 1 + x * 3;
      pixels[index] = r;
      pixels[index + 1] = g;
      pixels[index + 2] = b;
    }
  }
  return pixels;
}

function writePng(fileName, size, maskable) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;
  ihdr[9] = 2;
  const png = Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk("IHDR", ihdr),
    chunk("IDAT", deflateSync(paintIcon(size, maskable))),
    chunk("IEND", Buffer.alloc(0)),
  ]);
  writeFileSync(path.join(outDir, fileName), png);
}

mkdirSync(outDir, { recursive: true });
writePng("icon-192.png", 192, false);
writePng("icon-512.png", 512, false);
writePng("icon-192-maskable.png", 192, true);
writePng("icon-512-maskable.png", 512, true);
writePng("apple-touch-icon.png", 180, false);

const fingerprint = createHash("sha256").update(paintIcon(32, false)).digest("hex").slice(0, 12);
process.stdout.write(`Wrote PWA icons to ${outDir} (${fingerprint})\n`);
