## Purpose

Defines the asynchronous voice and text-to-speech (TTS) audio production capability (`generate_voice_asset`) that executes approved, provider-neutral `TtsVoiceover` `AssetRequirement` specifications from the active `Storyboard` into real spoken narration audio artifacts. Specifies the canonical `Job` domain reuse, capability-scoped background worker claiming (`VoiceGenerationBackgroundWorker`), the provider-neutral `VoiceProfile` and `IVoiceGenerationProvider` adapter boundary (supporting Gemini TTS programmatic API/SDK and deterministic development mock), canonical PCM 16-bit WAV audio master storage in MinIO, exact narration text derivation and lineage hashing from the approved `ScriptVersion`, immutable `GeneratedAsset` audio metadata tracking, measured vs. estimated duration comparison against a configurable `TimingTolerancePolicy`, multi-candidate voice generation, mandatory human voice QA listening and selection (`PendingReview`, `Approved`, `Rejected`), pronunciation guidance adjustments, upstream staleness gating, bounded idempotent retries, authorization boundaries, and the Angular 21 full-width Voice Production Studio.

## Requirements

### Requirement: Canonical Job domain reuse and capability-scoped worker claiming
The system SHALL model all long-running asynchronous voice generation operations using the canonical `Job` and `JobAttempt` entities under capability and `JobType` `"generate_voice_asset"`. The system SHALL NOT introduce secondary queue tables, competing worker architectures, or voice-specific job aggregates (`VoiceJob`, `TtsJob`, `AudioGenerationJob`).

A voice generation `Job` SHALL record:
- `Id` (`Guid`, unique identifier);
- `ContentItemId` and `ChannelId` (parent operational thread);
- `JobType` / `Capability` = `"generate_voice_asset"`;
- `SourceAssetRequirementId` (`Guid`, pointing to the `TtsVoiceover` requirement);
- `StoryboardId` and `StoryboardVersionId` (`Guid`, exact authorizing storyboard snapshot);
- `GenerationRevision` (`int`, 1-based revision counter distinguishing intentional regeneration cycles);
- `Status` (`Queued`, `Running`, `Succeeded`, `FailedRetryable`, `FailedActionRequired`, `Cancelled`);
- `Provider` (e.g. `"Gemini"`, `"Mock"`);
- `ModelOrWorkflowIdentifier` (the resolved provider model/workflow, e.g. `"gemini-3.8-flash-lite-tts"`);
- `AttemptCount` (`int`, total executed attempts);
- `MaxAttempts` (`int`, default 3);
- `AutomaticRetriesRemaining` (`int`, default 2);
- `CandidateCount` (`int`, 1 or 2 voice candidate variants);
- `StartedAtUtc`, `CompletedAtUtc`, and `DurationMs`;
- `EstimatedCostUsd` and `ActualCostUsd` (`decimal?`);
- `CorrelationId` (`string`, for distributed tracing);
- `ErrorCode` (`string?`, normalized failure category);
- `SanitizedErrorMessage` (`string?`, safe user-facing message without credentials or stack traces);
- `IsRetryable` (`bool`);
- `IdempotencyKey` (`string`, SHA-256 batch generation intent hash);
- `CreatedByEmail`, `CreatedAtUtc` and `UpdatedAtUtc`.

**Worker Claim Scope Invariant:**
The background execution SHALL be processed asynchronously by a dedicated ASP.NET Core `IHostedService` background queue worker (`VoiceGenerationBackgroundWorker`) backed by PostgreSQL using atomic conditional update claiming that strictly filters by `JobType`:
```sql
UPDATE jobs 
SET status = 'Running', started_at_utc = now, updated_at_utc = now 
WHERE id = @candidateId AND status = 'Queued' AND job_type = 'generate_voice_asset';
```
`VoiceGenerationBackgroundWorker` SHALL claim ONLY jobs where `JobType == "generate_voice_asset"`. `VisualGenerationBackgroundWorker` SHALL continue to claim ONLY jobs where `JobType == "generate_visual_asset"`. No worker SHALL claim a Job belonging to another capability.

#### Scenario: Dispatch asynchronous voice generation job
- **WHEN** an operator requests voice generation for an approved `TtsVoiceover` AssetRequirement
- **THEN** the system validates eligibility, derives canonical GenerationRevision, computes the batch idempotency key, and persists a `Job` with `JobType = "generate_voice_asset"` and `Status = "Queued"`
- **AND** the API immediately returns HTTP 202 Accepted containing the Job representation
- **AND** `VoiceGenerationBackgroundWorker` claims the Job atomically, transitions it to "Running", and dispatches execution to the configured voice provider adapter.

#### Scenario: Background worker claims only voice jobs
- **WHEN** `VoiceGenerationBackgroundWorker` polls for queued jobs
- **THEN** it queries only jobs where `JobType == "generate_voice_asset"` and `Status == "Queued"`
- **AND** it ignores any queued jobs where `JobType == "generate_visual_asset"`.

#### Scenario: Accidental duplicate dispatch reuses active Job
- **WHEN** an operator or client accidentally dispatches voice generation for the same requirement, candidate count, and narration configuration while a Job is already "Queued" or "Running"
- **THEN** the system identifies the matching batch idempotency key and returns the existing active Job without enqueuing duplicate work.

#### Scenario: Intentional regeneration creates new revision
- **WHEN** an operator intentionally requests "Regenerate Voice" (optionally with adjusted pronunciation guidance) after a previous batch completes
- **THEN** the system creates a new explicit `GenerationRevision` (e.g. revision 2) with a distinct idempotency key, allowing a fresh generation cycle while preserving historical voice candidates.

---

### Requirement: Pre-dispatch voice eligibility and upstream lineage revalidation
The system SHALL revalidate production eligibility immediately before creating and dispatching a voice generation `Job`. Voice generation SHALL be permitted IF AND ONLY IF:
1. The parent `ContentItem` has a current active `Storyboard` (`IsCurrent == true`);
2. The `Storyboard` is in status `Approved`;
3. The `Storyboard` is NOT stale (`IsStale == false`);
4. The embedded `AssetPlan` is in status `ReadyForGeneration`;
5. The specified `AssetRequirement` belongs to the approved `StoryboardVersion` and has `AssetType == "TtsVoiceover"`;
6. The upstream linked `Script` is in status `Approved` and is current;
7. The exact `ScriptVersion` snapshot matches the `ScriptVersionId` captured on the approved `StoryboardVersion`.

If any condition is violated, the system SHALL reject the dispatch request with HTTP 400 Bad Request or HTTP 409 Conflict, returning a structured actionable blocker message, and SHALL NOT create a `Job` or synthesize narration from outdated or unapproved editorial intent.

#### Scenario: Voice generation dispatch succeeds for approved non-stale lineage
- **WHEN** an operator dispatches voice generation for a `TtsVoiceover` requirement on a ContentItem with approved, current, non-stale Storyboard and Script
- **THEN** pre-dispatch validation passes and the voice generation Job is persisted and queued.

#### Scenario: Voice generation blocked when Storyboard is unapproved or stale
- **WHEN** an operator attempts to dispatch voice generation for a Storyboard in "Draft" or whose `IsStale` evaluates to true
- **THEN** the backend rejects the request with HTTP 409 Conflict and message "Cannot generate voice from an unapproved or stale Storyboard. Reconcile and approve the Storyboard first."
- **AND** no Job record is created and no provider execution occurs.

---

### Requirement: Exact narration derivation and normalization from approved ScriptVersion
Before provider dispatch, the system SHALL deterministically derive and normalize spoken narration text exclusively from the approved `ScriptVersion` snapshot.

Narration normalization rules:
1. The system SHALL extract narration text scene-by-scene from the ordered `ScriptScene` collection (`Hook`, `Problem`, `Insight`, `Climax`, `CallToAction`), preserving chronological scene sequence;
2. Spoken narration SHALL include ONLY `ScriptScene.NarrationText`;
3. The system SHALL EXCLUDE visual prompts (`ScriptScene.VisualPrompt`), camera directions, scene type labels, and editorial notes;
4. The system SHALL merge scene narration segments with appropriate natural pause separators (e.g. double newline or paragraph break);
5. The system SHALL calculate a SHA-256 fingerprint (`NarrationFingerprint = SHA256(NormalizedNarrationText)`) and record it in the generation parameters snapshot;
6. If editorial spoken wording must change, that change SHALL require a new `Script` revision and re-approval; the voice generator SHALL NOT silently rewrite or paraphrase script text.

#### Scenario: Spoken narration text derived exclusively from ScriptScenes
- **WHEN** the voice generation service prepares the request payload for a Script with 5 scenes
- **THEN** it concatenates the 5 `NarrationText` strings in exact scene order
- **AND** visual prompts and scene headers are excluded from the synthesized audio payload
- **AND** the computed `NarrationFingerprint` is stored in the Job execution snapshot.

---

### Requirement: Provider-neutral VoiceProfile identity, history, and language/locale separation
The voice generation architecture SHALL isolate editorial voice intent from provider-specific voice IDs through a provider-neutral `VoiceProfile` and `IVoiceGenerationProvider` abstraction.

A provider-neutral `VoiceProfile` SHALL encapsulate:
- `ProfileKey` / `ProfileId` (stable identifier, e.g. `"ia-simple-es-default"`);
- `ProfileName` (e.g. `"IA Simple ES Narrator"`);
- `Language` (editorial language, e.g. `"es"`, matching channel definition);
- `ConfiguredLocale` (explicit locale/accent mapping, e.g. `"es-ES"`, `"es-419"`, `"es-MX"`, or `"neutral-latam"`);
- `SpeakingStyle` (e.g. `"Explanatory"`, `"Conversational"`, `"Documentary"`);
- `Tone` (e.g. `"Credible"`, `"Approachable"`, `"Modern"`);
- `Warmth` (e.g. `"MediumHigh"`, `"Neutral"`);
- `Energy` (e.g. `"Dynamic"`, `"Calm"`);
- `Pace` (e.g. `"Normal"`, `"Brisk"`, matching script WPM target);
- `PronunciationHints` (collection of word/phonetic mappings, e.g. `{"GPT-4": "ge-pe-te cuatro"}`).

**VoiceProfile Snapshot Invariant:**
The effective `VoiceProfile` used for each generation batch SHALL be immutably snapshotted in `GeneratedAsset.GenerationParametersSnapshot` and included in the batch configuration fingerprint. Modifying channel or profile configuration later SHALL NOT alter the interpretation, metadata, or audit lineage of historically generated assets.

**Language vs. Locale Invariant:**
Editorial language (`Language = "es"`) and provider-specific accent/voice selection SHALL be modeled as separate concerns. `ConfiguredLocale` informs provider prompting and synthesis but does not require a deterministic locale-to-prebuilt-voice mapping; initial Gemini voice selection remains provider configuration. The domain SHALL NOT hard-code `"es-ES"` as canonical for `IA Simple ES` unless explicitly configured.

The `VoiceGenerationRequest` record SHALL contain:
- `JobId` (`Guid`) and `CorrelationId` (`string`);
- `ContentItemId`, `ChannelId`, `ScriptId`, `ScriptVersionId`, `AssetRequirementId` (`Guid`);
- `Language` (`string`, e.g. `"es"`);
- `ConfiguredLocale` (`string?`);
- `NarrationText` (`string`, normalized spoken text);
- `VoiceProfile` (`VoiceProfile`, effective neutral voice profile);
- `PronunciationGuidance` (`string?`, optional operator overrides for this batch);
- `CandidateCount` (`int`, 1 or 2);
- `TargetAudioFormat` (`string`, canonical `"audio/wav"`).

#### Scenario: Neutral VoiceProfile mapped to channel and snapshotted immutably
- **WHEN** voice generation is dispatched for channel `IA Simple ES` with `Language = "es"`
- **THEN** the service resolves the active VoiceProfile and dispatches the neutral request
- **AND** the full VoiceProfile state is recorded in `GeneratedAsset.GenerationParametersSnapshot`
- **AND** a subsequent edit to the channel's default VoiceProfile does not alter the historical asset record.

---

### Requirement: Canonical PCM 16-bit WAV audio master storage in MinIO
All generated voice audio artifacts produced by Content Factory SHALL be stored as canonical uncompressed PCM WAV masters:
- **Audio Container**: WAV container (`.wav`, MIME `audio/wav`);
- **Encoding**: PCM 16-bit signed integer;
- **Channel Layout**: Mono (1 channel);
- **Sample Rate**: The provider-returned sample rate (e.g. 24000 Hz, 44100 Hz, or 48000 Hz) SHALL be preserved and recorded without intermediate downsampling or lossy transcoding;
- **No Transcoding Subsystem**: This change SHALL NOT introduce an MP3/AAC transcoding pipeline. Gemini TTS PCM streams SHALL be packaged directly into standard WAV headers.

The system SHALL store audio binaries in canonical MinIO object storage (`content-factory-assets` bucket) under deterministic hierarchical keys:
`content-factory/{environment}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav`

Storage operations SHALL:
1. Verify `ContentType == "audio/wav"`;
2. Compute and verify SHA-256 checksum during upload;
3. Record exact object file size in bytes;
4. Ensure MinIO buckets remain private and unexposed;
5. Expose audio streams to authenticated frontend clients via secure backend streaming endpoints (`GET /api/generated-assets/{id}/audio-stream`) supporting HTTP range headers (`Accept-Ranges: bytes`, `Content-Range`) for HTML5 `<audio>` playback.

#### Scenario: PCM WAV audio stored in MinIO with deterministic key
- **WHEN** a voice provider returns PCM audio data
- **THEN** the storage service wraps/verifies the PCM 16-bit mono WAV container and streams it to MinIO under key `content-factory/development/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav`
- **AND** records SHA-256 checksum, file size, sample rate, bit depth (16), and channels (1) in `GeneratedAsset`.

#### Scenario: Authenticated audio streaming endpoint serves audio stream
- **WHEN** an authenticated operator requests playback via `GET /api/generated-assets/{id}/audio-stream`
- **THEN** the backend streams the audio from MinIO with `Content-Type: audio/wav`
- **AND** unauthenticated requests are rejected with HTTP 401 Unauthorized.

---

### Requirement: Gemini TTS programmatic provider adapter (`GeminiTtsProvider`)
The system SHALL implement `GeminiTtsProvider` behind `IVoiceGenerationProvider` using Google AI / Gemini programmatic REST API or official SDK.

Provider implementation rules:
1. The provider SHALL communicate directly over HTTPS with the Gemini / Google AI API endpoints;
2. The runtime integration SHALL be fully programmatic and autonomous, suitable for unattended background `Job` execution;
3. The provider SHALL NOT execute or depend on interactive CLI tools (`gemini` CLI, `antigravity` CLI, interactive terminal logins, or developer shell processes);
4. The provider SHALL translate the neutral `VoiceProfile` and `NarrationText` into Gemini-compatible audio generation parameters (voice selection matching configured locale, speaking rate, pitch/style instructions);
5. The provider SHALL package the returned PCM stream into a standard 16-bit mono WAV master container;
6. Gemini API credentials (`GEMINI_API_KEY`) and endpoint settings SHALL reside strictly in backend configuration and environment variables, never committed to source code or returned in client payloads;
7. The provider SHALL return a standardized `VoiceGenerationResult` containing output audio byte streams, MIME type `audio/wav`, sample rate, channel count (1), bit depth (16), measured audio duration in seconds, provider identifier (`"Gemini"`), model name, and execution telemetry.

#### Scenario: Gemini provider synthesizes narration via API
- **WHEN** `GeminiTtsProvider` executes a valid `VoiceGenerationRequest`
- **THEN** it sends the programmatic API request to the Google/Gemini endpoint with configured credentials
- **AND** packages the resulting PCM audio into a valid 16-bit mono WAV stream, extracts duration and sample rate, and returns a successful `VoiceGenerationResult`.

---

### Requirement: Deterministic development mock voice provider (`MockVoiceGenerationProvider`)
The system SHALL provide a `MockVoiceGenerationProvider` implementing `IVoiceGenerationProvider` for local development, automated testing, and CI environments when live Gemini API credentials are not configured or available.

The mock provider SHALL:
1. Accept `VoiceGenerationRequest` without requiring external network access;
2. Simulate realistic asynchronous execution latency (e.g. 500ms - 1500ms);
3. Synthesize a deterministic, valid 16-bit mono PCM WAV audio binary stream (with valid RIFF/WAVE header, 24000 Hz or 44100 Hz sample rate, and synthetic tone payload) whose byte length corresponds to realistic spoken duration;
4. Calculate realistic `DurationSeconds` derived from narration word count and script pacing (e.g. `WordCount / (PacingWpm / 60.0)`);
5. Support deterministic failure simulation via prompt/text tokens (e.g. `[mock:retryable-failure]`, `[mock:action-required-failure]`, `[mock:timeout]`);
6. Populate realistic telemetry (mock duration ms, mock cost $0.0015, provider `"Mock"`);
7. Clearly identify `Provider = "Mock"` in all output metadata so synthetic audio is never confused with live Gemini production audio.

#### Scenario: Mock provider generates valid WAV candidate in local development
- **WHEN** running in development mode with `VOICE_PROVIDER=Mock`
- **THEN** `MockVoiceGenerationProvider` generates a valid PCM 16-bit mono WAV binary stream with accurate duration and sample rate metadata
- **AND** the Job succeeds with `Provider = "Mock"` and realistic telemetry.

#### Scenario: Mock provider simulates transient retryable error
- **WHEN** voice generation is dispatched with narration containing "[mock:retryable-failure]"
- **THEN** `MockVoiceGenerationProvider` returns a transient 503 Provider Unavailable result
- **AND** the Job transitions to `FailedRetryable` with normalized error code `"PROVIDER_TRANSIENT_503"`.

---

### Requirement: GeneratedAsset audio metadata and measured duration tracking with configurable timing policy
The system SHALL reuse the canonical `GeneratedAsset` entity for produced voice audio artifacts, recording:
- `Id` (`Guid`, unique identifier);
- `ContentItemId` and `ChannelId`;
- `StoryboardId` and `StoryboardVersionId` (authorizing planning snapshot);
- `AssetRequirementId` (authorizing `TtsVoiceover` requirement);
- `JobId` (originating execution job);
- `VariantIndex` (`int`, 1-based index for candidate variants);
- `AssetType` = `"TtsVoiceover"`;
- `MediaType` = `"Audio"`;
- `StorageProvider` = `"MinIO"`;
- `StorageKey` (deterministic audio object storage path);
- `ContentType` = `"audio/wav"`;
- `FileSizeBytes` (`long`);
- `DurationSeconds` (`double`, actual measured audio duration in seconds);
- `ChecksumSha256` (`string`, SHA-256 hash of audio binary);
- `Provider` (e.g. `"Gemini"`, `"Mock"`);
- `ProviderModelOrWorkflow` (`string`);
- `GenerationParametersSnapshot` (`string`, JSON snapshot of VoiceProfile, sample rate, channels, bit depth, narration fingerprint, pronunciation hints, candidate index);
- `Status` (`PendingReview`, `Approved`, `Rejected`);
- `RejectionReason` (`string?`);
- `ReviewedAtUtc` and `ReviewedByEmail` (`string?`);
- `IsSelectedForAssembly` (`bool`, indicates active selection for downstream video assembly);
- `CreatedAtUtc` and `UpdatedAtUtc`.

The system SHALL preserve the distinction between:
- `Script.EstimatedDurationSeconds`: Editorial estimate computed from word count and pacing WPM;
- `GeneratedAsset.DurationSeconds`: Actual measured/produced audio duration.

The system SHALL NOT overwrite historical Script estimated duration with measured audio duration.

**Configurable Timing Tolerance Invariant:**
Timing deviation thresholds SHALL be evaluated against a configurable `TimingTolerancePolicy` (defined in channel or system configuration, e.g. `NoticeThresholdSeconds` default 2.0s, `WarningThresholdSeconds` default 4.0s). The system SHALL NOT treat timing thresholds as immutable hardcoded constants.

The UI SHALL calculate `TimingDeviation = MeasuredDuration - EstimatedDuration` and classify it according to the active policy:
- `Normal`: `|TimingDeviation| <= NoticeThresholdSeconds`
- `Notice`: `NoticeThresholdSeconds < |TimingDeviation| <= WarningThresholdSeconds`
- `Warning`: `|TimingDeviation| > WarningThresholdSeconds`

#### Scenario: Generated voice asset stores measured duration and evaluates timing deviation policy
- **WHEN** a voice generation Job finishes for a Script with `EstimatedDurationSeconds = 45.0`
- **THEN** the persisted `GeneratedAsset` records actual measured `DurationSeconds = 47.5`
- **AND** the Script's estimated duration remains unmodified at 45.0s
- **AND** the Voice Production overview surfaces a `+2.5s` deviation classified against the configured policy (Notice badge).

---

### Requirement: Multi-candidate voice generation and batch idempotency
The system SHALL support generating 1 or 2 voice candidate variants in a single generation request. Each output SHALL be persisted as an independent `GeneratedAsset` record linked to the same `AssetRequirementId` and batch `JobId`, with unique `VariantIndex` (1, 2).

The canonical batch idempotency key SHALL be computed as:
```
ConfigFingerprint = SHA256($"{NormalizedNarrationText}:{CanonicalVoiceProfileJson}:{PronunciationGuidance}:{ProviderName}")
IdempotencyKey = SHA256($"{StoryboardVersionId}:{AssetRequirementId}:{CandidateCount}:{GenerationRevision}:{ConfigFingerprint}")
```

`CanonicalVoiceProfileJson` SHALL deterministically include every effective VoiceProfile semantic: profile key/name, language, configured locale, speaking style, tone, warmth, energy, pace, and pronunciation hints (with hint keys canonically ordered).

Resilience & idempotency rules:
1. Accidental duplicate dispatch with matching active `IdempotencyKey` SHALL reuse the active `Job`;
2. Intentional regeneration SHALL increment `GenerationRevision = maxRevision + 1`, generating fresh candidates while preserving historical assets for audit;
3. `VariantIndex` belongs to individual `GeneratedAsset` records, NOT `Job` identity.

#### Scenario: Multi-candidate voice generation creates two distinct audio assets
- **WHEN** an operator requests 2 voice candidates for a TtsVoiceover requirement
- **THEN** the system generates 2 candidate audio streams
- **AND** persists 2 `GeneratedAsset` records sharing the same `AssetRequirementId` and `JobId`, with `VariantIndex: 1` and `VariantIndex: 2`
- **AND** both are placed in status "PendingReview" with `IsSelectedForAssembly = false`.

---

### Requirement: Mandatory human voice QA listening, review, and assembly selection
Generated narration audio SHALL NEVER automatically become approved or selected for assembly. The system SHALL enforce explicit human QA review by an operator with `EDITORIAL` role:
1. `PendingReview` -> `Approved`: Operator listens to candidate audio and approves it. The system marks `Status = Approved`, sets `IsSelectedForAssembly = true`, and records `ReviewedAtUtc` and `ReviewedByEmail`. Atomically, all sibling candidates for the same `AssetRequirementId` have `IsSelectedForAssembly` set to `false`.
2. `PendingReview` -> `Rejected`: Operator rejects candidate, providing a mandatory non-empty `RejectionReason` (e.g. "Mispronounced brand name in scene 2", "Pacing too fast"). The system marks `Status = Rejected`, records `RejectionReason`, `ReviewedAtUtc`, `ReviewedByEmail`, and sets `IsSelectedForAssembly = false`.
3. `Select Candidate`: Operator can switch the active assembly candidate among approved candidates, atomically updating `IsSelectedForAssembly` to true on the target asset and false on siblings.
4. Exactly ONE approved candidate with `IsSelectedForAssembly == true` SHALL satisfy the voice requirement for future video assembly.
5. A `PendingReview` or `Rejected` voice candidate SHALL NOT be selected for assembly.
6. `TtsVoiceover` candidate review and selection SHALL be handled only by voice-service semantics; visual-service semantics SHALL fail closed for a voice asset.

#### Scenario: Human editorial operator approves voice candidate
- **WHEN** an operator with EDITORIAL role approves voice candidate 1
- **THEN** candidate 1 status becomes "Approved" and `IsSelectedForAssembly` becomes true
- **AND** candidate 2 for the same requirement has `IsSelectedForAssembly` set to false
- **AND** an audit log event is recorded for "GeneratedAsset.Approved".

#### Scenario: Rejection requires non-empty reason
- **WHEN** an operator attempts to reject a voice candidate without a reason string
- **THEN** the backend rejects the request with HTTP 400 Bad Request and message "Rejection reason is required."

---

### Requirement: Editorial pronunciation guidance and regeneration
The system SHALL enable editorial operators to supply provider-neutral pronunciation guidance (e.g. phonetic spellings, acronym expansions, stress markers) when dispatching or regenerating voice assets, WITHOUT modifying the approved canonical `Script` narration text.

Rules:
1. Pronunciation guidance SHALL be captured in the `VoiceGenerationRequest` and preserved in `GeneratedAsset.GenerationParametersSnapshot`;
2. If editorial spoken wording itself must change, the operator SHALL revise the `Script`, which transitions the script to `Draft` and requires re-approval;
3. Pronunciation guidance adjustments SHALL NOT alter the `Script` entity or its scene narration texts.

#### Scenario: Adjust pronunciation guidance without changing approved Script
- **WHEN** an operator provides pronunciation guidance `{"API": "a-pe-i"}` and clicks "Regenerate"
- **THEN** the voice generation job receives the pronunciation guidance and increments `GenerationRevision`
- **AND** the upstream `Script` narration text and approval status remain completely unchanged.

---

### Requirement: Upstream staleness gating and historical audio preservation
If an upstream `Script` or `Storyboard` is revised, superseded, or modified after voice assets have been generated:
1. All existing `GeneratedAsset` records in PostgreSQL and MinIO SHALL be preserved intact for audit, lineage, and compliance history;
2. Historical voice assets SHALL dynamically evaluate `IsEligibleForAssembly = false` because their `StoryboardVersionId` no longer matches the current active approved `StoryboardVersionId`;
3. Future video assembly and render pipelines SHALL refuse to consume voice assets whose lineage is stale or superseded;
4. The system SHALL require fresh voice generation against the new approved `StoryboardVersion`.

#### Scenario: Script revision preserves old audio but invalidates assembly eligibility
- **WHEN** a Script is revised and Storyboard is reconciled to version 2
- **THEN** existing voice assets from version 1 remain stored in PostgreSQL and MinIO
- **AND** queries evaluating `IsEligibleForAssembly` return false for version 1 voice assets
- **AND** the Voice Production studio indicates that fresh voice generation is required for version 2.

---

### Requirement: Error classification, sanitized telemetry, and bounded retries
The system SHALL classify voice generation failures into canonical failure categories:
1. `retryable-transient`: Gemini API rate limits (HTTP 429), temporary 500/502/503/504 server errors, network timeouts. The worker SHALL decrement `AutomaticRetriesRemaining` and retry with exponential backoff up to `MaxAttempts` (3). If retries are exhausted, status transitions to `FailedRetryable` or `FailedActionRequired`.
2. `action-required`: Invalid API credentials, unsupported voice profile, blocked prompt policy, permanent 4xx rejection. The job SHALL transition immediately to `FailedActionRequired` without automatic retry.
3. `non-retryable-input`: Malformed request payloads rejected before queueing (HTTP 400).

Resilience rules:
- Prior `JobAttempt` records SHALL NEVER be overwritten or erased upon automatic or manual retry;
- `SanitizedErrorMessage` SHALL exclude API keys, tokens, or raw internal URLs;
- Operators with `TECHNICAL` role SHALL be able to manually retry `FailedRetryable` and `FailedActionRequired` jobs after correcting credentials or configuration.

#### Scenario: Action-required failure stops immediately with sanitized error
- **WHEN** Gemini API returns HTTP 401 Invalid API Key
- **THEN** the Job transitions to `FailedActionRequired` with `ErrorCode = "PROVIDER_AUTH_FAILURE"`
- **AND** `SanitizedErrorMessage` displays "Voice provider authentication failed. Check configured API credentials."
- **AND** no automatic retry loop is initiated.

---

### Requirement: Operational attention and dashboard observability
Voice production failures and unreviewed candidate batches SHALL surface in Dashboard Attention:
1. `FailedActionRequired` voice jobs and exhausted `FailedRetryable` jobs SHALL surface with high urgency linking directly to Content Detail with the technical diagnostics drawer open;
2. Generated voice batches in `PendingReview` SHALL surface in editorial attention when voice generation completes;
3. The system SHALL NOT create an `EditorialTask` for every queued or running job.

#### Scenario: Voice failure surfaces in dashboard attention
- **WHEN** a voice generation job fails with status `FailedActionRequired`
- **THEN** the Dashboard Attention widget reflects the operational failure count
- **AND** clicking the item navigates directly to the Voice Studio with diagnostics open.

---

### Requirement: Role-based authorization (`EDITORIAL` vs `TECHNICAL`)
The backend SHALL enforce explicit role-based access control on all voice generation endpoints:
- `EDITORIAL` role (or development GOD mode) is REQUIRED to:
  - Request voice generation and regeneration;
  - Provide pronunciation guidance;
  - Listen to candidate audio streams;
  - Approve, reject, or select voice candidates for assembly.
- `TECHNICAL` role (or development GOD mode) is REQUIRED to:
  - Inspect provider execution diagnostics, attempt history, and sanitized failure details;
  - Trigger manual retries on failed voice jobs;
  - Update voice provider configuration.
- Non-authorized users SHALL be rejected with HTTP 403 Forbidden.
- Playback and production overview visibility for `EDITORIAL` does not grant technical diagnostics access.

#### Scenario: Non-authorized user cannot approve voice candidate
- **WHEN** an authenticated user without EDITORIAL role attempts to approve a voice candidate
- **THEN** the backend rejects the request with HTTP 403 Forbidden.

---

### Requirement: Angular 21 full-width Voice Production Studio
The frontend SHALL provide a dedicated Voice Production Studio within the Content Detail view (under Storyboard / Production navigation alongside Visual Assets) built on Angular 21, PrimeNG 21, and Tailwind CSS 4 adhering to the canonical full-width operational layout contract:
1. **Header & Progress Toolbar**: Displays voice requirement status, configured `VoiceProfile` badge, estimated vs. measured duration summary, timing deviation badge (evaluated against active `TimingTolerancePolicy`), active running jobs indicator, and "Generate Voice" action.
2. **Narration & Scene Breakdown Panel**: Displays the approved Script text scene-by-scene (Hook, Problem, Insight, Climax, CTA) with word counts, estimated durations, and pronunciation guidance triggers.
3. **Voice Candidate Audio Cards**: High-density cards for each candidate variant displaying:
   - Waveform or lightweight audio player controls (Play, Pause, Seek, Volume, Current Time / Duration);
   - Review status badge (`PendingReview`, `Approved`, `Rejected`);
   - "Selected for Assembly" star/badge;
   - Measured duration vs. estimated script duration comparison with policy-based badge;
   - Technical metadata chips (Provider, Model, Sample Rate e.g. 24kHz, Channels: 1, 16-bit WAV, File Size);
   - Quick action buttons ("Approve & Select", "Reject" opening reason modal, "Download Audio").
4. **Pronunciation Guidance Modal**: Dialog allowing operators to specify phonetic substitutions and pronunciation tips before dispatching regeneration.
5. **Technical Job Diagnostics Drawer**: Expandable drawer for TECHNICAL operators showing attempt history, duration ms, cost USD, provider name, correlation ID, and sanitized error messages with "Retry Job" button.
6. **Full-Width Desktop Layout (>= 1280px)**: Spans 100% available viewport width with side-by-side narration context and candidate audio cards without arbitrary max-width constraints.
7. **Responsive Mobile Layout (~390px)**: Stacks narration preview and candidate audio cards cleanly with touch-friendly playback controls and drawer-based rejection modal without horizontal overflow.

#### Scenario: Full-width Voice Studio layout on desktop
- **WHEN** an operator opens the Voice Production Studio on desktop (>= 1280px)
- **THEN** the narration script panel and voice candidate comparison utilize the full available screen width
- **AND** audio player controls, duration comparisons, and Approve/Reject buttons remain immediately accessible.

#### Scenario: Mobile responsive voice review
- **WHEN** an operator reviews voice candidates on mobile (~390px)
- **THEN** candidate audio cards stack vertically with responsive play/pause buttons and a sticky action footer.

---

### Requirement: Live Gemini TTS smoke test procedure
The system and verification suite SHALL distinguish between mock-provider validation and live Gemini provider execution. A live smoke test SHALL require explicit `RUN_LIVE_GEMINI_TTS_TESTS=true` opt-in in addition to valid development credentials:
1. The verification suite SHALL execute a small end-to-end smoke test synthesizing an approved script excerpt through `GeminiTtsProvider`, uploading audio to MinIO, and verifying authenticated playback;
2. The verification test SHALL record provider name, model, measured duration, sample rate, and HTTP status WITHOUT logging secrets or API tokens;
3. If live credentials are not available, the system SHALL record `LIVE_GEMINI_TTS_SMOKE_TEST: NOT_EXECUTED` without claiming live Gemini validation and without blocking core domain implementation.

#### Scenario: Live Gemini smoke test records execution result without secrets
- **WHEN** live Gemini credentials are present and the smoke test is executed
- **THEN** real audio is generated via Gemini API, uploaded to MinIO as 16-bit mono WAV, and played back
- **AND** verification reports success, recording model and duration while keeping API keys redacted.
