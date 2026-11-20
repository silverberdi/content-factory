# Tasks

## 1. Inventario y alineación mínima

- [x] 1.1 (escenario `backend-dotnet-compatibility:1:1`) Reconfirmar el estado efectivo de `ContentFactory.sln`, `src/api/ContentFactory.Api/ContentFactory.Api.csproj`, `tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj`, dependencias directas y archivos de persistencia; verificar que el inventario distingue componentes conformes de incompatibilidades reales.
- [x] 1.2 (escenario `backend-dotnet-compatibility:1:2`) Alinear `src/api/ContentFactory.Api/ContentFactory.Api.csproj` a `net10.0` y actualizar únicamente ASP.NET Core/OpenAPI, EF Core y `Npgsql.EntityFrameworkCore.PostgreSQL` cuando el inventario demuestre incompatibilidad; verificar el diff y las versiones restauradas.
- [x] 1.3 (escenario `backend-dotnet-compatibility:1:2`) Alinear `tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj` a `net10.0` y actualizar únicamente la dependencia de pruebas ASP.NET Core incompatible; verificar que conserva sus referencias de test y al proyecto API.

## 2. Regresión y compatibilidad de persistencia

- [x] 2.1 (escenario `backend-dotnet-compatibility:3:1`) Ejecutar restore y build de `ContentFactory.sln` en el entorno reproducible aislado; verificar que ambos proyectos compilan con `net10.0` y registrar SDK, comandos y resultados reales.
- [x] 2.2 (escenarios `backend-dotnet-compatibility:2:1`, `3:1`) Ejecutar el check configurado por `project.checks` (`POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 POSTGRES_USER=minime_app POSTGRES_PASSWORD=<secreto-del-entorno> dotnet test ContentFactory.sln`) usando el mecanismo seguro de secretos; verificar contratos funcionales, pruebas afectadas y regresiones atribuibles al cambio.
- [x] 2.3 (escenario `backend-dotnet-compatibility:2:2`) Validar `AppDbContext` y el snapshot/modelo EF Core, generar SQL de migración en un entorno aislado y comprobar que no hay operaciones de esquema ajenas al objetivo; verificar que no se edita el historial ni se aplica migración en producción.

## 3. Documentación y evidencia

- [x] 3.1 (escenario `backend-dotnet-compatibility:3:2`) Actualizar solo la referencia canónica o instrucción local directamente afectada, si el diff demuestra que es necesaria; verificar que no se alteran documentos no relacionados.
- [x] 3.2 (escenario `backend-dotnet-compatibility:3:2`) Registrar en el walkthrough/evidencia los comandos y resultados reales de restore, build, test, SDK y versiones efectivas de ASP.NET Core/EF Core/Npgsql; verificar que no se afirma despliegue ni validación de base de datos productiva.
- [x] 3.3 (escenarios `backend-dotnet-compatibility:2:1`, `3:2`) Confirmar en la evidencia que no se requiere nuevo seed data porque la alineación a .NET 10 no altera contratos ni añade comportamiento; verificar la preservación de contratos sin modificar producción.

## 4. Cierre gobernado

- [x] 4.1 (escenario `backend-dotnet-compatibility:3:2`) Ejecutar `openspec validate --strict req-2-cf-01-alinear-api-y-pruebas-con-net-10-l` y el flujo OpenSpec Verify; verificar coherencia entre propuesta, spec, design, tasks, implementación y evidencia fresca.
