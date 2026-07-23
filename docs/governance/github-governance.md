# GitHub Governance

## Basic protection (w00-s01)

Establish and evidence actual GitHub repository protection for:

- `main` rejects direct pushes;
- `main` rejects force pushes and deletion;
- pull requests are required for `main`;
- `wave/*` follows the slice-to-wave pull-request model;
- slice PRs target `wave/*`;
- wave PRs target `main`;
- Silverio manually merges completed waves to `main`.

If applying or verifying these settings requires Silverio authorization, prepare exact CLI/UI
steps and record Silverio confirmation. Do not claim protection exists without evidence.

## CI automation (w00-s04 — out of scope for w00-s01)

The following remain assigned to `w00-s04` and must not be claimed as delivered by `w00-s01`:

- GitHub Actions required checks on pull requests;
- Nx validation in CI;
- CI-driven merge gates;
- fully automated slice auto-merge.

## Merge preferences

- Slice → wave: squash merge; auto-merge eligibility after checks and `READY_TO_MERGE` once
  `w00-s04` automation exists.
- Wave → main: merge commit; Silverio manually merges after `READY_FOR_MAIN`.

Cursor and Codex provide technical cross-review evidence; a second formal bot identity is not
required in MVP.
