# Tasks

## 1. Contracts, route and shell coexistence

- [x] 1.1 Inventory the CF-05 URL-analysis, TruthSource, ContentItem, ContentIdea and Script DTO/service contracts and record any missing operation as an OpenSpec update; verify the implementation uses existing contracts only.
- [x] 1.2 Add the authenticated standalone route `/editorial/studio` and optional `/editorial/studio/:contentItemId`, preserving guards and existing route destinations; verify route navigation and deep-link reload in a real browser.
- [x] 1.3 Add one discreet shell/mobile navigation entry for Editorial Studio without changing existing entries; verify all prior studio routes remain reachable and authorized.

## 2. Editorial Studio state and evidence flow

- [x] 2.1 Implement the full-width shared page composition and three-area desktop workspace using Angular 22 standalone, PrimeNG 22 Aura, Tailwind 4, PrimeIcons, existing ThemeService, `.dark`, semantic tokens and shared page primitives; verify light/dark rendering at 1440×900 and 1920×1080 without avoidable global scroll.
- [x] 2.2 Implement source/channel/angle input, analysis loading, empty, unsupported, insufficient-findings, recoverable-error, blocking-error and permission states; verify real backend error reasons and no fabricated findings or citations.
- [x] 2.3 Render variable persisted findings with bounded internal scrolling, F-ID, title/synthesis, provenance, status and evidence retrieval; verify finding count is API-driven and suggested videos remain visible when evidence is expanded.
- [x] 2.4 Implement separated verbatim/translation/paraphrase/locator presentation based on evidence metadata; verify unverified original text and unsupported speaker/timestamp attribution are omitted.

## 3. Approval, ideas and independent scripts

- [x] 3.1 Implement TruthSource summary/review/approval UI with role-aware disabled states and backend error handling; verify idea generation cannot be invoked while the TruthSource is unapproved.
- [x] 3.2 Integrate existing idea generation with configurable Count and explicit provider identity, rendering variable proposals with title, angle, provenance F-ID, duration metadata and only real actions; verify default Count 3 does not force three findings or proposals.
- [x] 3.3 Persist “Create this video” as an independent ContentItem/selected ContentIdea and connect the existing script generation, editing, saving, reopening and stale-lineage states; verify two angles create two non-overwriting ContentItems.
- [x] 3.4 Verify reload recovery of the same ContentItem, selected proposal and saved Script, including optimistic-concurrency conflict and stale-lineage messaging, using browser evidence.

## 4. Production preview and responsive behavior

- [x] 4.1 Implement the conceptual 9:16 preview/production panel and contextual Storyboard, Visual and Voice links from real resource state; verify no action claims to render, download, or publish a nonexistent MP4.
- [x] 4.2 Implement tablet/mobile Tabs or compact reordering for 768×1024 and 390×844 with keyboard focus, touch targets, readable status messages and no horizontal overflow; verify browser screenshots/observations in light and dark themes.
- [x] 4.3 Confirm existing Storyboard, Visual and Voice views are reused by navigation and not duplicated or redesigned; verify regression navigation from the new screen.

## 5. Seed data, automated evidence and project checks

- [x] 5.1 Add or reuse deterministic development/test data for a supported URL with variable findings, an unapproved TruthSource, an approved TruthSource with at least two proposals, two independent ContentItems, a script, an unsupported URL, and permission-denied states; verify data is explicitly labeled Mock or real and contains no secrets.
- [x] 5.2 Add/run browser checks with the configured Chromium executor for route access, analysis states, evidence separation, approval gate, variable proposals, script persistence, second independent video, unsupported source, permission failure, responsive viewports and existing-route regression; verify observable result evidence is captured and Mock is not reported as live AI.
- [x] 5.3 Run the configured project check `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 POSTGRES_USER=minime_app POSTGRES_PASSWORD=[REDACTED] dotnet test ContentFactory.sln -m:1 -p:UseSharedCompilation=false -nodeReuse:false`; verify the result is recorded without exposing credentials.
- [x] 5.4 Synchronize affected canonical documentation and the human verification walkthrough with route, states, provider labeling, seed data, four required viewports and non-goals; verify documentation matches the implemented contracts.
- [x] 5.5 Run native OpenSpec verify, resolve critical findings, and obtain orchestrator-governed review before completion; verify no implementation task is marked complete without its evidence.
