---
name: deepseek-cross-review
description: Use after implementation to obtain an independent DeepSeek review of a Content Factory change against its OpenSpec artifacts and canonical constraints.
---


# Goal
Use DeepSeek as an independent second brain, not as a rubber stamp.

# Inputs
Provide:
- active change proposal/specs/design/tasks;
- relevant canonical constraints;
- code diff;
- test/build results.

# Run
Use `tools/deepseek-review/review.py` when `DEEPSEEK_API_KEY` is configured.

Example:
`git diff main...HEAD | python3 tools/deepseek-review/review.py --change foundation-access-control-center`

# Required review dimensions
- spec compliance;
- missing acceptance paths;
- domain invariant violations;
- security flaws;
- UX contract violations;
- unnecessary complexity;
- test gaps;
- scope drift.

# Output contract
The reviewer must end with:
READY_TO_MERGE
or
CHANGES_REQUIRED

Antigravity must independently validate findings against canonical context before applying them.

