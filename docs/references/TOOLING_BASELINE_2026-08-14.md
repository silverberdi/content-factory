# Tooling Baseline — 2026-08-14

This file is informational; canonical stack decisions live under `docs/canonical/`.

Verified baseline at package creation:

- OpenSpec latest release observed: 1.9.0.
- OpenSpec supports Antigravity tool initialization.
- OpenSpec requires Node.js 20.19+.
- Antigravity supports workspace rules/workflows and project Agent Skills.
- Current Google Antigravity codelabs document `.agents/` workspace customizations and `SKILL.md` Agent Skills.
- .NET 10 is current LTS through 2028.
- Angular/PrimeNG stable pairing selected: Angular 21 + PrimeNG 21.
- PrimeNG 22 was still presented as release-candidate content during verification, so it is not the baseline.
- Tailwind CSS 4 is the selected baseline.
- DeepSeek API is OpenAI-compatible; provider/model remains configuration.

Before dependency installation, Antigravity should confirm exact current stable patch versions from official package metadata and lock them.
It MUST NOT silently jump to an RC/preview major version.
