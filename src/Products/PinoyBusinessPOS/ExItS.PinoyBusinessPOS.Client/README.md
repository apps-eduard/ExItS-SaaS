# ExItS Pinoy Business POS Client

React Mobile Client host (Gate C complete; Gate D PWA foundation in this package). Sibling of MAUI, not a replacement yet.

- Path: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, Lucide, vite-plugin-pwa
- Default locale: English (`en`); secondary: `fil-PH`
- Default theme: System (Light / Dark supported)
- PWA: installable static shell + prompt updates. **Not** a production rollout, LocalStore, or financial offline SoR
- Capacitor / authentication / selling: **not** in this package

```powershell
npm install
npm run dev
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run test:pwa
npm run test:e2e
```

Do not import Platform Admin business UI. Do not modify MAUI.
