## Context

Content Factory enforces a strict separation between editorial planning and production execution. Under `storyboard-production-planning`, `Storyboard`, `StoryboardFrame`, `AssetPlan`, and `AssetRequirement` define WHAT media needs to be produced using provider-neutral editorial descriptors. Under `visual-asset-generation`, Content Factory established the canonical asynchronous production execution architecture (`Job`, `JobAttempt`, database-backed hosted queue worker, atomic claiming, MinIO storage, `GeneratedAsset`, and human QA).

The Storyboard planning layer also generates provider-neutral `TtsVoiceover` requirements derived from the approved `Script`. This design establishes the HOW layer for voice/TTS synthesis, converting `TtsVoiceover` requirements into real spoken narration audio artifacts while strictly reusing the canonical execution infrastructure without introducing secondary job engines or audio-specific queue tables.

## Goals / Non-Goals

**Goals:**
- Reuse the canonical `Job` and `JobAttempt` aggregate under capability and `JobType` `"generate_voice_asset"`, processed by a capability-scoped ASP.NET Core hosted queue worker (`VoiceGenerationBackgroundWorker`) with atomic database claiming filtering strictly by `JobType`.
- Implement the provider-neutral `IVoiceGenerationProvider` adapter boundary with two concrete implementations:
  1. `GeminiTtsProvider`: Programmatic REST API / official SDK integration with Google AI / Gemini for unattended autonomous TTS generation.
  2. `MockVoiceGenerationProvider`: Deterministic local development adapter generating valid PCM 16-bit mono WAV audio binaries and realistic duration telemetry.
- Deterministically extract and normalize spoken narration text exclusively from the approved `ScriptVersion.Scenes`, ignoring visual prompts, camera directions, and editorial notes, and capturing a SHA-256 narration fingerprint.
- Define a provider-neutral `VoiceProfile` domain concept with stable identifier (`ProfileKey`), separating editorial language (`Language = "es"`) from provider-specific accent/locale selection (`ConfiguredLocale`), with the effective profile immutably snapshotted in generation metadata.
- Store produced audio binaries as canonical PCM 16-bit mono WAV masters in canonical MinIO object storage under hierarchical keys (`content-factory/{env}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav`) with authenticated streaming endpoints (`GET /api/generated-assets/{id}/audio-stream`).
- Persist audio technical metadata (sample rate, channel count, bit depth, duration, file size, SHA-256 checksum) in `GeneratedAsset`.
- Compare actual measured audio `DurationSeconds` against the editorial `Script.EstimatedDurationSeconds` using a configurable `TimingTolerancePolicy`, surfacing timing deviations without overwriting historical script estimates.
- Support multi-candidate voice generation (1 or 2 variants) with batch idempotency, intentional regeneration revisions, and atomic single-candidate assembly selection (`IsSelectedForAssembly`).
- Deliver an Angular 21 full-width Voice Production Studio inside Content Detail for narration inspection, audio playback, candidate comparison, pronunciation guidance editing, and human editorial QA.

**Non-Goals:**
- No background music generation, music selection, or sound effect synthesis (deferred to future audio changes).
- No audio mixing, cross-fading, ducking, or audio mastering workstation features.
- No MP3/AAC transcoding pipeline (PCM 16-bit mono WAV is the canonical master; transcoding is deferred to final video rendering/assembly).
- No generic production worker refactoring (workers remain capability-scoped: `VoiceGenerationBackgroundWorker` and `VisualGenerationBackgroundWorker` claim only their respective `JobType`).
- No subtitle generation, timestamp alignment, or subtitle burning (deferred to future subtitle pipeline).
- No video rendering or timeline assembly (deferred to assembly change).
- No interactive CLI execution (`gemini` CLI, `antigravity` CLI) or developer terminal processes in production runtime.
- No secondary job queues, queue tables, or external brokers (RabbitMQ, Kafka, Redis).

## Decisions

### 1. Reuse Canonical Job Aggregate & Capability-Scoped Atomic Database Claiming
**Decision:** Execute all voice generation work via the existing `Job` and `JobAttempt` schema in PostgreSQL, executed by a dedicated `VoiceGenerationBackgroundWorker`.

- `Job.JobType` / `Capability` = `"generate_voice_asset"`.
- `Job.SourceAssetRequirementId` points to the `TtsVoiceover` `AssetRequirement`.
- `Job.StoryboardVersionId` anchors execution to the approved snapshot.
- **Worker Claim Scope Invariant:**
  `VoiceGenerationBackgroundWorker` claims ONLY jobs where `JobType == "generate_voice_asset"` using atomic SQL conditional updates:
  ```sql
  UPDATE jobs 
  SET status = 'Running', started_at_utc = now, updated_at_utc = now 
  WHERE id = @candidateId AND status = 'Queued' AND job_type = 'generate_voice_asset';
  ```
  `VisualGenerationBackgroundWorker` continues to claim exclusively `JobType == "generate_visual_asset"`. No worker claims another capability's jobs. No generic multi-capability worker refactor is introduced in this change.
- **Job Attempt History Invariant:** Every execution attempt (initial, automatic retry, manual retry) creates an immutable `JobAttempt` row with start/completion timestamps, duration ms, cost USD, and sanitized error diagnostics. Historical attempts are never overwritten or deleted.

---

### 2. Provider-Neutral VoiceProfile Identity, History, and Language/Locale Separation
**Decision:** Isolate editorial voice requirements from provider-specific voice IDs using `VoiceProfile` with stable identity, explicit language/locale separation, and immutable generation snapshotting.

```csharp
public record VoiceProfile(
    string ProfileKey,
    string ProfileName,
    string Language,
    string? ConfiguredLocale,
    string SpeakingStyle,
    string Tone,
    string Warmth,
    string Energy,
    string Pace,
    Dictionary<string, string> PronunciationHints
);

public record VoiceGenerationRequest(
    Guid JobId,
    string CorrelationId,
    Guid ContentItemId,
    Guid ChannelId,
    Guid ScriptId,
    Guid ScriptVersionId,
    Guid StoryboardId,
    Guid StoryboardVersionId,
    Guid AssetRequirementId,
    string Language,
    string? ConfiguredLocale,
    string NarrationText,
    VoiceProfile VoiceProfile,
    string? PronunciationGuidance,
    int CandidateCount,
    string TargetAudioFormat = "audio/wav"
);

public record VoiceGeneratedMediaOutput(
    int VariantIndex,
    byte[] AudioBytes,
    string ContentType,
    string FileExtension,
    double DurationSeconds,
    int SampleRateHz,
    int Channels,
    int BitDepth,
    string ProviderModel,
    string GenerationParametersSnapshot
);

public record VoiceGenerationResult(
    bool Success,
    List<VoiceGeneratedMediaOutput> Outputs,
    string? ErrorCode,
    string? SanitizedErrorMessage,
    bool IsRetryable,
    long ExecutionDurationMs,
    decimal? EstimatedCostUsd,
    decimal? ActualCostUsd
);

public interface IVoiceGenerationProvider
{
    string ProviderName { get; }
    IReadOnlyList<string> SupportedAudioFormats { get; }
    Task<VoiceGenerationResult> GenerateVoiceAssetAsync(
        VoiceGenerationRequest request,
        CancellationToken cancellationToken = default);
}
```

*Language vs. Locale Invariant:*
- Editorial language is `Language = "es"` (matching Channel definition).
- `ConfiguredLocale` informs provider prompting and synthesis behavior. The initial Gemini prebuilt voice is provider configuration (`GEMINI_VOICE_NAME`) and does not imply a deterministic locale-to-Gemini-voice-name mapping. Provider-specific mappings, if later required, belong in provider configuration rather than domain semantics.

*VoiceProfile Snapshot Invariant:*
- The effective `VoiceProfile` used for each generation batch is snapshotted into `GeneratedAsset.GenerationParametersSnapshot`.
- Changing the channel's default VoiceProfile later will NOT modify or reinterpret historical `GeneratedAsset` records.

---

### 3. Canonical PCM 16-Bit Mono WAV Audio Master & Storage
**Decision:** Standardize all generated and stored voice assets on uncompressed PCM 16-bit mono WAV format.

- **Audio Container**: WAV (`.wav`, MIME `audio/wav`).
- **Encoding**: PCM 16-bit signed integer.
- **Channels**: Mono (1 channel).
- **Sample Rate**: The provider-returned sample rate (e.g. 24000 Hz, 44100 Hz, or 48000 Hz) is preserved and recorded in metadata without intermediate downsampling.
- **No Transcoding Subsystem**: Raw Gemini TTS PCM streams are packaged directly into standard WAV headers. No MP3/AAC transcoding pipeline is introduced in this change.
- **Storage Path in MinIO (`content-factory-assets`)**:
  ```
  content-factory/{environment}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav
  ```
- **Authenticated Streaming**: Served through `GET /api/generated-assets/{id}/audio-stream` supporting HTTP range requests for HTML5 `<audio>` playback and seeking.

---

### 4. Programmatic Gemini TTS Provider Adapter (`GeminiTtsProvider`)
**Decision:** Implement `GeminiTtsProvider` communicating directly via Google AI / Gemini programmatic REST API / official SDK.

- **Programmatic-Only Execution:** Autonomous HTTP communication over HTTPS with API key authentication (`GEMINI_API_KEY`). Strictly zero spawning of `gemini` or `antigravity` CLI binaries and zero dependence on interactive browser sessions.
- **Payload Translation:** Translates `VoiceProfile` (matching `ConfiguredLocale`) and normalized `NarrationText` into Gemini voice configuration and style prompts. Injects operator pronunciation hints.
- **PCM to WAV Packaging:** Wraps raw PCM only when provider MIME metadata supplies an explicit sample rate; otherwise fails closed. Existing WAV is parsed by RIFF chunks, preserving the provider sample rate and rejecting anything other than PCM 16-bit mono.
- **Authentication:** Sends the Gemini API key through the `x-goog-api-key` request header; logs, diagnostics, and URLs never contain the key.
- **Telemetry:** Measures execution latency, extracts exact duration, estimates API cost based on character count, and redacts all credentials from logs and error payloads.

---

### 5. Deterministic Development Mock Voice Provider (`MockVoiceGenerationProvider`)
**Decision:** Implement `MockVoiceGenerationProvider` to allow full local development and CI testing without external API credentials.

- Generates valid binary PCM 16-bit mono WAV streams with standard RIFF/WAVE headers (24000 Hz or 44100 Hz) and synthetic tone payload.
- Computes realistic `DurationSeconds = WordCount / (PacingWpm / 60.0)`.
- Supports test failure triggers via prompt tokens: `[mock:retryable-failure]`, `[mock:action-required-failure]`.
- Clearly identifies `Provider = "Mock"` in metadata to prevent synthetic audio from being mistaken for live production media.

---

### 6. Exact Narration Text Derivation & Lineage Fingerprinting
**Decision:** Construct the spoken audio input deterministically from the approved `ScriptVersion` snapshot.

- Iterate ordered `ScriptVersion.Scenes` and concatenate `ScriptScene.NarrationText`.
- Explicitly omit `VisualPrompt`, scene titles, and editorial notes.
- Compute `NarrationFingerprint = SHA256(NormalizedNarrationText)`.
- If editorial narration wording must change, operators must revise the `Script` (creating a new `ScriptVersion` and requiring re-approval). Pronunciation guidance (phonetic hints) is passed separately without modifying canonical script text.

---

### 7. GeneratedAsset Audio Metadata & Configurable Timing Tolerance Policy
**Decision:** Persist produced audio in the canonical `GeneratedAsset` entity with audio-specific metadata and evaluate timing deviations against a configurable policy:

- `AssetType = "TtsVoiceover"`
- `MediaType = "Audio"`
- `ContentType = "audio/wav"`
- `DurationSeconds` = actual measured audio duration in seconds.
- `ChecksumSha256` = SHA-256 hash of audio binary.
- `GenerationParametersSnapshot` = JSON snapshot of VoiceProfile, sample rate, channels, bit depth, narration fingerprint, pronunciation guidance, and candidate index.
- **Configurable Timing Tolerance Policy:**
  ```csharp
  public record TimingTolerancePolicy(
      double NoticeThresholdSeconds = 2.0,
      double WarningThresholdSeconds = 4.0
  );
  ```
  Timing deviation thresholds are configuration, NOT hardcoded domain constants. The UI evaluates `TimingDeviation = MeasuredDuration - EstimatedDuration` against the active policy and displays appropriate badges:
  - `Normal`: `|Δ| <= NoticeThresholdSeconds`
  - `Notice`: `NoticeThresholdSeconds < |Δ| <= WarningThresholdSeconds`
  - `Warning`: `|Δ| > WarningThresholdSeconds`

---

### 8. Multi-Candidate Generation, Batch Idempotency & Regeneration
**Decision:** Support generating 1 or 2 voice candidates per batch with deterministic idempotency.

- **Batch Idempotency Key:**
  ```
  ConfigFingerprint = SHA256($"{NormalizedNarrationText}:{CanonicalVoiceProfileJson}:{PronunciationGuidance}:{ProviderName}")
  IdempotencyKey = SHA256($"{StoryboardVersionId}:{AssetRequirementId}:{CandidateCount}:{GenerationRevision}:{ConfigFingerprint}")
  ```
  `CanonicalVoiceProfileJson` deterministically includes all effective profile semantics and sorted pronunciation-hint keys.
- Accidental duplicate dispatch reuses active `Job`.
- Intentional "Regenerate" increments `GenerationRevision = maxRevision + 1`, preserving historical candidates while creating a fresh generation batch.

### 9. Capability Ownership and Authorization
Shared generated-asset actions dispatch by persisted `AssetType`: `TtsVoiceover` is owned exclusively by `VoiceGenerationService`; visual types are owned by `VisualGenerationService`; each service fails closed for the other capability. Editorial operators can retrieve voice production overviews and authenticated candidate playback for QA. Those overview job summaries omit correlation identifiers, errors, and attempt history; `TECHNICAL` remains responsible for retries and full provider/job-attempt diagnostics.

---

### 9. Human Voice QA & Selection Semantics
**Decision:** Enforce mandatory human listening and decision-making by an `EDITORIAL` operator:

- Flow: `PendingReview` -> `Approved` / `Rejected`.
- Approving a candidate sets `IsSelectedForAssembly = true` and atomically clears `IsSelectedForAssembly` on sibling candidates for that requirement.
- Rejecting a candidate requires a non-empty `RejectionReason`.
- Only candidates with `Status == Approved && IsSelectedForAssembly == true && Storyboard.IsCurrent && !Storyboard.IsStale` evaluate to `IsEligibleForAssembly = true`.

---

### 10. Upstream Staleness & Assembly Gating
**Decision:** Dynamic evaluation of assembly eligibility.

- If upstream `Script` or `Storyboard` is revised, existing `GeneratedAsset` rows remain in the database and MinIO for audit history, but their `IsEligibleForAssembly` dynamically resolves to `false`.
- Future video assembly engines will reject stale or superseded voice assets.

---

### 11. Error Classification & Resilience
**Decision:** Classify failures into canonical categories:
- `retryable-transient`: Automatic bounded retry with exponential backoff up to 3 attempts.
- `action-required`: Immediate transition to `FailedActionRequired` with sanitized error message for operator attention.
- `non-retryable-input`: Rejected immediately with HTTP 400.
- `TECHNICAL` operators can manually retry failed jobs after resolving credentials or configuration.

---

### 12. Authorization Matrix
**Decision:** Enforce strict role-based access:

| Operation | Required Role | Non-Authorized Response |
| :--- | :--- | :--- |
| Dispatch Voice Generation / Regenerate | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Review Voice Candidate (Approve / Reject) | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Select Voice Candidate for Assembly | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Set Pronunciation Guidance | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Stream Audio Playback | `EDITORIAL` or `TECHNICAL` | HTTP 401 / 403 |
| Inspect Technical Voice Telemetry / Payloads | `TECHNICAL` (or GOD mode) | HTTP 403 Forbidden |
| Retry Failed Voice Job | `TECHNICAL` (or GOD mode) | HTTP 403 Forbidden |

---

### 13. Frontend Composition: Angular 21 Voice Production Studio
**Decision:** Integrate the Voice Production Studio into Content Detail (`features/content/voice-studio/`) with full-width operational layout:

- Adheres to canonical full-width layout contract (100% width on desktop >=1280px without `max-w-7xl` wrappers).
- Component decomposition:
  - `VoiceStudioComponent`: Main container, progress header, duration deviation overview with policy badge, voice requirement card.
  - `VoiceNarrationPanelComponent`: Scene-by-scene script text breakdown, word counts, pacing estimate, pronunciation guidance button.
  - `VoiceCandidateCardComponent`: Audio playback widget, waveform/timeline scrubber, review status badges, assembly selection star, timing comparison chip (Normal / Notice / Warning), technical metadata chips (16-bit WAV, sample rate, channels, file size), and Approve/Reject buttons.
  - `PronunciationGuidanceModalComponent`: Dialog for entering phonetic hints before dispatching regeneration.
  - `RejectVoiceCandidateModalComponent`: Mandatory rejection reason modal.
  - `JobDiagnosticsDrawerComponent`: Technical attempt telemetry, duration, cost, provider name, and retry button.

---

## Risks / Trade-offs

- **[Risk] Gemini TTS API latency or rate limits** → *Mitigation:* Pure asynchronous execution via `Job` entity, HTTP `202 Accepted`, background hosted queue worker, and bounded automatic retries with exponential backoff.
- **[Risk] Offline development / CI blocked by lack of live Gemini credentials** → *Mitigation:* `MockVoiceGenerationProvider` generates valid synthetic PCM WAV audio streams with realistic durations and deterministic failure simulation.
- **[Risk] Narration timing deviation vs Script estimate affects final video duration** → *Mitigation:* Actual measured `DurationSeconds` is tracked on `GeneratedAsset` and compared against script estimate with visual warning badges evaluated against `TimingTolerancePolicy`; future assembly will consume measured duration as authoritative timing.
- **[Risk] Storage growth from multiple voice candidates** → *Mitigation:* Deterministic object keys structured by channel, content item, and storyboard version allow future retention and lifecycle policies.

## Migration Plan

1. No breaking database schema changes required: `jobs`, `job_attempts`, and `generated_assets` tables already exist and support audio fields (`DurationSeconds`, `MediaType = "Audio"`).
2. Seed initial voice configuration (`VOICE_PROVIDER = "Gemini"` in production with fallback to `"Mock"` in development; `TimingTolerancePolicy` defaults).
3. Rollback: If needed, revert voice services and endpoints; existing visual generation jobs and planning entities remain untouched.

## Open Questions

None. All technical boundaries and domain requirements are resolved.
