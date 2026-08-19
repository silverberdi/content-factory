## 1. Domain Model, Database & Persistence

- [x] 1.1 Define `Script`, `ScriptScene`, `ScriptSceneEvidenceReference`, `ScriptVersion`, `ScriptStatus`, and `SceneType` entities, DTOs, and lifecycle states in `ContentModels.cs`.
- [x] 1.2 Configure EF Core entity relationships, table mappings, foreign keys, version concurrency tokens, and indexes in `AppDbContext`.
- [x] 1.3 Generate and apply PostgreSQL EF Core migration for `scripts`, `script_scenes`, `script_scene_evidence_references`, and `script_versions` tables.
- [x] 1.4 Implement `IScriptService` and `ScriptService` for script creation, scene/claim editing, configurable WPM duration calculation, stale-lineage evaluation, version snapshotting, and optimistic concurrency.
- [x] 1.5 Write backend unit tests for script entity invariants, configurable WPM duration calculation, claim reference integrity, stale lineage detection, and concurrency conflict handling.

## 2. AI Capabilities & Provider Routing

- [x] 2.1 Extend `IAiProviderRouter` and `AiProviderRouter` with `generate_script` and advisory `review_script` capabilities.
- [x] 2.2 Define Spanish short-form prompt policies (30-60s duration, configured channel WPM, hook retention 0-3s, visual prompts, TruthSource factual claim bounding and structured references).
- [x] 2.3 Implement deterministic mock generation and review adapters returning structured claim references and advisory critique (`Pass`/`Warning`/`Critical`).
- [x] 2.4 Integrate `AIRecommendation` telemetry recording for script generation and advisory critique (tokens, latency, estimated cost, prompt policy version, no chain-of-thought).
- [x] 2.5 Write unit tests for AI script generation, advisory critique schema parsing, claim reference extraction, and telemetry capture.

## 3. Editorial Workflow, Authorization & Tasks

- [x] 3.1 Implement script editorial review methods in `ScriptService` (`SubmitForReview`, `Approve`, `Reject` with mandatory reason, `Reopen` for revision, gating against unapproved TruthSource or stale lineage).
- [x] 3.2 Update `IEditorialTaskService` to create `ReviewScript` tasks on review submission and complete them on approval/rejection.
- [x] 3.3 Advance `ContentItem` lifecycle stages (`ScriptDrafted`, `ScriptUnderReview`, `ScriptApproved`) and update content summary projections.
- [x] 3.4 Implement REST API endpoints in `ContentControllers.cs` with explicit `EDITORIAL` authorization (`/api/v1/content-items/{id}/scripts`, `/generate`, `/review`, `/approve`, `/reject`, `/reopen`, `/history`).
- [x] 3.5 Write integration tests covering end-to-end script lifecycle, rejection and reopen workflows, concurrency conflicts (HTTP 409), authorization (HTTP 403), and validation.

## 4. Angular 21 Script Studio & UI

- [x] 4.1 Build `ScriptStudioComponent` in `src/web/src/app/features/content/` with live stats header (total words, live duration meter using channel configured WPM, 30-60s pacing meters, do-not-say constraints banner, stale lineage alert).
- [x] 4.2 Build scene/beat interactive editor (`ScriptSceneCardComponent`) with scene type badges, narration input, visual cues, claim reference tags, and reordering.
- [x] 4.3 Build `GenerateScriptModalComponent` for AI script generation with tone and pacing preferences.
- [x] 4.4 Build `ScriptReviewPanelComponent` for displaying advisory AI critique findings (`Pass`/`Warning`/`Critical`), claim verification flags, and suggestions.
- [x] 4.5 Build `ScriptVersionHistoryDrawerComponent` with version timeline, rejection notes, and snapshot diff comparison.
- [x] 4.6 Build `RejectScriptModalComponent` enforcing mandatory rejection feedback.
- [x] 4.7 Integrate Script Studio tab into `ContentDetailComponent`, action bar ("Submit for Review", "Approve", "Reject", "Reopen Script"), and update Dashboard Attention widget with direct deep-links.
- [x] 4.8 Write frontend unit tests for script studio interactions, configurable live duration calculations, claim reference rendering, and review/reopen workflows.

## 5. Seed Data, Human Verification & Documentation

- [x] 5.1 Update `SeedDataService` to seed sample scripts in `Draft`, `UnderReview`, `Approved`, `Rejected`, and `Stale` states for channel `IA Simple ES`.
- [x] 5.2 Execute automated test suite (`dotnet test` and `npm test`) and perform end-to-end responsive human verification in browser.
- [x] 5.3 Sync canonical documentation and run `/cf-verify-change` / DeepSeek cross-review before completion.

