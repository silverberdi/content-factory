# Proposal

## Why

La API puede ocultar una configuración o un fallo bloqueante de PostgreSQL seleccionando EF InMemory como recuperación, haciendo que el sistema parezca operativo mientras el estado durable no está disponible. CF-01 ya está integrado y deja el backend en el runtime compatible; este cambio establece un contrato verificable para que PostgreSQL obligatorio falle explícitamente y para que InMemory exista solo cuando el entorno autorizado lo solicita de forma expresa.

## What Changes

- Definir una selección de proveedor explícita y validada por entorno: PostgreSQL para los entornos que lo declaran obligatorio e InMemory únicamente para pruebas o desarrollo no productivo autorizado.
- Rechazar configuraciones ausentes o inválidas de PostgreSQL sin activar EF InMemory como fallback.
- Impedir que la API se presente como lista ante fallos bloqueantes de inicialización PostgreSQL y mantener diagnósticos sanitizados, sin secretos.
- Distinguir condiciones idempotentes o no bloqueantes justificadas de fallos obligatorios; no convertir automáticamente una excepción en `seeding deferred`.
- Permitir fixtures aislados con InMemory explícito sin conectividad hacia producción, y rechazar cualquier intento de InMemory en producción.
- Preservar el modelo EF, las migraciones existentes y el guard que rechaza `development-bypass` en producción.
- Añadir pruebas deterministas positivas y negativas de selección, arranque, inicialización y regresión del backend usando recursos efímeros autorizados; no afirmar conexión productiva.
- Sincronizar documentación técnica relacionada solo si cambian las condiciones documentadas de configuración o arranque.

### Non-goals

- No cambiar el esquema, crear instancias PostgreSQL, modificar credenciales o secretos reales, ejecutar backups, cambiar CI/CD o desplegar.
- No introducir cambios de dominio, endpoints, contratos de negocio, migraciones históricas ni una nueva arquitectura de persistencia.
- No convertir InMemory en proveedor predeterminado ni conservarlo como recuperación silenciosa.
- No validar conectividad o credenciales de una base de datos productiva.
- No cambiar el flujo Mini Me de revisión, PR, integración, archive o cierre.

## Capabilities

### New Capabilities

- `postgresql-persistence-startup`: selección explícita y segura del proveedor de persistencia, validación de configuración, política de arranque e inicialización y pruebas aisladas de InMemory.

### Modified Capabilities

- Ninguna. `backend-dotnet-compatibility` y `identity-access` se preservan; el guard existente contra `development-bypass` en producción continúa siendo obligatorio, pero no cambia su requisito.

## Impact

- Backend ASP.NET Core/EF Core: configuración del proveedor, construcción de `DbContext`, health/readiness y flujo de inicialización/seeding.
- Suite `ContentFactory.Api.Tests`: fixtures deterministas para modos PostgreSQL/InMemory, configuraciones inválidas y fallos de arranque sanitizados.
- Modelo EF y migraciones: solo verificación de preservación; no se autoriza una modificación de esquema.
- Documentación técnica de configuración/arranque, únicamente si el comportamiento documentado cambia.
- Dependencia declarada: CF-01 (alineación API/pruebas con .NET 10 LTS) está completada e integrada.
