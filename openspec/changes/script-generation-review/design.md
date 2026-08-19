# Design: Script Generation and Review Pipeline

## Context

Following the completion of TruthSource evidence extraction and ContentIdea matrix selection, the factory requires a structured, versioned, and AI-assisted workflow to transform creative ideas into approved short-form scripts. The system runs on ASP.NET Core (.NET 10 LTS) with PostgreSQL persistence, EF Core, and Angular 21 PWA (PrimeNG 21 + Tailwind CSS 4).

See `proposal.md` for business motivation and `specs/` for normative requirements.

## Goals / Non-Goals

**Goals:**
- Implement `Script`, `ScriptScene` (Beat), `ScriptSceneEvidenceReference`, and `ScriptVersion` domain entities with immutable lineage to `ContentItemId`, `ChannelId`, `ContentIdeaId`, `ContentIdeaVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.
- Support configurable speaking-rate pacing estimation (WPM) per channel/format, persisting effective `PacingWpm` on scripts and version snapshots while decoupling editorial duration estimates from future measured audio/TTS timings.
- Preserve lightweight factual claim traceability (`ScriptSceneEvidenceReference`) linking scene narration to approved TruthSource claims, keeping do-not-say constraints visible in the editor.
- Implement upstream lineage invalidation / stale-script semantics: detect when the selected idea or approved TruthSource version evolves, preserving historical scripts while gating downstream production until explicit editorial reconciliation.
- Implement advisory AI script synthesis (`generate_script`) and critique (`review_script`) via `IAiProviderRouter` with `AIRecommendation` telemetry, ensuring AI critique remains strictly advisory with human `EDITORIAL` approval authoritative.
- Implement an explicit, unambiguous review lifecycle: `Draft` -> `UnderReview` -> `Approved` OR `Rejected`, with an explicit `Rejected` -> `Reopen` -> `Draft` transition.
- Enforce full-spectrum optimistic concurrency (`expectedVersion: long`, HTTP 409 `CONCURRENCY_CONFLICT`) and `EDITORIAL` backend authorization.
- Automate `EditorialTask` (`ReviewScript`) lifecycle and surface pending script reviews in Dashboard Attention.
- Deliver an Angular 21 Script Studio with live duration meter, scene cards, claim reference tags, AI critique panel, stale reconciliation banner, version diff viewer, and approval/rejection/reopen action bar.

**Non-Goals:**
- Heavyweight citation infrastructure, knowledge graphs, or vector databases (factual references are minimal and deterministic).
- Visual generation, audio TTS synthesis, or video rendering (deferred to Wave 3 Production Pipeline).
- Direct platform upload or scheduling (deferred to Wave 4 Publication Operations).
- Speculative infrastructure: no external message brokers, Redis caches, or microservice splits.

## Decisions

### 1. Domain Entities & Database Schema

- **`Script`**:
  - `Id: Guid`
  - `ContentItemId: Guid` (FK to `ContentItem`)
  - `ChannelId: Guid` (FK to `Channel`)
  - `ContentIdeaId: Guid` (FK to `ContentIdea`)
  - `ContentIdeaVersionId: Guid` (Immutable lineage to exact idea version)
  - `TruthSourceId: Guid` (FK to `TruthSource`)
  - `TruthSourceVersionId: Guid` (Immutable lineage to exact approved TruthSource version)
  - `Title: string`
  - `TargetDurationSeconds: int` (Default: 45s, bounds: 30-60s)
  - `PacingWpm: int` (Effective words per minute used for estimation, resolved from channel configuration, default 140 WPM for `IA Simple ES`)
  - `EstimatedDurationSeconds: double` (Computed as `Math.Round((TotalWordCount / (PacingWpm / 60.0)), 1)`)
  - `TotalWordCount: int`
  - `Language: string` ("es-ES")
  - `Status: ScriptStatus` (`Draft`, `UnderReview`, `Approved`, `Rejected`)
  - `RejectionReason: string?`
  - `ApprovedAtUtc: DateTimeOffset?`, `ApprovedByEmail: string?`
  - `RejectedAtUtc: DateTimeOffset?`, `RejectedByEmail: string?`
  - `SubmittedForReviewAtUtc: DateTimeOffset?`, `SubmittedForReviewByEmail: string?`
  - `Version: long` (Optimistic concurrency token)
  - `CreatedAtUtc: DateTimeOffset`, `CreatedByEmail: string`
  - `UpdatedAtUtc: DateTimeOffset`, `UpdatedByEmail: string`

- **`ScriptScene`**:
  - `Id: Guid`
  - `ScriptId: Guid` (FK to `Script`)
  - `OrderIndex: int`
  - `SceneType: SceneType` (`Hook`, `Problem`, `Insight`, `Climax`, `CallToAction`)
  - `NarrationText: string` (Spoken words)
  - `VisualPrompt: string` (Visual cues, b-roll direction, on-screen text)
  - `EstimatedDurationSeconds: double`
  - `WordCount: int`
  - `EvidenceReferences: List<ScriptSceneEvidenceReference>`

- **`ScriptSceneEvidenceReference`**:
  - `Id: Guid`
  - `ScriptSceneId: Guid` (FK to `ScriptScene`)
  - `TruthSourceClaimId: Guid?` (Stable reference to approved TruthSource claim)
  - `ClaimStatement: string` (Verifiable claim text from TruthSource)
  - `EditorialNote: string?` (Concise editorial note or rationale)

- **`ScriptVersion`**:
  - `Id: Guid`
  - `ScriptId: Guid` (FK to `Script`)
  - `VersionNumber: long`
  - `SnapshotJson: string` (Full JSON serialization of script, scenes, claim references, and effective pacing)
  - `ChangeSummary: string`
  - `Status: ScriptStatus`
  - `RejectionReason: string?`
  - `PacingWpm: int`
  - `EstimatedDurationSeconds: double`
  - `TotalWordCount: int`
  - `CreatedByEmail: string`
  - `CreatedAtUtc: DateTimeOffset`

### 2. Configurable Speaking Rate & Pacing Semantics

- Spoken duration is an **editorial estimate**, strictly distinct from future measured audio/TTS waveforms.
- The speaking rate is configurable per channel and/or format (e.g. `TargetPacingWpm` on `Channel`, defaulting to 140 WPM for `IA Simple ES`).
- UI provides short-form guidance around **130-150 WPM** in Spanish.
- Changing channel WPM configuration does NOT require changing Script domain code.
- Effective `PacingWpm` is persisted on `Script` and in `ScriptVersion` snapshots so historical calculations remain understandable.
- Backend is authoritative for persisted aggregates; frontend executes identical formula live on keystroke:
  `Duration = Math.Round(WordCount / (PacingWpm / 60.0), 1)`.
- Pacing visual alerts:
  - `< 30s`: Amber warning (Under minimum short-form length).
  - `30s - 60s`: Green optimal (Standard YouTube Shorts / Reel range).
  - `> 60s`: Red warning (Exceeds 60-second platform hard cap).

### 3. Explicit Factual Lineage & Claims Model

- Minimal structured factual-reference model: `ScriptSceneEvidenceReference`.
- Connects specific scenes to approved `TruthSource` claims and verifiable statements.
- `generate_script` returns structured claim references for factual assertions in narration.
- Human editors can add, adjust, or remove narration and claim references in Script Studio.
- `review_script` inspects narration against those claim references and TruthSource claims/do-not-say constraints.
- Do-not-say constraints inherited from `TruthSource` are pinned in the Script Studio header.
- No AI-generated statement becomes factual authority merely because the model generated it.

### 4. Upstream Lineage Invalidation / Stale-Script Semantics

- A Script is evaluated for stale lineage deterministically:
  - `IsStale = (Script.ContentIdeaVersionId != ActiveSelectedIdea.CurrentVersionId) || (Script.TruthSourceVersionId != TruthSource.CurrentApprovedVersionId)`
- When upstream foundations change:
  - Historical Script and all `ScriptVersions` are preserved intact; lineage is never rewritten.
  - Script is marked as `Stale`/`Superseded` for downstream production.
  - Workspace and Script Studio display a prominent "Lineage Superseded / Reconciliation Required" banner.
  - Downstream production gates (storyboard/video rendering) require `Script.Status == Approved && !IsStale`.
  - Reconciling or regenerating a stale script produces a new `ScriptVersion` snapshot with updated lineage.

### 5. Unambiguous Review, Rejection & Reopen Lifecycle

- Lifecycle state machine:
  ```
  [Draft] --------(Submit for Review)-------> [UnderReview]
    ^                                            |       |
    |                                 (Approve)  |       | (Reject with reason)
    |                                            v       v
    +--------------(Reopen / Revise)----------- [Approved] [Rejected]
  ```
- Transitions:
  1. `Draft` -> `UnderReview`: Submits script for review, creates `EditorialTask` (`ReviewScript`).
  2. `UnderReview` -> `Approved`: Requires `EDITORIAL` role, verifies `TruthSource.Status == Approved` and `!IsStale`, advances parent `ContentItem` to `ScriptApproved`, completes task.
  3. `UnderReview` -> `Rejected`: Requires `EDITORIAL` role and non-empty `rejectionReason`, sets persistent `Rejected` state, records `ScriptVersion` snapshot with rejection notes, completes task.
  4. `Rejected` -> `Reopen` -> `Draft`: Explicit editorial action requiring `expectedVersion`, clears active rejection block, sets status to `Draft`, records `ScriptVersion` snapshot ("Reopened for revision"), preserves historical rejection evidence.
- Optimistic Concurrency: All transitions require `expectedVersion: long`. Mismatch returns HTTP 409 `CONCURRENCY_CONFLICT`.

### 6. Advisory AI Routing & Critique

- `generate_script`: Prompt receives approved TruthSource claims, constraints, and selected idea. Returns structured JSON with scenes, narration, visual prompts, and claim references.
- `review_script`: Advisory prompt evaluates claims, constraint compliance, hook retention (0-3s), and pacing. Returns structured JSON (`overallStatus` [`Pass`, `Warning`, `Critical`], factual score, retention analysis, scene notes, actionable recommendations).
- **Advisory Principle**: AI critique does NOT approve or reject scripts. Human `EDITORIAL` approval remains authoritative. Critical findings are prominently surfaced in UI, but the human operator decides whether to revise, approve, or reject.
- Telemetry: `AIRecommendation` records persisted with model, prompt-policy version, tokens, latency, and estimated cost without private chain-of-thought.

### 7. EditorialTask & Dashboard Attention Integration

- `IEditorialTaskService` handles `ReviewScript` task type.
- Transitioning a script to `UnderReview` creates an `EditorialTask` (`ReviewScript`).
- Transitioning to `Approved` or `Rejected` marks the pending `ReviewScript` task `Completed`.
- Dashboard Attention aggregates pending `ReviewScript` tasks and deep-links to `/content/{contentItemId}?tab=script`.

### 8. Angular 21 Script Studio Architecture

- Path: `src/web/src/app/features/content/`
- Components:
  - `script-studio.component.ts`: Root container displaying live stats, pacing meters, stale reconciliation banner, do-not-say constraints bar, scene breakdown, and decision action bar.
  - `script-scene-card.component.ts`: Individual scene card with scene type badge, duration meter, narration editor, visual prompt editor, and claim reference tags.
  - `generate-script-modal.component.ts`: Modal for AI script generation with tone and pacing preferences.
  - `script-review-panel.component.ts`: Advisory panel for AI critique findings (`Pass`/`Warning`/`Critical`) and suggestion cards.
  - `script-version-history-drawer.component.ts`: Drawer for inspecting previous `ScriptVersion` snapshots and comparing diffs.
  - `reject-script-modal.component.ts`: Modal requiring rejection feedback reason before completing rejection.

## Risks / Trade-offs

- **[Risk: AI script output hallucinating facts beyond TruthSource]** → *Mitigation: System prompt strictly confines AI to approved TruthSource claims; structured `ScriptSceneEvidenceReference` links claims; advisory `review_script` runs factual critique before human approval.*
- **[Risk: Stale script used in downstream video production after upstream idea/truth changes]** → *Mitigation: Deterministic stale-lineage evaluation blocks production gates if `ContentIdeaVersionId` or `TruthSourceVersionId` does not match active foundation.*
- **[Risk: Disconnect between editorial duration estimate and final audio timing]** → *Mitigation: Explicit domain boundary decoupling estimated WPM narration pacing from measured audio waveform duration in future Wave 3.*
- **[Risk: Concurrent edits overwriting editorial changes]** → *Mitigation: Mandatory `expectedVersion` concurrency token on all mutation endpoints with HTTP 409 conflict responses.*

## Migration Plan

1. Create PostgreSQL migration adding `scripts`, `script_scenes`, `script_scene_evidence_references`, and `script_versions` tables with foreign keys and indexes.
2. Update seed data with sample scripts in `Draft`, `UnderReview`, `Approved`, `Rejected`, and `Stale` states for channel `IA Simple ES`.
3. Apply migration to local development database `content_factory_dev`.
