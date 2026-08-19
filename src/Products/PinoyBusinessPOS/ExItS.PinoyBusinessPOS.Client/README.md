# ExItS Pinoy Business POS Client

React Mobile Client host (Gate C foundation). Sibling of MAUI, not a replacement yet.

- Path: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, Lucide
- Default locale: English (`en`); secondary: `fil-PH`
- Default theme: System (Light / Dark supported)
- PWA / Capacitor / authentication / selling: **not** in this package

```powershell
npm install
npm run dev
npm run typecheck
npm run lint
npm run format:check
npm test
npm run test:e2e
npm run build
```

Do not import Platform Admin business UI. Do not modify MAUI.
