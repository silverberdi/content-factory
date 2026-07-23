# Content Factory

Private multi-editorial-line AI audiovisual production platform.

Delivery hierarchy: `Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

## Current status

- First formal wave: `W00 — Project Foundation` (`IN_PROGRESS`)
- First formal slice: `W00-S01 — Repository Governance and OpenSpec Foundation` (`IN_PROGRESS`)
- Branches: `wave/w00-project-foundation`, `slice/w00-s01-repository-governance-and-openspec-foundation`
- OpenSpec: `1.6.0` installed with Cursor and Codex integrations generated
- No User Story or slice is completed until OpenSpec Verify is exactly `PASS` and the full
  slice contract is satisfied
- GitHub basic protection for `main` requires Silverio confirmation (see
  `docs/waves/w00-project-foundation/evidence/w00-s01-github-protection.md`)

Bootstrap artifacts already in the repository are pre-existing candidate implementation for
`chg-w00-s01-repository-governance-and-openspec-foundation`. They must be adopted, reviewed,
corrected, verified, synchronized, and archived through that change.

## Start here

1. Read `docs/context/current-state.md`.
2. Run `node scripts/context/check-context-pack.mjs`.
3. Read `docs/context/generated/current-context-pack.md`.
4. Read `AGENTS.md` and the active wave contract.
5. Execute only the active slice. Never start a later wave before the prior wave is `COMPLETED`.

## Context automation

```bash
node scripts/context/generate-context-pack.mjs
node scripts/context/check-context-pack.mjs
```

Generated output:

- `docs/context/generated/current-context-pack.md`
- `docs/context/generated/context-manifest.json`

Regenerate and validate at every completed slice and every completed wave. Do not hand-edit the
generated pack; correct canonical sources and regenerate.

## w00-s01 automated checks

```bash
node scripts/governance/run-w00-s01-checks.mjs
```

Includes machine-ID validation, doc/agent/OpenSpec workflow contracts, context-pack tests, and
integrity check.

## Preserved OpenSpec-generated surfaces

Do not replace or simplify these generated/integration surfaces outside their governing change:

- `openspec/config.yaml`
- `.cursor/commands/`
- `.cursor/skills/`
- `.codex/skills/`

## Canonical docs

- Roadmap: `docs/roadmap/roadmap.md`
- Backlog: `docs/backlog/backlog.md`
- Methodology: `docs/methodology/delivery-methodology.md`
- Wave contracts: `docs/waves/`
- File index: `FILE-INDEX.md`
- Package summary: `package-summary.json`
