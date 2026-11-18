## 1. Domain, Models, and Storage Infrastructure

- [x] 1.1 Add `JobTypes.GenerateVoiceAsset = "generate_voice_asset"` and update `JobType.All` in `JobModels.cs`
- [x] 1.2 Define `VoiceProfile` (with `ProfileKey`, `Language`, `ConfiguredLocale`), `VoiceGenerationRequest`, `VoiceGeneratedMediaOutput` (with sample rate, channels, bit depth), `VoiceGenerationResult`, and `IVoiceGenerationProvider` in `Modules/Ai`
- [x] 1.3 Update `IStorageService` and `MinioStorageService` to support deterministic audio object keys (`.../audio/voice/{assetRequirementId}/{generatedAssetId}.wav`)
- [x] 1.4 Define voice DTOs (`VoiceProductionOverviewDto`, `VoiceRequirementProductionDto`, `DispatchVoiceGenerationRequest`, `PronunciationGuidanceDto`) and `TimingTolerancePolicy` in `ContentModels.cs`
- [x] 1.5 Configure initial `VoiceProfile` preset for channel `IA Simple ES` (`Language = "es"`, configurable Spanish explanatory voice profile)

## 2. Voice Provider Adapters

- [x] 2.1 Implement `MockVoiceGenerationProvider` with synthetic PCM 16-bit mono WAV audio byte fixtures (valid RIFF headers + tone), realistic duration calculation from word count, and token-based failure simulation (`[mock:retryable-failure]`, `[mock:action-required-failure]`)
- [x] 2.2 Implement `GeminiTtsProvider` using Google AI / Gemini programmatic REST API / SDK with PCM-to-WAV packaging, configuration-driven voice selection, pronunciation hint injection, unattended execution, and credential sanitization
- [x] 2.3 Register `IVoiceGenerationProvider` implementations in `Program.cs` with configurable provider resolution via `VOICE_PROVIDER`

## 3. Voice Generation Application Service & Background Queue Worker

- [x] 3.1 Implement `IVoiceGenerationService` and `VoiceGenerationService` with pre-dispatch eligibility validation (checking current approved non-stale Storyboard and ScriptVersion lineage)
- [x] 3.2 Implement deterministic narration extraction and normalization from `ScriptVersion.Scenes` with SHA-256 fingerprinting and VoiceProfile snapshotting into `GenerationParametersSnapshot`
- [x] 3.3 Implement batch idempotency key calculation, accidental duplicate dispatch prevention, and intentional regeneration revision handling
- [x] 3.4 Implement atomic database job claiming and execution loop with `JobAttempt` immutable recording and error classification (`retryable-transient`, `action-required`, `non-retryable-input`)
- [x] 3.5 Implement candidate review actions (`Approve`, `Reject` with mandatory reason, `SelectCandidate` for assembly) with atomic single-selection per requirement
- [x] 3.6 Implement `VoiceGenerationBackgroundWorker` hosted service claiming exclusively `JobType == "generate_voice_asset"`
- [x] 3.7 Implement `GET /api/generated-assets/{id}/audio-stream` supporting authenticated audio streaming and range requests
- [x] 3.8 Implement `VoiceGenerationController` with `EDITORIAL` vs `TECHNICAL` role-based authorization

## 4. Backend Automated Testing & Resilience Suite

- [x] 4.1 Create unit tests for `VoiceGenerationService` (dispatch eligibility, stale storyboard blocking, narration text extraction, VoiceProfile snapshotting, and batch idempotency)
- [x] 4.2 Create unit tests for `MockVoiceGenerationProvider` and `GeminiTtsProvider` error classification, WAV header structure, duration computation, and payload mapping
- [x] 4.3 Create integration tests for capability-scoped atomic job claiming, attempt history preservation, automatic bounded retries, and candidate review concurrency
- [x] 4.4 Create security authorization tests validating `EDITORIAL` vs `TECHNICAL` endpoint restrictions

## 5. Angular 21 Voice Production Studio (Frontend)

- [x] 5.1 Create `VoiceStudioComponent` inside `features/content/` adhering to the canonical full-width operational layout contract
- [x] 5.2 Create `VoiceNarrationPanelComponent` showing scene-by-scene script narration text, word counts, estimated duration, and pronunciation guidance triggers
- [x] 5.3 Create `VoiceCandidateCardComponent` with HTML5 audio playback (play/pause/seek), waveform/timeline scrubber, review badges, assembly star, metadata chips (PCM 16-bit WAV, sample rate, channels, size), and timing deviation chip evaluated against `TimingTolerancePolicy`
- [x] 5.4 Create `PronunciationGuidanceModalComponent` and `RejectVoiceCandidateModalComponent`
- [x] 5.5 Integrate `JobDiagnosticsDrawerComponent` for technical attempt telemetry, latency, cost, and manual retry trigger
- [x] 5.6 Add "Voice" navigation tab in Content Detail with batch generation progress indicators
- [x] 5.7 Create frontend component unit tests for voice studio and candidate card components

## 6. Verification, Live Smoke Test, Canonical Docs & Merge Readiness

- [x] 6.1 Execute full automated test suite (backend dotnet test + frontend ng test)
- [ ] 6.2 Validate Gemini TTS provider with a live approved Spanish narration and verify returned audio format/metadata/storage/playback without exposing credentials
- [x] 6.3 Perform human-assisted multi-resolution visual and interactive verification (1440x900, 1920x1080, 768x1024, 390x844)
- [x] 6.4 Synchronize canonical documentation (`docs/canonical/03_DOMAIN_MODEL_V2.md`, `09_AI_ROUTING_AND_REASONING.md`, `14_API_ASYNC_CONTRACTS.md`)
- [ ] 6.5 Run `openspec validate` and complete DeepSeek independent cross-review before merge
