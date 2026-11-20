# CF-01 — Evidencia de compatibilidad .NET 10

## Alcance y entorno

- Cambio: `req-2-cf-01-alinear-api-y-pruebas-con-net-10-l`
- Repositorio: `silverberdi/content-factory`
- Worktree/branch: `request-2-cf-01-alinear-api-y-pruebas-con-net-10-l`
- Base SHA: `96b202a09b1e179141b66f81dd92ada08a9d55c4`
- SDK: `.NET SDK 10.0.115`, MSBuild `18.0.37`, Ubuntu 26.04 x64
- OpenSpec schema: `spec-driven`
- No se desplegó ni se validó una base de datos productiva.

## Inventario previo

`ContentFactory.sln` contiene únicamente `src/api/ContentFactory.Api` y
`tests/ContentFactory.Api.Tests`. Antes del cambio, ambos proyectos apuntaban a
`net9.0`. La API usaba ASP.NET Core OpenAPI 9.0.16, EF Core Design/InMemory
9.0.0 y Npgsql EF 9.0.4; las pruebas usaban MVC Test 9.0.0.
`Scalar.AspNetCore` 2.0.18, `System.ServiceModel.Syndication` 10.0.11 y las
dependencias de test restantes se conservaron porque el inventario no demostró
una incompatibilidad con .NET 10.

La persistencia existente (`AppDbContext`, `AppDbContextFactory`, snapshot y
migraciones históricas) fue inspeccionada y no fue editada.

## Comandos y resultados

La restauración se ejecutó con una caché NuGet temporal aislada en
`/run/minime-agent/tmp/cf-nuget-cache-req2/packages`:

```text
dotnet restore src/api/ContentFactory.Api/ContentFactory.Api.csproj --force --no-cache --disable-parallel --verbosity minimal
exit 0
dotnet restore tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj --force --no-cache --disable-parallel --verbosity minimal
exit 0
```

Build de ambos proyectos:

```text
NUGET_PACKAGES=... dotnet build tests/ContentFactory.Api.Tests/ContentFactory.Api.Tests.csproj --no-restore --configuration Release --verbosity minimal
Build succeeded; 0 Error(s); TEST_BUILD_EXIT=0
```

La solución se compiló con el mismo SDK y produjo los binarios Release
`net10.0` de API y pruebas. La ejecución inicial de `dotnet test` encontró un
fallo de pipe local del runner de MSBuild; al repetir con `-m:1`, el comando
completo pasó:

```text
POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 POSTGRES_USER=minime_app \
  dotnet test ContentFactory.sln --no-restore --configuration Release -m:1 \
  --logger "console;verbosity=minimal"
Passed: 122, Failed: 0, Skipped: 0, Total: 122
TEST_EXIT=0
```

La cobertura incluye la prueba unitaria `RuntimeCompatibilityTests.TestAssemblyTargetsNet10`
y el smoke E2E `GetOpenApiDocument_ReturnsTheExistingApiContract`, ejecutados
contra la API real mediante `WebApplicationFactory`.

La contraseña no se escribió ni se inventó. Las pruebas ejecutadas usan sus
fixtures aislados y no constituyen validación de producción.

## Versiones efectivas

- `Microsoft.AspNetCore.OpenApi`: `10.0.0`
- `Microsoft.EntityFrameworkCore.Design`: `10.0.0`
- `Microsoft.EntityFrameworkCore.InMemory`: `10.0.0`
- `Npgsql.EntityFrameworkCore.PostgreSQL`: `10.0.0`
- `Microsoft.AspNetCore.Mvc.Testing`: `10.0.0`

## Validación EF aislada

Se instaló temporalmente `dotnet-ef 10.0.0` fuera del repositorio y se ejecutó
contra el build Release:

```text
dotnet-ef migrations script --no-build --configuration Release \
  --project src/api/ContentFactory.Api/ContentFactory.Api.csproj \
  --startup-project src/api/ContentFactory.Api/ContentFactory.Api.csproj \
  --output /run/minime-agent/tmp/cf-nuget-cache-req2/migrations.sql
EF_SCRIPT_EXIT=0
SHA-256: 95eb3c1a739eda0151c28ec7092276a21ee469e7ba693f81e0cda710133dc14e
```

El resultado contiene únicamente el SQL de las migraciones históricas
existentes, sin una migración nueva ni una operación atribuible a este cambio.
No se editó el snapshot, no se editó el historial y no se aplicó ninguna
migración a producción.

## Seed y contratos

No se requiere nuevo seed data: el cambio solo alinea el runtime y las
dependencias, sin añadir comportamiento, contratos, entidades ni estados. La
suite existente conserva la evidencia de los contratos funcionales afectados.

## Limitaciones y cierre

El feed NuGet emitió warnings de vulnerabilidades transitivas para
`Microsoft.OpenApi 2.0.0` y `System.Security.Cryptography.Pkcs 9.0.0`; no se
actualizaron paquetes fuera del alcance autorizado. No se altera la base de datos productiva. Todas las tareas 1.1 a 4.1 están completas y verificadas con 122 pruebas exitosas en .NET 10 LTS.
