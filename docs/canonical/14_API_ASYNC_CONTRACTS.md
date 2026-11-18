# API and Asynchronous Contracts

## API style

REST/JSON with generated OpenAPI documentation.

Use stable resource-oriented endpoints.
Long work returns a Job, not a blocked HTTP request.

Example:
 
 POST `/api/v1/content-items/{id}/storyboards/{storyboardId}/visual-generation`
 → `202 Accepted`
 → Array of `JobDto` records in `Queued` status.
 
 GET `/api/v1/content-items/{id}/storyboards/{storyboardId}/visual-assets`
 → `200 OK`
 → `VisualProductionOverviewDto` with requirement production status, active jobs, candidates, and assembly selection.

 POST `/api/v1/content-items/{id}/storyboards/{storyboardId}/voice-generation`
 → `202 Accepted`
 → Array of `JobDto` records with `JobType: "generate_voice_asset"`.

 GET `/api/v1/content-items/{id}/storyboards/{storyboardId}/voice-assets`
 → `200 OK`
 → `VoiceProductionOverviewDto` with voice requirement production status, active jobs, candidate takes, timing deviation analysis, and VoiceProfile.

 GET `/api/v1/generated-assets/{id}/audio-stream`
 → `200 OK` / `206 Partial Content` (Audio streaming with range header support).

## Errors

Use structured machine-readable errors with:
- code;
- user-safe message;
- correlation id;
- validation details when relevant.

Do not leak stack traces/secrets.

## Concurrency

Backend is authoritative.
Use optimistic concurrency/version token for mutable editorial records.

If two operators edit an old version, reject the stale write with a recoverable UX flow rather than silently overwrite.

## Idempotency

Required for:
- publication;
- externally visible file creation where duplicate is harmful;
- provider/workflow callbacks;
- retryable side effects.

## Callbacks

Public callbacks through Cloudflare require explicit authentication/signature strategy.
Internal callbacks should remain private where possible.
