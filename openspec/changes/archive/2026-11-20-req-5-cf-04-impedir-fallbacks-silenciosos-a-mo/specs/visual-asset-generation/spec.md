# Spec Delta

## ADDED Requirements

### Requirement: La generación visual asíncrona no oculta simulaciones

Un Job de generación visual vinculado a un proveedor real SHALL conservar un estado de error verificable cuando la invocación falla o la configuración es insuficiente. El sistema SHALL NOT registrar un recurso Mock como producto real, salvo que Mock haya sido seleccionado explícitamente por una configuración o solicitud válida.

#### Scenario: Fallo de Comfy seleccionado
- **WHEN** un Job vinculado a Comfy falla durante su ejecución
- **THEN** el Job conserva un estado de error verificable y un diagnóstico seguro
- **AND** no se registra un recurso Mock como producto real

#### Scenario: Configuración visual ausente
- **WHEN** Comfy está seleccionado pero falta configuración necesaria
- **THEN** el Job o la solicitud comunica configuración insuficiente sin secretos
- **AND** no cambia silenciosamente a Mock

#### Scenario: Mock visual solicitado explícitamente
- **WHEN** Mock está seleccionado válidamente para una generación visual
- **THEN** se genera un recurso determinista de prueba
- **AND** el recurso registra proveedor Mock y naturaleza simulada

#### Scenario: Identidad y naturaleza de recurso visual
- **WHEN** se registra un recurso visual generado
- **THEN** se registra la identidad del proveedor y si el recurso es real o simulado en los metadatos de generación existentes
- **AND** la respuesta no presenta un recurso simulado como producto real

### Requirement: Rutas visuales y fallos se verifican de extremo a extremo local

La especificación de generación visual SHALL cubrir el censo de selección/fallback en respuestas síncronas relacionadas y Jobs asíncronos, con propagación de error compatible con reintentos, versiones, lineage y contratos API existentes.

#### Scenario: Censo de rutas visuales
- **WHEN** se identifican los puntos de selección y fallback visual
- **THEN** se incluyen respuestas síncronas y Jobs asíncronos
- **AND** se verifica primero el uso y selección de Mock en cada flujo

#### Scenario: Pruebas visuales deterministas
- **WHEN** se prueban éxito Mock explícito, ausencia de configuración, fallo de Comfy y propagación del fallo
- **THEN** se usan dobles o servidores falsos locales sin APIs pagadas
- **AND** una prueba Mock no acredita que Comfy real funcione
