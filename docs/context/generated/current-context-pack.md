# Generated Current Context Pack

> Do not edit manually.


---

## SOURCE: docs/context/project-context.md

# Project Context

Content Factory is a private multi-editorial-line AI audiovisual production platform.

It is fully independent from Avatares AI and the LinkedIn automator, although shared physical
infrastructure and providers may be reused under isolated product boundaries.

## Initial users

- `silverio.bernal@gmail.com`: technical and editorial administrator.
- `ltmoralesp84@gmail.com`: editorial administrator.

## Initial lines

1. AI music: roughly four-minute songs, lyrics, music, sung voice, music video, YouTube publication,
   derived promotions, and a manual distributor package.
2. Business/entrepreneurship shorts: researched vertical content for YouTube Shorts, Facebook Reels,
   Instagram Reels, and TikTok.

## Isolation

- Public URL: `studio.silverman.pro`.
- PostgreSQL database: `content_factory`.
- Exclusive MinIO bucket and credentials.
- n8n prefix `CF —`, tag `content-factory`, webhook prefix `/cf/`.
- No shared domain model, application, secrets, or storage namespace with unrelated products.

---

## SOURCE: docs/context/current-state.md

# Current State

## Lifecycle

- Repository lifecycle: `PRE_COMMIT_BOOTSTRAP_RECONCILED`
- Active wave state: `READY`
- Active slice state: `READY`
- No User Story is `COMPLETED`
- No slice is `COMPLETED`
- No wave is `COMPLETED`

## Active scope

| Field | Value |
|---|---|
| Active wave | `W00 — Project Foundation` |
| Active wave ID | `W00` |
| Active wave directory | `docs/waves/w00-project-foundation` |
| Active slice | `W00-S01 — Repository Governance and OpenSpec Foundation` |
| Active slice ID | `W00-S01` |
| Assigned implementer | `CURSOR` |
| Cross-reviewer | `CODEX` |
| Expected OpenSpec change | `CHG-W00-S01-repository-governance-and-openspec-foundation` |
| Expected wave branch | `wave/w00-project-foundation` |
| Expected slice branch | `slice/w00-s01-repository-governance-and-openspec-foundation` |

## Bootstrap facts

- Public repository is initialized and is not empty.
- Canonical project definition is imported.
- OpenSpec `1.6.0` is installed.
- Cursor and Codex OpenSpec integrations are generated.
- Present governance docs, rules, scripts, and indexes are pre-existing candidate implementation.
- Those candidates must be adopted, reviewed, corrected, verified, synchronized, and archived
  through `CHG-W00-S01-repository-governance-and-openspec-foundation`.

## Human validation

Required for functional, visual, or operational acceptance when the active wave or slice contract
says so. For `W00-S01`, human validation applies where governance/process acceptance cannot be
proven by automation alone; deploy/smoke/Silverio `GO` apply when the slice contract requires them.

## Explicit next action

1. Create wave branch `wave/w00-project-foundation` when formal W00 execution starts.
2. Create slice branch `slice/w00-s01-repository-governance-and-openspec-foundation` from the wave
   branch.
3. Create OpenSpec change `CHG-W00-S01-repository-governance-and-openspec-foundation`.
4. Adopt and correct the pre-existing bootstrap artifacts against US-W00-S01-001 through
   US-W00-S01-004 without marking any US completed until Verify is exactly `PASS` and the full
   slice contract is satisfied.

---

## SOURCE: docs/context/openspec-context-index.md

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

---

## SOURCE: docs/requirements/product-requirements.md

# Product Requirements

- Dynamic editorial lines and channels.
- One language per channel: Spanish, English, or Portuguese.
- Structured executable strategies with editorial review and technical approval.
- BETA and PRODUCTION channel modes.
- MANUAL, SCHEDULED, and AUTOMATIC publication modes.
- Automatic publication implemented but globally disabled by default.
- Platform-wide asset catalog with origin and ALLOWED/RESTRICTED AI usage.
- External research and verifiable source evidence when required.
- DeepSeek, ElevenLabs, and Comfy Cloud through capability abstractions.
- PostgreSQL canonical job state; n8n orchestration without long sleeping jobs.
- Budgets, hard limits, retries, idempotency, notifications, health, logs, and basic analytics.
- Real MVP publication and explicit human acceptance.
- No backups, cleanup/deletion, full audit, email, comments, advanced analytics, or multi-environment
  delivery until post-MVP.

---

## SOURCE: docs/architecture/architecture.md

# Architecture

## Monorepo

Nx monorepo with:

- `apps/console`: Angular 22 PWA.
- `apps/api`: NestJS with Fastify.
- `apps/media-worker`: Node/TypeScript FFmpeg worker.
- Capability-focused libraries with enforced Nx boundaries.

## UI

PrimeNG and PrimeIcons are mandatory. The UI is Spanish-first, translation-ready, responsive,
mobile-first, and accessible. It supports `light`, `dark`, and `system`; system mode respects
`prefers-color-scheme`, and user choice is persisted.

## Data and runtime

- Prisma with dedicated PostgreSQL database.
- Exclusive MinIO bucket.
- n8n as orchestrator; PostgreSQL as canonical job/schedule state.
- Docker containers; no host runtime installation.
- Cloudflare Tunnel routes `/` to the console and `/api/*` to the API.

---

## SOURCE: docs/methodology/delivery-methodology.md

# Delivery Methodology

## Hierarchy

`Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

- A wave delivers a major objective.
- A slice is the complete unit assigned to Cursor or Codex.
- User Stories remain the functional backlog units.
- One slice normally maps to one OpenSpec change.

## Sequence

No wave closes partially. No later wave starts before the previous wave is `COMPLETED`.

States: `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `VALIDATING`, `COMPLETED`.

## Operator model

One operator implements the whole slice; the other performs cross-review. Difficulty does not allow
reassignment. A genuine external blocker pauses the same operator.

## OpenSpec closure

A slice must have:

- OpenSpec Verify exactly `PASS`;
- automated checks `PASS`;
- acceptance criteria satisfied;
- review `READY_TO_MERGE`;
- documentation synchronized;
- context pack regenerated and validated;
- deployment, smoke tests, and Silverio `GO` when human validation applies;
- synchronized and archived OpenSpec change;
- no hidden deferred work.

## Git

- `slice/*` PRs target `wave/*` and may auto-merge after gates.
- `wave/*` PRs target `main`.
- Silverio manually merges waves into `main`.

---

## SOURCE: docs/methodology/deviation-policy.md

# Deviation Policy

On an unplanned requirement, dependency, or decision:

1. Stop affected execution.
2. Analyze impact.
3. Create or update the User Story.
4. Synchronize roadmap, backlog, User Stories, and affected wave contracts.
5. Create or update the OpenSpec change.
6. Validate consistency.
7. Resume with the same operator.

Do not hide a deviation as a note or debt to force closure.

---

## SOURCE: docs/methodology/evidence-standard.md

# Evidence Standard

Each slice records wave, slice, User Stories, implementer, reviewer, change ID, Verify PASS,
automated checks, acceptance evidence, deployment/smoke evidence, Silverio GO when applicable,
documentation/context hashes, PR result, and confirmation of zero hidden scope.

Each wave additionally records full integration checks, exit guarantees, snapshot manifest, and
one final state: `READY_FOR_MAIN`, `BLOCKED`, or `INCOMPLETE`.

---

## SOURCE: docs/roadmap/roadmap.md

# Roadmap

The roadmap is sequential and contract-driven.

| Wave | Name | Objective | Slices | State |
|---|---|---|---:|---|
| W00 | Project Foundation | Establish the governed monorepo, canonical documentation, OpenSpec workflow, CI, container foundations, and Angular/PrimeNG application shell. | 4 | READY |
| W01 | Identity, Access, and Session Security | Allow only approved Google identities to access the console with role-aware capabilities and secure browser/PWA session behavior. | 3 | PLANNED |
| W02 | Editorial Lines and Channel Foundations | Enable administrators to create editorial lines and exclusive external-account channel records with language, audience, brand, and lifecycle foundations. | 4 | PLANNED |
| W03 | Executable Channel Strategy | Create structured AI-assisted channel strategies that become executable only after editorial review and technical approval. | 3 | PLANNED |
| W04 | Asset Library and Controlled Reuse | Provide a secure platform-wide asset library with origin, rights, usage policy, validation, and MinIO-backed uploads. | 3 | PLANNED |
| W05 | Provider Abstractions, Research, and Evidence | Integrate interchangeable providers and evidence-aware research without pretending to browse or retaining unnecessary copyrighted material. | 3 | PLANNED |
| W06 | Jobs, Budgets, Reliability, and Notifications | Run persistent, resumable, budget-aware jobs with safe retries, dependency pauses, and role-specific PWA notifications. | 4 | PLANNED |
| W07 | Music Content Pipeline | Deliver a real AI music cycle from idea through a roughly four-minute song, visual output, review, scheduling, YouTube publication, and manual distributor package. | 4 | PLANNED |
| W08 | Business Short-Video Pipeline | Deliver evidence-aware, platform-adapted vertical business content from topic to real multi-platform publication. | 4 | PLANNED |
| W09 | Operations, Health, and Observability | Provide dependency health, selective pause/resume, safe logs, durable operational events, and basic consumption visibility. | 4 | PLANNED |
| W10 | MVP Hardening and Acceptance | Prove all agreed MVP exit criteria through real end-to-end operation, security, recovery, documentation, and human acceptance. | 4 | PLANNED |
| W11 | Post-MVP Delivery Evolution | Introduce explicitly deferred production-maturity capabilities without contaminating the MVP roadmap. | 4 | PLANNED |

MVP ends only after `W10`. `W11` is post-MVP and may not conceal incomplete MVP scope.

---

## SOURCE: docs/backlog/backlog.md

# Product Backlog

| User Story | Title | Slice | Primary Executor | State | OpenSpec Change |
|---|---|---|---|---|---|
| US-W00-S01-001 | Govern the repository through waves, slices, pull requests, and protected branches | W00-S01 | CURSOR | PLANNED | CHG-W00-S01-repository-governance-and-openspec-foundation |
| US-W00-S01-002 | Initialize OpenSpec with the expanded verified workflow | W00-S01 | CURSOR | PLANNED | CHG-W00-S01-repository-governance-and-openspec-foundation |
| US-W00-S01-003 | Provide canonical operating rules for Cursor and Codex | W00-S01 | CURSOR | PLANNED | CHG-W00-S01-repository-governance-and-openspec-foundation |
| US-W00-S01-004 | Generate and validate the current OpenSpec context pack | W00-S01 | CURSOR | PLANNED | CHG-W00-S01-repository-governance-and-openspec-foundation |
| US-W00-S02-001 | Create the Nx monorepo and enforce module boundaries | W00-S02 | CODEX | PLANNED | CHG-W00-S02-nx-application-foundation |
| US-W00-S02-002 | Create the Angular 22 PWA console foundation | W00-S02 | CODEX | PLANNED | CHG-W00-S02-nx-application-foundation |
| US-W00-S02-003 | Integrate PrimeNG, PrimeIcons, responsive layout, and accessible design tokens | W00-S02 | CODEX | PLANNED | CHG-W00-S02-nx-application-foundation |
| US-W00-S02-004 | Support light, dark, and system appearance modes | W00-S02 | CODEX | PLANNED | CHG-W00-S02-nx-application-foundation |
| US-W00-S02-005 | Create NestJS Fastify API and FFmpeg media-worker foundations | W00-S02 | CODEX | PLANNED | CHG-W00-S02-nx-application-foundation |
| US-W00-S03-001 | Define Docker Compose services without host runtime dependencies | W00-S03 | CURSOR | PLANNED | CHG-W00-S03-container-and-data-foundation |
| US-W00-S03-002 | Configure the dedicated PostgreSQL database boundary | W00-S03 | CURSOR | PLANNED | CHG-W00-S03-container-and-data-foundation |
| US-W00-S03-003 | Configure the dedicated MinIO bucket and credential boundary | W00-S03 | CURSOR | PLANNED | CHG-W00-S03-container-and-data-foundation |
| US-W00-S03-004 | Provide environment templates, health checks, and non-destructive deployment scripts | W00-S03 | CURSOR | PLANNED | CHG-W00-S03-container-and-data-foundation |
| US-W00-S04-001 | Run required GitHub Actions checks on pull requests | W00-S04 | CODEX | PLANNED | CHG-W00-S04-continuous-integration-and-quality-gates |
| US-W00-S04-002 | Validate OpenSpec, documentation, context, tests, builds, workflows, and secrets | W00-S04 | CODEX | PLANNED | CHG-W00-S04-continuous-integration-and-quality-gates |
| US-W00-S04-003 | Support cross-review evidence and slice auto-merge eligibility | W00-S04 | CODEX | PLANNED | CHG-W00-S04-continuous-integration-and-quality-gates |
| US-W00-S04-004 | Generate formal wave completion evidence before main integration | W00-S04 | CODEX | PLANNED | CHG-W00-S04-continuous-integration-and-quality-gates |
| US-W01-S01-001 | Sign in with Google using an approved allowlist | W01-S01 | CURSOR | PLANNED | CHG-W01-S01-google-oidc-and-allowlist |
| US-W01-S01-002 | Reject public registration and unauthorized identities | W01-S01 | CURSOR | PLANNED | CHG-W01-S01-google-oidc-and-allowlist |
| US-W01-S01-003 | Assign technical-editorial and editorial administrator roles | W01-S01 | CURSOR | PLANNED | CHG-W01-S01-google-oidc-and-allowlist |
| US-W01-S02-001 | Use secure HttpOnly cookie-based sessions | W01-S02 | CODEX | PLANNED | CHG-W01-S02-secure-session-lifecycle |
| US-W01-S02-002 | End normal browser sessions according to browser-session policy | W01-S02 | CODEX | PLANNED | CHG-W01-S02-secure-session-lifecycle |
| US-W01-S02-003 | Keep installed PWA sessions persistent and renewable | W01-S02 | CODEX | PLANNED | CHG-W01-S02-secure-session-lifecycle |
| US-W01-S03-001 | View and revoke personal active sessions | W01-S03 | CURSOR | PLANNED | CHG-W01-S03-session-administration-and-authorization |
| US-W01-S03-002 | Allow the technical administrator to revoke any session | W01-S03 | CURSOR | PLANNED | CHG-W01-S03-session-administration-and-authorization |
| US-W01-S03-003 | Enforce permissions in both UI and API | W01-S03 | CURSOR | PLANNED | CHG-W01-S03-session-administration-and-authorization |
| US-W02-S01-001 | Create and edit an editorial line | W02-S01 | CODEX | PLANNED | CHG-W02-S01-editorial-line-management |
| US-W02-S01-002 | Define an editorial line purpose and operating defaults | W02-S01 | CODEX | PLANNED | CHG-W02-S01-editorial-line-management |
| US-W02-S01-003 | List and inspect editorial lines | W02-S01 | CODEX | PLANNED | CHG-W02-S01-editorial-line-management |
| US-W02-S02-001 | Create a channel under one editorial line | W02-S02 | CURSOR | PLANNED | CHG-W02-S02-channel-domain-and-lifecycle |
| US-W02-S02-002 | Define platform, language, audience, and audience timezone | W02-S02 | CURSOR | PLANNED | CHG-W02-S02-channel-domain-and-lifecycle |
| US-W02-S02-003 | Enforce exclusive ownership of an external account | W02-S02 | CURSOR | PLANNED | CHG-W02-S02-channel-domain-and-lifecycle |
| US-W02-S02-004 | Operate BETA and PRODUCTION channel modes | W02-S02 | CURSOR | PLANNED | CHG-W02-S02-channel-domain-and-lifecycle |
| US-W02-S03-001 | Define channel name, proposition, tone, visual identity, and sonic identity | W02-S03 | CODEX | PLANNED | CHG-W02-S03-channel-brand-identity |
| US-W02-S03-002 | Request an AI-generated brand identity proposal | W02-S03 | CODEX | PLANNED | CHG-W02-S03-channel-brand-identity |
| US-W02-S03-003 | Edit and approve channel identity | W02-S03 | CODEX | PLANNED | CHG-W02-S03-channel-brand-identity |
| US-W02-S04-001 | Link channels across languages in an editorial family | W02-S04 | CURSOR | PLANNED | CHG-W02-S04-multilingual-editorial-families |
| US-W02-S04-002 | Define localized adaptation policy for linked channels | W02-S04 | CURSOR | PLANNED | CHG-W02-S04-multilingual-editorial-families |
| US-W02-S04-003 | Prevent duplicated editorial curation for approved adaptations | W02-S04 | CURSOR | PLANNED | CHG-W02-S04-multilingual-editorial-families |
| US-W03-S01-001 | Explicitly request an AI-generated channel strategy | W03-S01 | CURSOR | PLANNED | CHG-W03-S01-strategy-authoring |
| US-W03-S01-002 | Define objectives, audience, formats, cadence, research mode, budget, and publication policy | W03-S01 | CURSOR | PLANNED | CHG-W03-S01-strategy-authoring |
| US-W03-S01-003 | Edit a strategy as structured executable data | W03-S01 | CURSOR | PLANNED | CHG-W03-S01-strategy-authoring |
| US-W03-S02-001 | Move a strategy through draft, generated, reviewed, pending, approved, and active states | W03-S02 | CODEX | PLANNED | CHG-W03-S02-strategy-governance |
| US-W03-S02-002 | Require technical approval before activation | W03-S02 | CODEX | PLANNED | CHG-W03-S02-strategy-governance |
| US-W03-S02-003 | Require reapproval for material strategy changes | W03-S02 | CODEX | PLANNED | CHG-W03-S02-strategy-governance |
| US-W03-S03-001 | Define channel schedule windows and audience timezone | W03-S03 | CURSOR | PLANNED | CHG-W03-S03-calendar-seasons-and-special-dates |
| US-W03-S03-002 | Add and activate seasons, campaigns, and special dates | W03-S03 | CURSOR | PLANNED | CHG-W03-S03-calendar-seasons-and-special-dates |
| US-W03-S03-003 | Allow AI proposals without automatic activation | W03-S03 | CURSOR | PLANNED | CHG-W03-S03-calendar-seasons-and-special-dates |
| US-W04-S01-001 | Upload supported image, audio, video, subtitle, font, and document files | W04-S01 | CODEX | PLANNED | CHG-W04-S01-asset-upload-and-validation |
| US-W04-S01-002 | Apply type, MIME, size, batch, and aggregate limits | W04-S01 | CODEX | PLANNED | CHG-W04-S01-asset-upload-and-validation |
| US-W04-S01-003 | Use signed direct uploads for large assets | W04-S01 | CODEX | PLANNED | CHG-W04-S01-asset-upload-and-validation |
| US-W04-S01-004 | Reject unsafe files, executable content, and compressed packages | W04-S01 | CODEX | PLANNED | CHG-W04-S01-asset-upload-and-validation |
| US-W04-S02-001 | Catalog assets with origin, hash, language, type, creator, and timestamps | W04-S02 | CURSOR | PLANNED | CHG-W04-S02-asset-catalog-and-provenance |
| US-W04-S02-002 | Detect duplicate content by hash | W04-S02 | CURSOR | PLANNED | CHG-W04-S02-asset-catalog-and-provenance |
| US-W04-S02-003 | Search and inspect platform-wide assets | W04-S02 | CURSOR | PLANNED | CHG-W04-S02-asset-catalog-and-provenance |
| US-W04-S03-001 | Mark assets ALLOWED or RESTRICTED for automatic AI use | W04-S03 | CODEX | PLANNED | CHG-W04-S03-ai-usage-and-reuse-policy |
| US-W04-S03-002 | Keep restricted assets visible and manually selectable | W04-S03 | CODEX | PLANNED | CHG-W04-S03-ai-usage-and-reuse-policy |
| US-W04-S03-003 | Apply REUSE, ADAPT, then GENERATE_NEW decision policy | W04-S03 | CODEX | PLANNED | CHG-W04-S03-ai-usage-and-reuse-policy |
| US-W04-S03-004 | Preserve all generated and temporary assets during MVP | W04-S03 | CODEX | PLANNED | CHG-W04-S03-ai-usage-and-reuse-policy |
| US-W05-S01-001 | Configure providers by capability with primary and fallback positions | W05-S01 | CURSOR | PLANNED | CHG-W05-S01-provider-capability-registry |
| US-W05-S01-002 | Integrate DeepSeek for text and research-agent capabilities | W05-S01 | CURSOR | PLANNED | CHG-W05-S01-provider-capability-registry |
| US-W05-S01-003 | Integrate ElevenLabs and Comfy Cloud capability adapters | W05-S01 | CURSOR | PLANNED | CHG-W05-S01-provider-capability-registry |
| US-W05-S01-004 | Keep credentials outside Git and restrict provider administration | W05-S01 | CURSOR | PLANNED | CHG-W05-S01-provider-capability-registry |
| US-W05-S02-001 | Use internal-only, external, hybrid, or editorial-prompt research modes | W05-S02 | CODEX | PLANNED | CHG-W05-S02-research-modes-and-source-evidence |
| US-W05-S02-002 | Require external research for current or sensitive topics | W05-S02 | CODEX | PLANNED | CHG-W05-S02-research-modes-and-source-evidence |
| US-W05-S02-003 | Store source metadata, retrieval date, excerpts, evidence, and hashes | W05-S02 | CODEX | PLANNED | CHG-W05-S02-research-modes-and-source-evidence |
| US-W05-S02-004 | Prevent fabricated browsing claims | W05-S02 | CODEX | PLANNED | CHG-W05-S02-research-modes-and-source-evidence |
| US-W05-S03-001 | Apply configurable retention by evidence and publication status | W05-S03 | CURSOR | PLANNED | CHG-W05-S03-research-retention |
| US-W05-S03-002 | Preserve essential evidence while minimizing copied source material | W05-S03 | CURSOR | PLANNED | CHG-W05-S03-research-retention |
| US-W05-S03-003 | Expire raw provider and temporary fetched material on schedule | W05-S03 | CURSOR | PLANNED | CHG-W05-S03-research-retention |
| US-W06-S01-001 | Persist canonical job and step state in PostgreSQL | W06-S01 | CURSOR | PLANNED | CHG-W06-S01-persistent-job-orchestration |
| US-W06-S01-002 | Use n8n polling without long sleeping executions | W06-S01 | CURSOR | PLANNED | CHG-W06-S01-persistent-job-orchestration |
| US-W06-S01-003 | Maintain idempotency across retries and resumptions | W06-S01 | CURSOR | PLANNED | CHG-W06-S01-persistent-job-orchestration |
| US-W06-S02-001 | Configure monthly budgets by line and channel | W06-S02 | CODEX | PLANNED | CHG-W06-S02-budgets-and-cost-control |
| US-W06-S02-002 | Track estimated and actual cost by item, provider, and operation | W06-S02 | CODEX | PLANNED | CHG-W06-S02-budgets-and-cost-control |
| US-W06-S02-003 | Warn at configured thresholds and hard-block new paid work at the limit | W06-S02 | CODEX | PLANNED | CHG-W06-S02-budgets-and-cost-control |
| US-W06-S02-004 | Allow existing paid in-flight work to finish safely | W06-S02 | CODEX | PLANNED | CHG-W06-S02-budgets-and-cost-control |
| US-W06-S03-001 | Configure bounded retries with safe ranges | W06-S03 | CURSOR | PLANNED | CHG-W06-S03-retry-and-failure-handling |
| US-W06-S03-002 | Use exponential backoff and retryable versus terminal classification | W06-S03 | CURSOR | PLANNED | CHG-W06-S03-retry-and-failure-handling |
| US-W06-S03-003 | Escalate exhausted jobs for intervention | W06-S03 | CURSOR | PLANNED | CHG-W06-S03-retry-and-failure-handling |
| US-W06-S04-001 | Receive internal and PWA push notifications without email | W06-S04 | CODEX | PLANNED | CHG-W06-S04-notification-center-and-web-push |
| US-W06-S04-002 | Configure notification preferences by user and category | W06-S04 | CODEX | PLANNED | CHG-W06-S04-notification-center-and-web-push |
| US-W06-S04-003 | Separate technical and editorial notification categories | W06-S04 | CODEX | PLANNED | CHG-W06-S04-notification-center-and-web-push |
| US-W07-S01-001 | Create a music content idea under an active strategy | W07-S01 | CURSOR | PLANNED | CHG-W07-S01-song-planning-and-generation |
| US-W07-S01-002 | Generate lyrics, music, and sung voice through configured providers | W07-S01 | CURSOR | PLANNED | CHG-W07-S01-song-planning-and-generation |
| US-W07-S01-003 | Track lineage, cost, sources, and generated assets | W07-S01 | CURSOR | PLANNED | CHG-W07-S01-song-planning-and-generation |
| US-W07-S02-001 | Create LYRIC_VIDEO, VISUAL_MUSIC_VIDEO, or BOTH according to channel policy | W07-S02 | CODEX | PLANNED | CHG-W07-S02-music-video-assembly |
| US-W07-S02-002 | Assemble master media using the FFmpeg worker | W07-S02 | CODEX | PLANNED | CHG-W07-S02-music-video-assembly |
| US-W07-S02-003 | Create a clean reusable master without platform watermarks | W07-S02 | CODEX | PLANNED | CHG-W07-S02-music-video-assembly |
| US-W07-S03-001 | Accept the generated piece or request full AI reconstruction | W07-S03 | CURSOR | PLANNED | CHG-W07-S03-music-review-and-scheduling |
| US-W07-S03-002 | Schedule an accepted music release according to active strategy | W07-S03 | CURSOR | PLANNED | CHG-W07-S03-music-review-and-scheduling |
| US-W07-S03-003 | Prevent formal version retention for rejected reconstructions | W07-S03 | CURSOR | PLANNED | CHG-W07-S03-music-review-and-scheduling |
| US-W07-S04-001 | Publish a scheduled music video to YouTube with idempotency | W07-S04 | CODEX | PLANNED | CHG-W07-S04-youtube-and-distribution-package |
| US-W07-S04-002 | Generate and validate a manual distributor package | W07-S04 | CODEX | PLANNED | CHG-W07-S04-youtube-and-distribution-package |
| US-W07-S04-003 | Explain distributor purpose and record manual status and URL | W07-S04 | CODEX | PLANNED | CHG-W07-S04-youtube-and-distribution-package |
| US-W07-S04-004 | Allow download only for the distribution package | W07-S04 | CODEX | PLANNED | CHG-W07-S04-youtube-and-distribution-package |
| US-W08-S01-001 | Propose or submit a business topic | W08-S01 | CODEX | PLANNED | CHG-W08-S01-topic-research-and-script |
| US-W08-S01-002 | Select STRUCTURE_FIRST, DIRECT_BUILD, or STRATEGY_DECIDES | W08-S01 | CODEX | PLANNED | CHG-W08-S01-topic-research-and-script |
| US-W08-S01-003 | Research externally when required and generate a standalone script | W08-S01 | CODEX | PLANNED | CHG-W08-S01-topic-research-and-script |
| US-W08-S01-004 | Recommend duration within configured bounds | W08-S01 | CODEX | PLANNED | CHG-W08-S01-topic-research-and-script |
| US-W08-S02-001 | Generate narration and visual assets | W08-S02 | CURSOR | PLANNED | CHG-W08-S02-voice-visuals-and-assembly |
| US-W08-S02-002 | Assemble a vertical short through the media worker | W08-S02 | CURSOR | PLANNED | CHG-W08-S02-voice-visuals-and-assembly |
| US-W08-S02-003 | Validate technical quality and brand coherence | W08-S02 | CURSOR | PLANNED | CHG-W08-S02-voice-visuals-and-assembly |
| US-W08-S03-001 | Create complete autonomous adaptations for each selected destination | W08-S03 | CODEX | PLANNED | CHG-W08-S03-platform-adaptation |
| US-W08-S03-002 | Allow series identity while keeping every item standalone | W08-S03 | CODEX | PLANNED | CHG-W08-S03-platform-adaptation |
| US-W08-S03-003 | Avoid mechanical identical cross-platform copies | W08-S03 | CODEX | PLANNED | CHG-W08-S03-platform-adaptation |
| US-W08-S04-001 | Publish to YouTube Shorts, Facebook Reels, Instagram Reels, and TikTok | W08-S04 | CURSOR | PLANNED | CHG-W08-S04-real-multi-platform-publication |
| US-W08-S04-002 | Respect MANUAL, SCHEDULED, and AUTOMATIC publication modes | W08-S04 | CURSOR | PLANNED | CHG-W08-S04-real-multi-platform-publication |
| US-W08-S04-003 | Evaluate BETA or PRODUCTION mode at publication time | W08-S04 | CURSOR | PLANNED | CHG-W08-S04-real-multi-platform-publication |
| US-W08-S04-004 | Track partial success, retries, URLs, and external IDs | W08-S04 | CURSOR | PLANNED | CHG-W08-S04-real-multi-platform-publication |
| US-W09-S01-001 | View API, database, MinIO, n8n, provider, worker, and Cloudflare health | W09-S01 | CURSOR | PLANNED | CHG-W09-S01-dependency-health-console |
| US-W09-S01-002 | Show status, latency, sanitized errors, impact, action, and last check | W09-S01 | CURSOR | PLANNED | CHG-W09-S01-dependency-health-console |
| US-W09-S01-003 | Avoid paid generations during health checks | W09-S01 | CURSOR | PLANNED | CHG-W09-S01-dependency-health-console |
| US-W09-S02-001 | Pause affected operations by dependency | W09-S02 | CODEX | PLANNED | CHG-W09-S02-selective-pause-and-recovery |
| US-W09-S02-002 | Resume automatically when safe | W09-S02 | CODEX | PLANNED | CHG-W09-S02-selective-pause-and-recovery |
| US-W09-S02-003 | Recalculate missed schedules while respecting budget and idempotency | W09-S02 | CODEX | PLANNED | CHG-W09-S02-selective-pause-and-recovery |
| US-W09-S03-001 | Configure structured logs by level and category | W09-S03 | CURSOR | PLANNED | CHG-W09-S03-logging-and-operational-events |
| US-W09-S03-002 | Retain technical logs for one to seven days | W09-S03 | CURSOR | PLANNED | CHG-W09-S03-logging-and-operational-events |
| US-W09-S03-003 | Prevent secrets, tokens, cookies, and sensitive bodies from being logged | W09-S03 | CURSOR | PLANNED | CHG-W09-S03-logging-and-operational-events |
| US-W09-S03-004 | Retain durable operational events in PostgreSQL | W09-S03 | CURSOR | PLANNED | CHG-W09-S03-logging-and-operational-events |
| US-W09-S04-001 | View publication result, URL, and external IDs | W09-S04 | CODEX | PLANNED | CHG-W09-S04-basic-operational-analytics |
| US-W09-S04-002 | View basic interactions when APIs permit | W09-S04 | CODEX | PLANNED | CHG-W09-S04-basic-operational-analytics |
| US-W09-S04-003 | View cost and consumption by item, channel, and provider | W09-S04 | CODEX | PLANNED | CHG-W09-S04-basic-operational-analytics |
| US-W09-S04-004 | View generation and publication errors | W09-S04 | CODEX | PLANNED | CHG-W09-S04-basic-operational-analytics |
| US-W10-S01-001 | Verify authorization, session revocation, secrets, and public exposure controls | W10-S01 | CURSOR | PLANNED | CHG-W10-S01-security-and-governance-hardening |
| US-W10-S01-002 | Verify strategy approval and automatic-publication safeguards | W10-S01 | CURSOR | PLANNED | CHG-W10-S01-security-and-governance-hardening |
| US-W10-S01-003 | Verify minimum operational trace requirements | W10-S01 | CURSOR | PLANNED | CHG-W10-S01-security-and-governance-hardening |
| US-W10-S02-001 | Execute the complete real music cycle | W10-S02 | CODEX | PLANNED | CHG-W10-S02-end-to-end-music-acceptance |
| US-W10-S02-002 | Produce and validate the manual distribution package | W10-S02 | CODEX | PLANNED | CHG-W10-S02-end-to-end-music-acceptance |
| US-W10-S02-003 | Obtain Silverio GO for music MVP acceptance | W10-S02 | CODEX | PLANNED | CHG-W10-S02-end-to-end-music-acceptance |
| US-W10-S03-001 | Execute the complete real business-short cycle | W10-S03 | CURSOR | PLANNED | CHG-W10-S03-end-to-end-business-acceptance |
| US-W10-S03-002 | Publish successfully to all four agreed destinations | W10-S03 | CURSOR | PLANNED | CHG-W10-S03-end-to-end-business-acceptance |
| US-W10-S03-003 | Obtain Silverio GO for business MVP acceptance | W10-S03 | CURSOR | PLANNED | CHG-W10-S03-end-to-end-business-acceptance |
| US-W10-S04-001 | Validate every MVP exit criterion | W10-S04 | CODEX | PLANNED | CHG-W10-S04-mvp-completion-and-handoff |
| US-W10-S04-002 | Generate immutable completion snapshot and operational handoff | W10-S04 | CODEX | PLANNED | CHG-W10-S04-mvp-completion-and-handoff |
| US-W10-S04-003 | Mark deferred post-MVP capabilities explicitly without hiding debt | W10-S04 | CODEX | PLANNED | CHG-W10-S04-mvp-completion-and-handoff |
| US-W11-S01-001 | Introduce development, staging, and production environments | W11-S01 | CURSOR | PLANNED | CHG-W11-S01-multi-environment-delivery |
| US-W11-S01-002 | Add controlled deployment through GitHub Environments | W11-S01 | CURSOR | PLANNED | CHG-W11-S01-multi-environment-delivery |
| US-W11-S02-001 | Automate PostgreSQL and MinIO backups | W11-S02 | CODEX | PLANNED | CHG-W11-S02-backup-and-recovery |
| US-W11-S02-002 | Test restoration and recovery objectives | W11-S02 | CODEX | PLANNED | CHG-W11-S02-backup-and-recovery |
| US-W11-S03-001 | Add automated end-to-end suites | W11-S03 | CURSOR | PLANNED | CHG-W11-S03-advanced-testing-and-observability |
| US-W11-S03-002 | Introduce centralized logs and dashboards | W11-S03 | CURSOR | PLANNED | CHG-W11-S03-advanced-testing-and-observability |
| US-W11-S03-003 | Add a dedicated integration testing section | W11-S03 | CURSOR | PLANNED | CHG-W11-S03-advanced-testing-and-observability |
| US-W11-S04-001 | Add controlled asset cleanup and deletion | W11-S04 | CODEX | PLANNED | CHG-W11-S04-lifecycle-and-analytics-evolution |
| US-W11-S04-002 | Add advanced comparative analytics | W11-S04 | CODEX | PLANNED | CHG-W11-S04-lifecycle-and-analytics-evolution |
| US-W11-S04-003 | Evaluate direct distributor integrations | W11-S04 | CODEX | PLANNED | CHG-W11-S04-lifecycle-and-analytics-evolution |

Operators receive slices, not individual User Stories.

---

## SOURCE: docs/decisions/decision-register.md

# Decision Register

| ID | Decision |
|---|---|
| ADR-001 | Content Factory is an independent product. |
| ADR-002 | Nx monorepo. |
| ADR-003 | Angular 22 PWA with PrimeNG and PrimeIcons. |
| ADR-004 | Light, dark, and system modes from W00. |
| ADR-005 | NestJS Fastify API and Prisma/PostgreSQL. |
| ADR-006 | Exclusive MinIO bucket and isolated n8n conventions. |
| ADR-007 | PostgreSQL is canonical job state. |
| ADR-008 | Containerized FFmpeg worker. |
| ADR-009 | Google OIDC closed allowlist and secure cookie sessions. |
| ADR-010 | Wave → Slice → US → OpenSpec tasks. |
| ADR-011 | Cursor/Codex receive complete slices and cross-review each other. |
| ADR-012 | OpenSpec Verify PASS is mandatory. |
| ADR-013 | Human validation requires deployment and Silverio GO. |
| ADR-014 | Slice auto-merge; wave manual merge. |
| ADR-015 | Context regenerated per slice; immutable snapshot per wave. |
| ADR-016 | GitHub Actions is part of W00 CI, not MVP deployment. |

---

## SOURCE: docs/governance/github-governance.md

# GitHub Governance

Protect `main` and `wave/*`. Require PRs, current branches, required checks, resolved conversations,
and block force pushes/deletion.

- Slice → wave: squash merge; auto-merge after checks and `READY_TO_MERGE`.
- Wave → main: merge commit; Silverio manually merges after `READY_FOR_MAIN`.

Cursor and Codex provide technical cross-review evidence; a second formal bot identity is not
required in MVP.

---

## SOURCE: AGENTS.md

# Content Factory Agent Contract

Applies to Codex and all coding agents operating in this repository, including Cursor.

## Hierarchy

`Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

- A wave delivers one major objective and cannot close partially.
- A slice is the complete implementation and integration unit assigned to one operator.
- User Stories remain the functional backlog units inside the slice.
- Tasks are derived and maintained inside the OpenSpec change for that slice.
- One slice normally maps to one OpenSpec change.

## Start of every implementation or review

1. Run the context integrity check:
   `node scripts/context/check-context-pack.mjs`
2. Read `docs/context/generated/current-context-pack.md`.
3. Read the active wave contract, active slice assignment, included User Stories, and
   `AGENTS.md`.
4. Use the assigned OpenSpec change as the implementation contract.

If the context check fails, regenerate with `node scripts/context/generate-context-pack.mjs`,
re-check, and only then proceed.

## Complete-slice ownership

- Own one complete assigned slice end to end.
- Never divide a User Story or slice across implementers.
- Never abandon a slice because it is difficult.
- Difficulty is not a reason to reassign work.
- Stop only for a real external blocker; pause under the same operator.
- Never implement future-wave scope.
- Never hide deferred acceptance criteria, notes, or informal debt to force closure.

## Mandatory cross-review

- The non-implementing operator performs mandatory cross-review.
- Cross-review verdict is exactly `READY_TO_MERGE` or `CHANGES_REQUIRED`.
- Resolve all `CHANGES_REQUIRED` findings before merge eligibility.

## Branch and PR model

- `main` represents the last fully completed wave.
- `wave/*` branches integrate all slices of one wave.
- `slice/*` branches are created from the active wave branch.
- Slice pull requests target `wave/*`.
- Wave pull requests target `main`.
- Never push directly to protected wave branches or `main`.

## Slice auto-merge rules

- Slice → wave may auto-merge after required checks pass and cross-review is `READY_TO_MERGE`.
- Prefer squash merge for slice PRs into the wave branch.

## Wave manual merge rule

- Wave → `main` requires Silverio's manual merge after wave completion evidence is
  `READY_FOR_MAIN`.
- Prefer merge commit for wave PRs into `main`.

## Deviation synchronization procedure

When an unplanned requirement, dependency, decision, or blocker appears:

1. Stop affected execution.
2. Analyze impact.
3. Create or update the User Story.
4. Synchronize roadmap, backlog, User Stories, and affected wave contracts.
5. Create or update the OpenSpec change.
6. Validate consistency.
7. Resume with the same operator.

Do not hide a deviation as a note or debt.

## OpenSpec Verify

- Verify must be exactly `PASS`.
- `PASS WITH NOTES` is forbidden and is not closure.

## Deployment, smoke tests, and Silverio GO

When the wave or slice contract requires human validation:

1. Deploy safely and non-destructively.
2. Run health checks and smoke tests.
3. Provide clear test instructions for Silverio.
4. Obtain explicit Silverio `GO` before closure.

## Sync and archive

Synchronize docs and archive the OpenSpec change only after the whole slice contract is satisfied:

- acceptance criteria satisfied;
- automated checks `PASS`;
- OpenSpec Verify exactly `PASS`;
- cross-review `READY_TO_MERGE`;
- documentation synchronized;
- context pack regenerated and validated;
- deployment, smoke tests, and Silverio `GO` when applicable;
- no blockers or hidden deferred work.

## Mandatory context regeneration

Regenerate and validate the current context pack at every completed slice and every completed wave:

- `node scripts/context/generate-context-pack.mjs`
- `node scripts/context/check-context-pack.mjs`

Canonical generated path: `docs/context/generated/current-context-pack.md`.

## Evidence and completion report

Record wave, slice, User Stories, implementer, reviewer, change ID, Verify `PASS`, automated
checks, acceptance evidence, deployment/smoke evidence, Silverio `GO` when applicable,
documentation/context hashes, PR result, and confirmation of zero hidden scope.

Wave completion reports use exactly one final state: `READY_FOR_MAIN`, `BLOCKED`, or `INCOMPLETE`.

## Safety and repository integrity

- Never expose or commit secrets.
- Preserve `.env`, volumes, PostgreSQL data, MinIO objects, and n8n configuration.
- Never use destructive reset, volume deletion, or unsafe schema commands.
- Never delegate repository file edits to Silverio when the agent can perform them.

---

## SOURCE: .cursor/rules/00-project-governance.mdc

---
description: Mandatory project governance
alwaysApply: true
---

Read `docs/context/generated/current-context-pack.md`, the active wave contract, `AGENTS.md`, and
the assigned OpenSpec change.

Before starting implementation or review, run the context integrity check:

`node scripts/context/check-context-pack.mjs`

If the check fails, regenerate with `node scripts/context/generate-context-pack.mjs`, re-check, and
only then continue.

Use official OpenSpec `/opsx-*` commands generated by the installed version. Create and evolve
proposal, design, specs, and tasks through OpenSpec in Cursor.

Work only on the complete assigned slice. No future-wave scope. Verify must be exactly `PASS`.
Use `pbcopy` for long requested terminal outputs when appropriate. Do not ask Silverio to edit
files manually.

---

## SOURCE: .cursor/rules/30-delivery-evidence.mdc

---
description: Closure, evidence, deployment, and PR rules
alwaysApply: true
---

Before starting implementation or review, run:

`node scripts/context/check-context-pack.mjs`

Read `docs/context/generated/current-context-pack.md`.

A slice is incomplete until acceptance, tests, Verify exactly `PASS`, synchronized docs/context,
`READY_TO_MERGE` cross-review, required deployment/smoke/Silverio GO, archived change, and complete
PR evidence. Slice PRs target wave branches. Never push directly to protected wave branches or main.

---

## SOURCE: docs/waves/w00-project-foundation/contract.md

# W00 — Project Foundation Contract

## Objective

Establish the governed monorepo, canonical documentation, OpenSpec workflow, CI, container foundations, and Angular/PrimeNG application shell.

## Prerequisite

Proven before W00 formal execution:

- Public Content Factory repository initialized (not empty).
- Canonical project definition imported into the repository.
- OpenSpec `1.6.0` installed.
- Cursor and Codex OpenSpec integrations generated (commands and skills present).

The wave refuses to start when the prerequisite is not proven.

## Bootstrap note

Canonical planning docs, agent governance files, context scripts, and related repository
artifacts already present in the tree are **pre-existing candidate implementation**. They are not
completed W00 delivery. They must be adopted, reviewed, corrected, verified, synchronized, and
archived through `CHG-W00-S01-repository-governance-and-openspec-foundation`. No User Story or
slice is marked completed by the presence of these bootstrap artifacts.

## Wave state

`READY`

## Slices


### W00-S01 — Repository Governance and OpenSpec Foundation

- State: `READY`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W00-S01-repository-governance-and-openspec-foundation`
- User Stories: `US-W00-S01-001`, `US-W00-S01-002`, `US-W00-S01-003`, `US-W00-S01-004`
- Branch: `slice/w00-s01-repository-governance-and-openspec-foundation`

### W00-S02 — Nx Application Foundation

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W00-S02-nx-application-foundation`
- User Stories: `US-W00-S02-001`, `US-W00-S02-002`, `US-W00-S02-003`, `US-W00-S02-004`, `US-W00-S02-005`
- Branch: `slice/w00-s02-nx-application-foundation`

### W00-S03 — Container and Data Foundation

- State: `PLANNED`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W00-S03-container-and-data-foundation`
- User Stories: `US-W00-S03-001`, `US-W00-S03-002`, `US-W00-S03-003`, `US-W00-S03-004`
- Branch: `slice/w00-s03-container-and-data-foundation`

### W00-S04 — Continuous Integration and Quality Gates

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W00-S04-continuous-integration-and-quality-gates`
- User Stories: `US-W00-S04-001`, `US-W00-S04-002`, `US-W00-S04-003`, `US-W00-S04-004`
- Branch: `slice/w00-s04-continuous-integration-and-quality-gates`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer | State |
|---|---|---|---|---|
| W00-S01 | CHG-W00-S01-repository-governance-and-openspec-foundation | CURSOR | CODEX | READY |
| W00-S02 | CHG-W00-S02-nx-application-foundation | CODEX | CURSOR | PLANNED |
| W00-S03 | CHG-W00-S03-container-and-data-foundation | CURSOR | CODEX | PLANNED |
| W00-S04 | CHG-W00-S04-continuous-integration-and-quality-gates | CODEX | CURSOR | PLANNED |

## Exclusions

- No future-wave implementation.
- No direct merge to `main`.
- No hidden deferred acceptance criteria.
- No manual file-editing work delegated to Silverio.

## Human validation

Required where acceptance is functional/visual/operational.

## Exit contract

- All User Stories completed.
- All OpenSpec Verify results exactly `PASS`.
- All planned changes synchronized and archived.
- Required builds, lint, type checks, tests, and contract checks pass.
- Documentation and context pack synchronized.
- Cross-review `READY_TO_MERGE`.
- Deployment, smoke tests, and Silverio GO completed when applicable.
- No blockers or hidden deferred work.
- Completion report state `READY_FOR_MAIN`.

## Guarantees to the next wave

The objective is operational and verified; the next wave may depend on its published contracts.

---

## SOURCE: docs/waves/w00-project-foundation/execution-plan.md

# W00 Execution Plan

Wave branch: `wave/w00-project-foundation`

Wave state: `READY`

## Ordered slices

1. `W00-S01` — Repository Governance and OpenSpec Foundation — `CURSOR` — state `READY`
2. `W00-S02` — Nx Application Foundation — `CODEX` — state `PLANNED`
3. `W00-S03` — Container and Data Foundation — `CURSOR` — state `PLANNED`
4. `W00-S04` — Continuous Integration and Quality Gates — `CODEX` — state `PLANNED`

## Bootstrap adoption

Existing repository governance docs, agent rules, context automation, and related bootstrap
artifacts are pre-existing candidate implementation for `W00-S01`. Formal adoption, correction,
verification, synchronization, and archival happen only through
`CHG-W00-S01-repository-governance-and-openspec-foundation`. No User Story is completed yet.

Parallel execution is allowed only when context validation confirms no dependency, file, module,
migration, schema, or contract collision.

---

## SOURCE: docs/backlog/user-stories/w00-user-stories.md

# W00 — Project Foundation User Stories


## US-W00-S01-001 — Govern the repository through waves, slices, pull requests, and protected branches

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to govern the repository through waves, slices, pull requests, and protected branches, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S01-002 — Initialize OpenSpec with the expanded verified workflow

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to initialize OpenSpec with the expanded verified workflow, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S01-003 — Provide canonical operating rules for Cursor and Codex

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to provide canonical operating rules for Cursor and Codex, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S01-004 — Generate and validate the current OpenSpec context pack

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to generate and validate the current OpenSpec context pack, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S02-001 — Create the Nx monorepo and enforce module boundaries

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S02-nx-application-foundation`

As an authorized Content Factory user, I want to create the Nx monorepo and enforce module boundaries, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S02-002 — Create the Angular 22 PWA console foundation

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S02-nx-application-foundation`

As an authorized Content Factory user, I want to create the Angular 22 PWA console foundation, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S02-003 — Integrate PrimeNG, PrimeIcons, responsive layout, and accessible design tokens

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S02-nx-application-foundation`

As an authorized Content Factory user, I want to integrate PrimeNG, PrimeIcons, responsive layout, and accessible design tokens, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S02-004 — Support light, dark, and system appearance modes

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S02-nx-application-foundation`

As an authorized Content Factory user, I want to support light, dark, and system appearance modes, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S02-005 — Create NestJS Fastify API and FFmpeg media-worker foundations

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S02-nx-application-foundation`

As an authorized Content Factory user, I want to create NestJS Fastify API and FFmpeg media-worker foundations, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S03-001 — Define Docker Compose services without host runtime dependencies

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S03-container-and-data-foundation`

As an authorized Content Factory user, I want to define Docker Compose services without host runtime dependencies, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S03-002 — Configure the dedicated PostgreSQL database boundary

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S03-container-and-data-foundation`

As an authorized Content Factory user, I want to configure the dedicated PostgreSQL database boundary, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S03-003 — Configure the dedicated MinIO bucket and credential boundary

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S03-container-and-data-foundation`

As an authorized Content Factory user, I want to configure the dedicated MinIO bucket and credential boundary, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S03-004 — Provide environment templates, health checks, and non-destructive deployment scripts

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W00-S03-container-and-data-foundation`

As an authorized Content Factory user, I want to provide environment templates, health checks, and non-destructive deployment scripts, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S04-001 — Run required GitHub Actions checks on pull requests

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to run required GitHub Actions checks on pull requests, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S04-002 — Validate OpenSpec, documentation, context, tests, builds, workflows, and secrets

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to validate OpenSpec, documentation, context, tests, builds, workflows, and secrets, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S04-003 — Support cross-review evidence and slice auto-merge eligibility

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to support cross-review evidence and slice auto-merge eligibility, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W00-S04-004 — Generate formal wave completion evidence before main integration

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W00-S04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to generate formal wave completion evidence before main integration, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.
