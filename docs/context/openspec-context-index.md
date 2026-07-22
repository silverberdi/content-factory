# OpenSpec Context Index

Read:

1. `docs/context/generated/current-context-pack.md` after a successful integrity check.
2. Project and current context.
3. Product requirements and architecture.
4. Delivery methodology and deviation policy.
5. Roadmap and backlog.
6. Active wave contract and execution plan.
7. Active wave User Story catalog only.
8. Decision register.
9. `AGENTS.md` and applicable Cursor governance/delivery rules.

Integrity check before implementation or review:

`node scripts/context/check-context-pack.mjs`

Regenerate:

`node scripts/context/generate-context-pack.mjs`

Generated context: `docs/context/generated/current-context-pack.md`.
Manifest: `docs/context/generated/context-manifest.json`.

Regenerate at each completed slice and wave. Do not inject all future wave contracts or all User
Story catalogs into the active context pack.
