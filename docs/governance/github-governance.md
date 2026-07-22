# GitHub Governance

Protect `main` and `wave/*`. Require PRs, current branches, required checks, resolved conversations,
and block force pushes/deletion.

- Slice → wave: squash merge; auto-merge after checks and `READY_TO_MERGE`.
- Wave → main: merge commit; Silverio manually merges after `READY_FOR_MAIN`.

Cursor and Codex provide technical cross-review evidence; a second formal bot identity is not
required in MVP.
