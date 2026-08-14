---
description: Run the full Content Factory completion gate for an active OpenSpec change.
---

Execute in this order:
1. OpenSpec validation.
2. Build/lint/static checks.
3. Automated backend/frontend tests.
4. Security/authorization negative tests when applicable.
5. Responsive human/browser verification at required viewports when frontend changed.
6. Light/dark verification.
7. Verify desktop full-page scroll policy.
8. Run `/opsx:verify <change>`.
9. Run DeepSeek cross-review using the project skill/tool.
10. Reconcile findings.
11. Verify canonical documentation synchronization.
12. Output only one final verdict:
   - READY_TO_HUMAN_TEST
   - BLOCKED
with evidence and unresolved items.
