# UX/UI Product Constitution

## Product experience

The application is an operational PWA, not a CRUD admin site, landing page, ERP clone or email inbox.

The user should understand the system without training but the interface is for competent adult operators, not children.

## Mandatory experience

- light and dark themes from the first release;
- responsive from the first release;
- visually modern, calm and professional;
- compact typography appropriate to data-rich cross-device applications;
- clear hierarchy without giant headings;
- high information density without clutter;
- icons, badges, menus, context menus, dialogs, drawers, tooltips, data grids and charts used where they improve comprehension;
- consistent empty/loading/error/success states;
- keyboard and accessibility fundamentals;
- touch-friendly controls on mobile/tablet.

## Desktop viewport rule

Desktop is viewport-first.

Primary operational information MUST fit inside the useful viewport at common targets such as 1440×900 and 1920×1080 whenever reasonably possible.

Full-page vertical scroll is an exception, not the default layout strategy.

Before adding page scroll, consider:
- responsive grid;
- split panes;
- tabs;
- internal scroll containers;
- pagination/virtualization;
- drawers;
- modal dialogs;
- progressive disclosure;
- drill-down.

Long editorial documents may legitimately scroll.

## Width utilization & Canonical Layout Contract

Operational screens in Content Factory MUST utilize available screen width:
- For viewport widths >= 1280px (1440×900, 1920×1080 and ultrawides): do NOT apply arbitrary centered max-width wrappers (such as `max-w-7xl`) to operational page content;
- Page content width MUST effectively be viewport width minus compact shell/page padding (`px-3 sm:px-4 md:px-5`);
- Data tables, grids, split workspaces, and studio canvases MUST use 100% width;
- Every operational page follows the canonical composition: `PageContainer` -> `PageHeader` (title, metadata, actions) -> `PageToolbar` (search, filters, contextual controls) -> `PageContent`;
- Card wrappers must not be stacked redundantly; use panels only when communicating semantic operational grouping;
- Specialized studios (TruthSource Review, Idea Matrix, Script Studio) retain internal density optimizations while inheriting full width, outer padding, header hierarchy, and responsive behavior.

## Device intent

Desktop:
- complete control center and operational workflows.

Tablet:
- near-desktop operation when screen size permits.

Mobile:
- full capability for urgent/frequent operations including review, approve/reject, quick inspection and failure response;
- hierarchy/reordering may differ from desktop;
- mobile is not read-only.

## Navigation

Navigation is secondary to work.

Preferred top-level conceptual groups:
- Overview
- Content
- Channels
- Publishing
- Analytics
- System

Do not expose every domain entity as a permanent top-level menu.

## Anti-patterns

Forbidden unless explicitly justified:
- hero sections;
- oversized typography;
- huge cards containing one number;
- decorative whitespace causing unnecessary scroll;
- every action navigating to a new page;
- tables as the universal solution;
- sidebar with 15–20 domain entities;
- inbox metaphor as dominant experience;
- low-density "SaaS template" dashboard.
