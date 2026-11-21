# Proposal

## Why

CF-05 necesita una capacidad backend reutilizable que convierta una URL fuente, cuando existe texto accesible y verificable, en evidencia editorial y hallazgos trazables que la pantalla independiente del issue 2 pueda consumir. Hoy existen captura, `ContentItem`, evidencias, `TruthSource` y enrutamiento de IA, pero el recorrido específico debe conservar cardinalidad real, límites de seguridad y estados explícitos sin presentar una simulación como resultado real.

## What Changes

- Añadir un flujo de invocación y lectura para analizar una URL asociada a un canal y un ángulo opcional, reutilizando `ContentItem`, evidencias, servicios de captura, `IAiProviderRouter`, `AIRecommendation` y PostgreSQL/EF Core existentes.
- Capturar y conservar URL, metadatos únicamente comprobables, texto relevante y localizadores; rechazar o detener URL inseguras, inaccesibles o fuera de límites con errores explícitos.
- Priorizar artículos y transcripciones legítimamente accesibles; representar como no analizable la ausencia de transcripción y permitir texto manual proporcionado por el usuario sin ASR propio, scraping evasivo, descarga masiva ni conectores por plataforma.
- Persistir hallazgos con identificador local estable, síntesis y valor editorial en español, evidencia original, localizador y distinción explícita entre verbatim, traducción, paráfrasis e inferencia; permitir cero, uno o cualquier cantidad sustentada, sin rellenar cuotas.
- Exponer únicamente DTOs/endpoints mínimos de invocación y lectura integrados en la API .NET existente, sin acoplar al consumidor a detalles de scraping y sin crear UI.
- Mantener cada resultado generado como borrador, preservar versiones, permisos, auditoría, trazabilidad del proveedor y el bloqueo de downstream hasta `TruthSource Approved`; un Mock debe quedar identificado como Mock.
- Hacer idempotente el reintento para evitar duplicar contenido o resultados y permitir recuperar el análisis persistido sin volver a llamar a la IA.
- Mantener intactas las pantallas y rutas existentes, y no crear colas, workers, microservicios, bases de datos, infraestructura, un segundo router ni un subsistema paralelo `Editorial Intelligence`.

## Capabilities

### New Capabilities

- `editorial-url-analysis`: Análisis backend de URL con captura segura, evidencia persistente, hallazgos editoriales variables y contrato mínimo de invocación/lectura para la pantalla futura.

### Modified Capabilities

- `truth-source-evidence`: Extiende el contrato de evidencia y síntesis para conservar procedencia y tipos de contenido verificables de los hallazgos, sin alterar los estados, permisos ni el gate de aprobación humana.

## Impact

- Backend ASP.NET Core .NET 10 y su API REST/OpenAPI; modelos y servicios de contenido, captura, evidencias y `TruthSource`; posiblemente una migración EF Core acotada solo si la persistencia recuperable de hallazgos no cabe en el modelo actual.
- Persistencia PostgreSQL existente, auditoría y registro `AIRecommendation`; no se añade una base de datos ni cola.
- Pruebas focalizadas de artículos, cardinalidad variable, verbatim/traducción/paráfrasis, ausencia de transcripción, SSRF, persistencia, idempotencia, permisos y regresiones de estados.
- El dashboard y las pantallas existentes no cambian. El impacto de producto queda limitado a habilitar datos y contrato para una pantalla nueva independiente posterior.
- No incluye ideas, guiones, TTS, imágenes, MP4, UI, despliegue ni modificaciones de infraestructura.
