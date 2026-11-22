# editorial-independent-studio Specification

## Purpose

El Estudio Editorial permite convertir una fuente pública y sus evidencias en propuestas editoriales y guiones independientes, con aprobación humana, procedencia recuperable y estados honestos, sin reemplazar los estudios especializados del producto.

## Requirements

### Requirement: Independent editorial studio route and coexistence

The system SHALL provide an authenticated Editorial Studio at `/editorial/studio`, optionally accepting `:contentItemId` to reopen a persisted production thread, while preserving all existing discovery, evidence, idea, script, storyboard, visual, and voice routes and behaviors.

#### Scenario: Open the new studio without replacing existing modules
- **WHEN** an authenticated operator navigates to `/editorial/studio`
- **THEN** the Editorial Studio loads as an independent screen
- **AND** existing Discovery Triage, TruthSource Review Studio, Idea Matrix, Script Studio, Storyboard Studio, Visual Studio, and Voice Studio remain reachable at their existing routes
- **AND** no existing module is redirected, merged, or substituted.

#### Scenario: Reopen a persisted ContentItem
- **WHEN** an operator opens `/editorial/studio/:contentItemId` for an accessible ContentItem
- **THEN** the studio reloads the same source context, selected proposal, script state, and available evidence
- **AND** the backend remains authoritative for the displayed state.

### Requirement: Analyze a source with honest operational states

Given a channel, URL, and optional editorial angle, the system SHALL allow the operator to request analysis and SHALL expose empty, loading, unsupported-source, insufficient-findings, success, recoverable-error, blocking-error, permission, and explicit-provider states without inventing evidence.

#### Scenario: Analyze a supported URL
- **WHEN** an authenticated operator submits a supported URL with a channel and optional angle
- **THEN** the studio shows an analyzing state
- **AND** after completion it displays the returned source identity and a variable number of persisted findings
- **AND** each finding includes an F-ID, concise title or synthesis, provenance, status, and a way to recover its evidence.

#### Scenario: Unsupported or failed URL
- **WHEN** the submitted URL cannot be captured, is unsupported, or has no valid transcript
- **THEN** the studio shows the real sanitized reason and the source status
- **AND** when applicable it offers manual text entry
- **AND** it shows no fabricated quote, finding, timestamp, or demo content.

#### Scenario: Insufficient findings
- **WHEN** analysis completes without enough usable findings
- **THEN** the studio states that the findings are insufficient and preserves the source/error context
- **AND** it does not manufacture a fixed number of findings.

### Requirement: Separate evidence, translation, and attribution

The system SHALL distinguish verified verbatim text, Spanish translation, labeled paraphrase, and source locator, and SHALL only display original verbatim text when the evidence has been cross-checked.

#### Scenario: Expand a verified finding
- **WHEN** an operator expands a finding with available evidence
- **THEN** verified original text is labeled as verbatim
- **AND** Spanish translation is displayed in a separate labeled region
- **AND** “View source” uses the real available locator
- **AND** any paraphrase is labeled as paraphrase
- **AND** no speaker attribution is shown without supporting evidence.

#### Scenario: Finding without verified verbatim
- **WHEN** a finding has synthesis or translation but no cross-checked original quote
- **THEN** the studio omits the verbatim section or labels the evidence limitation
- **AND** it does not present translated text in quotation marks as original speech.

### Requirement: Human approval gates idea generation

The system SHALL show the current TruthSource approval state and SHALL require the authorized human approval action before invoking existing idea generation.

#### Scenario: Draft TruthSource blocks generation
- **WHEN** an operator attempts to generate video proposals for a draft or unapproved TruthSource
- **THEN** the studio displays the synthesis/findings and an approval or review action when the operator is authorized
- **AND** generation is not invoked before approval
- **AND** unauthorized operators receive the backend permission error without bypass.

#### Scenario: Approved TruthSource generates proposals
- **WHEN** an authorized operator has approved the TruthSource and requests proposals
- **THEN** the studio invokes the existing idea generator with a configurable Count or default Count of 3
- **AND** it identifies the actual provider, including explicit Mock when used
- **AND** it displays loading, error, empty, and success outcomes honestly.

### Requirement: Display accessible variable proposals

The system SHALL keep suggested videos visible below or beside findings and SHALL display each returned proposal with title, angle, provenance F-ID when supported, target or estimated duration when available, and an implemented action to create a video or open its script.

#### Scenario: View proposals without losing findings
- **WHEN** findings and proposals are available
- **THEN** the operator can inspect both panels without an expanded citation hiding the proposal panel
- **AND** the number of proposals is the returned number rather than a mandatory three
- **AND** unsupported actions are absent or disabled with an explanation.

#### Scenario: Select an independent proposal
- **WHEN** an operator chooses “Create this video” for a proposal
- **THEN** the selected idea is persisted for its own ContentItem
- **AND** another proposal from the same source can create a different ContentItem
- **AND** creating the second does not overwrite the first ContentItem, proposal, script, status, or lineage.

### Requirement: Persist and recover the existing script flow

The system SHALL reuse the existing ContentItem, ContentIdea, and Script lifecycle to select an idea, generate or edit a script, save versions, reopen it, and expose Hook, narrative development, verified support, and CTA when those fields exist in the real DTO.

#### Scenario: Generate and save a script
- **WHEN** an operator selects a proposal for an approved TruthSource and requests a script
- **THEN** the studio shows the real asynchronous state
- **AND** the resulting script displays its available structure and evidence relationships
- **AND** saving creates the normal versioned script state with its ContentItem and idea lineage.

#### Scenario: Reload saved script
- **WHEN** the operator leaves and reloads the studio route
- **THEN** the same selected idea and saved script are recovered from persisted state
- **AND** stale lineage, optimistic-concurrency conflicts, rejection reasons, and other existing lifecycle rules remain visible and actionable
- **AND** no second script is silently created for the same selected idea.

### Requirement: Honest production preview and specialized-study links

The system SHALL provide a useful conceptual 9:16 preview or production plan for a proposal with a script, show the real resource status, and link to existing Storyboard, Visual, and Voice studies where supported, without claiming nonexistent MP4 generation, download, timeline, rendering, synchronization, or publication.

#### Scenario: Preview without produced MP4
- **WHEN** a proposal has a script but no produced MP4
- **THEN** the production panel shows the script, conceptual preview/plan, resource state, and contextual links
- **AND** no enabled action claims to generate, download, or publish an unavailable MP4
- **AND** unavailable capabilities are hidden or disabled with an explanation.

### Requirement: Responsive accessible editorial workspace

The system SHALL use the existing Aura theme and semantic tokens, preserve light/dark behavior, use full operational width on desktop, internal scrolling where appropriate, and reorder the three editorial areas for tablet and mobile without horizontal overflow.

#### Scenario: Desktop viewport-first layout
- **WHEN** the operator uses the studio at 1440x900 or 1920x1080
- **THEN** source, findings, and proposals/production areas are discoverable in a dense three-area layout
- **AND** the content spans the useful viewport width without avoidable global scroll
- **AND** finding lists use bounded internal scrolling when they exceed the available area.

#### Scenario: Tablet and mobile layout
- **WHEN** the operator uses the studio at 768x1024 or 390x844
- **THEN** the areas reorder into tabs or a compact flow that still permits review, approval, selection, and script editing
- **AND** there is no horizontal page overflow
- **AND** keyboard focus, labels, status messages, and touch targets remain usable in light and dark themes.
