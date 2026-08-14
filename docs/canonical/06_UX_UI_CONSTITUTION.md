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

## Width utilization

Do not impose a narrow global content max-width on operational screens.
Use available horizontal space intelligently.
Large screens should gain useful columns/context, not empty margins.

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
