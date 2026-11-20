# Tasks

## 1. Reconfirmar el estado efectivo

- [x] 1.1 (escenario `frontend-toolchain-consolidation:1:1`) Censar `src/web/package.json`, `src/web/package-lock.json`, `src/web/angular.json` y `src/web/src/app/app.config.ts`; entregar una nota de evidencia que distinga estado real, contexto canónico y referencias a `@primeng/themes`.
- [x] 1.2 (escenario `frontend-toolchain-consolidation:1:1`) Confirmar la compatibilidad del entorno Node y npm de ejecución local con Angular 22 y el alcance de los checks existentes en `src/web/package.json`; verificar que no se incluyen backend, CI/CD, despliegue ni producción.

## 2. Consolidar dependencias y compilación

- [x] 2.1 (escenario `frontend-toolchain-consolidation:1:1`) Corregir únicamente dependencias o herramientas frontend cuya incompatibilidad quede demostrada por el censo, conservando Angular 22, PrimeNG 22, Tailwind CSS 4 y `@primeuix/themes` 3 en `src/web/package.json`; verificar el diff mínimo de dependencias.
- [x] 2.2 (escenario `frontend-toolchain-consolidation:1:1`) Retirar `@primeng/themes` 21 solo si no tiene consumidores en `src/web/src/app/app.config.ts`; si los tiene, aplicar la menor corrección compatible y dejar documentada la decisión; verificar con compilación.
- [x] 2.3 (escenario `frontend-toolchain-consolidation:1:1`) Ajustar o regenerar `src/web/package-lock.json` con npm y verificar que refleja exactamente las dependencias usadas, sin cambios no relacionados.

## 3. Verificación y documentación

- [x] 3.1 (escenario `frontend-toolchain-consolidation:1:1`) Ejecutar `npm ci` desde `src/web` en un entorno limpio y registrar resultado reproducible; verificar que no se requiere una edición manual posterior a la instalación.
- [x] 3.2 (escenario `frontend-toolchain-consolidation:1:1`) Ejecutar los tests locales declarados del frontend y el build desde `src/web`; verificar que terminan correctamente y que los tests focalizados cubren cualquier ajuste realmente realizado.
- [x] 3.3 (escenario `frontend-toolchain-consolidation:1:2`) Ejecutar checks focalizados de navegación, async UI, viewport y temas claro/oscuro para dashboard, discovery, canales, contenido, guiones, storyboard, imágenes y voz en Chromium usando `src/web/playwright.config.ts`; verificar evidencia observable de resultados en los viewports aplicables.
- [x] 3.4 (escenario `frontend-toolchain-consolidation:1:2`) Reutilizar los datos seed y fixtures existentes en `src/web/e2e/dashboard-viewports.spec.ts` y `src/web/e2e/discovery.spec.ts` para verificar el recorrido de pantallas afectadas en local, sin exigir producción ni dispositivo físico.
- [x] 3.5 (escenario `frontend-toolchain-consolidation:1:1`) Actualizar únicamente `docs/canonical/22_TECH_STACK_LOCK.md` y las referencias canónicas directamente afectadas en `.agents/CONTEXT.md` para describir el stack confirmado; verificar que no quedan afirmaciones obsoletas sobre Angular/PrimeNG 21 en el alcance de la consolidación.
- [x] 3.6 (escenario `frontend-toolchain-consolidation:1:2`) Revisar el diff completo para confirmar retiro seguro de consumidores/rutas sustituidos, ausencia de rediseño o cambios de producto y trazabilidad de cada ajuste a este change; verificar que el diff es mínimo.

## 4. Cierre gobernado

- [x] 4.1 (escenario `frontend-toolchain-consolidation:1:1`) Ejecutar `openspec validate req-3-cf-02-consolidar-frontend-angular-22-y-p` y el flujo de OpenSpec verify; verificar cero hallazgos críticos y tareas/evidencia coherentes.

## Apply evidence

- Census: `src/web/package.json` and lockfile already targeted Angular 22, PrimeNG 22, Tailwind CSS 4, and `@primeuix/themes` 3. `app.config.ts` imports only `Aura` from `@primeuix/themes/aura`; no `@primeng/themes` consumer exists under `src/web`.
- Runtime/check scope: Node `v22.22.3`, npm `11.16.0`; `npm ci` passed, `npm test -- --watch=false` passed with 53/53 tests, and `npm run build` passed. The package scripts and changes are frontend-only.
- Dependency diff: removed only `@primeng/themes` from `src/web/package.json` and its lockfile entries; no Angular, PrimeNG, Tailwind, or theme preset target changed.
- Browser evidence: `CI=1 npx playwright test --config=playwright.config.ts` executed Chromium 1248 across 1440x900, 1920x1080, 768x1024, and 390x844. Final candidate run: 48/48 passed with observable navigation, async fallback states, responsive viewport, and light/dark theme checks. The focused implementation changes only compacted the existing dashboard grid, preserved mobile branding, and aligned stale localized/duplicate locators; no new product capability was added.
- Documentation/diff review: `.agents/CONTEXT.md` and `docs/canonical/22_TECH_STACK_LOCK.md` now state Angular 22 and PrimeNG 22 stable; the implementation diff contains no product, backend, API, CI/CD, deployment, or production changes.
