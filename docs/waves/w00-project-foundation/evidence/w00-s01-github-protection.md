# w00-s01 GitHub Protection — Silverio Verification Steps

**Status:** PENDING Silverio confirmation  
**Repo:** https://github.com/silverberdi/content-factory  
**Why:** Branch protection / ruleset APIs require authentication. Local environment has no
`gh` CLI and no `GH_TOKEN`/`GITHUB_TOKEN`. Public probe shows repository rulesets = `[]`.
Protection details for `main` cannot be read or claimed without authenticated verification.

## Required outcomes (w00-s01)

1. `main` rejects direct pushes
2. `main` rejects force pushes
3. `main` rejects deletion
4. Pull requests are required for `main`
5. Documented model: slice PRs target `wave/*`; wave PRs target `main`
6. Silverio manually merges completed waves to `main`

**Out of scope for this slice (w00-s04):** GitHub Actions required checks, Nx CI validation,
CI-driven merge gates, fully automated slice auto-merge.

## Option A — GitHub UI

1. Open https://github.com/silverberdi/content-factory/settings/rules
2. Create or edit a ruleset targeting branch `main` (or classic branch protection on `main`):
   - Restrict deletions: **on**
   - Block force pushes: **on**
   - Require a pull request before merging: **on**
   - Do **not** require status checks yet (that is `w00-s04`)
3. Confirm direct push to `main` is blocked for non-admin bypass (or disable admin bypass if
   desired for strictness).
4. Confirm docs already state slice→`wave/*` and wave→`main` (no GitHub setting required beyond
   process + protection of `main`).
5. Reply in the slice PR or here with: `Silverio GO — main basic protection verified` and paste
   a screenshot or ruleset summary.

## Option B — GitHub CLI (after `brew install gh && gh auth login`)

```bash
# Create a ruleset for main (adjust IDs if a ruleset already exists)
gh api repos/silverberdi/content-factory/rulesets \
  --method POST \
  --input - <<'EOF'
{
  "name": "main-basic-protection-w00-s01",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews": false,
        "require_code_owner_reviews": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false
      }
    }
  ],
  "bypass_actors": []
}
EOF

# Verify
gh api repos/silverberdi/content-factory/rulesets
gh api repos/silverberdi/content-factory/branches/main/protection || true
```

## Option C — Verify existing classic branch protection

```bash
gh api repos/silverberdi/content-factory/branches/main/protection
```

Expect: `allow_force_pushes.enabled=false`, `allow_deletions.enabled=false`, and
`required_pull_request_reviews` (or equivalent ruleset PR rule) present.

## Evidence to attach

- CLI JSON output **or** UI screenshot of ruleset/protection
- Explicit line: `Silverio GO — GitHub basic protection for main confirmed on <date>`

Until that confirmation is recorded, `w00-s01` must **not** claim GitHub protection acceptance.
