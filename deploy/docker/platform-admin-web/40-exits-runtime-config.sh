#!/bin/sh
set -eu

same_origin=false
case "${PLATFORM_API_SAME_ORIGIN:-}" in
  true|TRUE|True|1)
    same_origin=true
    ;;
esac

url="${PLATFORM_API_PUBLIC_URL:-}"
if [ "$same_origin" = true ]; then
  url=""
else
  if [ -z "$url" ]; then
    echo "PLATFORM_API_PUBLIC_URL is required for Platform Admin Web unless PLATFORM_API_SAME_ORIGIN=true." >&2
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
fi

tools_enabled=false
case "${LOCAL_VALIDATION_TOOLS_ENABLED:-}" in
  true|TRUE|True|1)
    tools_enabled=true
    ;;
esac

build_sha="${EXITS_GIT_SHA:-${VITE_BUILD_SHA:-unknown}}"
if ! printf '%s' "$build_sha" | grep -Eq '^[A-Za-z0-9._-]+$'; then
  build_sha="unknown"
fi

web_origin() {
  value="${1:-}"
  case "$value" in
    http://*|https://*)
      ;;
    *)
      printf ''
      return
      ;;
  esac
  case "$value" in
    *['\"`$\;']*)
      printf ''
      return
      ;;
  esac
  printf '%s' "${value%/}"
}

org_origin="$(web_origin "${ORGANIZATION_WEB_ORIGIN:-}")"
personal_origin="$(web_origin "${PERSONAL_WEB_ORIGIN:-}")"

printf 'window.__EXITS_PLATFORM_ADMIN_WEB__={app:"Platform Admin React",platformApiBaseUrl:"%s",platformApiSameOrigin:%s,localValidationToolsEnabled:%s,buildSha:"%s",organizationWebOrigin:"%s",personalWebOrigin:"%s"};\n' \
  "$url" "$same_origin" "$tools_enabled" "$build_sha" "$org_origin" "$personal_origin" > /tmp/exits-platform-admin-web-config.js

proxy_target="${PLATFORM_API_PROXY_TARGET:-}"
if [ -n "$proxy_target" ]; then
  case "$proxy_target" in
    http://*|https://*)
      ;;
    *)
      echo "PLATFORM_API_PROXY_TARGET must be an http(s) origin." >&2
      exit 1
      ;;
  esac
  case "$proxy_target" in
    *['\"`$\;']*)
      echo "PLATFORM_API_PROXY_TARGET contains disallowed characters." >&2
      exit 1
      ;;
  esac
  # /api/ for authenticated Platform calls; /health(+ /ready) for dashboard Platform readiness
  # (must not hit SPA index.html or this container's /nginx-health).
  # A literal proxy_pass host is resolved once at nginx start. Recreating platform-api
  # then leaves Admin on a stale address, so /api answers 404 and the login page stays blank.
  printf 'resolver 127.0.0.11 valid=10s ipv6=off;\nset $exits_platform_api_upstream %s;\nlocation /api/ {\n    proxy_pass $exits_platform_api_upstream;\n    proxy_http_version 1.1;\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n    proxy_set_header Cookie $http_cookie;\n    proxy_pass_header Set-Cookie;\n    client_max_body_size 10m;\n}\nlocation = /health {\n    proxy_pass $exits_platform_api_upstream;\n    proxy_http_version 1.1;\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n}\nlocation = /health/ready {\n    proxy_pass $exits_platform_api_upstream;\n    proxy_http_version 1.1;\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n}\n' "$proxy_target" > /tmp/exits-api-proxy.conf
else
  printf '# no API reverse proxy\n' > /tmp/exits-api-proxy.conf
fi
