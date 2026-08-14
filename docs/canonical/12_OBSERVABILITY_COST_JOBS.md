# Observability, Cost and Jobs

## Goal

Operators must understand meaningful system health without reading raw server logs.

## Operational metrics

Track at minimum when capabilities exist:
- time to TruthSource;
- time to approved script;
- time to rendered video;
- time to publish;
- generation failure rate;
- regeneration rate;
- human review minutes;
- cost by stage;
- cost per approved video;
- cost per published video;
- provider latency/failure;
- throughput.

## Failure classes

Every Job failure must be categorized:

`retryable-transient`
Examples: timeout, provider 5xx.

`action-required`
Examples: expired credentials, invalid configuration, account disconnected.

`non-retryable-input`
Examples: unsupported file, validation failure.

Do not show an operator "Error 500" as the primary explanation if a meaningful class is known.

## Retries

- bounded;
- backoff where appropriate;
- idempotency required for side effects;
- retry count visible;
- no infinite automation loops.

## Audit timeline

Important objects expose a human-readable timeline such as:
- source suggested by DeepSeek;
- TruthSource generated;
- operator edited;
- Gemini reviewed;
- script approved;
- render failed/retried;
- video published.

Technical logs remain available server-side; the product surfaces operational meaning.
