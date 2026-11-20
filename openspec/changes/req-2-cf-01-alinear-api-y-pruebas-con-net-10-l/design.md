# Design

## Context

La autoridad canónica (`docs/canonical/00_CANONICAL_AUTHORITY.md`, `05_SYSTEM_ARCHITECTURE_V2.md`, `22_TECH_STACK_LOCK.md` y `24_TEST_STRATEGY.md`) fija ASP.NET Core sobre .NET 10 LTS y PostgreSQL mediante Npgsql. El estado inicial descrito por Request #2 debe volver a comprobarse: los dos `.csproj` observados usaban `net9.0`; la API referenciaba ASP.NET Core/OpenAPI 9, EF Core 9 y Npgsql 9, y las pruebas usaban `Microsoft.AspNetCore.Mvc.Testing` 9. El repositorio conserva `AppDbContext`, una fábrica de contexto y migraciones históricas que deben tratarse como estado existente.

## Goals / Non-Goals

**Goals:**

- Producir una alineación mínima y explícita de target frameworks y dependencias directas incompatibles.
- Mantener contratos HTTP, comportamiento de dominio, modelo EF, migraciones y datos.
- Verificar restore/build/tests y la generación aislada de SQL de migración con evidencia reproducible.
- Mantener la documentación y el diff limitados al objetivo de compatibilidad.

**Non-Goals:**

- No introducir cambios de esquema, nuevas migraciones funcionales ni aplicaciones contra producción.
- No cambiar API, entidades, relaciones, seguridad, proveedores, infraestructura, Docker, CI o workflows.
- No rediseñar el backend ni actualizar paquetes no afectados por incompatibilidad real.
- Quedan fuera de alcance GitHub Actions, workflows, runners, Dockerfiles, infraestructura de despliegue, despliegue y validación productiva, nuevos proveedores, cambios funcionales, refactorizaciones generales y nuevas características.

## Decisions

1. **Alinear por proyecto y dependencia directa, no por actualización masiva.** Se inspeccionará el grafo efectivo y se cambiarán `TargetFramework` y solo los paquetes que requieran una versión estable compatible con .NET 10. Esto conserva componentes ya conformes y evita drift de dependencias.
   - Alternativa descartada: elevar todos los paquetes a sus últimas versiones sin evidencia; ampliaría el diff y podría introducir cambios no relacionados.

2. **Usar la solución y el check de pruebas existentes como contrato de regresión.** La verificación usará restore, build y `dotnet test ContentFactory.sln` con el entorno aislado configurado por el repositorio; se añadirán o ajustarán pruebas solo si una incompatibilidad real lo exige.
   - Alternativa descartada: reemplazar la suite por scripts ad hoc o declarar éxito solo por compilación.

3. **Validar EF Core sin modificar estado productivo.** Se inspeccionará el modelo/context factory y se generará SQL de migración en un entorno aislado, comparando que no aparezca una operación ajena al cambio. Las migraciones históricas no se editarán y no se ejecutará ninguna migración productiva.
   - Alternativa descartada: crear una migración de “normalización” o usar la base productiva como entorno de prueba.

4. **Preservar los límites de seguridad y operación.** No se cambian autenticación, autorización, secretos, hosts, PostgreSQL expuesto, n8n ni despliegues. Si una prueba requiere persistencia, se usará únicamente el entorno de test ya declarado y sus credenciales/configuración aisladas.

5. **Evidencia explícita de versiones.** La verificación registrará el SDK/runtime efectivo, las versiones restauradas de ASP.NET Core/EF Core/Npgsql, comandos, resultados y cualquier limitación. No se afirmará validación de despliegue o de base de datos productiva.

6. **Gobernanza del cierre.** El cambio se limita estrictamente a la alineación técnica a .NET 10 LTS y a su verificación reproducible con la suite de pruebas completa pasando al 100%. Las etapas de PR y merge son gestionadas por el orquestador mini me conforme a la complejidad moderada del cambio.

## Risks / Trade-offs

- [Cambio transitorio de APIs entre EF Core 9 y 10] → compilar y ejecutar pruebas después de cada ajuste; corregir solo incompatibilidades demostradas.
- [Diferencias de SQL o model snapshot al cambiar el proveedor] → comparar el SQL generado en aislamiento y detener cualquier operación de esquema no explicada por compatibilidad.
- [Entorno de PostgreSQL de pruebas no disponible] → reportar la limitación concreta y conservar separadas las evidencias que sí puedan ejecutarse; no sustituirla por una afirmación de validación productiva.
- [Dependencias transitivas con versiones inconsistentes] → revisar restore/grafo efectivo y documentar la resolución, sin actualizar paquetes no afectados.
- [Revisión y gobernanza post-verificación] → la suite completa valida la compatibilidad funcional sin alterar producción; la apertura de PR sigue la política para complejidad moderada.

## Migration Plan

No hay migración de datos ni despliegue en este change. La implementación modifica archivos de proyecto y, solo si fuese estrictamente necesario, ajustes de pruebas/documentación. Rollback consiste en revertir esos cambios de compatibilidad; no se toca el historial de migraciones ni el esquema productivo.

## Open Questions

Las versiones efectivas quedaron fijadas durante Apply mediante las versiones estables 10.0.0 compatibles con .NET 10 LTS. No restan preguntas abiertas en este cambio.
