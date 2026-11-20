# Spec Delta

## ADDED Requirements

### Requirement: La generación de voz distingue Gemini real de Mock

La generación de voz SHALL identificar el proveedor y la naturaleza real o simulada de cada resultado. Cuando Gemini TTS está seleccionado y falla o carece de configuración, la operación SHALL informar un error seguro y SHALL NOT producir ni registrar audio Mock como resultado real.

#### Scenario: Fallo de Gemini TTS
- **WHEN** Gemini TTS seleccionado falla durante la generación
- **THEN** la respuesta o Job comunica un error identificable y seguro
- **AND** no registra audio Mock como producto real

#### Scenario: Configuración de voz ausente
- **WHEN** Gemini TTS está seleccionado sin credenciales o configuración necesaria
- **THEN** se informa configuración insuficiente sin exponer secretos
- **AND** no se cambia silenciosamente a Mock

#### Scenario: Mock de voz explícito
- **WHEN** el entorno o solicitud selecciona Mock de voz de forma válida
- **THEN** se genera audio de prueba determinista
- **AND** el resultado se identifica como proveedor Mock y simulado

#### Scenario: Error preservado en Job de voz
- **WHEN** falla un Job de generación de voz vinculado a Gemini TTS
- **THEN** el Job conserva un estado de error verificable y su clasificación existente
- **AND** no registra un candidato simulado como audio real

### Requirement: Metadatos de voz y pruebas no confunden simulación con ejecución real

Los datos o metadatos existentes de generación de voz SHALL registrar identidad del proveedor y naturaleza real o simulada, sin crear una plataforma de observabilidad. Las pruebas aisladas SHALL usar dobles o servidores falsos locales y SHALL distinguir explícitamente validación Mock de cualquier prueba live opt-in existente.

#### Scenario: Registro de identidad de voz
- **WHEN** se registra un resultado de voz real o simulado
- **THEN** se persiste la identidad del proveedor y la naturaleza del resultado
- **AND** no se inventa telemetría fuera de los contratos existentes

#### Scenario: Pruebas deterministas de voz
- **WHEN** se verifican Mock explícito, configuración ausente, fallo de Gemini y propagación del fallo
- **THEN** se ejecutan pruebas aisladas sin APIs pagadas
- **AND** una prueba con Mock no acredita que Gemini TTS real funcione

#### Scenario: Censo de rutas de voz
- **WHEN** se censan selección y fallback de voz
- **THEN** se incluyen respuestas síncronas y Jobs asíncronos y se verifica el uso de Mock antes del cambio
- **AND** se preservan contratos de aprobación, reintento, versionado y trazabilidad salvo el cambio indispensable
