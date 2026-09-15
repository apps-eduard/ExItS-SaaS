# ExItS Upload Standard

**Status:** PILOT
**Scope:** `ExItS.PinoyBusinessPOS.React`
**Canonical component:** `components/exits/ExitsUpload.tsx`
**Visual authority:** `/ui-standards` → **Upload**

Presentation-only control. Pages own validation, persistence, and upload APIs.

---

## Variants

| Variant | Use when |
|---------|----------|
| **dropzone** | Primary cover / hero image — large hit target, drag-and-drop |
| **tile** | Gallery slots (extra images) — compact square |
| **button** | Toolbars / forms — compact Outline button; set `multiple` + `values` / `onRemove` for multi-file chips with delete |

Do not invent a fourth upload chrome for product pages.

---

## Behavior

- Hidden native `<input type="file">`; click / keyboard / drop open the picker
- Filled dropzone/tile: preview image + danger clear (trash) + Replace
- Button compact: optional file chips with per-file remove (`onRemove`)
- Default `accept`: `image/jpeg,image/png,image/webp` (override per page)
- No network calls inside the component

---

## Usage

```tsx
<ExitsUpload
  variant="dropzone"
  value={cover}
  onSelectFiles={(files) => setCover(toItem(files[0]!))}
  onClear={() => setCover(null)}
/>

<div className="exits-upload-gallery">
  {slots.map((item, i) => (
    <ExitsUpload key={i} variant="tile" value={item} onSelectFiles={...} onClear={...} />
  ))}
</div>

<ExitsUpload
  variant="button"
  multiple
  uploadLabel="Upload files"
  values={files}
  onSelectFiles={(picked) => setFiles((cur) => [...cur, ...picked.map(toItem)])}
  onRemove={(id) => setFiles((cur) => cur.filter((f) => f.id !== id))}
/>
```
