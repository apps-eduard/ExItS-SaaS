# ExItS Pinoy Business POS Client

React Mobile Client host. Sibling of MAUI, not a replacement yet.

- Path: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, Lucide, vite-plugin-pwa
- Default locale: English (`en`); secondary: `fil-PH`
- Default theme: System (Light / Dark supported)
- PWA: installable static shell + prompt updates. **Not** a production rollout, LocalStore, or financial offline SoR
- Browser auth: same-origin `/platform-api` proxy to Platform. HttpOnly `.ExItS.Platform.Auth` cookie. JavaScript ignores `sessionToken`.
- Capacitor / PIN / workspace chooser / selling: **not** in this package

Local development:

```powershell
npm install
npm run dev -- --host 0.0.0.0
```

Browser origin: `http://localhost:5175`. API calls go to `http://localhost:5175/platform-api/...` and Vite proxies that prefix to loopback Platform (`http://127.0.0.1:8091` by default).

Future production host (not rolled out here):

```
https://<pwa-origin>/                 → React static application
https://<pwa-origin>/platform-api/*   → Platform API reverse proxy
```

```powershell
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run test:pwa
npm run test:e2e
```

Do not import Platform Admin business UI. Do not modify MAUI.
