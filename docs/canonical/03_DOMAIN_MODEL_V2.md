# Domain Model v2

## Aggregate concepts

### ContentItem

Operational root for one editorial production thread.

A ContentItem links:
- one or more source/discovery candidates;
- one TruthSource;
- one or more ContentIdeas;
- scripts;
- storyboard;
- production jobs;
- assets;
- canonical video master;
- platform publications;
- metric snapshots;
- audit timeline.

The operator must be able to answer "where is this piece?" from ContentItem.

### Source / DiscoveryCandidate

Unified representation regardless of origin:
- manually submitted URL;
- RSS/feed;
- DeepSeek suggestion;
- Gemini suggestion;
- future provider.

Mandatory provenance:
- origin;
- URL/identifier where applicable;
- language;
- source type;
- discovered timestamp;
- provider/manual actor;
- relevance/freshness/risk evidence.

### TruthSource

Approved editorial evidence layer derived from one or more Sources.

Contains:
- summary;
- key ideas;
- verifiable claims;
- evidence references;
- risk notes;
- do-not-say constraints;
- possible angles;
- localization/adaptation notes.

No script may become approved without an approved TruthSource.

### ContentIdea

Editorial transformation between evidence and script.

Contains:
- angle;
- audience value;
- format;
- hook strategy;
- intended outcome;
- freshness class;
- priority;
- channel;
- rationale;
- immutable lineage to exact approved TruthSourceVersionId;
- version and optimistic concurrency token.

Invariants:
- A ContentItem may accumulate multiple Proposed or Dismissed ideas, but at most ONE idea may be the active Selected idea for scripting at a time.
- Selecting a new idea atomically reverts the previously selected idea to Proposed, recording version snapshots for both.
- Every mutation (Update, Select, Dismiss, Reopen) produces an immutable ContentIdeaVersion snapshot.
- Deterministic application-level duplicate and near-duplicate similarity filtering prevents equivalent proposals from accumulating.

### Script

Versioned editorial script derived from the active Selected ContentIdea and approved TruthSource.

Contains:
- ordered `ScriptScene` collection (Hook, Problem, Insight, Climax, CTA) with narration text and visual prompts;
- lightweight `ScriptSceneEvidenceReference` linking verifiable claims to approved TruthSource claims;
- configurable speaking-rate pacing (`PacingWpm`, default 140 WPM for `IA Simple ES`, 130-150 WPM guidance);
- estimated spoken duration aggregate (decoupled from future measured TTS/audio duration);
- immutable lineage to exact `ContentIdeaVersionId` and `TruthSourceVersionId`;
- dynamic upstream staleness evaluation (`IsStale` when upstream idea or TruthSource version evolves);
- explicit human review lifecycle (`Draft` → `UnderReview` → `Approved` / `Rejected`, and `Reopen` → `Draft`);
- mandatory rejection reason and audit history;
- advisory AI critique (`review_script`) where human approval remains authoritative;
- immutable `ScriptVersion` snapshot history and full-spectrum optimistic concurrency.

### Storyboard & AssetPlan

Visual production specification and provider-agnostic asset planning derived from the approved Script.

Contains:
- ordered `StoryboardFrame` collection (vertical 9:16 format) capturing framing intent, composition notes, camera motion intent, subject, environment, visual style, visual prompt, negative prompt, audio cues, on-screen text, frame estimated duration, and transition intent;
- explicit linkage of every `StoryboardFrame` to its originating `ScriptScene`;
- embedded `AssetPlan` specifying WHAT media is needed across categories (AiImage, AiVideo, BRoll, GraphicOverlay, TtsVoiceover, BackgroundMusic, SoundEffect, SubtitleTrack) without encoding runtime provider execution parameters (workflows, samplers, checkpoints);
- lightweight planning lifecycle (`Planned`, `ReadyForGeneration`), strictly decoupled from production execution states (generating, generated, failed, MinIO references, cost/runtime measurements);
- single editorial gate: approving a Storyboard approves the exact `AssetPlan` captured in that `StoryboardVersion` snapshot;
- immutable lineage to `ScriptId`, `ScriptVersionId`, `TruthSourceId`, and `TruthSourceVersionId`;
- successor reconciliation creating a successor draft derived from new approved script versions while archiving predecessor (`IsCurrent = false`, `SupersededAtUtc`, `ReconciledFromStoryboardId`);
- strictly enforced "One Current Storyboard" invariant per ContentItem;
- downstream production gating precondition (`IsCurrent == true`, `Status == Approved`, `IsStale == false`, `AssetPlan.Status == ReadyForGeneration`).

### EditorialTask

Human-action item, not an inbox message.

Examples:
- review truth source;
- review script;
- review video;
- approve publication;
- resolve operational failure.

Fields include assignment, type, priority, status, due/freshness deadline and ContentItem.

### Job

Asynchronous work unit:
- ingestion;
- AI generation;
- visual generation;
- audio;
- render;
- publication;
- backup/archive.

States:
queued, running, succeeded, failed-retryable, failed-action-required, cancelled.

A Job records:
provider, model/tool, attempt, duration, cost, correlation id and sanitized error.

### Publication

Platform-specific immutable record of what was actually published.

### MetricSnapshot

Time-stamped normalized metrics for a publication.

### AIRecommendation

Auditable recommendation generated by any reasoning provider.

Store:
- capability;
- provider/model;
- policy/prompt version;
- structured recommendation;
- score/confidence if applicable;
- rationale/evidence summary;
- accepted/rejected outcome.

Never store private chain-of-thought.

### Experiment

Introduced later in analytics waves.
Tracks hypothesis, variants, primary metrics, result and decision.

## Core invariants

- Published lineage is immutable.
- Corrections create versions/derivatives.
- One ContentItem may have multiple Sources.
- No script approval without approved TruthSource.
- No video approval without approved script.
- No publication without approved video.
- Every rejection records reason.
- Every AI/human material edit is attributable.
- Backend is authoritative across all devices.
- Optimistic concurrency protects concurrent edits.
