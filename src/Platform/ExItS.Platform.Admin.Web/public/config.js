window.__EXITS_PLATFORM_ADMIN_WEB__ = Object.assign(
  window.__EXITS_PLATFORM_ADMIN_WEB__ || {},
  {
    // Local Validation Vite: browser calls /api on :8095; Vite proxies to Platform API :8091.
    // Works for http://localhost:8095 and http://<tailscale>:8095 without direct :8091 access.
    platformApiSameOrigin: true,
  },
);
