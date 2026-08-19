# ExItS Pinoy Loan Manager Client

React organization/field client host for Pinoy Loan Manager. Sibling of the ASP.NET Core Web host/BFF, not a replacement yet.

- Path: `src/Products/PinoyLoanManager/ExItS.PinoyLoanManager.Client/`
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, Lucide
- Locale: English (`en`) default; `fil-PH` secondary
- Theme: System default (Light / Dark supported)
- Gate B: product chrome only. No lending screens, auth, PWA, or Capacitor

```powershell
npm install
npm run dev
```

Browser origin: `http://localhost:5176`.

```powershell
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run test:e2e
```

Do not import PinoyBusinessPOS. Do not modify MAUI or Web in this package.
