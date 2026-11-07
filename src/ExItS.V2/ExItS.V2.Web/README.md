# ExItS V2 Web

Independent premium design-system and home foundation for ExItS.

This project is **separate** from PinoyBusinessPOS React (`:5177`). It does not call production APIs and uses mock data only.

## Stack

- React + TypeScript + Vite
- Tailwind CSS v4
- shadcn-style primitives (Radix UI)
- Lucide React
- Motion for React
- i18next (English only during development)

## Ports

| Mode | Port | Host |
|------|------|------|
| Dev | **5180** | `127.0.0.1` |
| Preview | **4180** | `127.0.0.1` |

Existing POS remains on `5177` / `4177`.

## Launch

```powershell
cd src\ExItS.V2\ExItS.V2.Web
npm install
npm run dev
```

Open [http://127.0.0.1:5180](http://127.0.0.1:5180)

### Other scripts

```powershell
npm run build
npm run preview
npm run lint
```

## Routes

| Route | Purpose |
|-------|---------|
| `/` | Home dashboard (mock) |
| `/design-system` | Component and token gallery |

## Appearance

Light / Dark only (default **Light**). Preference is stored in `localStorage` under `exits-v2-appearance`.

A Sun/Moon toggle in the top bar switches themes immediately on desktop and mobile. Legacy `"system"` values are resolved once from the OS preference and rewritten as `"light"` or `"dark"`.

Primary color pickers are intentionally not implemented yet.

## Language

English only. i18n keys are wired for future Tagalog, Cebuano, and other Philippine languages. Language selection UI is hidden during development.

## Isolation notes

- Lives under `src/ExItS.V2/ExItS.V2.Web`
- Does not modify the existing PinoyBusinessPOS frontend
- Does not change backend, database, or API contracts
- POS / Personal entry cards are placeholders for later V2 work packages
