# Spec Delta

## ADDED Requirements

### Requirement: Resultados editoriales distinguen proveedor real y simulado

La generación y revisión editorial SHALL conservar y comunicar la identidad del proveedor seleccionado y si el resultado es real o simulado. Si un proveedor real falla o carece de configuración requerida, la operación SHALL devolver un error identificable y seguro; SHALL NOT devolver contenido Mock como éxito del proveedor real.

#### Scenario: Fallo de proveedor editorial real
- **WHEN** un proveedor editorial real seleccionado falla durante una generación síncrona
- **THEN** la respuesta comunica un error identificable con mensaje seguro y correlación disponible
- **AND** no devuelve contenido Mock como éxito real

#### Scenario: Configuración editorial ausente
- **WHEN** un proveedor editorial real seleccionado no tiene la configuración necesaria
- **THEN** la operación informa configuración insuficiente sin exponer secretos
- **AND** no cambia silenciosamente a Mock

#### Scenario: Mock editorial explícito
- **WHEN** el entorno o la solicitud selecciona Mock de forma válida
- **THEN** se produce contenido de prueba determinista
- **AND** el resultado identifica el proveedor Mock y su naturaleza simulada

#### Scenario: Metadatos editoriales verificables
- **WHEN** se persiste un resultado editorial real o simulado
- **THEN** se registra la identidad del proveedor y si el resultado es real o simulado en los datos o metadatos existentes relevantes
- **AND** no se inventa una plataforma de observabilidad

### Requirement: Censo y contratos editoriales preservan errores de proveedor

El cambio SHALL incluir los puntos de selección y fallback de las respuestas editoriales síncronas y de los Jobs editoriales asíncronos identificados en el sistema. La propagación de errores SHALL preservar aprobaciones, reintentos, versiones, trazabilidad y contratos API, modificando solo lo indispensable para impedir que un resultado simulado se presente como real.

#### Scenario: Censo de rutas editoriales
- **WHEN** se censa la selección y ejecución de proveedores editoriales
- **THEN** se incluyen rutas síncronas y Jobs asíncronos
- **AND** se verifica el uso y selección existente de Mock antes de modificar el comportamiento

#### Scenario: Pruebas editoriales sin APIs pagadas
- **WHEN** se verifican éxito Mock explícito, configuración ausente y fallo del proveedor real
- **THEN** las pruebas son deterministas y usan dobles o servidores falsos locales
- **AND** no consumen APIs pagadas ni acreditan que DeepSeek real funcione
