# Pinoy Business POS Client

Future Pinoy Business POS React host. Sibling of MAUI and Organization Web, not a replacement until later authorized packages.

- Path: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`
- Package: `@exits/pinoy-business-pos-client` (private, ESM)
- Stack: React, TypeScript strict, Vite, Tailwind, React Router, TanStack Query, React Hook Form, Zod, Lucide
- Locale: English (`en`) default; `fil-PH` secondary
- Theme: System default (Light / Dark supported)
- Dev: `http://127.0.0.1:5177` (`strictPort`; `npm run dev` runs `adb reverse tcp:5177 tcp:5177` when an emulator is connected)
- Preview: `http://127.0.0.1:4177` (`strictPort`)

This package currently ships a foundation shell plus an online-first static PWA layer. It does not authenticate, call Platform/POS APIs, cache financial data, or include Capacitor.

```powershell
npm ci
npm run dev
```

```powershell
npm run typecheck
npm run lint
npm run format:check
npm run test
npm run build
npm run test:e2e
npm run test:pwa
```

Do not start Platform API `:8091` or POS API `:8092` for this foundation. Do not modify MAUI, Organization Web, or POS backend from this client.
