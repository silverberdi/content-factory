# Design

## Context

El cambio cruza el enrutamiento de capacidades, adaptadores de proveedores, generación síncrona, workers de `Job` y persistencia de resultados en las capacidades editoriales, visuales y de voz. La arquitectura canónica exige proveedor configurable, Jobs para trabajo largo, PostgreSQL como estado durable y errores API seguros; la motivación y los límites funcionales están en `proposal.md` y las deltas de `specs/`.

## Goals / Non-Goals

**Goals:**

- Hacer que la decisión de proveedor sea explícita en cada punto de selección y fallback identificado.
- Propagar un resultado de proveedor real, un Mock explícito o un fallo sin reinterpretarlo como otra categoría.
- Reutilizar los campos/metadatos y clasificación de errores existentes, agregando persistencia solo cuando sea indispensable para distinguir proveedor y naturaleza.
- Mantener compatibles los estados de Job, reintentos acotados, versionado, lineage, aprobación editorial y contratos REST/OpenAPI.
- Probar las cuatro rutas críticas con dobles locales y dejar evidencia reproducible para respuestas síncronas y Jobs.

**Non-Goals:**

- No introducir proveedores, nuevas pantallas, una plataforma de observabilidad, un nuevo pipeline ni configuración de créditos.
- No habilitar llamadas reales, producción, n8n de desarrollo, CI/CD o despliegues.
- No convertir una prueba Mock en evidencia de funcionamiento de DeepSeek, Comfy o Gemini.

## Decisions

1. **Semántica de resultado en la frontera de proveedor.** Cada adaptador/enrutador devolverá o propagará la identidad del proveedor y la naturaleza `real`/`simulated` mediante el contrato de resultado o los metadatos ya presentes. Se elige este punto porque evita que cada consumidor infiera la naturaleza desde el contenido. No se crea un servicio de observabilidad separado; como alternativa descartada, registrar solo logs no satisface la trazabilidad persistida.

2. **Fallback explícito, nunca implícito.** El enrutamiento solo podrá seleccionar Mock cuando la configuración o solicitud válida lo indique. Una excepción, respuesta inválida o configuración ausente del proveedor real se convierte en el error seguro y la clasificación de Job existente, no en una nueva invocación Mock. Se mantiene el fallback configurable existente como decisión de configuración explícita; se descarta un `catch` global que sustituya silenciosamente el adaptador.

3. **Síncrono y asíncrono con la misma verdad operacional.** Las respuestas síncronas expondrán el error estructurado existente; los workers persistirán el estado/clase de fallo y no crearán el artefacto simulado. Los reintentos seguirán las políticas actuales de cada capacidad y conservarán sus intentos. No se introduce un modelo paralelo de Job.

4. **Persistencia y compatibilidad.** Primero se mapearán los campos actuales de generación, `Job`, `JobAttempt`, `GeneratedAsset` y candidatos de voz. Si falta una representación durable de proveedor/naturaleza, se añadirá una migración PostgreSQL mínima, nullable o con backfill seguro para históricos, manteniendo lectura compatible. No se eliminarán ni reescribirán resultados históricos.

5. **Superficie de UI y dashboard.** Se reutilizarán diagnósticos, badges y atención existentes si ya soportan el dato. Solo se ajustará la presentación necesaria para no llamar “éxito real” a un Mock o para mostrar un fallo accionable; no se crea una pantalla nueva ni se añade una vista de observabilidad.

6. **Pruebas aisladas y n8n seguro.** Las pruebas usarán dobles de proveedor o servidores falsos locales, configuración de prueba y base PostgreSQL de test conforme al tooling del repositorio. Los flujos que atraviesen orquestación no invocarán n8n real: se probará el adaptador/contrato localmente o se usará un endpoint falso explícito.

## Risks / Trade-offs

- **[Históricos sin metadatos completos]** → Tratar los registros anteriores como naturaleza desconocida/no inferida; no etiquetarlos artificialmente como reales o simulados y cubrir el comportamiento en la migración.
- **[Consumidores que esperan contenido tras cualquier excepción]** → Actualizar contratos y pruebas de propagación antes de retirar el fallback, conservando códigos de error y correlación existentes.
- **[Reintentos que vuelvan a ocultar el fallo]** → Hacer que cada intento conserve proveedor, estado y error sanitizado; verificar agotamiento y reintento manual sin crear artefactos falsos.
- **[Exposición accidental de configuración]** → Usar mensajes sanitizados y aserciones que comprueben que claves, tokens y URLs internas no aparecen.
- **[Diferencias entre capacidades]** → Ejecutar un censo inicial y mantener una matriz de rutas por capacidad; cualquier ruta no identificada se documenta como pendiente antes de cambiarla.

## Migration Plan

1. Censar rutas y campos existentes; confirmar si el esquema ya conserva proveedor y naturaleza.
2. Si es necesario, aplicar una migración PostgreSQL mínima y reversible antes del cambio de consumidores; no borrar datos ni históricos.
3. Implementar el contrato de resultado y el rechazo de fallback implícito por capacidad, con pruebas junto a cada ajuste.
4. Verificar localmente con dobles; en caso de rollback, revertir la selección estricta y la migración solo si no se han creado datos dependientes, preservando los registros ya existentes.

## Open Questions

No quedan preguntas que cambien el alcance: la forma exacta de los campos se resolverá inspeccionando los modelos existentes sin alterar la semántica especificada.
