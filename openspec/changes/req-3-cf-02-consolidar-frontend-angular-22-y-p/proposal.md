# Proposal

## Why

El frontend debe tener una instalación limpia y reproducible con el estado objetivo ya observado en el repositorio: Angular 22, PrimeNG 22, Tailwind CSS 4 y `@primeuix/themes` 3. La presencia residual de `@primeng/themes` 21 y la documentación canónica que aún declara Angular/PrimeNG 21 pueden producir confusión, incompatibilidades o regresiones durante el mantenimiento; este cambio consolida únicamente esas referencias y dependencias sin alterar la experiencia editorial existente.

## What Changes

- Reconfirmar, antes de editar, las versiones efectivas en `src/web/package.json`, `src/web/package-lock.json`, las importaciones de temas y los consumidores reales de `@primeng/themes`.
- Mantener Angular 22, PrimeNG 22, Tailwind CSS 4 y `@primeuix/themes` 3 como versiones objetivo; alinear solo dependencias o herramientas que resulten efectivamente incompatibles.
- Retirar `@primeng/themes` 21 únicamente si el censo demuestra que no tiene consumidores; si los tiene, aplicar la menor corrección compatible y conservar la dependencia necesaria.
- Regenerar o ajustar el lockfile para que represente exclusivamente las dependencias realmente usadas y verificar instalación limpia, tests locales y build reproducible.
- Preservar navegación, vistas, estilos, temas claro/oscuro, responsive y comportamiento de dashboard, discovery, canales, contenido, guiones, storyboard, imágenes y voz; no rediseñar pantallas.
- Actualizar únicamente las referencias canónicas afectadas, incluyendo `22_TECH_STACK_LOCK.md` y cualquier fuente canónica que siga afirmando Angular/PrimeNG 21.
- Añadir o ajustar solo pruebas focalizadas y comprobaciones de navegador necesarias para demostrar que la normalización no cambia el comportamiento existente.

### Non-goals

- No se añaden pantallas, componentes ni capacidades editoriales nuevas.
- No se realiza rediseño UX, migración de backend, cambio de API, configuración de GitHub Actions, CI/CD, despliegue ni pruebas en producción.
- No se cambian las versiones objetivo ni se introducen frameworks, infraestructura o abstracciones nuevas.

## Capabilities

### New Capabilities

- frontend-toolchain-consolidation: Consolidar Angular 22, PrimeNG 22 y Tailwind CSS 4 con instalacion limpia y build reproducible.

### Modified Capabilities

None. No functional requirement changes are authorized; existing frontend capabilities remain the behavioral contract.

## Impact

- **Código y dependencias:** `src/web/package.json`, `src/web/package-lock.json`, configuración/importaciones del frontend solo si la evidencia de incompatibilidad lo exige.
- **Verificación:** comandos declarados del frontend, tests existentes, build y checks de navegador ejecutados por el executor configurado cuando sean necesarios para probar navegación, viewport, async UI y temas.
- **Documentación:** únicamente `22_TECH_STACK_LOCK.md` y referencias canónicas directamente afectadas.
- **Alcance de datos/API/seguridad:** sin cambios de backend, persistencia, contratos, autorización ni despliegue.

`specs/` se omite deliberadamente mediante `skip_specs: true`: crear un delta especulativo convertiría una preservación de comportamiento en un cambio funcional no solicitado.
