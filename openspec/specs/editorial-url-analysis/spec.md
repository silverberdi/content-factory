# editorial-url-analysis Specification

## Purpose

Permite analizar de forma segura una URL fuente y conservar evidencia y hallazgos editoriales verificables para consumidores backend posteriores, con resultados recuperables, cardinalidad sustentada y sin alterar las pantallas existentes.

## Requirements

### Requirement: Solicitud idempotente de análisis por canal y URL

El sistema SHALL aceptar una solicitud autenticada con canal, URL HTTP(S) y ángulo opcional, reutilizar un `ContentItem` compatible o crear uno dentro del flujo ordinario, y devolver un identificador estable del análisis. Reintentos equivalentes SHALL reutilizar el contenido y resultado persistido, sin duplicar fuentes, evidencias ni recomendaciones.

#### Scenario: Reintento de una solicitud equivalente
- **WHEN** un operador solicita dos veces el análisis de la misma URL normalizada para el mismo canal y contexto compatible
- **THEN** el sistema conserva un único contenido y una única procedencia de captura aplicable
- **AND** la segunda respuesta permite recuperar el resultado persistido sin ejecutar nuevamente la IA

#### Scenario: Ángulo opcional no rompe la semántica de fuente
- **WHEN** se solicita el análisis con un ángulo editorial opcional y existe un `ContentItem` previo compatible
- **THEN** el ángulo queda asociado al análisis sin reemplazar ni duplicar la fuente original

### Requirement: Captura verificable y protección de acceso a URL

El sistema SHALL aceptar solo URL HTTP(S) válidas y SHALL rechazar o detener explícitamente destinos loopback, internos o privados, resoluciones DNS a destinos privados, redirecciones a destinos privados, respuestas que excedan límites de tiempo o tamaño y orígenes inaccesibles. SHALL conservar URL, título, idioma y autor solo cuando sean comprobables, texto relevante y localizadores originales; SHALL registrar el fallo sanitizado y nunca presentarlo como captura correcta.

#### Scenario: URL insegura o fuera de límites
- **WHEN** una URL no HTTP(S), una resolución o redirección apunta a red privada, o la respuesta supera tiempo o tamaño permitido
- **THEN** la solicitud termina con un error explícito y estado no exitoso
- **AND** no se accede ni persiste contenido del destino inseguro

#### Scenario: Captura pública inaccesible
- **WHEN** un origen público no responde o no puede capturarse
- **THEN** el resultado queda registrado como fallido con diagnóstico sanitizado
- **AND** no se presenta evidencia como capturada correctamente

#### Scenario: Metadatos y localizador comprobables
- **WHEN** una página pública contiene texto legible y metadatos observables
- **THEN** la evidencia conserva el texto relevante, URL, título, idioma y autor únicamente si están respaldados
- **AND** un localizador de texto se conserva sin equiparar HTML crudo con contenido hablado

### Requirement: Soportes permitidos y estado explícito de no analizable

El sistema SHALL priorizar artículos y transcripciones textuales obtenibles mediante acceso legítimo y verificable. Para un video o podcast sin transcripción accesible SHALL devolver un estado recuperable de no analizable por ausencia de texto, sin inventar autor, timestamps, transcripción ni hallazgos; SHALL admitir texto manual proporcionado por el usuario y SHALL excluir ASR propio, scraping evasivo, descarga masiva y una pila de conectores por plataforma.

#### Scenario: Video sin transcripción accesible
- **WHEN** una URL multimedia no ofrece texto de transcripción mediante un acceso permitido y legítimo
- **THEN** el análisis queda en estado explícito de no analizable por ausencia de texto
- **AND** no se generan citas, timestamps ni hallazgos ficticios
- **AND** el operador puede reintentar aportando texto manual

#### Scenario: Texto manual legítimo
- **WHEN** el operador aporta texto asociado a una URL multimedia no transcribible automáticamente
- **THEN** el sistema puede analizar ese texto y conserva su procedencia como entrada manual
- **AND** el resultado distingue que el texto fue proporcionado por el usuario

### Requirement: Hallazgos variables, trazables y recuperables

El sistema SHALL conservar cero o más hallazgos según el soporte real del contenido, sin cuota fija ni relleno ficticio. Cada hallazgo SHALL incluir identificador local estable, título o síntesis en español, valor o ángulo editorial, referencia a evidencia real y localizador original de texto, segmento o timestamp solo cuando sea comprobable. El resultado SHALL poder leerse después sin invocar nuevamente a la IA.

#### Scenario: Artículo con varios hallazgos
- **WHEN** un artículo capturado contiene soporte suficiente para varios ángulos distintos
- **THEN** el sistema conserva la evidencia original y devuelve tantos hallazgos sustentados como corresponda, por ejemplo uno, tres, diez o más
- **AND** cada hallazgo referencia evidencia y localizador reales

#### Scenario: Fuente con una sola idea valiosa
- **WHEN** el texto solo sustenta un hallazgo relevante
- **THEN** el resultado contiene exactamente un hallazgo
- **AND** no agrega hallazgos para completar una cantidad visual esperada

#### Scenario: Fuente sin soporte
- **WHEN** el contenido no proporciona soporte suficiente para un hallazgo
- **THEN** el resultado puede contener cero hallazgos
- **AND** no inventa contenido ni referencias

### Requirement: Contrato mínimo de invocación y lectura

La API SHALL exponer únicamente las operaciones mínimas autenticadas para iniciar o recuperar el análisis y leer su estado, evidencia y hallazgos persistidos. El contrato SHALL ocultar detalles de captura o scraping, SHALL devolver estados explícitos de éxito, fallo y no analizable, y SHALL permitir que un consumidor posterior combine la salida con ideas y guiones existentes sin requerir UI nueva.

#### Scenario: Recuperación del borrador reabierto
- **WHEN** un consumidor consulta un análisis previamente completado
- **THEN** recibe el estado, procedencia, evidencia, hallazgos y referencia al borrador asociado
- **AND** la consulta no llama nuevamente al proveedor de IA

#### Scenario: Error recuperable mediante contrato
- **WHEN** la captura falla o no existe texto analizable
- **THEN** la lectura devuelve el estado explícito y un código de error estable
- **AND** el consumidor puede presentar la condición como recuperable sin interpretarla como éxito

### Requirement: Fronteras de alcance del backend

La capacidad SHALL reutilizar persistencia, servicios, auditoría y enrutamiento de IA existentes y SHALL evitar una base de datos, cola, worker, microservicio, infraestructura, segundo router o subsistema paralelo. SHALL crear únicamente datos y contrato backend; no SHALL modificar, reemplazar o rediseñar pantallas, rutas o componentes existentes ni generar ideas, guiones, TTS, imágenes o MP4.

#### Scenario: Implementación acotada
- **WHEN** se incorpora el análisis de URL
- **THEN** el resultado se persiste en el almacenamiento relacional existente y usa las operaciones de contenido y evidencia disponibles
- **AND** no se crea una arquitectura paralela ni se intervienen pantallas existentes
