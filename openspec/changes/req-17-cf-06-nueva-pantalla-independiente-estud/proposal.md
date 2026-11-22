# Proposal

## Why

Los operadores necesitan pasar de una URL pública a evidencia revisable, propuestas editoriales y un guion persistente sin abandonar el contexto de la fuente ni confundir contenido simulado con evidencia real. CF-05 ya aporta extracción, hallazgos, verificación de original, traducción y errores de captura; este cambio entrega una pantalla independiente que hace utilizable ese circuito completo, preservando los estudios especializados existentes y sus contratos.

## What Changes

- Añadir un Estudio Editorial independiente, accesible en `/editorial/studio` y opcionalmente con `:contentItemId`, con layout operativo de tres áreas en escritorio y reordenamiento por tabs o flujo compacto en tablet/móvil.
- Integrar en la pantalla el análisis de canal, URL y ángulo opcional, estados vacío/carga/error/fuente no soportada/hallazgos insuficientes, lista acotada de hallazgos con scroll interno y recuperación de evidencias.
- Separar visual y semánticamente texto original cotejado, traducción, paráfrasis y localizador real de fuente; no mostrar citas ni hallazgos de demostración como contenido auténtico.
- Reutilizar los contratos y servicios existentes de TruthSource, ContentItem, ContentIdea y Script para aprobar evidencia, generar propuestas, seleccionar ideas, generar/editar/guardar/reabrir guiones y conservar versionado, concurrencia y linaje stale.
- Mantener videos sugeridos accesibles junto a los hallazgos, usar el generador existente con `Count` configurable y ofrecer únicamente acciones reales para crear un ContentItem o abrir un guion.
- Exigir el gate humano de aprobación de TruthSource antes de generar ideas y conservar la autorización backend como autoridad.
- Añadir solo el acceso puntual del Estudio Editorial al shell/router; mantener intactos Discovery Triage, TruthSource Review Studio, Matriz de Ideas, Script Studio, Storyboard Studio, Visual Studio y Voice Studio.
- Mostrar un panel 9:16 de preview o planificación conceptual y enlaces contextuales a Storyboard, Visual y Voice sin afirmar render, descarga o publicación de un MP4 inexistente.
- Usar Angular 22 standalone, PrimeNG 22 Aura, Tailwind 4, PrimeIcons 8, Signals/RxJS según corresponda, `ThemeService`, `.dark`, tokens Aura y los primitives de layout existentes; incluir accesibilidad, light/dark, viewport-first y responsive.
- Incluir verificación browser automatizada con el ejecutor configurado para los flujos observables y un procedimiento humano reproducible con datos semilla; distinguir Mock explícito de proveedor real.

### Non-goals

- No sustituir, fusionar, migrar, rediseñar ni reestructurar los estudios o rutas existentes.
- No crear backend orquestador paralelo, endpoints fachada, framework de flujos, agente BA, buses, jobs, colas, tablas extra, microservicios ni un motor Kinetic paralelo.
- No conectar Google Stitch, copiar HTML/Tailwind del wireframe ni crear un tema visual o controles propios.
- No implementar renderizador, timeline profesional, sincronización fonema a fonema, compositor audiovisual ni publicación en redes.
- No simular regeneración parcial, citas, timestamps, transcripciones, IA real o acciones de cliente que no existan en los contratos actuales.

## Capabilities

### New Capabilities

- `editorial-independent-studio`: Pantalla independiente desde URL a evidencia revisable, ideas seleccionables y guion persistente, con estados, provenance, aprobación humana, responsive, preview conceptual y no-regresión.

### Modified Capabilities

- `application-shell-dashboard`: Añadir un acceso autenticado, discreto y reconocible al Estudio Editorial sin alterar los accesos ni los flujos de los módulos existentes.

## Impact

- Frontend Angular: nueva ruta y pantalla standalone, composición PrimeNG/Tailwind/Aura, estado local y reutilización de `ApiService`, `ContentItem/detail`, `TruthSourceService`, `ContentIdeaService` y `ScriptService`; adición puntual al shell/router.
- Backend/API: reutilización de endpoints y DTOs existentes de CF-05 y del pipeline editorial; cualquier ausencia de contrato se resolverá mediante el flujo OpenSpec antes de implementar, sin fachada paralela ni persistencia duplicada.
- Dominio: preserva `ContentItem`, `TruthSource`, `ContentIdea`, `Script`, snapshots, aprobación humana, optimistic concurrency y detección de linaje stale; cada ángulo creado desde la misma fuente conserva un ContentItem independiente.
- Verificación: Playwright/browser real para navegación, análisis, aprobación, propuestas, selección, guion, recarga, responsive, permisos, fuente no soportada y no sobrescritura; pruebas del proyecto según el check configurado, sin ejecutar scripts durante esta propuesta.
- Dependencias: CF-05 (#16), especificaciones existentes de URL analysis, TruthSource evidence, content ideas, script pipeline, content workspace y estudios Storyboard/Visual/Voice.
- Contexto canónico leído: `docs/canonical/00_CANONICAL_AUTHORITY.md`, `01_BUSINESS_INTENT.md`, `02_PRODUCT_VISION.md`, `03_DOMAIN_MODEL_V2.md`, `04_IDENTITY_SECURITY.md`, `05_SYSTEM_ARCHITECTURE_V2.md`, `06_UX_UI_CONSTITUTION.md`, `08_DESIGN_SYSTEM.md`, `14_API_ASYNC_CONTRACTS.md`, `16_DEFINITION_OF_DONE.md`, `17_ROADMAP.md`, `18_BACKLOG.md`, `22_TECH_STACK_LOCK.md` y `24_TEST_STRATEGY.md`. Las rutas legacy recomendadas por algunas salvaguardas (`docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, etc.) no existen en este checkout.
