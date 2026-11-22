# Design

## Context

See proposal.md and the two delta specs for the observable contract. CF-05 (#16) supplies URL analysis, persisted findings, original verification, translation, source errors, and the associated contracts. Existing TruthSource, ContentIdea, Script, Content Workspace, and specialized production studies remain the source of truth.

The canonical stack lock specifies Angular 22 standalone, PrimeNG 22, Aura, Tailwind 4, PrimeIcons 8, Signals/RxJS and Playwright. The embedded OpenSpec context still mentions Angular/PrimeNG 21; that is treated as stale context because the higher-authority canonical stack lock and this request explicitly require version 22.

## Goals / Non-Goals

**Goals:**

- Add one independently routable, authenticated vertical slice from URL analysis through approved evidence, proposals, selected idea, persisted script and honest conceptual preview.
- Compose existing API contracts and services without duplicating domain state or specialized studio engines.
- Preserve provenance, evidence separation, approval gates, versioning, optimistic concurrency and stale-lineage rules.
- Deliver a full-width, dense, accessible, light/dark responsive workspace with bounded internal scrolling.
- Produce deterministic browser evidence for real navigation and async UI behavior in Apply or Verify, plus a short human validation procedure and seed data.

**Non-Goals:**

- No new durable entities, migrations, orchestration service, queue, provider, endpoint façade, n8n workflow, renderer, MP4 artifact, or publication integration.
- No redesign or migration of existing studios. Only the new route and one shell access are in scope.
- No claim that Mock results are real AI/provider results.

## Decisions

### 1. New screen as an orchestration-free composition layer

The route component owns view state and coordinates existing services; it does not become a second domain workflow. It loads channel/content context, delegates analysis, approval, idea generation, selection, script operations, and reread-after-save to the existing API services and DTOs.

Alternative rejected: a new backend editorial-studio façade or workflow framework. It would duplicate authorization, state transitions, and lineage authority.

### 2. Persist through existing aggregates and one ContentItem per angle

Analysis reuses CF-05 persistence and creates/reopens the normal ContentItem context. Each selected proposal/angle gets its own ContentItem and Script lineage. The UI never stores a second local source/idea/script database and never places multiple independent scripts into one ContentItem.

Alternative rejected: a studio-specific aggregate or client-only draft store, because backend authority, reload persistence, and stale detection would be lost.

### 3. Explicit state machine in the UI

Signals represent the local display state: input, analyzing, findings, evidence expansion, TruthSource approval, idea generation, selected idea, script generation/saving, reload, provider identity, and error/permission states. RxJS remains the boundary for HTTP and asynchronous streams. Each action has success, empty, loading, recoverable error, blocking error, and disabled/permission rendering as applicable.

Long operations remain Jobs when the existing API returns Jobs; the screen observes the existing status contract and does not block the application or invent polling semantics.

### 4. Evidence presentation has separate typed regions

A finding view renders independent regions for verified verbatim, Spanish translation, labeled paraphrase, provenance/F-ID, and real locator. The component derives visibility from evidence metadata and never treats translation or synthesis as a quote. No speaker or timestamp is synthesized by the UI.

Alternative rejected: a single rich-text citation block, because it makes attribution and verification ambiguous.

### 5. Three-area responsive composition using shared primitives

Desktop uses the canonical PageContainer → PageHeader → PageToolbar → PageContent composition and a full-width three-area grid: source/analysis, findings/evidence, and proposals/script/production. PrimeNG Button, InputText, Select, Tag, Panel/Accordion, ScrollPanel and Tabs are used only where their semantics fit. Existing Aura tokens, .dark, ThemeService, shared page primitives and Tailwind layout utilities provide styling; no alternate theme or normal-control CSS layer is introduced.

At 768px and below, the areas become Tabs or a compact ordered flow. Finding/proposal regions use internal scroll containers; the page avoids unnecessary desktop scroll and horizontal overflow.

### 6. Authorization remains backend-first

The UI may hide or disable an action based on the current permission state, but it must handle HTTP 401/403 and backend validation as authoritative. TruthSource approval is a real human action; idea generation cannot be dispatched while unapproved. Existing role capabilities and audit/version rules are reused.

No secrets, raw provider errors, private chain-of-thought, or internal stack traces are exposed. Provider identity and Mock status are displayed from returned metadata.

### 7. No data model or API migration by default

The design assumes all required CF-05 and editorial pipeline contracts already exist. No migration is planned. If implementation discovers a missing contract that changes the specs or lineage semantics, Apply must stop and request an OpenSpec update rather than introduce an ad-hoc endpoint or table.

### 8. Verification is browser-real and provider-honest

Apply/Verify adds or runs browser checks with the configured Chromium executor for route navigation, async states, approval gate, variable findings/proposals, script persistence after reload, second independent ContentItem, unsupported URL, permission failure, responsive layouts, and regression links. Tests use explicit Mock fixtures/adapters where real provider credentials are unavailable and label results accordingly; a Mock check never counts as live AI evidence.

Backend/project checks use the configured dotnet test ContentFactory.sln command when implementation reaches Apply/Verify. This proposal does not run checks.

### 9. Orchestration safety

The screen calls existing API boundaries only. It does not invoke n8n directly. Development keeps provider adapters/mock configuration explicit and prevents accidental production workflow invocation. No new workflow or callback is introduced by this change.

## Risks / Trade-offs

- [CF-05 contract gap] → Keep the UI adapter thin; stop for an OpenSpec update if a required operation is absent rather than inventing a façade.
- [Variable DTO richness] → Render only fields present in the real DTO and label estimated duration; omit unavailable controls.
- [Large findings or long scripts] → Bound findings/proposal panes with internal ScrollPanel and allow the editorial document itself to scroll.
- [Concurrent edits or stale lineage] → Send existing version tokens, surface conflict/stale responses, reread authoritative state, and never silently overwrite.
- [Provider confusion] → Display provider and Mock identity from contracts; never label synthetic data as real.
- [Existing routes regression] → Add one route/access entry only and verify every pre-existing studio route remains reachable.
- [Responsive density] → Validate 390×844, 768×1024, 1440×900, and 1920×1080 in browser evidence in both themes where applicable.
- [Reference wireframe overfitting] → Use the PNG only for hierarchy, distribution, density and cleanliness; do not copy its markup, hardcoded copy/times, or styling.

