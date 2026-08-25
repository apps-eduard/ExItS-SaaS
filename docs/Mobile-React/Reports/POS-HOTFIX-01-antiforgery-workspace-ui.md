# POS-HOTFIX-01 — Antiforgery mutation failure + workspace UI regression

**Branch:** `feat/pos-react-client`  
**Package:** POS-HOTFIX-01  
**Status:** Complete

## Root cause

`platform-http.ts` (commit `16014681`) treated antiforgery bootstrap **403/404** as `"unavailable"` and continued cookie-session **mutations without `X-XSRF-TOKEN`**. The Local Validation Platform API enforces browser antiforgery for cookie callers; mutations then failed with `platform.antiforgery.invalid`.

## Fix

- Client fail-closed bootstrap; one stale-token retry in shared `platformRequest`
- Logout no longer treats generic **403** as already signed out
- Platform API browser antiforgery middleware + `GET /api/v1/platform/antiforgery/token` wired on branch worktree
- Workspace/sign-out errors mapped to i18n (no raw backend antiforgery text in UI)

## Live validation

Owner flow against `http://127.0.0.1:5177` + Platform `:8091`: login → workspace bind (Main Branch + second branch) → logout → login again.
