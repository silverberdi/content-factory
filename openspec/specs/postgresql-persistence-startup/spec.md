# postgresql-persistence-startup Specification

## Purpose

Establece una selección de proveedor de persistencia explícita y segura para que los entornos PostgreSQL obligatorios no oculten fallos mediante EF InMemory, mientras los tests aislados conservan un proveedor efímero autorizado.

## Requirements

### Requirement: La selección del proveedor es explícita y segura por entorno

El sistema MUST seleccionar PostgreSQL cuando el modo declarado lo exige y MUST permitir EF InMemory solo cuando un entorno no productivo autorizado lo solicite explícitamente. La configuración MUST rechazar InMemory en producción y MUST rechazar cualquier fallback silencioso desde PostgreSQL.

#### Scenario: PostgreSQL obligatorio sin configuración
- **WHEN** la API inicia en un entorno cuyo modo de persistencia es PostgreSQL obligatorio y faltan parámetros de conexión obligatorios
- **THEN** el arranque falla explícitamente
- **AND** EF InMemory no se activa

#### Scenario: Npgsql inválido en modo PostgreSQL
- **WHEN** el modo declarado es PostgreSQL y la configuración del proveedor PostgreSQL es inválida
- **THEN** el arranque falla de forma clara
- **AND** EF InMemory no se activa

#### Scenario: InMemory explícito en prueba aislada
- **WHEN** un entorno de pruebas solicita explícitamente EF InMemory al crear el contexto de datos
- **THEN** se utiliza el proveedor de prueba
- **AND** no se requiere conectividad hacia producción

#### Scenario: InMemory rechazado en producción
- **WHEN** un entorno de producción intenta seleccionar persistencia InMemory
- **THEN** la configuración se rechaza de forma segura
- **AND** la aplicación no queda disponible como si tuviera persistencia durable

#### Scenario: InMemory autorizado en entorno no productivo
- **WHEN** un entorno no productivo autorizado solicita explícitamente EF InMemory
- **THEN** se permite la selección
- **AND** no se utiliza como recuperación silenciosa de PostgreSQL

### Requirement: El arranque refleja fielmente la inicialización durable

Cuando PostgreSQL es obligatorio, un fallo bloqueante de configuración, conexión o inicialización MUST impedir que la aplicación se presente como lista. Los diagnósticos expuestos MUST ser accionables y sanitizados, sin contraseñas, cadenas de conexión, tokens ni otros secretos.

#### Scenario: Fallo bloqueante de inicialización
- **WHEN** una inicialización de base de datos obligatoria produce un error bloqueante
- **THEN** el arranque se impide
- **AND** la excepción no se convierte en `seeding deferred`

#### Scenario: Fallo de PostgreSQL durante el arranque
- **WHEN** una inicialización PostgreSQL obligatoria falla después de una configuración válida
- **THEN** la aplicación no se presenta como lista
- **AND** los diagnósticos no revelan secretos

#### Scenario: Condición idempotente o no bloqueante justificada
- **WHEN** durante el arranque se procesa una condición documentada como idempotente o no bloqueante
- **THEN** se distingue de un fallo obligatorio
- **AND** no se bloquea injustificadamente el arranque

### Requirement: El cambio preserva contratos de persistencia y seguridad existentes

La selección e inicialización del proveedor MUST preservar el modelo EF, las migraciones existentes y los contratos backend no relacionados. MUST conservarse el guard que rechaza `development-bypass` en producción y no MUST ejecutarse ningún cambio productivo de esquema como parte de esta capacidad.

#### Scenario: Modelo y migraciones preservados
- **WHEN** se modifica la selección o inicialización del proveedor de persistencia
- **THEN** el modelo y las migraciones existentes permanecen preservados
- **AND** no se introduce un cambio de esquema no requerido

#### Scenario: Guard de bypass preservado
- **WHEN** se evalúa una configuración de producción con `development-bypass`
- **THEN** el guard existente continúa rechazando el arranque
- **AND** la modificación del proveedor no lo debilita ni lo sustituye

### Requirement: La evidencia de selección y arranque es determinista y acotada

La capacidad MUST contar con pruebas aislables y deterministas para modos válidos e inválidos, selección de proveedor, readiness/arranque, sanitización de diagnósticos y regresión del backend. Las pruebas MUST usar fixtures y recursos efímeros autorizados, no un servidor real ni credenciales productivas, y MUST distinguir evidencia local de una conexión productiva no comprobada.

#### Scenario: Pruebas positivas y negativas reproducibles
- **WHEN** se ejecutan las pruebas de selección e inicialización con fixtures aislados
- **THEN** cubren resultados positivos y negativos de PostgreSQL e InMemory de forma determinista
- **AND** no requieren un servidor real ni credenciales productivas

#### Scenario: Cierre sin afirmar conexión productiva
- **WHEN** se registra la evidencia del cambio
- **THEN** incluye pruebas de arranque, selección de proveedor y regresión de backend ejecutables aisladamente
- **AND** no declara comprobada una conexión productiva

### Requirement: La documentación de configuración se sincroniza condicionalmente

La documentación técnica relacionada MUST actualizarse cuando cambien sus condiciones documentadas de configuración o arranque, y MUST permanecer sin cambios cuando esas condiciones no cambien.

#### Scenario: Condiciones documentadas cambian
- **WHEN** la implementación modifica una condición documentada de configuración o arranque
- **THEN** se sincroniza la documentación técnica relacionada

#### Scenario: Condiciones documentadas no cambian
- **WHEN** las condiciones de configuración y arranque documentadas permanecen iguales
- **THEN** no se exige una sincronización documental no relacionada
