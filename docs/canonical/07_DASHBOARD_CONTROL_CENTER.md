# Dashboard Control Center

## Definition

The dashboard exists from the first product slice and grows with each relevant slice.

It is the operational cockpit of the content farm.

Within a few seconds it must answer:

1. How is the factory functioning?
2. What needs attention?
3. Where can I act now?

## Layers

### Global state

Compact presentation of:
- factory health;
- active/pilot/paused channels;
- content throughput;
- production/publishing status;
- critical failures;
- cost summary when available.

Healthy state consumes little visual emphasis.

### Exceptions and opportunities

Prominent only when actionable:
- failed jobs;
- reviews required;
- expiring/fresh content;
- provider/account problems;
- anomalous cost;
- unusually promising source/topic;
- publication failure.

### Action

Common actions should be executable without unnecessary context loss.

Prefer:
- drawer;
- side panel;
- modal;
- contextual action;
- compact drill-down.

Example: script review should support preview → approve/reject/request rewrite → next without forcing repeated navigation.

## Notifications versus Attention

Notification = informative event.
Attention = decision/problem needing action.

Never merge them into a noisy email-like feed.

## Widgets

Dashboard architecture is composable.

Candidate widget families:
- FactoryHealthWidget
- ChannelSummaryWidget
- AttentionWidget
- PipelineHealthWidget
- ProductionWidget
- PublicationWidget
- CostWidget
- PerformanceWidget
- RecentActivityWidget
- BackupHealthWidget

Each slice MUST state whether it:
- adds a widget;
- extends a widget;
- adds a dashboard action;
- has no dashboard effect.

## Charts

A chart must answer a question.
No decorative charts.

Examples:
- content distribution by pipeline stage;
- publications over time;
- views/retention by channel;
- cost per published video;
- job failures by stage;
- throughput;
- revenue versus cost later.

## Personalization

Allow limited useful preferences only.
Do not build a dashboard-builder product.
