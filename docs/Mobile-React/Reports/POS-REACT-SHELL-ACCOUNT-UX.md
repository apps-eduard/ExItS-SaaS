# POS React — Shell / Account UX polish

**Branch:** `feat/pos-react-client`  
**Status:** Complete (visual approval still awaiting product owner)  
**Scope:** Authenticated application shell, account menu, workspace context, Preferences controls.  
**Not in scope:** WP06 cash checkout, POST `/sales`, Capacitor, MAUI changes, WP renumbering.

## Delivered

- Product / workspace / signed-in user header layout
- Avatar initials (no profile-image API available) + accessible account menu
- Preferences and Sign out moved into the account menu
- Compact Language / Theme settings selects (segmented giant controls removed)
- Neutral no-workspace presentation (no error-like “No workspace selected” chrome)
- Sign-out contract preserved (`POST …/auth/logout` + PWEB-20 CSRF + session/workspace/cart/SellingMode clears)

## Evidence

Folder: [`impl-pos-react-shell-account-ux/`](./impl-pos-react-shell-account-ux/)

| # | File | Notes |
|---|------|-------|
| 01 | `01-desktop-shell-1440x900-en-light.png` | Desktop authenticated shell |
| 02 | `02-desktop-account-menu-1440x900-en-light.png` | Account menu open |
| 03 | `03-desktop-preferences-1440x900-en-light.png` | Preferences light / EN |
| 04 | `04-desktop-preferences-1440x900-en-dark.png` | Preferences dark / EN |
| 05 | `05-desktop-preferences-1440x900-fil-dark.png` | Preferences dark / fil-PH |
| 06 | `06-tablet-landscape-shell-1024x768-en-light.png` | Sell floor + compact shell |
| 07 | `07-tablet-portrait-shell-768x1024-en-light.png` | Tablet portrait |
| 08 | `08-phone-account-menu-375x812-en-light.png` | Phone account menu |
| 09 | `09-phone-shell-320x568-en-light.png` | Small phone |
| 10 | `10-sign-in-1440x900-en-light.png` | Sign-in brand consistency |

## Exclusions

- WP06 / cash checkout / POST sales
- Profile image upload/storage
- Invented workspace switching beyond existing `/workspace` chooser
- MAUI / Capacitor / Platform Admin feature work

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
