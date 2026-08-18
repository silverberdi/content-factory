# Environments and Deployment

## Development

Workstation: macOS.

Frontend/backend run locally.

Authentication:
`development-bypass` GOD mode by default.

Database:
Ubuntu `content_factory_dev` on existing PostgreSQL instance (`192.168.0.194:5432`) reached directly over the trusted private LAN using scoped role `content_factory_app`. SSH access is administrative only. PostgreSQL is never publicly exposed.

n8n:
production-only; local development uses mocks by default.

## Production

Host: Ubuntu server.

Database:
`content_factory_prod` on existing PostgreSQL instance using scoped role `content_factory_app`.

Public access:
Cloudflare Tunnel.

Default hosts:
- `factory.silverman.pro`
- `factory-core.silverman.pro`

Internal:
- PostgreSQL
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
