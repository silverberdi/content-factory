# CF-05 — Recorrido verificable de análisis de URL

Este cambio entrega únicamente backend. La verificación local usa el canal seed `IA Simple ES` y el endpoint autenticado:

```http
POST /api/editorial-url-analyses
Content-Type: application/json

{
  "channelId": "00000000-0000-0000-0000-000000000010",
  "url": "https://example.com/",
  "angle": "explicación breve",
  "manualText": "Texto de artículo proporcionado por el operador cuando la fuente no ofrece texto accesible."
}
```

La respuesta conserva `Completed`, el `ContentItem`, la evidencia, el `TruthSource` en `Draft`, los hallazgos sustentados y la procedencia del proveedor. Repetir la misma URL normalizada, canal y contexto devuelve el mismo `AnalysisId` sin invocar otra vez la IA. `GET /api/editorial-url-analyses/{AnalysisId}` recupera el resultado persistido.

Formatos comprobados en este alcance:

- HTML público accesible por HTTP(S), con texto legible y metadatos observables (`title`, `lang`, `author` cuando están presentes).
- Texto manual asociado a una URL HTTP(S), identificado como `manual`.
- URL sin texto analizable: estado `NotAnalyzable`, sin citas, timestamps ni hallazgos inventados.

No se afirma acceso productivo ni soporte para ASR, scraping evasivo, descarga masiva, conectores específicos de video/podcast, UI o formatos no verificados. Las URL loopback, privadas, no HTTP(S), redirecciones inseguras, respuestas demasiado grandes y fallos de captura se rechazan o quedan como error sanitizado recuperable.
