# Tasks

## 1. Inventario y contrato de configuración

- [x] 1.1 Reconfirmar en el código de `Program.cs`, `DatabaseInitializer`, health/readiness, `AppDbContextFactory` y fixtures de tests los caminos actuales de proveedor, inicialización y fallback; verificar que el diff inicial no modifica modelo ni migraciones.
- [x] 1.2 Implementar y cubrir en el código la matriz de modos PostgreSQL obligatorio, InMemory explícito autorizado, InMemory en producción, modo ausente y configuración Npgsql inválida; verificar que cada caso tiene prueba unitaria ejecutable.

## 2. Selección explícita del proveedor

- [x] 2.1 Implementar la resolución única de configuración y entorno, con validación fail-fast de parámetros PostgreSQL y rechazo seguro de InMemory en producción; verificar con pruebas unitarias que no existe fallback implícito.
- [x] 2.2 Registrar `AppDbContext` usando únicamente el proveedor resuelto, manteniendo la ruta InMemory disponible solo para solicitudes explícitas de fixtures no productivos; verificar el proveedor efectivo en pruebas positivas y negativas.
- [x] 2.3 Preservar el guard de `AUTH_MODE=development-bypass` en producción y los contratos existentes del backend; verificar con la regresión de seguridad que el arranque productivo sigue rechazando el bypass.

## 3. Inicialización y readiness

- [x] 3.1 Ajustar la coordinación de `DatabaseInitializer` para propagar fallos obligatorios de configuración, conexión, migración o seed, distinguiéndolos de condiciones idempotentes/no bloqueantes justificadas; verificar que ningún fallo obligatorio termina como `seeding deferred`.
- [x] 3.2 Hacer que el estado de readiness/health refleje el resultado de la inicialización obligatoria y sanitizar logs/respuestas; verificar que los diagnósticos no contienen contraseña, cadena de conexión, token ni secreto.
- [x] 3.3 Mantener migraciones, modelo EF y bootstrap productivo sin cambios de esquema; verificar mediante revisión del diff y validación aislada del modelo/migraciones que no se genera ni aplica una migración no requerida.

## 4. Pruebas deterministas y regresión

- [x] 4.1 Añadir fixtures de prueba que soliciten InMemory explícitamente y pruebas de resolución para configuraciones ausentes, inválidas, productivas y no productivas; verificar aislamiento sin servidor ni credenciales productivas.
- [x] 4.2 Añadir pruebas de arranque/readiness para fallo PostgreSQL bloqueante, condición idempotente/no bloqueante y sanitización; verificar resultados observables y deterministas.
- [x] 4.3 Ejecutar restore/build y la suite backend configurada (`dotnet test ContentFactory.sln -p:UseSharedCompilation=false -nodeReuse:false` con el entorno autorizado); registrar resultados reales sin afirmar conexión productiva.
- [x] 4.4 Ejecutar una verificación reproducible de los casos positivos y negativos de selección y arranque en un entorno efímero autorizado; verificar que el guion no solicita credenciales reales ni navegación de navegador.

## 5. Documentación y cierre gobernado

- [x] 5.1 Comparar las condiciones documentadas de configuración/arranque con el comportamiento final y sincronizar únicamente las referencias técnicas directamente afectadas en `docs/canonical/`; verificar que no se actualiza documentación si no cambió su contrato.
- [x] 5.2 Ejecutar `openspec validate req-4-cf-03-evitar-persistencia-inmemory-silen --strict` y el flujo de verificación OpenSpec; verificar que la evidencia cubre todas las relaciones de requisitos y las exclusiones de alcance.
