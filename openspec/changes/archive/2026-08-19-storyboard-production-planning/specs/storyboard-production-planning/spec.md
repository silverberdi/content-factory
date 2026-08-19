## Purpose

Defines the `Storyboard`, `StoryboardFrame`, `AssetPlan`, and `AssetRequirement` domain entities, provider-agnostic visual and audio production specification, 9:16 vertical video prompt and framing intent engineering, scene-to-frame linkage and timing coherence with the approved `Script`, strict immutable lineage and successor reconciliation semantics, "one current storyboard" invariant, unified single-gate review/rejection/reopen lifecycle, AI-assisted storyboard synthesis (`plan_storyboard`) and advisory visual critique (`review_storyboard`) via configured provider routing, immutable `StoryboardVersion` snapshot history, full-spectrum optimistic concurrency, downstream media production eligibility gating, backend authorization, and the Angular 21 Storyboard & Production Planning Studio.

## ADDED Requirements

### Requirement: Structured Storyboard and Frame schema with extensible visual intent and immutable lineage

A `Storyboard` SHALL represent the concrete visual and audio production specification for a parent `ContentItem`, derived from the piece's approved `Script` (and its active `ScriptVersion`). Each `Storyboard` SHALL record immutable lineage: `ContentItemId`, `ChannelId`, `ScriptId`, `ScriptVersionId`, `TruthSourceId`, and `TruthSourceVersionId`. Each `Storyboard` SHALL also track: `IsCurrent` (`bool`), `SupersededAtUtc` (`DateTime?`), `ReconciledFromStoryboardId` (`Guid?`), `Title`, `TargetDurationSeconds` (matching or refining script duration, typically 30-60s for short-form vertical video), `TotalEstimatedDurationSeconds` (calculated from the sum of frame durations), status (`Draft`, `UnderReview`, `Approved`, `Rejected`), rejection reason (when rejected), creator/editor attribution, and a `Version` (`long`) optimistic concurrency token.

A `Storyboard` SHALL contain an ordered collection of `StoryboardFrame` items representing individual visual cuts/shots. Each `StoryboardFrame` SHALL include:
- `OrderIndex` (1-based sequence index);
- `ScriptSceneId` and `ScriptSceneOrderIndex` (explicit linkage to the originating script beat: Hook, Problem, Insight, Climax, CTA);
- `FramingIntent` (extensible framing descriptor with standard presets: `ExtremeCloseUp`, `CloseUp`, `MediumShot`, `WideShot`, `IsometricUi`, `MotionGraphic`);
- `CompositionIntent` (composition and subject placement notes, e.g. "Rule of thirds, subject center-top, bottom 30% reserved for captions");
- `CameraMotionIntent` (extensible camera motion descriptor with standard presets: `Static`, `SlowZoomIn`, `PanUp`, `TrackingShot`, `DynamicGlitch`);
- `Subject` (primary visual focus or character description);
- `Environment` (background, setting, lighting, and color palette description);
- `StyleIntent` (visual aesthetic descriptor, e.g. "Clean modern tech aesthetic, dark moody gradient lighting");
- `VisualPrompt` (detailed generation prompt optimized for vertical 9:16 framing and visual clarity without text artifacts);
- `NegativePrompt` (undesired visual artifacts, watermarks, distorted hands/faces, noisy backgrounds);
- `AudioCue` (narration text slice corresponding to this frame, voiceover pacing/pause notes, sound effect description);
- `EstimatedDurationSeconds` (visual duration for this specific frame, typically 2-6s);
- `OnScreenText` (captions, kinetic typography, or graphic text overlay intent);
- `TransitionIntent` (extensible transition descriptor with standard presets: `Cut`, `Dissolve`, `Wipe`, `ZoomIn`, `Glitch`, `PanUp`).

#### Scenario: Storyboard creation with script lineage completeness
- **WHEN** an operator creates or generates a Storyboard for a ContentItem with an approved Script
- **THEN** the Storyboard record is initialized with status "Draft", `IsCurrent: true`, `Version: 1`, total estimated duration calculated from frame durations, and ordered frames linked to script scenes
- **AND** it is explicitly and immutably associated with `ContentItemId`, `ChannelId`, `ScriptId`, `ScriptVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.

#### Scenario: Storyboard creation blocked without approved non-stale Script
- **WHEN** an operator attempts to create or generate a Storyboard on a ContentItem whose Script is missing, in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the backend rejects the request with HTTP 400 Bad Request and message "Storyboard creation requires an approved, non-stale Script".

### Requirement: Scene-to-frame linkage and timing coherence with approved Script

Every `StoryboardFrame` SHALL maintain an explicit linkage to its originating `ScriptScene` (`ScriptSceneId` and `ScriptSceneOrderIndex`). A `ScriptScene` MAY be subdivided into multiple `StoryboardFrame` items (e.g. a 12-second Insight scene composed into three 4-second shots). The system SHALL evaluate timing coherence between storyboard frames and the approved script:
1. The sum of frame durations for a `ScriptScene` SHOULD align with that scene's estimated narration duration;
2. The total storyboard estimated duration SHOULD align with the approved script's total estimated duration (within acceptable tolerance, e.g. +/- 5 seconds).

If an editorial edit creates a material timing discrepancy, the system SHALL surface an advisory timing warning in the Storyboard Studio and advisory AI review, without overriding human editorial authority. The system SHALL NOT require SMPTE or frame-accurate timecode infrastructure in this planning phase.

#### Scenario: Scene subdivided into multiple visual frames with duration alignment
- **WHEN** a 9-second ScriptScene (Hook) is decomposed into two StoryboardFrames of 4 seconds and 5 seconds
- **THEN** both frames record `ScriptSceneOrderIndex` corresponding to the Hook scene
- **AND** the combined scene frame duration (9.0s) matches the script scene narration duration
- **AND** no timing warning is flagged.

#### Scenario: Material timing mismatch surfaces advisory warning
- **WHEN** an operator modifies frame durations such that total Storyboard duration deviates by more than 5 seconds from the approved Script duration
- **THEN** the Storyboard Studio displays an advisory timing notice highlighting the discrepancy
- **AND** the operator retains the authority to submit, edit, or approve the storyboard if the timing difference is editorially intentional.

### Requirement: Provider-agnostic AssetPlan and AssetRequirement specification

A `Storyboard` SHALL include an `AssetPlan` that decomposes the storyboard frames into concrete asset requirements ready for future media generation pipelines. The `AssetPlan` is strictly a planning specification (WHAT media is needed) and SHALL NOT include execution lifecycle states (`Generating`, `Generated`, `Failed`) or runtime execution artifacts (MinIO storage keys, runtime errors, actual cost, execution durations, or provider-specific job IDs).

The `AssetPlan` SHALL track:
- `ContentItemId`, `StoryboardId`, `Version` (`long`), and planning status (`Planned`, `ReadyForGeneration`);
- An ordered collection of `AssetRequirement` records, each specifying:
  - `FrameId` and `FrameOrderIndex` (frame linkage);
  - `AssetType` (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`, `TtsVoiceover`, `BackgroundMusic`, `SoundEffect`, `SubtitleTrack`);
  - `AspectRatio` (default "9:16" for vertical short-form);
  - `VisualPrompt` and `NegativePrompt` (for visual assets);
  - `StyleIntent` and `MotionIntent` (for visual/video assets);
  - `TargetDurationSeconds` (target asset duration);
  - `VoiceIntent` (voice persona descriptor, e.g. "Sober Spanish male narrator, tech curiosity tone");
  - `MusicMood` (music genre/mood descriptor, e.g. "Ambient tech, minimalist, subtle tension");
  - `SoundEffectIntent` (sound effect description, e.g. "Subtle UI digital click at 0.5s");
  - `SubtitleProfile` (subtitle styling intent, e.g. "Center-bottom kinetic captions, active word highlight");
  - `OverlaySpecification` (graphic overlay details).

The asset specification SHALL remain strictly provider-neutral: it SHALL NOT encode ComfyUI workflow IDs, sampler names/settings, checkpoint hashes, scheduler parameters, node graph definitions, or provider execution identifiers.

#### Scenario: AssetPlan generated as provider-agnostic specification
- **WHEN** a Storyboard is created, generated, or updated
- **THEN** the system synchronizes the `AssetPlan`, creating or updating `AssetRequirement` records for each frame's visual, voiceover, sound effect, and subtitle requirements
- **AND** all requirements are specified using provider-neutral intents (aspect ratio 9:16, visual prompts, style intents, voice descriptors)
- **AND** no provider-specific node graphs or runtime execution fields are persisted.

### Requirement: Upstream lineage invalidation, immutable reconciliation, and "One Current Storyboard" invariant

The system SHALL enforce strict upstream lineage traceability and immutable historical truth:
1. If the underlying `Script` is edited, regenerated, reopened, rejected, or updated to a newer approved `ScriptVersion` that supersedes the storyboard's origin `ScriptVersionId`, or if the foundational `TruthSource` version advances, the Storyboard SHALL dynamically evaluate `IsStale = true`.
2. A stale Storyboard MUST NOT be made current by overwriting its immutable lineage identifiers (`ScriptId`, `ScriptVersionId`, `TruthSourceVersionId`).
3. Reconciling a stale storyboard SHALL create an immutable successor `Storyboard` (in status `Draft`, `IsCurrent = true`) derived from the new approved `ScriptVersion`, mark the prior Storyboard as `IsCurrent = false` and `SupersededAtUtc = DateTime.UtcNow`, optionally copy compatible frame planning and prompts from the prior storyboard, and record `ReconciledFromStoryboardId` pointing to the predecessor.
4. The system SHALL enforce the invariant that exactly ONE `Storyboard` is `IsCurrent = true` for a `ContentItem` at any time. Prior/superseded storyboards and their `StoryboardVersion` snapshot history remain immutable and permanently accessible for audit and lineage inspection.

#### Scenario: Storyboard dynamically marked stale on upstream script update
- **WHEN** an operator approves a new version of the Script on a ContentItem with an existing Storyboard
- **THEN** the existing Storyboard is preserved with its immutable lineage intact
- **AND** the system evaluates `Storyboard.IsStale` as true because its `ScriptVersionId` does not match the active approved Script version
- **AND** the Content Workspace and Storyboard Studio surface a "Lineage Superseded / Reconcile Required" alert.

#### Scenario: Reconcile creates immutable successor Storyboard
- **WHEN** an operator triggers "Reconcile Storyboard" on a stale Storyboard
- **THEN** the system creates a new successor Storyboard in status "Draft" with `IsCurrent = true` linked to the current approved `ScriptVersionId` and `TruthSourceVersionId`
- **AND** compatible frame prompts, intents, and audio cues from the previous storyboard are migrated into the successor
- **AND** `ReconciledFromStoryboardId` is set to the previous Storyboard ID
- **AND** the previous Storyboard is marked `IsCurrent = false` with `SupersededAtUtc` set
- **AND** the historical Storyboard and its `StoryboardVersion` snapshots remain unmodified in database history.

### Requirement: Single editorial review and approval gate for Storyboard and AssetPlan

The `AssetPlan` SHALL be an integral part of the `Storyboard` production specification and SHALL be included inside `StoryboardVersion` snapshots. The system SHALL NOT maintain a separate, independent approval workflow for the AssetPlan. When an operator with `EDITORIAL` role approves a `Storyboard`, the exact `AssetPlan` captured in that version becomes the approved production specification in a single atomic editorial action.

The `Storyboard` review lifecycle SHALL be:
1. `Draft` -> `UnderReview` (via `SubmitForReview`): Requires `expectedVersion`, validates that all frames have non-empty visual prompts and duration, sets status to `UnderReview`, advances parent `ContentItem` stage to `StoryboardUnderReview`, creates an `EditorialTask` of type `ReviewStoryboard`, and persists a `StoryboardVersion` snapshot.
2. `UnderReview` -> `Approved` (via `Approve`): Requires `EDITORIAL` role and `expectedVersion`, verifies that the underlying `Script` remains `Approved` and `IsStale` is false, sets `ApprovedAtUtc` and `ApprovedByEmail`, advances status to `Approved`, advances parent `ContentItem` stage to `StoryboardApproved`, completes pending `ReviewStoryboard` tasks, and persists an immutable `StoryboardVersion` snapshot.
3. `UnderReview` -> `Rejected` (via `Reject`): Requires `EDITORIAL` role, `expectedVersion`, and a mandatory non-empty `rejectionReason`, sets `RejectedAtUtc` and `RejectedByEmail`, marks status `Rejected`, completes pending `ReviewStoryboard` tasks, and persists an immutable `StoryboardVersion` snapshot documenting the rejection reason.
4. `Rejected` -> `Draft` (via `Reopen` / `Revise`): Explicit editorial transition requiring `expectedVersion`, clears the rejection block for continued editing while preserving historical rejection records in version snapshots, reverts status to `Draft`, and logs an audit event.

#### Scenario: Single-gate Storyboard and AssetPlan approval
- **WHEN** an operator with EDITORIAL role approves a Storyboard in "UnderReview"
- **THEN** the backend verifies that the linked Script remains approved and the storyboard is not stale
- **AND** the Storyboard status transitions to "Approved" with `ApprovedAtUtc` and `ApprovedByEmail`
- **AND** the embedded AssetPlan is approved as part of the immutable StoryboardVersion snapshot
- **AND** the parent ContentItem lifecycle stage advances to "StoryboardApproved"
- **AND** pending "ReviewStoryboard" tasks are completed.

#### Scenario: Reject Storyboard requires mandatory reason
- **WHEN** an operator with EDITORIAL role rejects a Storyboard in "UnderReview" providing a non-empty rejection reason
- **THEN** the Storyboard transitions to "Rejected" state with `RejectionReason` recorded
- **AND** a `StoryboardVersion` snapshot is persisted containing the rejection reason
- **AND** pending "ReviewStoryboard" tasks are completed.

#### Scenario: Reject Storyboard without reason fails validation
- **WHEN** an operator attempts to reject a Storyboard with an empty rejection reason
- **THEN** the backend rejects the request with HTTP 400 Bad Request and message "Rejection reason is required".

#### Scenario: Reopen rejected Storyboard for revision
- **WHEN** an operator executes "Reopen" on a "Rejected" Storyboard providing expectedVersion
- **THEN** the Storyboard transitions to "Draft"
- **AND** an immutable `StoryboardVersion` snapshot is recorded with change summary "Reopened for revision"
- **AND** the parent ContentItem stage reflects "StoryboardDrafted".

### Requirement: AI-assisted storyboard synthesis and refinement (`plan_storyboard`)

The system SHALL support invoking the capability `plan_storyboard` through `IAiProviderRouter` to synthesize a structured multi-frame storyboard from an approved script. The prompt SHALL incorporate the approved script (scenes, narration, pacing, visual cues) and channel formatting rules ("IA Simple ES", vertical 9:16, modern tech/curiosity visual tone). The generated output SHALL provide:
- Ordered frames decomposing each script scene into 2-6 second visual shots;
- Provider-neutral visual prompts detailing subjects, environment, lighting, color palette, camera angles, and vertical 9:16 composition without text hallucination;
- Negative prompts filtering visual deformities;
- Audio cues matching narration beats and sound effects;
- On-screen text suggestions for key takeaway hooks;
- Suggested transition intents (Cut, ZoomIn, PanUp, Glitch);
- Initial provider-neutral `AssetPlan` requirements.

AI execution SHALL record an `AIRecommendation` telemetry record (tokens, latency, estimated cost, prompt policy version, without private chain-of-thought). No AI-generated prompt SHALL become authoritative without human editorial review.

#### Scenario: AI generates structured storyboard from approved script
- **WHEN** an operator triggers "Generate Storyboard" on a ContentItem with an approved Script
- **THEN** `IAiProviderRouter` routes the request to the configured reasoning provider (DeepSeek default or development mock)
- **AND** the AI generates a multi-frame storyboard with 9:16 vertical visual prompts, framing intents, audio cues, on-screen text, and frame durations aligned with script timing
- **AND** the Storyboard and AssetPlan are persisted in "Draft" status with `Version: 1`
- **AND** an `AIRecommendation` record is logged.

#### Scenario: Deterministic development mock storyboard generation
- **WHEN** running in development environment without live AI provider credentials
- **THEN** `IAiProviderRouter` uses a deterministic mock adapter to generate realistic Spanish short-form visual storyboard frames and asset requirements for channel "IA Simple ES".

### Requirement: Advisory AI-assisted storyboard critique (`review_storyboard`)

The system SHALL support invoking the capability `review_storyboard` through `IAiProviderRouter` to analyze an existing storyboard draft against vertical video best practices and channel constraints. The AI reviewer SHALL evaluate:
- Hook visual impact (0-3s): does Frame 1 provide immediate visual intrigue and high retention potential?
- Visual variety and shot pacing: are framing intents diversified (avoiding monotonous static framing)?
- 9:16 vertical composition and safe zones: do visual prompts account for vertical framing and avoid essential subjects in UI overlay zones?
- Visual prompt fidelity: do prompts match script narration beats and avoid conflicting imagery?
- Timing alignment: are frame durations coherent with script scene timings?

The evaluation SHALL return structured findings (`Pass`, `Warning`, `Critical`, dimension scores, specific frame critique notes, and actionable recommendations) persisted as an `AIRecommendation` record. AI critique SHALL be strictly advisory: AI critique findings SHALL NOT automatically approve or reject the storyboard; human `EDITORIAL` approval remains the sole authoritative gate.

#### Scenario: AI storyboard critique returns advisory findings
- **WHEN** an operator triggers "AI Review" on a Storyboard draft
- **THEN** `IAiProviderRouter` dispatches the review prompt evaluating framing variety, hook visual strength, 9:16 composition, timing alignment, and prompt fidelity
- **AND** the system returns structured advisory critique results (`Pass`, `Warning`, or `Critical`) with specific frame notes
- **AND** an `AIRecommendation` telemetry record is stored
- **AND** the storyboard status remains unchanged until human editorial action.

#### Scenario: Critical AI findings displayed prominently without overriding human authority
- **WHEN** AI critique reports "Critical" findings (e.g. static framing across 15 seconds or prompt violating visual guidelines)
- **THEN** the Storyboard Studio highlights the critical warnings on affected frames
- **AND** the human editor retains sole authority to decide whether to revise, approve, or reject.

### Requirement: Storyboard versioning (`StoryboardVersion`) and full-spectrum optimistic concurrency

The system SHALL treat human edits to storyboards and asset requirements as first-class, immutable version snapshots rather than destructive in-place overwrites. Every mutation (editing frames/prompts/durations/audio cues, adding/splitting/reordering/deleting frames, updating asset requirements, submitting for review, approving, rejecting, reopening) SHALL require the client to supply `expectedVersion: long`. If `expectedVersion` does not match the database version, the backend SHALL reject the request with HTTP 409 Conflict (`CONCURRENCY_CONFLICT`), applying NO changes and logging NO history. For valid mutations, the system SHALL increment `Version`, update duration aggregates, and persist an immutable `StoryboardVersion` snapshot capturing the complete storyboard state, frames, asset requirements, change summary, author, and timestamp.

#### Scenario: Edit storyboard frames with version increment and history snapshot
- **WHEN** an operator updates visual prompts, framing intents, or frame durations on a Storyboard at version 1 providing expectedVersion 1
- **THEN** the storyboard record is updated with version 2 and updated total estimated duration
- **AND** an immutable `StoryboardVersion` snapshot of version 1 is persisted in the history log
- **AND** an audit event is logged with action "Storyboard.Updated".

#### Scenario: Concurrency conflict rejection across all storyboard mutations
- **WHEN** an operator attempts an Update, SubmitForReview, Approve, Reject, or Reopen action providing expectedVersion 1 when the database has progressed to version 2
- **THEN** the backend rejects the request with HTTP 409 Conflict and code "CONCURRENCY_CONFLICT"
- **AND** the database state remains untouched and no spurious version history is recorded
- **AND** the frontend prompts the user to refresh and reconcile changes.

### Requirement: Downstream media generation eligibility contract

This capability establishes the domain precondition that future production capabilities (ComfyUI image/video generation, TTS audio synthesis, video rendering) MUST enforce. A `ContentItem` SHALL be eligible for downstream media production IF AND ONLY IF:
1. A current Storyboard exists (`IsCurrent == true`);
2. `Storyboard.Status == Approved`;
3. `Storyboard.IsStale == false`;
4. Its approved `StoryboardVersion` contains a complete `AssetPlan` with status `ReadyForGeneration`; and
5. Upstream `Script` and `TruthSource` lineages remain current and approved.

The system SHALL NOT implement fake generation endpoints or mock rendering execution in this change.

#### Scenario: Verify downstream media generation eligibility
- **WHEN** a domain query or downstream gate evaluates production readiness on a ContentItem in stage "StoryboardApproved" with a non-stale approved Storyboard
- **THEN** the eligibility query returns `IsEligibleForGeneration: true` with a summary of ready asset requirements.

#### Scenario: Ineligible when Storyboard is unapproved or stale
- **WHEN** a domain query evaluates production readiness on a ContentItem whose Storyboard is in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the eligibility query returns `IsEligibleForGeneration: false` with the specific blocker reason.

### Requirement: Backend authorization for storyboard operations

All storyboard mutation endpoints (AI generation, AI critique, manual frame editing, splitting, reordering, submitting for review, approving, rejecting, reopening, reconciling) SHALL be explicitly authorized on the backend, requiring the `EDITORIAL` role (or development GOD mode).

#### Scenario: Non-editorial user cannot edit, approve, or reject storyboards
- **WHEN** a user without the `EDITORIAL` role attempts to edit, generate, approve, reject, reopen, or reconcile a storyboard
- **THEN** the backend rejects the request with HTTP 403 Forbidden.

### Requirement: Angular 21 Storyboard & Production Planning Studio

The frontend SHALL provide a dedicated Storyboard & Production Planning Studio inside the Content Detail view on Angular 21 (PrimeNG 21, Tailwind CSS 4) featuring:
1. Header metrics: total frame count, live duration meter (with 30-60s target bounds indicator), status badge, version token, and stale lineage warning banner with one-click "Reconcile Storyboard" trigger if upstream Script changed.
2. Frame timeline & grid editor: visual cards for each frame showing frame sequence number, linked script beat badge, 9:16 vertical framing container/placeholder, framing intent selector, composition notes, camera motion selector, visual prompt textarea, negative prompt input, audio cue / voiceover text preview, on-screen text input, frame duration input (in seconds) with scene timing comparison, transition intent selector, and frame action controls (add frame, split frame, delete frame, move left/right).
3. Asset Plan summary panel: collapsible drawer or panel summarizing all required visual assets (AI image/video prompts in 9:16 format), voiceover audio profiles, and subtitle overlays with planning readiness status.
4. Script reference panel: collapsible side panel displaying the approved Script narration and scene structure for side-by-side reference during visual prompt editing.
5. AI Generation modal: modal for configuring storyboard generation visual style presets, camera movement intensity, and frame density.
6. Advisory AI Review panel: collapsible panel presenting AI visual critique findings (`Pass`/`Warning`/`Critical`), retention assessment, 9:16 composition warnings, timing alignment checks, and actionable suggestions.
7. Version history & diff drawer: timeline of previous `StoryboardVersion` snapshots with frame diffs, rejection notes, and author attribution.
8. Decision action bar: contextual actions ("Submit for Review", "Approve", "Reject" with reason modal, "Reopen Storyboard" for rejected storyboards, "Reconcile Storyboard" for stale storyboards).
9. Full-width desktop layout: spans 100% available horizontal viewport width (>=1280px) without arbitrary max-width constraints.
10. Responsive mobile layout: stacked card view with touch-friendly actions supporting full storyboard review, AI critique inspection, and decision workflows on mobile viewports (~390px).

#### Scenario: High-density storyboard timeline editing on desktop
- **WHEN** an operator views the Storyboard tab of a ContentItem on desktop (viewport >= 1280px)
- **THEN** frames are rendered in a high-density visual sequence with live duration totals, 9:16 aspect previews, prompt editors, and scene timing indicators
- **AND** the view utilizes full horizontal screen width without arbitrary max-width constraints
- **AND** total duration meter indicates whether the piece falls within the 30-60s target range.

#### Scenario: Responsive mobile storyboard review and decision
- **WHEN** an operator opens the Storyboard Studio on a mobile device (~390px)
- **THEN** frames render cleanly as stacked collapsible cards with visual prompts and audio cues
- **AND** the sticky action bar allows one-touch AI critique review, approval, rejection with reason input, or reopening.
