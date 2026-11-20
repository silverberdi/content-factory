# Provider route census

| Capability | Selection | Synchronous boundary | Job/worker boundary | Explicit Mock route | Real-provider failure |
| --- | --- | --- | --- | --- | --- |
| Editorial (DeepSeek/Mock) | AiProviderRouter.ResolveProvider, AI_DEFAULT_PROVIDER, AiRoutingContext.PreferredProvider | BuildTruthSource, GenerateIdeas, GenerateScript, ReviewScript, PlanStoryboard, ReviewStoryboard | Editorial services persist AIRecommendation; no separate editorial worker exists | PreferredProvider=Mock or AI_DEFAULT_PROVIDER=Mock | Structured AiCapabilityResult failure; no recommendation and no Mock content |
| Visual (Comfy/Mock) | VisualGenerationService._configuredProviderName (VISUAL_PROVIDER / VisualGeneration:Provider) | Dispatch creates a Job; provider runs in ProcessJobAsync | VisualGenerationBackgroundWorker and ProcessQueuedJobsAsync | Provider configuration explicitly names Mock | Job/attempt failure; no GeneratedAsset persisted |
| Voice (Gemini/Mock) | VoiceGenerationService._configuredProviderName (VOICE_PROVIDER / VoiceGeneration:Provider) | Dispatch creates a Job; provider runs in ProcessJobAsync | VoiceGenerationBackgroundWorker and ProcessQueuedJobsAsync | Provider configuration explicitly names Mock | Job/attempt failure; no audio GeneratedAsset persisted |

Before this change, the census found implicit Mock selection in DeepSeek key absence, DeepSeek HTTP/exception paths, and missing visual/voice provider resolution. Those routes now return safe errors or failed Jobs. Existing deterministic tests set Mock explicitly.
