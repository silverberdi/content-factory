## Why

Content Factory currently automates the editorial pipeline from discovery through script approval, storyboard production planning, and visual asset generation (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`). The Storyboard production plan generates provider-neutral `TtsVoiceover` requirements, but Content Factory currently lacks the capability to synthesize actual spoken narration.

To advance toward complete vertical video assembly without human bottlenecks or manual voice recording, Content Factory needs a real voice production engine. This change introduces voice/TTS generation that directly translates approved script narration into high-quality spoken audio using Google's Gemini TTS API (with a deterministic development mock), stores generated audio in MinIO, computes measured audio duration vs. script estimates, and presents editorial operators with an intuitive listening, candidate comparison, and QA approval studio.

## What Changes

- **Provider-Neutral Voice Profile & Request Model**: Introduce a provider-agnostic `VoiceProfile` domain concept with stable identity (capturing channel assignment, language, configurable locale/accent, speaking style, tone, warmth, pace, and pronunciation guidance) where the effective profile is immutably snapshotted in generation metadata.
- **Explicit Language & Locale Separation**: Model editorial language as `Language = "es"` (matching channel definition) while making provider-specific voice, accent, and dialect mappings explicit and configurable.
- **Canonical PCM WAV Audio Master**: Standardize all generated and stored audio masters on a WAV container (PCM 16-bit, mono, preserving and recording provider-returned sample rate, e.g. 24000 Hz / 44100 Hz / 48000 Hz, with no intermediate MP3/AAC transcoding subsystem).
- **Gemini TTS Provider Adapter**: Implement `GeminiTtsProvider` communicating directly via Google AI / Gemini programmatic REST API / SDK (unattended HTTP API execution without CLI dependencies or interactive browser logins), translating provider-neutral VoiceProfiles and Spanish narration into clear, natural speech.
- **Deterministic Mock Voice Provider**: Implement `MockVoiceGenerationProvider` generating valid synthetic PCM WAV audio binaries and realistic duration metadata for local offline testing and CI without consuming external API credits.
- **Canonical Job Domain Reuse with Capability-Scoped Worker**: Reuse the existing `Job` and `JobAttempt` entities under capability `generate_voice_asset`. Implement `VoiceGenerationBackgroundWorker` using atomic database claiming specifically scoped to `JobType == "generate_voice_asset"` (leaving `VisualGenerationBackgroundWorker` to claim only `generate_visual_asset`).
- **Exact Narration Derivation & Lineage**: Deterministically extract and normalize spoken narration text exclusively from the current approved `ScriptVersion` (ignoring visual prompts, camera directions, and editorial notes), computing a SHA-256 narration fingerprint to guarantee immutable lineage and auditability.
- **Generated Audio Storage & Metadata**: Persist generated narration in `GeneratedAsset` with `AssetType = TtsVoiceover`, `MediaType = Audio`, `ContentType = "audio/wav"`, file size, SHA-256 checksum, sample rate, channels, bit depth, and actual measured `DurationSeconds`, stored under deterministic MinIO paths (`content-factory/{env}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav`).
- **Configurable Timing Deviation Policy**: Compare actual measured voice `DurationSeconds` against the editorial script's `EstimatedDurationSeconds` using a configurable `TimingTolerancePolicy` (e.g. channel or global tolerance thresholds) rather than hard-coded domain constants.
- **Human Voice QA Studio**: Deliver an Angular 21 full-width Voice Production Studio in the Content Workspace featuring audio playback controls (play, pause, seek), candidate audio cards, multi-candidate comparison, single-candidate assembly selection (`IsSelectedForAssembly`), and mandatory rejection rationale capture.
- **Dynamic Assembly Eligibility & Staleness Guard**: Ensure only approved, selected voice assets from current, non-stale storyboards are marked `IsEligibleForAssembly = true`. Upstream revisions preserve historical audio for audit while invalidating assembly eligibility.
- **Authorization Enforcement**: Enforce strict separation where `EDITORIAL` operators trigger generation, adjust pronunciation hints, and perform QA review, while `TECHNICAL` operators inspect execution telemetry, diagnose provider failures, and trigger manual retries.

## Capabilities

### New Capabilities
- `audio-voice-generation`: Asynchronous narration audio production, Gemini TTS programmatic provider adapter, mock voice generator, audio MinIO storage, measured duration tracking, multi-candidate human listening and QA review, and assembly eligibility gating.

### Modified Capabilities
<!-- No requirement changes to existing specs -->

## Impact

- **Backend / API**:
  - Adds `JobTypes.GenerateVoiceAsset = "generate_voice_asset"`.
  - Implements `IVoiceGenerationProvider`, `GeminiTtsProvider`, and `MockVoiceGenerationProvider` under `Modules/Ai`.
  - Implements `IVoiceGenerationService` and `VoiceGenerationService` with atomic queue processing, pre-dispatch eligibility validation, and candidate review under `Modules/Content`.
  - Adds `VoiceGenerationBackgroundWorker` hosted service claiming exclusively `JobType == "generate_voice_asset"`.
  - Adds streaming/playback endpoints (`GET /api/generated-assets/{id}/audio-stream`).
  - Extends `IStorageService` and `MinioStorageService` to support deterministic audio object key paths.
- **Database**:
  - Reuses existing `jobs`, `job_attempts`, and `generated_assets` tables (no breaking schema changes required; columns like `duration_seconds` and `media_type` already support audio assets).
- **Frontend**:
  - Introduces `VoiceStudioComponent`, `VoiceCandidateCardComponent`, `AudioPlayerComponent`, and `PronunciationGuidanceModalComponent` inside `features/content/`.
  - Adds a dedicated "Voice" tab in Content Detail alongside Storyboard and Visual Assets.
- **Configuration & Security**:
  - Adds `VOICE_PROVIDER` (Gemini vs Mock), `GEMINI_API_KEY`, and `TimingTolerancePolicy` configuration.
  - Enforces role-based access for voice generation and candidate review.
