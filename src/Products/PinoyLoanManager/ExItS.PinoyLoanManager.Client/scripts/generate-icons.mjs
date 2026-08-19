import { createHash } from "node:crypto";
import { writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";

const rootDir = path.dirname(fileURLToPath(import.meta.url));
const publicDir = path.resolve(rootDir, "../public");

const GREEN = [0x16, 0x65, 0x34, 0xff];
const WHITE = [0xff, 0xff, 0xff, 0xff];

const E_GLYPH = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"];

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
  const crcInput = Buffer.concat([typeBuffer, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcInput));
  return Buffer.concat([length, typeBuffer, data, crc]);
}

function writePng(filePath, size, insetRatio) {
  const bytes = Buffer.alloc(size * size * 4);
  const inset = Math.round(size * insetRatio);
  const inner = size - inset * 2;
  const cell = Math.floor(inner / 7);
  const glyphWidth = cell * 5;
  const glyphHeight = cell * 7;
  const originX = Math.floor((size - glyphWidth) / 2);
  const originY = Math.floor((size - glyphHeight) / 2);

  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const offset = (y * size + x) * 4;
      bytes.set(GREEN, offset);
    }
  }

  for (let row = 0; row < E_GLYPH.length; row += 1) {
    for (let col = 0; col < E_GLYPH[row].length; col += 1) {
      if (E_GLYPH[row][col] !== "1") {
        continue;
      }
      for (let dy = 0; dy < cell; dy += 1) {
        for (let dx = 0; dx < cell; dx += 1) {
          const x = originX + col * cell + dx;
          const y = originY + row * cell + dy;
          if (x < 0 || y < 0 || x >= size || y >= size) {
            continue;
          }
          bytes.set(WHITE, (y * size + x) * 4);
        }
      }
    }
  }

  const raw = Buffer.alloc(size * (1 + size * 4));
  for (let y = 0; y < size; y += 1) {
    const rowStart = y * (1 + size * 4);
    raw[rowStart] = 0;
    bytes.copy(raw, rowStart + 1, y * size * 4, (y + 1) * size * 4);
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;

  const png = Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", ihdr),
    chunk("IDAT", deflateSync(raw)),
    chunk("IEND", Buffer.alloc(0)),
  ]);
  writeFileSync(filePath, png);
  return createHash("sha256").update(png).digest("hex").slice(0, 12);
}

const files = [
  { name: "icon-192.png", size: 192, inset: 0.18 },
  { name: "icon-512.png", size: 512, inset: 0.18 },
  { name: "icon-192-maskable.png", size: 192, inset: 0.28 },
  { name: "icon-512-maskable.png", size: 512, inset: 0.28 },
];

for (const file of files) {
  const hash = writePng(path.join(publicDir, file.name), file.size, file.inset);
  process.stdout.write(`${file.name} ${hash}\n`);
}
