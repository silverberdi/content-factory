## 1. Domain Entities & Persistence

- [x] 1.1 Create `DiscoverySource` and `DiscoveryCandidate` entities with required `ChannelId`, nullable `ExternalUrl`/`NormalizedUrl` for text leads, and mandatory provenance attributes in `Modules/Discovery`
- [x] 1.2 Configure EF Core entity mappings, indices (unique on `(ChannelId, OriginUrl)` for sources; unique on `(ChannelId, NormalizedUrl)` where not null for candidates), constraints, and relationships in `AppDbContext`
- [x] 1.3 Add backend unit tests verifying entity constraints, URL normalization, URL deduplication rules vs text-only submissions, and candidate triage lifecycle transitions

## 2. Source Sync Architecture & API Endpoints

- [x] 2.1 Implement `ISourceSyncAdapter` and `FeedSyncAdapter` with safe HTTP timeouts (10s), custom user-agent, and URL normalization logic
- [x] 2.2 Implement `IDiscoveryService` and `SourceSyncService` for channel-scoped source management, manual URL/text submissions, automated source synchronization, and candidate triage
- [x] 2.3 Add background polling runner (`DiscoveryBackgroundSyncService`) for periodic automated sync of active sources
- [x] 2.4 Implement `DiscoveryController` REST endpoints with role-based authorization (`TECHNICAL` and `EDITORIAL`) and audit logging
- [x] 2.5 Add integration tests covering source registration, sync trigger, candidate submission (URL and text-only), triage promotion/dismissal, and negative authorization

## 3. Seed Data & Environment Verification

- [x] 3.1 Update `DatabaseInitializer` to seed default AI/Tech discovery sources for pilot channel `IA Simple ES`
- [x] 3.2 Seed initial candidate items (including URL leads and text-only leads) with realistic Spanish-language AI editorial leads for validation testing
- [x] 3.3 Verify database migration/creation and startup in development environment

## 4. Frontend Discovery Workspace

- [x] 4.1 Create Angular Discovery models, API client service, and reactive signal state store
- [x] 4.2 Build `DiscoverySourcesComponent` with data table, health badges, status toggle, and contextual create/edit drawer
- [x] 4.3 Build `DiscoveryTriageComponent` with status filters, high-density candidate cards/table, and responsive triage actions
- [x] 4.4 Build `CandidatePreviewDrawer` component for instant article summary/notes inspection, provenance metadata review, and one-click Promote/Dismiss
- [x] 4.5 Build `QuickSubmitModal` ("Quick Submit" / "Add a URL or note for discovery") for rapid manual URL and note submission accessible globally
- [x] 4.6 Add frontend unit and component tests for state transitions and user interactions

## 5. Shell & Dashboard Integration

- [x] 5.1 Add Discovery navigation items (Triage & Sources) with unreviewed candidate badge in the application shell
- [x] 5.2 Extend Dashboard with Discovery Attention widget (pending candidate count, source health indicator) and "Quick Submit" button
- [x] 5.3 Verify responsive behavior on desktop (1440px), tablet (768px), and mobile (390px) viewports in both light and dark themes

## 6. Verification, Canonical Sync & Review Gate

- [x] 6.1 Execute automated test suite (`dotnet test` and Angular `npm test`)
- [ ] 6.2 Perform end-to-end human verification flow: register source, trigger sync, submit manual URL and text lead via Quick Submit, preview in drawer, triage candidate, verify dashboard counters
- [ ] 6.3 Update canonical documentation / backlog status
- [x] 6.4 Prepare DeepSeek cross-review package and verify change readiness
