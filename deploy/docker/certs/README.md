# TLS certificate mount directory (P14-WP03)

Place operator-owned TLS files here on the **deployment host** (or any path referenced by
`PRODUCTION_TLS_CERT_DIR`). Never commit real certificates or private keys to git.

Required filenames (match `nginx/production.conf`):

- `fullchain.pem` — certificate + intermediate chain
- `privkey.pem` — private key (mode 0600 recommended on host)

Renewal is environment-owned (certbot, ACME client, or enterprise PKI). After renewal,
reload or recreate the `reverse-proxy` container so nginx picks up new files.

This repository ships **templates only**. Automatic public certificate issuance is **not**
implemented or claimed by P14-WP03.
