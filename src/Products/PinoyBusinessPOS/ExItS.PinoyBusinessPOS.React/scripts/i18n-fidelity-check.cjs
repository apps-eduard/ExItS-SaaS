/**
 * I18N translation fidelity check: regional locales vs fil-PH.
 * Usage: node scripts/i18n-fidelity-check.cjs
 * Pass criterion: identical-to-fil percentage UNDER 35% for ceb / ilo / hil.
 */

const fs = require("fs");
const path = require("path");

function loadLocale(file, exportName) {
  let s = fs.readFileSync(file, "utf8");
  s = s.replace(/^import[\s\S]*?;\s*/m, "");
  s = s.replace(new RegExp(`export const ${exportName}[^=]*=\\s*`), "module.exports = ");
  s = s.replace(/;\s*$/, "");
  const tmp = path.join(__dirname, `_tmp_${exportName}.cjs`);
  fs.writeFileSync(tmp, s + "\n");
  delete require.cache[require.resolve(tmp)];
  const obj = require(tmp);
  fs.unlinkSync(tmp);
  return obj;
}

const dir = path.join(__dirname, "..", "src", "i18n", "locales");
const en = loadLocale(path.join(dir, "en.ts"), "en");
const fil = loadLocale(path.join(dir, "fil-PH.ts"), "filPH");
const ceb = loadLocale(path.join(dir, "ceb-PH.ts"), "cebPH");
const ilo = loadLocale(path.join(dir, "ilo-PH.ts"), "iloPH");
const hil = loadLocale(path.join(dir, "hil-PH.ts"), "hilPH");
const keys = Object.keys(en);

function stats(name, loc) {
  let sameFil = 0;
  let sameEn = 0;
  let unique = 0;
  const samples = [];
  for (const k of keys) {
    if (!(k in loc)) {
      console.error(`Missing key in ${name}: ${k}`);
      process.exit(1);
    }
    if (loc[k] === fil[k]) sameFil++;
    else if (loc[k] === en[k]) sameEn++;
    else unique++;
    if (loc[k] !== fil[k] && samples.length < 5) {
      samples.push({ key: k, regional: loc[k], fil: fil[k] });
    }
  }
  const pctFil = (100 * sameFil) / keys.length;
  console.log(name, {
    keys: keys.length,
    sameFil,
    sameEn,
    unique,
    pctFil: Math.round(pctFil * 10) / 10,
  });
  console.log(`  samples differing from fil-PH:`);
  for (const s of samples) {
    console.log(`  - ${s.key}`);
    console.log(`      ${name}: ${s.regional}`);
    console.log(`      fil: ${s.fil}`);
  }
  return pctFil;
}

const pCeb = stats("ceb", ceb);
const pIlo = stats("ilo", ilo);
const pHil = stats("hil", hil);

const threshold = 35;
const ok = pCeb < threshold && pIlo < threshold && pHil < threshold;
console.log(ok ? `PASS: all under ${threshold}%` : `FAIL: one or more >= ${threshold}%`);
process.exit(ok ? 0 : 2);
