# Design

## Context

La propuesta y la delta spec definen el contrato observable. El arranque actual decide InMemory por varias banderas y, ante una cadena PostgreSQL ausente o una excepción de `UseNpgsql`, cae a InMemory; además, el inicializador captura cualquier excepción y la registra como seeding diferido mientras `/health` continúa devolviendo `healthy`. Los tests de dominio ya construyen contextos InMemory directamente. CF-01 deja API y pruebas en `net10.0`; las migraciones PostgreSQL existentes son parte del estado que debe preservarse.

## Goals / Non-Goals

**Goals:**

- Centralizar una decisión de proveedor validada antes de registrar `AppDbContext`.
- Hacer que PostgreSQL obligatorio sea fail-fast en configuración e inicialización, con readiness coherente.
- Mantener InMemory como una ruta explícita y testeable para fixtures no productivos.
- Separar errores bloqueantes de condiciones idempotentes/no bloqueantes justificadas.
- Probar el contrato sin servidor PostgreSQL real, credenciales productivas ni cambios de esquema.

**Non-Goals:**

- Cambiar el modelo, las migraciones, el esquema, los datos productivos o el proveedor Npgsql.
- Crear abstracciones de infraestructura especulativas, servicios externos, contenedores o nuevas instancias de base de datos.
- Cambiar autenticación; el guard de `development-bypass` en producción permanece intacto.

## Decisions

### 1. Resolver el modo antes del registro de EF Core

Se implementará una resolución única de configuración que normalice el modo declarado y valide el entorno. El modo PostgreSQL exige una cadena completa construible a partir de configuración; el modo InMemory exige una solicitud explícita y un entorno no productivo autorizado. Una configuración desconocida, ambigua o incompatible con el entorno termina el arranque con un error sanitizado.

Alternativas consideradas:

- Mantener la detección distribuida actual: se descarta porque permite combinaciones ambiguas y fallback silencioso.
- Inferir InMemory por ausencia de configuración: se descarta porque oculta un despliegue incompleto.

### 2. No usar excepciones de configuración para cambiar de proveedor

La configuración de Npgsql se registra directamente en la ruta PostgreSQL. Los errores de construcción/configuración y los errores de conexión o migración se propagan al coordinador de arranque; nunca se sustituyen por `UseInMemoryDatabase`. La ruta InMemory solo se registra tras la decisión explícita validada.

### 3. Hacer que readiness dependa de la inicialización obligatoria

La inicialización se ejecutará como una fase de arranque con resultado observable. Si falla una operación obligatoria, se conserva el fallo del host y el endpoint de disponibilidad no declarará `healthy`; una condición idempotente documentada se clasifica y registra por separado. Los mensajes y logs se sanitizan mediante códigos/contexto seguro, sin serializar cadenas de conexión ni contraseñas.

Alternativa considerada:

- Capturar y advertir para continuar sirviendo la API: se descarta para PostgreSQL obligatorio porque presenta estado no durable como operativo.

### 4. Mantener el inicializador y las migraciones como estado existente

Se conservará `DatabaseInitializer` y su uso de migraciones relacionales/fixture InMemory, modificando solo la clasificación de errores y el contrato de arranque. No se generarán ni aplicarán migraciones nuevas. El guard de autenticación de producción se deja en la misma fase de fail-fast y se cubre con regresión.

### 5. Cubrir selección y arranque con seams de prueba aislables

Las pruebas unitarias ejercitarán la resolución de modo y la sanitización con configuraciones sintéticas. Las pruebas de integración usarán `WebApplicationFactory`/fixtures con InMemory explícito y un doble controlado para fallos de inicialización, sin credenciales productivas. La suite existente que usa `.UseInMemoryDatabase(...)` seguirá declarando esa intención en sus fixtures; no se reinterpretará como configuración de la aplicación.

La evidencia final incluirá el comando de proyecto configurado, resultados reales y una nota explícita de que no se probó conectividad productiva. No hay comportamiento UI, por lo que no se asigna una comprobación de navegador.

## Risks / Trade-offs

- [Risk] Tests existentes que dependen de defaults implícitos pueden fallar al desaparecer el fallback. → Mitigar declarando InMemory explícito en sus fixtures y añadiendo pruebas de configuración inválida.
- [Risk] Un fallo de readiness puede dificultar diagnósticos locales. → Mitigar con códigos de error estables y mensajes sanitizados que indiquen modo, fase y acción sin secretos.
- [Risk] El seed de desarrollo es amplio y puede mezclar errores idempotentes con errores obligatorios. → Mitigar delimitando fases y clasificando únicamente condiciones justificadas; cualquier fallo obligatorio se propaga.
- [Risk] Cambiar el estado de `/health` puede afectar consumidores existentes. → Mantener el contrato de respuesta compatible donde sea posible y cubrir explícitamente estado listo/no listo en pruebas.

## Migration Plan

1. Inventariar la resolución actual, inicializador, readiness, fixtures y migraciones antes de editar.
2. Implementar la resolución explícita y el flujo de arranque fail-fast sin modificar el modelo ni las migraciones.
3. Actualizar/añadir pruebas deterministas de selección, seguridad, readiness, inicialización y regresión.
4. Ejecutar restore/build y la suite backend configurada en el entorno autorizado; revisar evidencia y sanitización.
5. Si la verificación falla, revertir únicamente los cambios de selección/arranque y conservar intactas las migraciones y el modelo.
