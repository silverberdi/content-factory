# Proposal: Script Generation and Review Pipeline

## Why

Following the implementation of TruthSource evidence curation and ContentIdea matrix selection, the next required milestone in Wave 1 (CF-012/CF-013) is transforming approved creative ideas into production-ready short-form scripts. Currently, operators can select a creative idea, but have no structured, versioned, or AI-assisted capability to draft scenes/beats, evaluate duration and retention pacing against configurable speaking rates, track lightweight factual claims against the approved TruthSource, run advisory AI factual/quality critique, or manage an unambiguous review/rejection/reopen lifecycle with strict editorial gates.

This change delivers the end-to-end `Script` domain aggregate, configurable speaking-rate pacing estimation, lightweight factual claim tracing (`ScriptSceneEvidenceReference`), advisory AI script synthesis (`generate_script`) and critique (`review_script`), immutable version lineage (`ScriptVersion`), upstream stale-lineage invalidation, explicit `Draft` -> `UnderReview` -> `Approved` / `Rejected` -> `Reopen` lifecycle, `EditorialTask` integration (`ReviewScript`), and a high-density Angular 21 Script Studio.

## What Changes

- **Domain Model & Persistence**:
  - Introduce `Script`, `ScriptScene` (Beat), `ScriptSceneEvidenceReference`, and `ScriptVersion` domain entities.
  - Record immutable lineage to `ContentItemId`, `ChannelId`, `ContentIdeaId`, `ContentIdeaVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.
  - Implement lightweight factual traceability (`ScriptSceneEvidenceReference`) linking scene narration claims to approved `TruthSource` claim/evidence identifiers with concise editorial notes. Do-not-say constraints inherited from `TruthSource` are preserved and displayed.
  - Implement configurable speaking rate (WPM) estimation: resolve pacing from channel/format configuration (default 140 WPM for `IA Simple ES`, with UI guidance for 130-150 WPM), persist effective `PacingWpm` on `Script` and in `ScriptVersion` snapshots, and maintain clear separation from future measured audio/TTS durations.
  - Implement upstream lineage invalidation / stale-script semantics: if the parent `ContentItem`'s selected `ContentIdea` changes or a newer approved `TruthSourceVersion` supersedes the script's foundation, the existing script is preserved but flagged as `Stale`/`Superseded` and blocked from downstream production gates until explicitly reconciled or regenerated.
  - Implement unambiguous rejection and reopen lifecycle:
    - `Draft` -> `UnderReview` -> `Approved` (with approved TruthSource verification);
    - `Draft` -> `UnderReview` -> `Rejected` (with mandatory rejection reason, timestamp, actor, and immutable snapshot);
    - `Rejected` -> `Reopen` -> `Draft` (explicit transition requiring `expectedVersion`, clearing active rejection block while preserving historical rejection audit).
  - Enforce full-spectrum optimistic concurrency (`expectedVersion: long`, HTTP 409 `CONCURRENCY_CONFLICT`).
- **AI Router & Advisory Capabilities**:
  - Implement `generate_script` capability producing structured Spanish short-form scripts (target 30-60s) with scenes (Hook 0-3s, Problem, Insight, Climax, CTA), visual prompts, estimated duration using configured WPM, and structured claim references linking to the approved `TruthSource`.
  - Implement `review_script` advisory capability performing factual critique against TruthSource claims, constraint adherence, hook retention, and pacing.
  - Clarify that AI critique is strictly advisory (`Pass`, `Warning`, `Critical` with prominent visual cues) and has NO authority to approve or reject scripts; human `EDITORIAL` decision remains authoritative.
  - Record auditable `AIRecommendation` telemetry for all script AI invocations (token usage, latency, cost, prompt-policy version, without private chain-of-thought).
  - Seed deterministic development mock adapters for offline local execution.
- **Editorial Tasks & Dashboard Attention**:
  - Auto-create `EditorialTask` of type `ReviewScript` when a script transitions to `UnderReview`.
  - Auto-complete pending `ReviewScript` tasks upon script approval or rejection.
  - Integrate script attention counts and contextual deep-links into Dashboard Attention widgets.
- **Content Workspace Progression**:
  - Advance `ContentItem` lifecycle stages: `ScriptDrafted`, `ScriptUnderReview`, `ScriptApproved`.
  - Display Script summary metrics, stale/reconciliation alerts, and status badges in Content Workspace cards and detail navigation.
- **Angular 21 Script Studio & UI**:
  - High-density Script Studio inside Content Detail: scene/beat breakdown, live word count, live spoken duration meter using channel configured WPM, hook retention cues, visual prompts, and do-not-say constraints banner.
  - Factual claim reference tags on scene cards linking directly to TruthSource claims.
  - AI Script Generation dialog and advisory AI Critique review panel with actionable suggestions.
  - Stale lineage reconciliation banner when upstream idea or TruthSource version has evolved.
  - Version history drawer with diff comparison between revisions.
  - Review action bar: "Submit for Review", "Approve", "Reject" (with mandatory reason modal), and "Reopen Script" for rejected drafts.
- **Security & Authorization**:
  - Backend explicit authorization requiring `EDITORIAL` role (or development GOD mode) for all script mutations, reviews, and reopenings.

## Capabilities

### New Capabilities
- `script-editorial-pipeline`: Complete `Script` entity, scene/beat structure, factual claim references (`ScriptSceneEvidenceReference`), configurable speaking-rate pacing, immutable lineage to selected `ContentIdeaVersion` and approved `TruthSourceVersion`, stale lineage detection, AI generation (`generate_script`), advisory AI critique (`review_script`), `ScriptVersion` snapshots, optimistic concurrency, `Draft`/`UnderReview`/`Approved`/`Rejected`/`Reopen` lifecycle, backend authorization, and Angular 21 Script Studio.

### Modified Capabilities
- `content-workspace`: Extend `ContentItem` lifecycle stages (`ScriptDrafted`, `ScriptUnderReview`, `ScriptApproved`), expose script summary & stale reconciliation status in content cards/detail view, and enforce downstream production gating against unapproved or stale scripts.
- `editorial-task-attention`: Add `ReviewScript` task type, automatic task lifecycle synchronization on script review status transitions, and deep-linking into Script Review Studio from Dashboard Attention.

## Impact

- **Backend**:
  - New EF Core entities: `Script`, `ScriptScene`, `ScriptSceneEvidenceReference`, `ScriptVersion`.
  - New interfaces and services: `IScriptService`, `IScriptReviewService`.
  - Updated `IAiProviderRouter` with `generate_script` and `review_script` capabilities and prompt policies.
  - Updated `IEditorialTaskService` and `IContentService`.
  - PostgreSQL database migration for script tables and indexes.
  - New REST API controller: `/api/v1/content-items/{id}/scripts`.
- **Frontend**:
  - New components in `src/web/src/app/features/content/`:
    - `script-studio.component.ts` (main editing & review studio)
    - `script-scene-card.component.ts` (individual scene/beat editor with claim references)
    - `generate-script-modal.component.ts` (AI generation configuration)
    - `script-review-panel.component.ts` (advisory AI critique and editorial notes)
    - `script-version-history-drawer.component.ts` (version snapshot viewer & diff)
    - `reject-script-modal.component.ts` (mandatory rejection feedback)
  - Updated `ContentDetailComponent` tab navigation and script status badges.
  - Updated `DashboardComponent` attention counters.
- **Tests & Seeds**:
  - Unit and integration test suites for script lifecycle, configurable WPM calculations, factual reference integrity, stale lineage detection, rejection and reopen transitions, concurrency conflicts, AI mock generation/critique, task auto-creation/completion, and authorization.
  - Seed data with pre-configured scripts in `Draft`, `UnderReview`, `Approved`, `Rejected`, and `Stale` states for channel `IA Simple ES`.
