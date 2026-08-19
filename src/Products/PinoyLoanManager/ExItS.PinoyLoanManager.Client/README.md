# ExItS Pinoy Loan Manager Client

React organization/field client host for Pinoy Loan Manager. Sibling of the ASP.NET Core Web host/BFF, not a replacement yet.

- Path: `src/Products/PinoyLoanManager/ExItS.PinoyLoanManager.Client/`
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, Lucide
- Locale: English (`en`) default; `fil-PH` secondary
- Theme: System default (Light / Dark supported)
- Gate B: product chrome. Gate C: installable online-first PWA. Gate D0: same-origin `/platform-api` cookie transport. Gate D1: Sign In + session UI. No Register/Reset, lending screens, or Capacitor

```powershell
npm install
npm run dev
```

Browser origin: `http://localhost:5176`. Platform API is reached only as same-origin `/platform-api` (Vite proxy to loopback `:8091`). Do not call `:8091` from browser JavaScript.

```powershell
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run test:e2e
npm run test:pwa
```

Do not import PinoyBusinessPOS. Do not modify MAUI or Web in this package.
