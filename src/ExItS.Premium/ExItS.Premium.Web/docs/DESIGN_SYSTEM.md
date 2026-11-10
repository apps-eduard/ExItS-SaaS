# Design system

ExItS Premium is a Modern Solid interface for the Personal module. The reference is Apple’s level of finish, not a copy of Apple’s UI.

## Surfaces

- Page, surface, and elevated surface come from theme tokens.
- Borders are 1px. Elevation is five levels, 0 through 4. Level 0 is flat. Shadows stay short and are tuned separately for light and dark.
- Default corners are Soft. Pill rounds controls, and keeps cards, dialogs, and sheets practical.
- One primary palette colors interactive fills. Success, warning, danger, information, and Personal feature colors do not change with that palette.
- Warning fill stays `#ff5722`. Its text is `#1a0a00` because white fails contrast.
- Text on a fill needs a 4.5:1 contrast ratio. Status and feature ink on page and surface uses the same bar.

## Type

System font stack. Sizes use the tokens in `src/styles/tokens.scss`. Body text is 1rem. Do not set labels below the caption size.

## Icons

Material Symbols Rounded through `ExIcon`. See `ICON_STANDARDS.md`.

## Playground

`http://127.0.0.1:5180/design-system` shows foundations, components, and the sample personal concept. Colors and Elevation are the visual reference for later Personal work. The concept uses labeled sample data and does not call an API.
