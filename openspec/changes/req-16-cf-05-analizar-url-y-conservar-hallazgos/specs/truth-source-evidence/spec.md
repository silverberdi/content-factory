# Spec Delta

## ADDED Requirements

### Requirement: Tipos de contenido editorial distinguibles y verbatim verificable

Los hallazgos y borradores de `TruthSource` SHALL distinguir explícitamente texto exacto verificable, traducción al español, paráfrasis e inferencia o interpretación del modelo. Un verbatim en otro idioma SHALL coincidir con la evidencia original y su traducción SHALL estar rotulada por separado; una cita no verificable SHALL degradarse a paráfrasis o afirmación por revisar y nunca SHALL inventar hablante ni tiempo.

#### Scenario: Verbatim y traducción respaldados
- **WHEN** existe un fragmento original verificable en otro idioma
- **THEN** el campo de texto exacto coincide con la evidencia capturada
- **AND** la traducción al español aparece en un campo separado y rotulado

#### Scenario: Cita no verificable
- **WHEN** el texto exacto no puede comprobarse contra la evidencia
- **THEN** el hallazgo se conserva como paráfrasis o afirmación por revisar
- **AND** no se presenta como verbatim ni se agregan hablante o timestamp ficticios

### Requirement: Hallazgos de análisis conservados como borrador y con procedencia del proveedor

Cuando el análisis editorial produzca hallazgos o síntesis, el sistema SHALL conservarlos dentro del resultado borrador asociado a la evidencia y SHALL registrar la procedencia estructurada del proveedor, modelo, recomendación y fallas aplicables mediante el registro existente. Un proveedor Mock SHALL identificarse explícitamente como Mock. El sistema SHALL conservar estados, versiones, permisos y control de autorización existentes; nunca SHALL crear automáticamente un `TruthSource Approved`.

#### Scenario: Generación Mock explícita
- **WHEN** el router ejecuta el análisis con un proveedor Mock
- **THEN** el borrador y su recomendación identifican explícitamente el proveedor Mock
- **AND** el resultado no se presenta como captura o análisis real de un proveedor externo

#### Scenario: Gate de aprobación preservado
- **WHEN** se persiste un análisis con hallazgos y síntesis
- **THEN** el `TruthSource` permanece en `Draft` hasta una acción humana autorizada
- **AND** el consumidor no puede usarlo como `Approved` ni avanzar a ideas o guiones antes de la aprobación
