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

printf 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"%s"};\n' "$url" > /tmp/exits-platform-admin-web-config.js