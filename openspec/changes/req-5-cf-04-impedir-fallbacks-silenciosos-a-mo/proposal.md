# Proposal

## Why

Los fallos de un proveedor real no deben convertirse silenciosamente en contenido Mock presentado como resultado válido: eso puede contaminar decisiones editoriales, trabajos asíncronos, versiones y trazabilidad. Este cambio hace explícita la identidad y naturaleza de cada resultado, preserva el uso determinista de Mock cuando se solicita válidamente y comunica configuración insuficiente o fallos reales sin exponer secretos.

## What Changes

- Censar los puntos de selección y fallback de proveedores en respuestas síncronas y Jobs asíncronos, incluyendo DeepSeek/Mock, Comfy/Mock y Gemini TTS/Mock, verificando primero el uso existente de mocks en esos flujos.
- Eliminar la conversión silenciosa de un fallo o una configuración ausente de proveedor real en un éxito Mock; devolver errores identificables y estados de Job verificables con mensajes seguros.
- Mantener Mock como ruta válida únicamente cuando el entorno o la solicitud lo selecciona explícitamente, con contenido determinista y resultado identificado como simulado.
- Registrar en los metadatos/datos existentes la identidad del proveedor y si el resultado es real o simulado, sin inventar una plataforma de observabilidad.
- Propagar la semántica de proveedor y los fallos a reintentos, versiones, trazabilidad, contratos API y productos asíncronos sin rediseñar el pipeline editorial.
- Añadir pruebas unitarias o de integración aisladas para éxito Mock explícito, configuración ausente, fallo de proveedor real y propagación del fallo, usando dobles o servidores falsos locales y sin APIs pagadas.
- Actualizar únicamente los contratos o documentos canónicos afectados por esta semántica, si la verificación confirma que corresponde.

## Capabilities

### New Capabilities

Ninguna. El cambio refuerza contratos de capacidades existentes.

### Modified Capabilities

- `script-editorial-pipeline`: los resultados de generación/revisión editorial deben distinguir proveedor real y Mock, y los fallos reales no pueden producir un éxito simulado.
- `visual-asset-generation`: la ejecución asíncrona Comfy/Mock debe conservar identidad/naturaleza del resultado y propagar fallos sin registrar un recurso Mock como producto real.
- `audio-voice-generation`: la ejecución Gemini TTS/Mock debe distinguir simulación explícita de proveedor real, informar configuración/fallos de forma segura y preservar el estado de error del Job.

## Impact

- Afecta las abstracciones de enrutamiento/adaptadores de proveedores, la ejecución y persistencia de Jobs y resultados, los DTOs/errores de API y las pruebas de generación editorial, visual y de voz.
- Puede requerir cambios acotados de persistencia o metadatos para conservar proveedor y naturaleza del resultado; cualquier migración debe ser explícita y compatible con PostgreSQL.
- La UI/dashboard solo se extiende si ya existe un punto de diagnóstico relevante para mostrar el error o la naturaleza simulada; no se crean nuevas pantallas.
- No añade proveedores, acceso a servicios reales, créditos, CI/CD, despliegues, pruebas de producción ni observabilidad nueva.
