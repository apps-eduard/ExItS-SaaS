# POS-LIVE-QR-01 — Live Browser Camera QR Scanning

**Package:** POS-LIVE-QR-01  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `cbc72dc25990fca4cd4fa0ccf84c8e804e948cb7`  
**Status:** **AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW**

---

## Summary

Production-quality live browser camera QR scanning for POS React / Personal React using the **existing** ExItS public-ID QR contracts. No new QR formats, no second identity system, no backend broadening.

Live camera is an **additional** input method alongside upload and manual ExItS ID entry.

---

## Flags

```
POS_LIVE_QR_01=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW
LIVE_CAMERA_IMPLEMENTED=YES
LIVE_CAMERA_AUTOMATED_VERIFIED=PASS
LIVE_CAMERA_DEVICE_VERIFIED=NO
CAMERA_PERMISSION_UX=PASS
CAMERA_STREAM_CLEANUP=PASS
QR_PURPOSE_GUARDS=PASS
QR_FILE_FALLBACK=PASS
QR_MANUAL_FALLBACK=PASS
QR_PRIVACY=PASS
TAILSCALE_HTTP_CAMERA_SECURE_CONTEXT=BROWSER_BLOCKED
PLATFORM_ADMIN_REACT_MODIFIED=NO
COM_INT_04_AUTHORIZED=NO
RMAP_TAX_AUTHORIZED=NO
RMAP_B05_AUTHORIZED=NO
MERGE_TO_MAIN_AUTHORIZED=NO
PRODUCTION_CUTOVER=NO
```

---

## Decoder strategy (`DECODER_STRATEGY`)

| Layer | Choice |
| ----- | ------ |
| Camera | `navigator.mediaDevices.getUserMedia` via `openPreferredCamera()` |
| Live frame decode | `BarcodeDetector` (when available) → **jsQR** fallback (already in dependencies) |
| Still image decode | Unchanged — existing `decode-qr-from-image.ts` (jsQR + optional BarcodeDetector) |
| Parse / purpose | Unchanged — `parseExItsQr` / `assertExItsQrPurpose` in `envelope.ts` |

**Browser support:** Chrome/Edge (BarcodeDetector + jsQR), Firefox/Safari (jsQR). No exclusive BarcodeDetector dependency.

Live frames are scaled to max **640px** width before decode to limit CPU/battery use.

---

## Camera implementation

| File | Role |
| ---- | ---- |
| `src/lib/qr/camera-access.ts` | Secure-context checks, environment-first camera, stream stop |
| `src/lib/qr/decode-qr-frame.ts` | Live frame capture + decode + test hook |
| `src/features/qr/LiveQrCameraScanner.tsx` | Reusable bottom-sheet scanner (returns parsed result only) |
| `src/features/qr/QrScanOrEnter.tsx` | Chooser: **Scan with camera** / **Upload QR image** / **Enter ExItS ID manually** |

**Rear camera:** `facingMode: { ideal: "environment" }` with fallback to any available camera.

**Optional torch:** Shown only when `track.getCapabilities().torch` exists.

**Scan stability:** 180ms decode interval; scan lock after valid decode; duplicate payload suppression; wrong-purpose/malformed QR shows inline error and continues scanning.

---

## Secure context (`CAMERA_SECURE_CONTEXT_REQUIREMENT`)

| Environment | Camera |
| ----------- | ------ |
| HTTPS production host | Supported |
| `http://localhost` | Browser localhost exception — supported |
| `http://127.0.0.1` | Generally supported |
| Plain HTTP remote LAN / Tailscale | **Blocked by browser** — not a product defect |

When blocked, UI shows `qr.insecureContext` and offers upload + manual fallback.

```
TAILSCALE_HTTP_CAMERA_SECURE_CONTEXT=BROWSER_BLOCKED
```

Do not weaken browser security or fake a PASS on insecure remote HTTP.

---

## Permission UX (`CAMERA_PERMISSION_UX`)

States implemented in `LiveQrCameraScanner`:

| State | User message / action |
| ----- | --------------------- |
| Initial | “Scan QR code” + **Open camera** |
| Requesting | “Starting camera…” |
| Scanning | Live preview + targeting frame |
| Permission denied | “Camera access is blocked.” + Try again / Upload / Manual |
| No camera | “No camera is available on this device.” + fallbacks |
| Unsupported / insecure | Appropriate message + fallbacks |

Accessible dialog title, `role="status"` / `role="alert"`, keyboard-reachable Close and fallbacks.

---

## Privacy (`PRIVACY_BEHAVIOR`)

- All decoding is **client-side**
- No video upload, frame upload, screenshots, recordings, or analytics on decoded payloads
- Device registration tokens are **not logged** or displayed in full
- `stopMediaStream()` on: successful scan, dialog close, unmount, camera switch, `document.hidden` (decode pause)

---

## Purpose guards (`PURPOSE_GUARDS`)

Authoritative via `assertExItsQrPurpose`. Wrong purpose → **“This QR code can't be used here.”** — no cross-workflow resolution.

| Workflow | Expected purpose |
| -------- | ---------------- |
| Checkout customer selection | `personal` |
| Customer Personal link panel | `personal` |
| Device registration (when UI exists) | `pos-device-registration` |
| Organization consumption (when UI exists) | `organization` |

Canonical payloads unchanged:

- Personal: `exits://qr/v1/personal/EX-...` (legacy `exits://user/v1/EX-...` where already supported)
- Organization: `exits://qr/v1/organization/ORG######`
- Device registration: `exits://qr/v1/pos-device-registration/{opaqueToken}`

---

## Wired workflows

### Checkout customer QR

`CheckoutPersonalCustomerPicker` → `QrScanOrEnter` → live camera → resolve public Personal ID → existing customer lookup → **selection/confirmation only** (no auto-sale, no silent link).

### Customer linking

`CustomerPersonalLinkPanel` → same `QrScanOrEnter` → resolve → **explicit confirmation** → existing link API.

### Device registration QR

**Known gap:** React has no token-redeem registration QR UI today (`OrgPosDevicesPage` uses direct register). Live camera **not wired** to a non-existent flow. API remains authoritative when a future UI is authorized.

### Organization QR consumption

**Known gap:** Organization QR is **display-only** (`OrgBusinessQrPage`). No existing consumer workflow to extend with live camera without inventing B05/discovery behavior.

---

## Fallbacks

| Method | Status |
| ------ | ------ |
| Live camera | **NEW** |
| Upload QR image | Retained |
| Enter ExItS ID manually | Retained |

---

## Offline behavior

Camera decode may work offline. Server-authoritative resolution/linking shows network-required errors from existing APIs — no unsafe offline identity cache added.

---

## Tests

### Unit (Vitest)

- `src/lib/qr/camera-access.test.ts` — environment camera, fallback, permission, secure context, stream stop
- `src/lib/qr/decode-qr-frame.test.ts` — test hook, no payload logging
- `src/features/qr/LiveQrCameraScanner.test.tsx` — UX states, purpose guards, scan lock, cleanup
- `src/features/qr/QrScanOrEnter.test.tsx` — chooser + live wiring
- `src/features/checkout/checkout-personal-customer-picker.test.tsx` — live Personal QR checkout path

### Playwright (mocked camera)

- `e2e/pos-live-qr-01-camera.spec.ts` — deterministic `getUserMedia` + `__EXITS_LIVE_QR_DECODE__` harness

**Does not equal physical device verification.**

---

## Owner manual checklist (`LIVE_CAMERA_DEVICE_VERIFIED=NO`)

### Phone

1. Open POS PWA over **HTTPS** or localhost-capable test environment.
2. Checkout → Select customer → **Scan with camera**.
3. Allow camera.
4. Rear camera opens.
5. Scan Personal QR.
6. Customer resolves.
7. Confirm customer selection.
8. Close scanner — verify browser camera indicator turns off.

Also test: deny permission, upload fallback, manual ID, Organization QR rejection, portrait/landscape.

If testing over plain Tailscale HTTP and browser blocks camera → record **SECURE_CONTEXT_BLOCKED**, not product failure.

---

## Files changed

| Path | Change |
| ---- | ------ |
| `src/lib/qr/camera-access.ts` | New |
| `src/lib/qr/decode-qr-frame.ts` | New |
| `src/features/qr/LiveQrCameraScanner.tsx` | New |
| `src/features/qr/QrScanOrEnter.tsx` | Live camera chooser |
| `src/i18n/locales/*.ts` | QR camera strings (5 locales) |
| `e2e/pos-live-qr-01-camera.spec.ts` | New mocked E2E |
| `docs/Mobile-React/Reports/POS-REACT-RMAP-24-final-validation.md` | Addendum only |

---

## Known gaps

| Gap | Notes |
| --- | ----- |
| Physical device camera | Owner checklist pending |
| Device registration QR UI | No React redeem flow to wire |
| Organization QR consumer | Display-only; no authorized workflow |
| Plain HTTP Tailscale camera | Browser secure-context block — use HTTPS/Serve later |

---

## Regression

Run before push:

```powershell
cd src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React
npm test
npm run typecheck
npm run lint
npm run build
npm run test:e2e -- e2e/pos-live-qr-01-camera.spec.ts
```

COM-INT-01/02/03, RMAP-21 offline Cash, checkout paths, PWA — must remain green.

**HARD STOP** — No additional POS packages after POS-LIVE-QR-01.
