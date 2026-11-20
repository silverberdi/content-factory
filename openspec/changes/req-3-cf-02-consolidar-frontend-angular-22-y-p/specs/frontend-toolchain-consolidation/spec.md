# Frontend Toolchain Consolidation Specification

## Purpose
Consolidates Angular 22, PrimeNG 22, and Tailwind CSS 4 toolchain with clean installation and build reproducibility.

## ADDED Requirements

### Requirement: Frontend toolchain consolidation with reproducible installation
The frontend application SHALL maintain Angular 22, PrimeNG 22, Tailwind CSS 4, and @primeuix/themes 3 as its standard toolchain with reproducible installation from lockfile and behavior preservation across all surfaces.

#### Scenario: Clean installation and reproducible build
- **GIVEN** current frontend code and compatible Node version
- **WHEN** dependencies are aligned and npm ci and build are executed
- **THEN** installation and build complete cleanly without errors.

#### Scenario: Preserved navigation and existing behavior
- **GIVEN** dashboard, discovery, channels, content, scripts, storyboard, images and voice views
- **WHEN** toolchain consolidation is applied
- **THEN** navigation and behavior are preserved across light and dark modes and responsive viewports.
