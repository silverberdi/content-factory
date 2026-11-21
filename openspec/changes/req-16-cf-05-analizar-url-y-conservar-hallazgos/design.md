# Design

## Context

El cambio se apoya en `ContentItem`, fuentes/evidencias, `TruthSource`, `AIRecommendation`, `IAiProviderRouter` y PostgreSQL/EF Core existentes. La autoridad canónica aplicable es `docs/canonical/00_CANONICAL_AUTHORITY.md`; la especificación base relevante es `openspec/specs/truth-source-evidence/spec.md`. No se añadirá UI ni una arquitectura de ejecución nueva.

## Goals / Non-Goals

**Goals:**

- Proporcionar un recorrido backend idempotente desde canal + URL hasta evidencia persistida, hallazgos variables y borrador recuperable.
- Mantener una frontera clara entre captura segura, texto aportado, análisis de IA, procedencia y gate editorial humano.
- Entregar DTOs de lectura/invocación estables para que el issue 2 no dependa del mecanismo de captura.
- Hacer verificables SSRF, límites de red, cardinalidad, verbatim, ausencia de transcripción, persistencia y regresiones de autorización.

**Non-Goals:**

- No crear UI, rutas de estudio, navegador automatizado, conectores por plataforma, ASR, scraping evasivo ni descarga masiva.
- No sustituir el router de IA, crear fallback silencioso, generar ideas/guiones o cambiar los gates de `TruthSource`.
- No introducir cola, worker, microservicio, base de datos, caché distribuida, n8n ni infraestructura nueva.

## Decisions

### 1. Extender el agregado y servicios de contenido existentes

El análisis se modelará como una operación de `ContentItem` y sus evidencias, con una relación estable entre solicitud normalizada, captura, resultado y recomendaciones. Se reutilizará el modelo de `TruthSource` para el borrador y se añadirá solo la estructura mínima de hallazgo/procedencia que no exista. Se evita un agregado paralelo `Editorial Intelligence` porque duplicaría ciclo de vida, auditoría y autorización.

Alternativa descartada: crear un servicio o esquema independiente para análisis editorial. Aumentaría la superficie de persistencia y rompería la regla de una sola fuente de verdad.

### 2. Captura HTTP con validación en cada salto

La capa de captura validará el esquema antes de abrir conexión, resolverá y comprobará cada destino contra loopback, rangos privados y otros destinos no permitidos, y repetirá la comprobación después de cada redirección. Aplicará límites de tiempo y tamaño y devolverá errores sanitizados. El texto relevante se separará del HTML crudo mediante el servicio existente o su extensión mínima; nunca se tratará una página HTML como transcripción hablada.

Alternativa descartada: confiar únicamente en un bloqueo inicial de hostname o en el cliente HTTP por defecto. No cubre DNS rebinding, redirecciones ni destinos privados resueltos posteriormente.

### 3. Texto analizable explícito y proveedor único

El flujo aceptará texto de artículo/transcripción legítima o texto manual identificado como tal. Si no hay texto, persistirá `NotAnalyzable` con una razón estable. Cuando haya texto, solicitará el capability existente de análisis mediante `IAiProviderRouter`, registrará `AIRecommendation` y conservará solo salida estructurada, evidencia y racional breve; no se creará un segundo router ni se persistirá chain-of-thought.

Alternativa descartada: implementar ASR o conectores específicos para plataformas. Eso ampliaría el alcance y permitiría eludir restricciones de acceso que CF-05 exige respetar.

### 4. Contrato de lectura desacoplado del scraping

La API expondrá un comando de análisis y una consulta por identificador/`ContentItem`, con DTOs que incluyan estado, procedencia, evidencia, hallazgos, tipos de contenido y referencias al borrador. No expondrá selectores, HTML crudo ni detalles del cliente de captura. Las respuestas de fallo y no analizable serán estables y recuperables.

La autorización se aplicará con las políticas existentes para operadores del canal y conservará control optimista de concurrencia. Un resultado existente se devuelve por la clave de idempotencia normalizada antes de iniciar otra ejecución.

### 5. Persistencia y migración condicional

Primero se comprobará si `ContentItemEvidence`, versiones y campos actuales pueden representar procedencia y hallazgos. Solo si la recuperación completa exige columnas/relaciones nuevas se añadirá una migración EF Core acotada, reversible y sin tocar producción durante este cambio. No se usará InMemory como fallback del entorno PostgreSQL; las pruebas aisladas usarán únicamente el proveedor explícitamente autorizado.

### 6. Verificación focalizada y evidencia reproducible

Se añadirán pruebas unitarias/de integración del backend para validación de URL, DNS/redirecciones, límites, captura fallida, artículo, cardinalidad cero/uno/muchos, verbatim y traducción, paráfrasis, texto manual, Mock explícito, idempotencia, lectura sin nueva llamada, permisos, versión y gate `Approved`. Se incluirá fixture/seed determinista de una página de artículo, una fuente sin soporte y una transcripción manual; no se afirmará acceso a producción ni se ejecutarán pruebas de navegador porque el alcance no crea UI.

## Risks / Trade-offs

- [SSRF por resolución o redirección cambiante] → Resolver y validar cada salto, limitar red, tiempo y tamaño, y cubrir destinos privados con pruebas negativas.
- [La extracción de texto conserva ruido del HTML] → Usar el extractor existente, conservar el origen y marcar la evidencia; no convertir contenido no localizado en cita.
- [La IA genera una cita no respaldada] → Validar verbatim contra evidencia antes de persistirlo y degradar a paráfrasis/afirmación por revisar.
- [Reintentos concurrentes duplican resultados] → Clave idempotente normalizada, restricción/consulta transaccional y control optimista.
- [Un Mock se interpreta como resultado externo] → Registrar proveedor y estado de simulación en recomendación y DTO, con pruebas de contrato.
- [La migración amplia el cambio accidentalmente] → Inventario previo, migración solo si es imprescindible, revisión del SQL generado y ningún cambio productivo.

## Migration Plan

1. Inventariar modelos, servicios, endpoints y condiciones de persistencia existentes; identificar el mínimo hueco de almacenamiento.
2. Implementar captura/validación y contrato contra el modelo existente; añadir migración EF únicamente si el inventario lo justifica.
3. Ejecutar pruebas focalizadas y la suite configurada de backend en entorno aislado, incluyendo restauración del borrador y regresión de aprobación/permisos.
4. Verificar el recorrido real con una URL pública accesible y documentar únicamente los formatos efectivamente soportados.
5. Aplicar la migración solo mediante el flujo normal del entorno autorizado; rollback mediante reversión de la migración y retirada del contrato si la verificación falla. No desplegar infraestructura ni modificar producción como parte del cambio.

## Open Questions

No quedan preguntas abiertas que cambien las especificaciones, el enfoque o el desglose de tareas. Los nombres exactos de rutas y columnas se resolverán durante el inventario de implementación sin alterar el contrato.
