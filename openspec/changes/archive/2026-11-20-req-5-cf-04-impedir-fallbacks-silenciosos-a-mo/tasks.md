# Tasks

## 1. Censo y contrato de resultado

- [x] 1.1 Censar en código y pruebas todos los puntos de selección, fallback y ejecución de DeepSeek/Mock, Comfy/Mock y Gemini TTS/Mock en respuestas síncronas y Jobs; verificar el inventario mediante una matriz de rutas entregada en el cambio
- [x] 1.2 Inspeccionar los contratos actuales de enrutamiento, errores, Job, JobAttempt y resultados; definir el mapeo mínimo de proveedor y naturaleza real/simulada y verificarlo contra las deltas de las tres capacidades
- [x] 1.3 Implementar la regla común que permite Mock solo por selección explícita y propaga fallo/configuración ausente del proveedor real; verificar con pruebas unitarias que ninguna excepción o configuración faltante produce un éxito Mock implícito

## 2. Generación editorial síncrona y asíncrona

- [x] 2.1 Ajustar generación/revisión editorial para conservar proveedor y naturaleza del resultado, errores estructurados y mensajes sanitizados; verificar éxito Mock explícito, fallo real y configuración ausente con dobles locales
- [x] 2.2 Ajustar Jobs editoriales, reintentos, versiones y trazabilidad para conservar el estado de error sin fabricar contenido; verificar propagación y clasificación con un worker aislado
- [x] 2.3 Añadir o actualizar datos de prueba deterministas para Script/AIRecommendation sin afirmar funcionamiento de DeepSeek real; verificar que las aprobaciones editoriales y contratos API existentes permanezcan compatibles

## 3. Producción visual y de voz

- [x] 3.1 Ajustar generación visual Comfy/Mock para registrar identidad/naturaleza y no persistir recurso simulado tras fallo real; verificar Job fallido, Mock explícito, configuración ausente y reintento con dobles locales
- [x] 3.2 Ajustar generación Gemini TTS/Mock para registrar identidad/naturaleza y no persistir audio/candidato simulado tras fallo real; verificar Job fallido, Mock explícito, configuración ausente y propagación segura con dobles locales
- [x] 3.3 Verificar que los metadatos existentes de GeneratedAsset y candidatos de voz no rompan lineage, versionado, aprobación, selección ni reproducción; ejecutar pruebas de compatibilidad del dominio afectado

## 4. Persistencia, API y presentación existente

- [x] 4.1 Aplicar solo la migración PostgreSQL mínima si el censo demuestra que faltan campos durables, con backfill seguro/no destructivo; verificar migración y lectura de históricos sin etiquetar naturaleza desconocida como real
- [x] 4.2 Actualizar DTOs, códigos/mensajes de error y diagnósticos existentes para exponer proveedor/naturaleza sin secretos; verificar OpenAPI, correlación y ausencia de tokens, claves o URLs internas en respuestas
- [x] 4.3 Ajustar únicamente los puntos existentes de atención/dashboard o badges cuando sea necesario para distinguir fallo o simulación; verificar comportamiento en los estados desktop/tablet/mobile aplicables sin crear una pantalla nueva

## 5. Documentación y verificación de cierre

- [x] 5.1 Sincronizar únicamente los documentos canónicos o contratos afectados por la semántica de proveedor, y verificar que no se hayan modificado decisiones no relacionadas
- [x] 5.2 Ejecutar las pruebas unitarias/integración aisladas de las rutas censadas usando dobles o servidores falsos locales y sin invocar APIs pagadas, n8n real ni producción; verificar evidencia de éxito Mock explícito, configuración ausente, fallo real y propagación
- [x] 5.3 Ejecutar el comando de test configurado del repositorio y registrar sus resultados; si el entorno requerido no está disponible, conservar el fallo como evidencia y no declararlo exitoso
- [x] 5.4 Realizar la prueba humana reproducible con un caso Mock explícito y casos de proveedor real fallido/sin configuración, comprobando etiquetas, errores, estados de Job y ausencia de artefacto simulado; registrar datos seed y observaciones
- [x] 5.5 Ejecutar  para este cambio y solicitar la revisión independiente gobernada por el orquestador; verificar que propuesta, specs, diseño, tareas, evidencia y retiro de fallback silencioso sean coherentes antes del cierre

> Apply evidence for 5.5: `openspec validate --type change --strict --no-interactive --json req-5-cf-04-impedir-fallbacks-silenciosos-a-mo` passed (`1/1`, no issues). The installed OpenSpec `1.13.1` CLI has no `verify` command (`openspec verify` exits with `unknown command 'verify'`), and no independent orchestrator reviewer is available in this worktree. Keep this task unchecked for coordinator Verify/review.
>
> Apply follow-up evidence (2026-11-20): backend dependencies restored successfully and `dotnet test tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj --no-restore` passed (`127/127`, exit 0). A read-only independent-review dispatch was attempted through the available orchestration tool but failed before reviewer execution with `invalid thread-store request: no rollout found`; therefore no independent review was performed and this task remains unchecked.

> Apply follow-up evidence (2026-11-20, second pass): fixed the reviewed visual/voice implicit-provider defaults, classified worker provider-resolution failures, authoritative visual/voice result metadata, successful DeepSeek result metadata, and Comfy error sanitization. The same configured test command passed again (`127/127`, exit 0), and `openspec validate --type change --strict --no-interactive --json req-5-cf-04-impedir-fallbacks-silenciosos-a-mo` passed (`1/1`, no issues). A second read-only independent review executed against the current uncommitted worktree and confirmed those fixes, but remained `BLOCKED` because native `openspec verify` is unavailable (`unknown command 'verify'`) and no immutable final candidate SHA exists yet; task 5.5 remains unchecked for coordinator Verify/review.
