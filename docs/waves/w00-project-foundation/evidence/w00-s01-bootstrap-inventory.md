# w00-s01 Bootstrap Inventory

Date: 2026-07-22  
Change: `chg-w00-s01-repository-governance-and-openspec-foundation`  
Operator: `CURSOR`

## Project-owned candidates (adopt / correct)

| Artifact | Status at inventory |
|---|---|
| `AGENTS.md` | Present — hierarchy, branch model, Verify PASS, deviation, safety |
| `.cursor/rules/00-project-governance.mdc` | Present — context check gate |
| `.cursor/rules/30-delivery-evidence.mdc` | Present — closure / PR gates |
| `openspec/config.yaml` | Present — OpenSpec 1.6.0 project context/rules |
| `docs/methodology/delivery-methodology.md` | Present — adopted/corrected in this slice |
| `docs/methodology/deviation-policy.md` | Present — adopted/corrected in this slice |
| `docs/methodology/evidence-standard.md` | Present |
| `docs/governance/github-governance.md` | Present — adopted/corrected; S01 vs S04 split |
| `docs/context/openspec-context-index.md` | Present |
| `scripts/context/generate-context-pack.mjs` | Present — active-wave scoped |
| `scripts/context/check-context-pack.mjs` | Present — stale/drift detection |
| `scripts/context/validate-machine-ids.mjs` | Present |

## Generated OpenSpec integrations (immutable — validate only)

| Tree | Status |
|---|---|
| `.cursor/commands/opsx-*.md` | Present (propose/apply/verify/sync/archive + others) |
| `.cursor/skills/openspec-*` | Present |
| `.codex/skills/openspec-*` | Present |

**Rule:** Do not manually edit `.cursor/commands/`, `.cursor/skills/`, or `.codex/skills/`.
Regenerate only via official `openspec update` when required.

## Mapping to User Stories

- `us-w00-s01-001` — repository governance docs + GitHub protection evidence
- `us-w00-s01-002` — OpenSpec expanded verified workflow
- `us-w00-s01-003` — agent operating rules
- `us-w00-s01-004` — context pack automation
