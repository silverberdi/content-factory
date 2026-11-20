# Proposal — CF-01: Alinear API y pruebas con .NET 10 LTS

## Why

El backend debe ser efectivamente compatible con .NET 10 LTS, que es la decisión canónica del proyecto, pero los proyectos de API y pruebas observados todavía apuntan a .NET 9 y a dependencias de la rama 9. Request #2 autoriza reconciliar el estado actual (fuente SHA-256 `393a6f10f5c75f5721aa7ff9ab8b133d9d7eeece9a34c623df8461c3a687732c`) y volver a verificarlo al iniciar, porque `main` puede haber cambiado.

## What Changes

- Inventariar nuevamente los proyectos .NET y sus dependencias directas afectadas antes de modificar nada.
- Alinear `src/api/ContentFactory.Api` y `tests/ContentFactory.Api.Tests` con `net10.0`.
- Alinear únicamente las referencias ASP.NET Core, EF Core, OpenAPI y `Npgsql.EntityFrameworkCore.PostgreSQL` que el inventario demuestre incompatibles con .NET 10, usando versiones estables y compatibles; conservar las ya conformes.
- Verificar restore, build y la suite backend en un entorno reproducible y aislado, registrando comandos y resultados reales.
- Validar el modelo EF Core y la generación de SQL de migraciones sin aplicar migraciones en producción ni introducir cambios de esquema ajenos al objetivo.
- Documentar la evidencia de compatibilidad y actualizar solo referencias canónicas o instrucciones locales directamente afectadas. El cierre debe incluir validación OpenSpec y el cross-review independiente exigido por la gobernanza cuando exista un revisor autorizado.

### Non-goals

- No cambiar contratos, endpoints, entidades, relaciones, migraciones históricas, datos ni comportamiento funcional del backend.
- No modificar GitHub Actions, workflows, runners, Dockerfiles ni infraestructura de despliegue.
- No desplegar ni validar producción o una base de datos productiva.
- No añadir proveedores, funcionalidades de negocio, refactorizaciones generales ni nueva infraestructura.
- No redefinir los flujos Mini Me de revisión, PR, sync, archive o cierre.
- No convertir la selección de factibilidad ni una lista de bloqueadores en evidencia de precondición satisfecha; la capacidad y ruta del revisor independiente deben verificarse por separado.

## Capabilities

### New Capabilities

- `backend-dotnet-compatibility`: compatibilidad efectiva de los proyectos API y pruebas con .NET 10 LTS, incluida la verificación de dependencias y regresión del backend.

### Modified Capabilities

- Ninguna. Las capacidades de producto existentes no cambian sus requisitos.

## Impact

- Proyectos y dependencias en `src/api/ContentFactory.Api/ContentFactory.Api.csproj` y `tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj`.
- Restore/build/tests de `ContentFactory.sln`, junto con validación aislada del modelo EF Core y del SQL de migraciones.
- Posibles instrucciones o referencia canónica directamente afectada por la alineación, sin tocar documentación no relacionada.
- Sin impacto de dashboard, UI, autorización, despliegue, persistencia productiva ni datos: el objetivo es compatibilidad de plataforma y evidencia de no-regresión.
