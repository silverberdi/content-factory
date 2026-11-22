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

## CF-04 — Prueba humana de rutas de proveedor y ausencia de fallback silencioso

### Alcance y entorno

- Cambio: `req-5-cf-04-impedir-fallbacks-silenciosos-a-mo`
- Fecha de ejecución: `2026-11-20` (America/Bogota)
- Entorno: API local ASP.NET Core en `Development`, `AUTH_MODE=development-bypass`, base `InMemory` explícita y aislada por nombre; no se usaron PostgreSQL productivo, MinIO, n8n ni APIs pagadas.
- Seed: `IA Simple ES`, ContentItem `00000000-0000-0000-0000-000000000301`, Storyboard aprobado `00000000-0000-0000-0000-000000000501`; los IDs de requisitos se obtuvieron del overview seed en cada ejecución porque el requisito TTS es generado por el inicializador.

### Procedimiento reproducible

Iniciar la API con:

```bash
USE_IN_MEMORY_DB=true IN_MEMORY_DB_NAME=cf-human-req5 \
AUTH_MODE=development-bypass AI_DEFAULT_PROVIDER=Mock \
VISUAL_PROVIDER=Mock VOICE_PROVIDER=Mock ASPNETCORE_ENVIRONMENT=Development \
dotnet run --project src/api/ContentFactory.Api/ContentFactory.Api.csproj \
  --no-launch-profile --urls http://127.0.0.1:5027
```

Con los IDs de `GET /api/content-items/{contentItemId}/storyboards/{storyboardId}/voice-assets`, ejecutar POST de visual y voz en sus rutas de `.../visual-generation` y `.../voice-generation`, seleccionando `candidateCount=1`. Consultar cada Job con `GET /api/jobs/{jobId}` y los overviews de `visual-assets` y `voice-assets`.

### Observaciones reales

1. **Mock explícito:** los dos POST devolvieron `202`; los Jobs terminaron `Succeeded`, conservaron `provider=Mock` y sus assets nuevos se etiquetaron `isSimulated=true`. El recurso visual persistió como `PendingReview`; el audio también quedó como candidato `PendingReview`, sin presentarse como proveedor real.
2. **Proveedor real visual sin servicio:** reiniciando con `VISUAL_PROVIDER=Comfy` y sin servidor Comfy en `127.0.0.1:8188`, el Job conservó `provider=Comfy`, terminó `FailedActionRequired`, `errorCode=COMFY_CONNECTION_FAILED`, agotó sus tres intentos y no creó un nuevo GeneratedAsset Mock.
3. **Proveedor real de voz sin configuración:** reiniciando con `VOICE_PROVIDER=Gemini` y sin `GEMINI_API_KEY`, el Job conservó `provider=Gemini`, terminó `FailedActionRequired`, `errorCode=MISSING_API_KEY`, mostró únicamente `Gemini API key is not configured in backend settings.` y el overview informó `generatedCount=0`.
4. **Proveedor editorial síncrono sin configuración:** con `AI_DEFAULT_PROVIDER=DeepSeek` y sin clave, `POST /api/content-items/00000000-0000-0000-0000-000000000301/script/generate` respondió HTTP `409` con `AI script generation failed: DeepSeek provider is not configured.`; no devolvió contenido Mock.

La evidencia confirma etiquetas de proveedor/naturaleza, errores sanitizados, estados de Job y ausencia de artefacto simulado en las rutas reales fallidas. La ejecución Mock no se considera evidencia de funcionamiento live de DeepSeek, Comfy o Gemini.

### Comprobación automatizada de cierre Apply

```text
NUGET_PACKAGES=/run/minime-agent/tmp/cf-nuget-cache-req5/packages \
POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 \
POSTGRES_USER=minime_app \
dotnet test ContentFactory.sln --no-restore --configuration Release -m:1 \
  --logger "console;verbosity=minimal"
Passed: 127, Failed: 0, Skipped: 0, Total: 127
EXIT=0
```

La validación estricta disponible en OpenSpec 1.13.1 también pasó:
`openspec validate --type change --strict --no-interactive --json` → `1/1`, sin issues. El binario instalado no expone `openspec verify`; la revisión independiente queda pendiente del Verify gobernado por el coordinador.
<!-- verify pass 2 -->

## CF-06 — Editorial Studio independiente

### Alcance y contratos

La pantalla se agregó en `/editorial/studio` y `/editorial/studio/:contentItemId`, usando exclusivamente los contratos existentes de `editorial-url-analyses`, `ContentItem`, `TruthSource`, `ContentIdea` y `Script`. La selección de propuestas conserva cada propuesta generada como una entidad de idea independiente sin sobrescribir las demás propuestas ni el TruthSource origen. La generación y reapertura del guion usa el endpoint existente de Script; la edición y guardado continúan en Script Studio.

El workspace usa `PageHeaderComponent`, `PageToolbarComponent`, `.cf-page-container`, señales Angular, tokens semánticos y la clase `.dark`. Los estados de proveedor muestran `Mock` cuando `isSimulated=true`. La producción es conceptual 9:16: no hay acciones habilitadas para renderizar, descargar o publicar MP4 inexistente. Storyboard/Visual/Voice se reutilizan desde el Storyboard Studio existente.

### Datos de validación

`src/web/e2e/editorial-studio.spec.ts` contiene fixtures deterministas explícitos `Mock`: canal `IA Simple ES`, una fuente soportada, un TruthSource `Draft`, un hallazgo de tipo `Paraphrase` y un proveedor simulado. No contiene secretos ni afirma inferencia live.

### Comprobaciones automatizadas

```text
npm ci
npm run build                         # exit 0; warning existente de budget de estilos del componente
npm test -- --watch=false             # 56 passed, 0 failed
npx playwright test e2e/editorial-studio.spec.ts  # 28 passed, 0 failed
DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 POSTGRES_DATABASE=content_factory_test POSTGRES_HOST=192.168.0.194 POSTGRES_USER=minime_app POSTGRES_PASSWORD=[REDACTED] dotnet test ContentFactory.sln -m:1 -p:UseSharedCompilation=false -nodeReuse:false
# 137 passed, 0 failed, exit 0
```

Playwright ejecutó los checks en 1440×900, 1920×1080, 768×1024 y 390×844 (28 passed, 0 failed): navegación, análisis, etiqueta Mock, separación de paráfrasis/verbatim, gate de aprobación, ausencia de overflow horizontal, selección de propuestas independientes sin sobrescritura, generación de guion, recuperación tras reload del mismo ContentItem/idea/guion, estados de URL no soportada y manejo de permisos 401/403. Todas las verificaciones requeridas se reportan como PASS.
