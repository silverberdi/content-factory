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
Examples: timeout, provider 5xx, network socket exception. Job is marked `FailedRetryable` and queued for bounded backoff retry.

`action-required`
Examples: invalid workflow payload, endpoint unreachable, auth rejected, missing asset requirement. Job is marked `FailedActionRequired`, halted from retry, and surfaces on dashboard attention center.

`non-retryable-input`
Examples: unsupported media format, unapproved storyboard state.

Do not show an operator "Error 500" as the primary explanation if a meaningful class is known.

## Retries

- bounded (`maxAttempts = 3`);
- exponential backoff where appropriate;
- idempotency required for side effects (`idempotencyKey = SHA256(...)`);
- retry count visible on Job and JobAttempt;
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
