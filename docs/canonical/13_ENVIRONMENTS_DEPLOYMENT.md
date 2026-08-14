# Environments and Deployment

## Development

Workstation: macOS.

Frontend/backend run locally.

Authentication:
`development-bypass` GOD mode by default.

Database:
Ubuntu `content_factory_dev` reached through SSH tunnel.
MySQL is never publicly exposed.

n8n:
production-only; local development uses mocks by default.

## Production

Host: Ubuntu server.

Database:
`content_factory_prod` on existing MySQL instance.

Public access:
Cloudflare Tunnel.

Default hosts:
- `factory.silverman.pro`
- `factory-core.silverman.pro`

Internal:
- MySQL
- MinIO
- n8n internal endpoints/UI

## Runtime boundaries

Production MUST fail fast when:
- AUTH_MODE=development-bypass;
- required production auth secrets are absent;
- owner bootstrap integrity is invalid.

## Scale assumption

First-year expected concurrency < 10 users.
Optimize for correctness, simplicity and responsiveness rather than horizontal-scale infrastructure.

## Browser support

Modern evergreen browsers.
Primary validation:
- current Chrome;
- modern Edge;
- modern Safari including iOS/iPadOS.

No legacy-browser compatibility effort.
