# Roadmap

The target is useful production capability in weeks, not months.

## Wave 0 — Foundation Control Center

Outcome:
secure, responsive product shell usable by real operators.

Value slice:
`foundation-access-control-center`

Includes:
- solution scaffold;
- Angular PWA;
- .NET backend;
- MySQL dev/prod configuration;
- local GOD auth;
- Google production auth boundary;
- SYSTEM_OWNER;
- TECHNICAL/EDITORIAL roles;
- invitations/role administration baseline;
- audit foundation;
- dashboard foundation;
- channel registry;
- first channel;
- seed data;
- light/dark;
- responsive verification.

## Wave 1 — Editorial Evidence Loop

Outcome:
source → TruthSource → ContentIdea → Script with human review.

Capabilities:
- ContentItem;
- manual source submission;
- unified DiscoveryCandidate;
- multiple source provenance;
- TruthSource generation/edit/approval;
- ContentIdea generation;
- scripts/versioning;
- EditorialTasks;
- dashboard attention/pipeline widgets.

AI:
DeepSeek default, provider routing configuration seeded.

## Wave 2 — Discovery Intelligence

Outcome:
system proposes what is worth producing.

Capabilities:
- source catalog;
- RSS/feed discovery;
- DeepSeek topic/source suggestion;
- Gemini alternate routing;
- deduplication;
- freshness;
- relevance scoring;
- production priority;
- discovery dashboard.

## Wave 3 — Production Pipeline

Outcome:
approved script can become an approved short video.

Capabilities:
- storyboard;
- asset plan;
- Comfy integration;
- TTS;
- subtitles;
- render pipeline;
- technical QA;
- visual QA;
- partial regeneration;
- MinIO lineage;
- video review UX.

## Wave 4 — Publication Operations

Outcome:
approved master becomes platform-ready publications.

Initial targets:
YouTube Shorts first, model ready for Instagram/TikTok.

Capabilities:
- PlatformAccount;
- publication packages;
- manual publication tracking;
- idempotent automation where platform API is viable;
- scheduling;
- failure recovery.

## Wave 5 — Metrics and Learning

Outcome:
factory recommends what to scale/change/pause.

Capabilities:
- metric snapshots;
- channel score;
- weekly analysis;
- Experiment;
- AI recommendations;
- channel states active/scaling/paused/archived;
- cost/revenue analysis.

## Wave 6 — Resilience, Backup and Scale

Some resilience work may enter earlier when required.

Capabilities:
- automated MySQL backup to Google Drive;
- MinIO off-site archive copy;
- backup dashboard health;
- retention;
- restore verification;
- deeper notifications/push if zero incremental service cost;
- multi-language/channel scaling.
