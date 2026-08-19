## Context

Content Factory has implemented the editorial evidence loop (Sources → TruthSource → ContentIdeas → Script) with human review, versioning, and advisory AI critique. To advance into Wave 3 (Production Pipeline) for channel "IA Simple ES" (Spanish short-form vertical video, 9:16, 45-60s), the system requires decomposing approved scripts into structured visual frames, provider-neutral video/image generation prompts, framing/motion intents, audio cues, on-screen text overlays, and a provider-agnostic asset requirements plan (specifying visual, voiceover, music, and subtitle assets) before future compute-intensive media rendering is unlocked.

See `proposal.md` for motivation and business context.

## Goals / Non-Goals

**Goals:**
- Implement `Storyboard`, `StoryboardFrame`, `AssetPlan`, and `AssetRequirement` domain entities in ASP.NET Core .NET 10 LTS with PostgreSQL persistence.
- Keep asset planning strictly provider-agnostic and bounded to specification (WHAT media is needed), excluding execution lifecycle states and runtime execution artifacts.
- Record and enforce immutable upstream lineage to exact `ScriptVersionId` and `TruthSourceVersionId`.
- Evaluate upstream staleness (`IsStale`) dynamically when underlying scripts or truth sources evolve.
- Implement immutable successor reconciliation semantics: reconciling a stale storyboard creates an immutable successor `Storyboard` (in `Draft`) linked to the new approved `ScriptVersion`, preserving historical storyboards and snapshot lineage.
- Enforce the "One Current Storyboard" server-side invariant: exactly one active, non-superseded Storyboard (`IsCurrent = true`) per `ContentItem`.
- Enforce a single editorial review and approval gate: approving a `Storyboard` atomically approves the visual frames and embedded `AssetPlan` captured in that `StoryboardVersion`.
- Maintain scene-to-frame linkage and timing coherence with the approved script, surfacing advisory warnings for material duration mismatches without overriding human editorial authority.
- Provide an extensible frame intent model (framing, composition, camera motion, visual style, prompts, audio cues, transition intents).
- Implement AI capabilities `plan_storyboard` and `review_storyboard` via `IAiProviderRouter` with telemetry (`AIRecommendation`) and deterministic development mock fallback.
- Implement full-spectrum optimistic concurrency (`expectedVersion: long`) and immutable `StoryboardVersion` snapshot history across all mutations.
- Establish the domain downstream media generation eligibility contract for future production waves.
- Deliver an Angular 21 Storyboard Studio (PrimeNG 21, Tailwind CSS 4) obeying the canonical full-width operational layout contract and mobile (~390px) responsiveness.

**Non-Goals:**
- Executing ComfyUI image/video generation jobs or rendering output MP4 files (deferred to CF-031 / CF-033).
- TTS speech synthesis audio rendering or audio mixing execution (deferred to CF-032).
- Managing runtime execution states (`Generating`, `Generated`, `Failed`) or runtime artifact references (MinIO storage keys, execution errors, runtime costs, execution durations).
- Implementing SMPTE / frame-accurate timecode infrastructure.
- Platform publication operations (deferred to Wave 4 CF-040).

## Decisions

### 1. Domain Aggregate Boundaries & Lineage

The `Storyboard` entity serves as the operational root for visual production planning on a `ContentItem`. It directly owns its ordered `StoryboardFrame` collection and its associated `AssetPlan`.

```
ContentItem (Aggregate Root)
  └── TruthSource (Approved Evidence)
        └── ContentIdea (Selected Creative Lead)
              └── Script (Approved Narration & Scene Structure)
                    └── Storyboard (Visual & Production Planning Root)
                          ├── StoryboardFrame[] (1..N Ordered Frames: Framing Intent, Visual Prompt, Audio Cue, Timing)
                          ├── AssetPlan (Visual Requirements, Audio Tracks, Subtitle Spec)
                          │     └── AssetRequirement[] (Prompts, Voice Intent, Music Mood, SFX Intent, Overlay Spec)
                          └── StoryboardVersion[] (Immutable Historical Snapshots)
```

**Lineage & Current Storyboard Invariants:**
- A Storyboard cannot be created without an `Approved` and non-stale `Script`.
- Storyboard stores `ScriptId`, `ScriptVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.
- Server-side invariant: Exactly one Storyboard per `ContentItem` has `IsCurrent = true`. Prior/superseded storyboards have `IsCurrent = false` and `SupersededAtUtc` set.
- If the parent Script is modified, reopened, or updated to a newer version, the Storyboard's `IsStale` dynamically evaluates to `true`.

### 2. Provider-Agnostic Asset Planning Boundary

The `AssetPlan` and `AssetRequirement` entities represent production requirements (WHAT media is needed), not execution:
- **No execution lifecycle states**: States like `Generating`, `Generated`, `Failed` are excluded. The plan uses lightweight planning status: `Planned`, `ReadyForGeneration`.
- **No runtime execution artifacts**: MinIO storage keys, runtime error messages, actual costs, execution durations, and provider job IDs are excluded.
- **Provider-neutral specifications**: No ComfyUI workflow IDs, samplers, checkpoints, schedulers, or node graphs are stored. Specifications express provider-neutral intent: `AssetType`, `AspectRatio` (9:16), `VisualPrompt`, `NegativePrompt`, `StyleIntent`, `MotionIntent`, `TargetDurationSeconds`, `VoiceIntent`, `MusicMood`, `SoundEffectIntent`, `SubtitleProfile`, `OverlaySpecification`.

*Rationale:* Future ComfyUI/TTS adapters and Job execution models will translate these provider-neutral requirements into provider-specific workflows without polluting the canonical planning domain.

### 3. Single Editorial Approval Gate

The `AssetPlan` is an integral part of the `Storyboard` production specification and is captured inside `StoryboardVersion` snapshots.
- There is no separate, disconnected approval workflow for the AssetPlan.
- When an `EDITORIAL` operator approves a `Storyboard`, the exact visual frames and `AssetPlan` in that `StoryboardVersion` become approved in a single atomic editorial action.
- The downstream production gate requires: an `Approved`, non-stale `Storyboard` whose approved version includes an `AssetPlan` with status `ReadyForGeneration`.

### 4. Immutable Lineage and Exact Reconciliation Semantics

Historical lineage to `ScriptVersionId` and `TruthSourceVersionId` is immutable truth and is never overwritten.
When an upstream Script evolves:
1. The existing Storyboard dynamically becomes `IsStale = true`.
2. Reconciling the stale Storyboard triggers the `ReconcileStoryboardAsync` action.
3. The system creates a new successor `Storyboard` record in status `Draft`, with `IsCurrent = true`, linked to the new approved `ScriptVersionId` and `TruthSourceVersionId`.
4. Compatible editorial frame planning, prompts, and audio cues from the prior storyboard are migrated into the successor.
5. The successor records `ReconciledFromStoryboardId` pointing to the predecessor.
6. The predecessor is marked `IsCurrent = false` and `SupersededAtUtc = DateTime.UtcNow`.
7. The predecessor and all its `StoryboardVersion` snapshots remain permanently preserved in database history.
8. The successor requires human editorial review and approval before becoming `Approved`.

### 5. Scene-to-Frame Linkage and Timing Coherence

Storyboard timing is an editorial visual estimate derived from the approved Script:
- Every `StoryboardFrame` stores `ScriptSceneId` and `ScriptSceneOrderIndex`.
- Script scenes may be subdivided into multiple visual frames (e.g. 1 scene -> 2-3 frames).
- The system computes:
  - Sum of frame durations per scene vs. script scene estimated duration;
  - Total storyboard estimated duration vs. script total estimated duration.
- Material timing discrepancies (e.g. > 5 seconds total deviation) surface an advisory warning in Storyboard Studio and advisory AI review, without overriding human editorial authority.
- No SMPTE / frame-accurate timecode infrastructure is introduced in this planning slice.

### 6. Extensible Frame Intent Model

To ensure flexibility across future visual styles and video formats, frames use extensible intent descriptors rather than restrictive hardcoded enums:
- `FramingIntent`: standard convenience presets (`ExtremeCloseUp`, `CloseUp`, `MediumShot`, `WideShot`, `IsometricUi`, `MotionGraphic`) with support for custom framing descriptions;
- `CompositionIntent`: subject placement and safe zone notes (e.g. bottom 30% reserved for captions);
- `CameraMotionIntent`: motion presets (`Static`, `SlowZoomIn`, `PanUp`, `TrackingShot`, `DynamicGlitch`) with custom motion descriptions;
- `TransitionIntent`: transition presets (`Cut`, `Dissolve`, `Wipe`, `ZoomIn`, `Glitch`, `PanUp`);
- `Subject`, `Environment`, `StyleIntent`, `VisualPrompt`, `NegativePrompt`, `AudioCue`, `OnScreenText`.

### 7. Optimistic Concurrency and Immutable Version History

Every mutation to a storyboard (updating frames, reordering/adding/splitting frames, updating asset requirements, submitting for review, approving, rejecting, reopening, reconciling) requires `expectedVersion: long`.
- If `expectedVersion != Storyboard.Version`, the API returns HTTP 409 Conflict (`CONCURRENCY_CONFLICT`), modifying no records.
- On valid mutations, `Storyboard.Version` is incremented, duration totals are recalculated, and a complete immutable `StoryboardVersion` snapshot (including full frames JSON, asset requirements JSON, change summary, editor email, and timestamp) is inserted into `storyboard_versions`.

### 8. AI Capabilities & Provider Routing

Two capabilities are registered in `IAiProviderRouter`:
1. `plan_storyboard`: Generates structured frames decomposing script scenes into 2-6s shots, with 9:16 vertical prompt details, framing/motion intents, audio cues, on-screen text, and initial provider-neutral asset requirements.
2. `review_storyboard`: Analyzes storyboard drafts for visual pacing, hook visual strength (0-3s), framing variety, 9:16 composition / safe-zone adherence, scene timing alignment, and visual prompt fidelity against script narration. Returns structured findings (`Pass`, `Warning`, `Critical`) and actionable suggestions.

- Both capabilities log structured `AIRecommendation` records without private chain-of-thought.
- In development mode without API credentials, deterministic mock adapters produce realistic Spanish short-form storyboard frames for channel "IA Simple ES".
- Advisory critique never overrides human editorial authority.

### 9. Downstream Media Generation Eligibility Contract

This change establishes the domain precondition for future production capabilities (CF-031 / CF-032 / CF-033):
- A `ContentItem` is eligible for media generation if and only if:
  1. `IsCurrent == true` Storyboard exists;
  2. `Storyboard.Status == Approved`;
  3. `Storyboard.IsStale == false`;
  4. Approved `StoryboardVersion` contains an `AssetPlan` with status `ReadyForGeneration`;
  5. Upstream `Script` and `TruthSource` lineages remain current and approved.
- No fake ComfyUI / TTS / render execution endpoints are implemented in this change.

### 10. Frontend Architecture & Layout

The Angular 21 Storyboard Studio (`storyboard-studio.component.ts`) will be integrated into `content-detail.component.ts` as a primary tab:
- Adheres to the canonical full-width operational layout contract: spans 100% horizontal width on desktop (>=1280px).
- Frame timeline/grid: High-density cards displaying visual prompt textarea, negative prompt, 9:16 aspect ratio preview container, framing intent selector, composition notes, camera motion selector, audio cue text, on-screen text input, duration input (seconds) with scene timing comparison badge, transition selector, and frame action buttons (split, delete, move).
- Header duration meter: Visual progress bar with short-form vertical video target bounds (30-60s) and live duration recalculation.
- Side-by-side Script Reference drawer: Allows editors to reference approved narration beats while editing frame prompts.
- Asset Plan summary panel: Summarizes required visual assets, voiceover profile, background music, and subtitle configurations.
- Mobile responsiveness: Collapses into a vertical touch-friendly card stack at ~390px viewport with sticky bottom action controls.

### 11. Persistence & EF Core Migration

PostgreSQL schema additions in `src/api/ContentFactory.Api`:
- Tables:
  - `storyboards` (`id`, `content_item_id`, `channel_id`, `script_id`, `script_version_id`, `truth_source_id`, `truth_source_version_id`, `is_current`, `superseded_at_utc`, `reconciled_from_storyboard_id`, `title`, `target_duration_seconds`, `total_estimated_duration_seconds`, `status`, `rejection_reason`, `version`, `created_at_utc`, `created_by_email`, `updated_at_utc`, `updated_by_email`, `approved_at_utc`, `approved_by_email`, `rejected_at_utc`, `rejected_by_email`)
  - `storyboard_frames` (`id`, `storyboard_id`, `order_index`, `script_scene_id`, `script_scene_order_index`, `framing_intent`, `composition_intent`, `camera_motion_intent`, `subject`, `environment`, `style_intent`, `visual_prompt`, `negative_prompt`, `audio_cue`, `estimated_duration_seconds`, `on_screen_text`, `transition_intent`, `created_at_utc`, `updated_at_utc`)
  - `asset_plans` (`id`, `storyboard_id`, `content_item_id`, `status`, `version`, `created_at_utc`, `updated_at_utc`)
  - `asset_requirements` (`id`, `asset_plan_id`, `frame_id`, `frame_order_index`, `asset_type`, `aspect_ratio`, `visual_prompt`, `negative_prompt`, `style_intent`, `motion_intent`, `target_duration_seconds`, `voice_intent`, `music_mood`, `sound_effect_intent`, `subtitle_profile`, `overlay_specification`, `created_at_utc`, `updated_at_utc`)
  - `storyboard_versions` (`id`, `storyboard_id`, `version_number`, `snapshot_json`, `change_summary`, `rejection_reason`, `created_at_utc`, `created_by_email`)
- Enum updates:
  - `ContentLifecycleStage` includes `StoryboardDrafted`, `StoryboardUnderReview`, `StoryboardApproved`.
  - `EditorialTaskType` includes `ReviewStoryboard`.

## Risks / Trade-offs

- **[Risk: Script revisions invalidate approved storyboards]**  
  → *Mitigation:* Dynamic staleness detection flags `IsStale = true` and surfaces a one-click successor reconciliation action that migrates compatible frame planning while preserving historical records.

- **[Risk: Frame timing drifts from script narration timing]**  
  → *Mitigation:* Scene-to-frame linkage and live duration meters highlight scene-level and total duration discrepancies, alerting the editor and AI critique without blocking intentional editorial timing choices.

- **[Risk: Provider-specific details leak into planning domain]**  
  → *Mitigation:* AssetPlan and StoryboardFrame strictly enforce provider-neutral intent models (visual prompts, style intents, voice personas, aspect ratio 9:16), decoupling domain planning from ComfyUI/TTS execution adapters.

- **[Risk: Concurrent edits overwrite frames or review status]**  
  → *Mitigation:* Full-spectrum optimistic concurrency requiring `expectedVersion` on all mutations prevents race conditions, returning HTTP 409 Conflict.

- **[Risk: Multiple storyboards active simultaneously on one ContentItem]**  
  → *Mitigation:* Server-side "One Current Storyboard" invariant strictly maintains `IsCurrent = true` on at most one storyboard per item.

## Migration Plan

1. Generate and apply EF Core database migration for PostgreSQL dev/prod environments.
2. Seed sample storyboard and asset plan data for existing approved scripts in channel "IA Simple ES".
3. Verify backward compatibility: existing ContentItems with approved scripts remain valid and ready for storyboard generation.
