# w00-s01 GitHub Protection — Evidence

**Status:** CONFIRMED  
**Repo:** https://github.com/silverberdi/content-factory  
**Date:** 2026-07-23  
**Operator:** Silverio (`silverberdi`) via `gh`  
**Silverio GO:** `Silverio GO — main basic protection verified` (applied and listed by Silverio)

## Ruleset created

- **ID:** `19631861`
- **Name:** `main-basic-protection-w00-s01`
- **Target:** branch `refs/heads/main`
- **Enforcement:** `active`
- **HTML:** https://github.com/silverberdi/content-factory/rules/19631861

## Rules evidenced

| Requirement | Evidence |
|---|---|
| `main` rejects deletion | rule `deletion` |
| `main` rejects force pushes | rule `non_fast_forward` |
| pull requests required for `main` | rule `pull_request` |
| slice PRs target `wave/*` | documented + draft PR #1 base `wave/w00-project-foundation` |
| wave PRs target `main` | documented in methodology / AGENTS / github-governance |
| Silverio merges waves to `main` | documented; wave→main remains manual |

## Out of scope (w00-s04)

GitHub Actions required checks, Nx CI validation, CI-driven merge gates, fully automated slice auto-merge.

## Verification command (re-check)

```bash
gh api repos/silverberdi/content-factory/rulesets/19631861
gh api repos/silverberdi/content-factory/rulesets
```

## Draft slice PR

https://github.com/silverberdi/content-factory/pull/1  
(base: `wave/w00-project-foundation`, draft, non-merge-eligible until remaining closure gates)
