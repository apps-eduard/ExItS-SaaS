#!/bin/sh
set -eu

url="${PLATFORM_API_PUBLIC_URL:-}"
if [ -z "$url" ]; then
  echo "PLATFORM_API_PUBLIC_URL is required for Platform Admin Web." >&2
  exit 1
fi

case "$url" in
  http://*|https://*)
    ;;
  *)
    echo "PLATFORM_API_PUBLIC_URL must be an http(s) origin." >&2
    exit 1
    ;;
esac

case "$url" in
  *['\"`$\;']*)
    echo "PLATFORM_API_PUBLIC_URL contains disallowed characters." >&2
    exit 1
    ;;
esac

tools_enabled=false
case "${LOCAL_VALIDATION_TOOLS_ENABLED:-}" in
  true|TRUE|True|1)
    tools_enabled=true
    ;;
esac

printf 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"%s",localValidationToolsEnabled:%s};\n' "$url" "$tools_enabled" > /tmp/exits-platform-admin-web-config.js