# AI Routing and Reasoning

## Principle

Capability is domain.
Provider is configuration.

Code requests:
- suggest_topics
- score_source
- build_truth_source
- generate_ideas
- generate_script
- review_script
- review_visual
- generate_visual_asset
- generate_voice_asset
- analyze_metrics

Code MUST NOT encode business behavior as `callDeepSeekForX()`.

## Provider posture

The application is fully provider-agnostic. All AI operations are requested by capability through `IAiProviderRouter`:

Configurable Reasoning Providers (OpenAI-compatible APIs):
- configurable reasoning provider for text and reasoning capabilities;
- topic/source suggestions;
- scoring;
- idea generation;
- script drafts;
- analysis;
- operational reasoning.

Multimodal Providers (e.g. Gemini):
- configurable alternate for reasoning/multimodal;
- primary real voice generation provider (Gemini TTS API) producing WAV 16-bit mono masters;
- preferred candidate for multimodal/visual/video analysis where it performs better;
- load-balancing/quality comparison option.

Local models:
- later cheap classification, deduplication, embeddings and preliminary processing.

## Routing precedence

1. channel override, if configured;
2. capability override;
3. global default.

Default configuration must be seeded so the system works without manual routing setup.

## Modes

A capability may define:
- primary;
- fallback;
- reviewer;
- parallel-candidate.

Do not implement all modes in the first slice unless required; the domain/config model must not prevent them.

## Observability

Track per invocation:
- capability;
- provider;
- model;
- input/output usage available;
- latency;
- estimated/actual cost;
- status;
- correlation/job id;
- policy/prompt version.

## Recommendations

Important AI decisions generate structured `AIRecommendation` records.
Store rationale/evidence suitable for audit.
Never require or persist private chain-of-thought.

## Budgets

Support configurable provider/channel budget thresholds and alerts.
Initial enforcement may be informational, but unbounded automated spending is forbidden.
