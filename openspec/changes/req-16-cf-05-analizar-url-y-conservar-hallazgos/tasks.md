# Tasks

## 1. Inventario y contrato backend

- [x] 1.1 Inventariar `ContentItem`, fuentes, evidencias, `TruthSource`, `AIRecommendation`, router y endpoints existentes; verificar en el diff de diseño que se reutiliza el modelo actual y que no se introduce un subsistema paralelo.
- [x] 1.2 Definir los DTOs y estados mínimos de invocación/lectura para éxito, fallo y `NotAnalyzable`; verificar con pruebas de contrato que no exponen detalles de scraping ni crean UI.
- [x] 1.3 Resolver la clave de idempotencia para URL normalizada, canal y contexto compatible; verificar con una prueba concurrente que los reintentos no duplican contenido, evidencia ni recomendación.

## 2. Captura segura y texto analizable

- [x] 2.1 Extender la captura existente para validar HTTP(S), DNS, loopback/red privada, cada redirección, tiempo y tamaño; verificar con pruebas negativas que ningún destino inseguro se solicita ni persiste.
- [x] 2.2 Conservar título, idioma, autor comprobable, texto relevante y localizadores sin tratar HTML crudo como transcripción; verificar con fixture de artículo y captura fallida que el estado y diagnóstico son correctos.
- [x] 2.3 Implementar el estado recuperable de ausencia de texto y la entrada manual identificada; verificar que video/podcast sin transcripción no inventa citas, timestamps o hallazgos y que el texto manual sí puede analizarse.

## 3. Hallazgos y TruthSource en borrador

- [x] 3.1 Añadir únicamente la persistencia mínima para hallazgos, procedencia y tipos de contenido, con migración EF Core solo si el inventario lo exige; verificar el modelo y SQL generado sin cambios no relacionados ni migración productiva.
- [x] 3.2 Integrar el análisis con `IAiProviderRouter` y `AIRecommendation`, conservando racional estructurado, evidencia, proveedor, modelo, fallas y Mock explícito; verificar que no aparece un segundo router ni se persiste chain-of-thought.
- [x] 3.3 Validar cardinalidad variable y trazabilidad desde cero hasta múltiples hallazgos; verificar fixtures de fuente sin soporte, una idea y varios hallazgos, cada uno con evidencia y localizador reales.
- [x] 3.4 Validar verbatim, traducción, paráfrasis e inferencia antes de persistir; verificar que el texto no comprobable se degrada y que nunca se inventan hablante o timestamp.
- [x] 3.5 Mantener versiones, autorización y gate editorial; verificar que todo resultado queda en `Draft`, que no se crea `Approved` automáticamente y que el downstream permanece bloqueado.

## 4. Persistencia, lectura y datos de verificación

- [x] 4.1 Implementar la consulta de recuperación del resultado persistido sin nueva llamada a IA; verificar que devuelve estado, procedencia, evidencia, hallazgos y referencia al borrador después de reabrir el `ContentItem`.
- [x] 4.2 Añadir fixtures/seed deterministas para artículo público simulado, fuente sin soporte, transcripción manual, URL insegura y proveedor Mock; verificar que los datos no se confunden con contenido real ni requieren infraestructura adicional.
- [x] 4.3 Documentar el recorrido real verificable con una URL pública accesible y los formatos efectivamente soportados; verificar que el registro no afirma acceso productivo, UI ni formatos no comprobados.

## 5. Verificación y cierre normal

- [x] 5.1 Ejecutar las pruebas focalizadas de captura, cardinalidad, verbatim, transcripción ausente, SSRF, idempotencia, persistencia, permisos, versiones y estados; verificar resultados observables y errores sanitizados.
- [x] 5.2 Ejecutar la suite configurada `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 POSTGRES_USER=minime_app POSTGRES_PASSWORD=[REDACTED] dotnet test ContentFactory.sln -m:1 -p:UseSharedCompilation=false -nodeReuse:false`; verificar que no se atribuye evidencia a producción y que las regresiones relevantes pasan.
- [x] 5.3 Ejecutar OpenSpec Verify nativo contra este cambio y corregir cualquier discrepancia entre proposal, specs, design, tasks e implementación; verificar que cada Requirement mantiene al menos un Scenario.
- [x] 5.4 Preparar la revisión y PR mediante el flujo normal de Mini Me, incluyendo evidencia determinista, sincronización documental canónica solo si cambian condiciones documentadas y confirmación de que no hay UI ni infraestructura modificada.

> Apply evidence for 5.3 (2026-11-21): strict validation passed (`1/1`, no issues); every delta requirement has at least one scenario. The installed OpenSpec `1.13.1` CLI exposes no `verify` command (`openspec verify` exits with `unknown command 'verify'`), so the native command was attempted and its limitation is recorded; the supported validation plus candidate-scoped implementation/test coverage review found and corrected unstable finding identifiers. No published-version evidence is used.

> Apply evidence for 5.4 (2026-11-21): current worktree contains the backend-only implementation, deterministic walkthrough, focused integration tests, and no frontend/infrastructure changes. `git diff --check` passed and the configured suite passed (`137/137`, exit 0). Review/PR material is prepared in the change artifacts and walkthrough; no PR was published and no immutable final candidate SHA is asserted or required during Apply.

> Apply correction evidence (2026-11-21): the focused integration suite passed (`10/10`, exit 0) and the configured solution suite passed (`137/137`, exit 0) after correcting IPv6 unique-local (`fc00::/7`), IPv4-mapped private IPv6, and manual-text provenance behavior. Manual text is analyzed through the text-only capture path without remote URL capture; the submitted URL remains associated provenance. Strict OpenSpec validation passed (`1/1`, no issues). `git diff --check` passed. No published-version evidence is used and no final immutable candidate SHA is asserted during Apply.
