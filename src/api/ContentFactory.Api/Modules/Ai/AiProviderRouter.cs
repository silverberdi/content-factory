using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Ai;

public class AiProviderRouter(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AiProviderRouter> logger) : IAiProviderRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AiCapabilityResult<BuildTruthSourceResponse>> BuildTruthSourceAsync(
        BuildTruthSourceRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        // Use mock if configured, or if DeepSeek key is missing in development/test
        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.BuildTruthSource, context.ChannelId);
            return await ExecuteMockBuildTruthSourceAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekBuildTruthSourceAsync(request, context, apiKey!, cancellationToken);
        }

        // Default fallback to mock
        return await ExecuteMockBuildTruthSourceAsync(request, context, cancellationToken);
    }

    private string ResolveProvider(AiRoutingContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.PreferredProvider))
        {
            return context.PreferredProvider;
        }

        var configuredDefault = configuration["AI_DEFAULT_PROVIDER"] 
            ?? Environment.GetEnvironmentVariable("AI_DEFAULT_PROVIDER") 
            ?? AiProviders.DeepSeek;

        return configuredDefault;
    }

    private async Task<AiCapabilityResult<BuildTruthSourceResponse>> ExecuteDeepSeekBuildTruthSourceAsync(
        BuildTruthSourceRequest request,
        AiRoutingContext context,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var model = context.PreferredModel ?? configuration["DEEPSEEK_MODEL"] ?? "deepseek-chat";
        var endpoint = configuration["DEEPSEEK_API_URL"] ?? "https://api.deepseek.com/chat/completions";
        var promptVersion = "1.0";

        var stopwatch = Stopwatch.StartNew();
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt = BuildSystemPrompt(request);
        var userPrompt = BuildUserPrompt(request);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("DeepSeek API error {StatusCode}: {ErrorBody}. Falling back to deterministic mock.",
                    response.StatusCode, errorBody);
                return await ExecuteMockBuildTruthSourceAsync(request, context, cancellationToken);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var root = JsonNode.Parse(responseJson);

            var messageContent = root?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";
            var tokensIn = root?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;
            var tokensOut = root?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;
            var cost = CalculateCost(tokensIn, tokensOut);

            var parsed = JsonSerializer.Deserialize<BuildTruthSourceResponse>(messageContent, JsonOptions);
            if (parsed == null)
            {
                throw new InvalidOperationException("Failed to deserialize DeepSeek structured output.");
            }

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                Capability = AiCapabilities.BuildTruthSource,
                Provider = AiProviders.DeepSeek,
                Model = model,
                PromptPolicyVersion = promptVersion,
                StructuredOutputJson = messageContent,
                Confidence = 0.92,
                Rationale = parsed.ConciseRationale,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                EstimatedCostUsd = cost,
                AcceptedState = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.AiRecommendations.Add(recommendation);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new AiCapabilityResult<BuildTruthSourceResponse>(true, parsed, recommendation, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "DeepSeek execution failed. Falling back to deterministic mock.");
            return await ExecuteMockBuildTruthSourceAsync(request, context, cancellationToken);
        }
    }

    private async Task<AiCapabilityResult<BuildTruthSourceResponse>> ExecuteMockBuildTruthSourceAsync(
        BuildTruthSourceRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-reasoning-engine-v1";
        var promptVersion = "1.0-mock";

        var primaryEvidence = request.Evidences.FirstOrDefault(e => e.Role == EvidenceRole.PrimaryLead)
            ?? request.Evidences.FirstOrDefault();

        var primaryTitle = primaryEvidence?.Title ?? "Temática de Inteligencia Artificial";
        var primaryId = primaryEvidence?.EvidenceId ?? Guid.NewGuid();

        var claims = new List<VerifiableClaimDto>();
        var evidenceRefs = new List<Guid>();

        foreach (var ev in request.Evidences)
        {
            evidenceRefs.Add(ev.EvidenceId);
            var snippetText = !string.IsNullOrWhiteSpace(ev.ExtractedText) 
                ? (ev.ExtractedText.Length > 120 ? ev.ExtractedText[..120] + "..." : ev.ExtractedText) 
                : ev.Title;

            claims.Add(new VerifiableClaimDto(
                $"Hecho extraído: {snippetText}",
                ev.OriginUrl ?? "Nota editorial de contexto",
                ev.EvidenceId
            ));
        }

        var responseData = new BuildTruthSourceResponse(
            Summary: $"Síntesis factual sobre '{primaryTitle}' orientada al canal {request.ChannelName} ({request.ChannelNiche}). Explica de forma clara y accesible cómo aplicar estas herramientas con criterio analítico y verificación humana.",
            KeyIdeas:
            [
                $"La adopción práctica de '{primaryTitle}' optimiza procesos reales sin necesidad de programación avanzada.",
                "El criterio analítico y la verificación de fuentes son indispensables para evitar errores operativos.",
                "La supervisión humana continua asegura el cumplimiento de estándares de calidad y privacidad."
            ],
            VerifiableClaims: claims,
            EvidenceReferences: evidenceRefs,
            RiskNotes: "Evitar promesas absolutas sobre sustitución laboral total o resultados garantizados sin esfuerzo.",
            DoNotSayConstraints:
            [
                "No usar frases sensacionalistas como 'la IA te dejará sin trabajo en un mes'.",
                "No promover herramientas no verificadas o fórmulas mágicas de éxito rápido.",
                "No emplear jerga técnica excesiva sin explicarla en términos prácticos y comprensibles."
            ],
            PossibleAngles:
            [
                $"Las 3 claves de {primaryTitle} que debes conocer en 2026",
                "Cómo aplicar esta tecnología en tu trabajo diario paso a paso",
                "Errores comunes al usar IA en tareas profesionales y cómo evitarlos"
            ],
            LocalizationNotes: "Español neutro y accesible, con terminología clara para profesionales de España y Latinoamérica.",
            ConciseRationale: $"Síntesis derivada de {request.Evidences.Count} fuente(s) con foco en rigor factual, guardrails anti-sensacionalismo y tono sobrio para {request.ChannelName}."
        );

        stopwatch.Stop();
        var simulatedTokensIn = 450 + request.Evidences.Count * 180;
        var simulatedTokensOut = 320;
        var cost = CalculateCost(simulatedTokensIn, simulatedTokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            Capability = AiCapabilities.BuildTruthSource,
            Provider = AiProviders.Mock,
            Model = model,
            PromptPolicyVersion = promptVersion,
            StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
            Confidence = 0.95,
            Rationale = responseData.ConciseRationale,
            LatencyMs = Math.Max(stopwatch.ElapsedMilliseconds, 25),
            TokensIn = simulatedTokensIn,
            TokensOut = simulatedTokensOut,
            EstimatedCostUsd = cost,
            AcceptedState = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.AiRecommendations.Add(recommendation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AiCapabilityResult<BuildTruthSourceResponse>(true, responseData, recommendation, null);
    }

    private static string BuildSystemPrompt(BuildTruthSourceRequest request) =>
        $@"Eres el motor de análisis editorial y síntesis de evidencia de Content Factory.
Tu objetivo es producir un borrador estructurado de 'TruthSource' para el canal '{request.ChannelName}' (Nicho: {request.ChannelNiche}, Idioma: {request.ChannelLanguage}).
Reglas estrictas:
1. Extrae únicamente hechos verificables directamente presentes en las fuentes suministradas.
2. Cada afirmación en 'verifiableClaims' DEBE referenciar el 'evidenceId' de la fuente correspondiente.
3. Genera guardrails específicos en 'doNotSayConstraints' evitando sensacionalismo, clickbait y promesas irreales.
4. Redacta en español claro, sobrio y accesible.
5. Devuelve EXCLUSIVAMENTE un objeto JSON válido con los campos: summary, keyIdeas (array), verifiableClaims (array de {{claim, sourceCitation, evidenceId}}), evidenceReferences (array de IDs), riskNotes, doNotSayConstraints (array), possibleAngles (array), localizationNotes, conciseRationale.
NUNCA incluyas razonamiento privado ni bloques de pensamiento fuera del JSON.";

    private static string BuildUserPrompt(BuildTruthSourceRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Canal: {request.ChannelName} ({request.ChannelLanguage}) - Nicho: {request.ChannelNiche}");
        sb.AppendLine("Fuentes de evidencia disponibles:");
        foreach (var ev in request.Evidences)
        {
            sb.AppendLine($"- ID: {ev.EvidenceId}");
            sb.AppendLine($"  Título: {ev.Title}");
            sb.AppendLine($"  Rol: {ev.Role}");
            sb.AppendLine($"  URL: {ev.OriginUrl ?? "N/A"}");
            sb.AppendLine($"  Contenido: {ev.ExtractedText}");
        }
        sb.AppendLine("\nGenera la síntesis estructurada de TruthSource en formato JSON.");
        return sb.ToString();
    }

    public async Task<AiCapabilityResult<GenerateIdeasResponse>> GenerateIdeasAsync(
        GenerateIdeasRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.GenerateIdeas, context.ChannelId);
            return await ExecuteMockGenerateIdeasAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekGenerateIdeasAsync(request, context, apiKey!, cancellationToken);
        }

        return await ExecuteMockGenerateIdeasAsync(request, context, cancellationToken);
    }

    private async Task<AiCapabilityResult<GenerateIdeasResponse>> ExecuteDeepSeekGenerateIdeasAsync(
        GenerateIdeasRequest request,
        AiRoutingContext context,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var model = context.PreferredModel ?? configuration["DEEPSEEK_MODEL"] ?? "deepseek-chat";
        var endpoint = configuration["DEEPSEEK_API_URL"] ?? "https://api.deepseek.com/chat/completions";
        var promptVersion = "1.0";

        var stopwatch = Stopwatch.StartNew();
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt = BuildGenerateIdeasSystemPrompt(request);
        var userPrompt = BuildGenerateIdeasUserPrompt(request);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("DeepSeek API error for generate_ideas: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return await ExecuteMockGenerateIdeasAsync(request, context, cancellationToken);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedRoot = JsonNode.Parse(responseBody);
            var rawContent = parsedRoot?["choices"]?[0]?["message"]?[contentField]?.GetValue<string>()
                ?? parsedRoot?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return await ExecuteMockGenerateIdeasAsync(request, context, cancellationToken);
            }

            var tokensIn = parsedRoot?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 500;
            var tokensOut = parsedRoot?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 400;
            var cost = CalculateCost(tokensIn, tokensOut);

            var responseData = JsonSerializer.Deserialize<GenerateIdeasResponse>(rawContent, JsonOptions)
                ?? throw new JsonException("Failed to deserialize GenerateIdeasResponse");

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                TruthSourceVersionId = request.TruthSourceVersionId,
                Capability = AiCapabilities.GenerateIdeas,
                Provider = AiProviders.DeepSeek,
                Model = model,
                PromptPolicyVersion = promptVersion,
                StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
                Confidence = 0.92,
                Rationale = responseData.ConciseRationale,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                EstimatedCostUsd = cost,
                AcceptedState = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.AiRecommendations.Add(recommendation);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new AiCapabilityResult<GenerateIdeasResponse>(true, responseData, recommendation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling DeepSeek API for generate_ideas, falling back to mock provider");
            return await ExecuteMockGenerateIdeasAsync(request, context, cancellationToken);
        }
    }

    private static readonly string contentField = "content";

    private async Task<AiCapabilityResult<GenerateIdeasResponse>> ExecuteMockGenerateIdeasAsync(
        GenerateIdeasRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-reasoning-ideas-v1";
        var promptVersion = "1.0";

        var count = Math.Clamp(request.Count, 2, 5);
        var ideas = new List<GeneratedIdeaItem>
        {
            new(
                Title: $"3 Claves de {Truncate(request.Summary, 45)} que la IA No Reemplaza en 2026",
                Angle: "Enfoque contraintuitivo / Empoderamiento: Por qué el criterio crítico supera a la memorización de prompts en entornos reales.",
                HookStrategy: "¿Crees que un prompt te salvará en 2026? Estas 3 habilidades valen 10 veces más y ningún modelo las domina.",
                AudienceValue: "El espectador aprende a posicionarse con pensamiento crítico y auditoría humana frente a la automatización ciega.",
                Format: "YouTube Short 30-60s",
                IntendedOutcome: "Inspiración práctica / Retención alta",
                FreshnessClass: IdeaFreshnessClass.Timely,
                Priority: IdeaPriority.High,
                Rationale: "Aprovecha la síntesis factual sobre verificación y habilidades híbridas en el mercado laboral."
            ),
            new(
                Title: $"El Error de 1.000€ que Cometen al Delegar Tareas en IA",
                Angle: "Alerta de riesgo operativo / Caso de negocio: Auditoría de respuestas para evitar fallos legales y contables costosos.",
                HookStrategy: "Un error tonto en una respuesta de IA puede salirte carísimo si no aplicas esta regla de 30 segundos.",
                AudienceValue: "Checklist de 3 pasos para auditar resúmenes y extracciones de datos antes de enviarlos a clientes o jefes.",
                Format: "YouTube Short 30-60s",
                IntendedOutcome: "Prevención de errores / Tip accionable",
                FreshnessClass: IdeaFreshnessClass.Evergreen,
                Priority: IdeaPriority.Normal,
                Rationale: "Derivado de las notas de riesgo y guardrails sobre precisión de datos en tareas administrativas."
            ),
            new(
                Title: $"Cómo Usar Modelos de Razonamiento Paso a Paso Sin Complicarte",
                Angle: "Tutorial práctico de alta eficiencia: Flujo de trabajo directo para no técnicos enfocado en resultados inmediatos.",
                HookStrategy: "Deja de pelear con prompts kilométricos: este truco de estructura hace que la IA piense antes de contestar.",
                AudienceValue: "Técnica concreta de framing estructurado que reduce alucinaciones y ahorra tiempo en tareas repetitivas.",
                Format: "YouTube Short 30-60s",
                IntendedOutcome: "Tutorial paso a paso / Guardado y compartición",
                FreshnessClass: IdeaFreshnessClass.Timely,
                Priority: IdeaPriority.Normal,
                Rationale: "Enfocado en la audiencia no técnica del canal buscando soluciones aplicables sin fricción."
            )
        };

        if (count > 3)
        {
            ideas.Add(new(
                Title: "La Verdad Incómoda sobre la Automatización con IA en 2026",
                Angle: "Debunking / Análisis realista: Desmontar mitos sobre ganancias mágicas y mostrar la realidad del trabajo aumentado.",
                HookStrategy: "Todo el mundo te promete dinero fácil con IA... pero nadie te cuenta este detalle que cambia las reglas del juego.",
                AudienceValue: "Claridad mental para evitar cursos engañosos y centrarse en herramientas con ROI demostrable.",
                Format: "YouTube Short 30-60s",
                IntendedOutcome: "Construcción de confianza / Autoridad editorial",
                FreshnessClass: IdeaFreshnessClass.Evergreen,
                Priority: IdeaPriority.Low,
                Rationale: "Fortalece la autoridad editorial y los valores de sobriedad y rigor del canal."
            ));
        }

        var responseData = new GenerateIdeasResponse(
            Ideas: ideas.Take(count).ToList(),
            ConciseRationale: $"Propuestas creativas estructuradas para '{request.ChannelName}' a partir de TruthSource v{request.TruthSourceVersionId.ToString()[..8]}, con diversos estilos de gancho (Pregunta provocadora, Alerta de riesgo, Tutorial directo)."
        );

        stopwatch.Stop();
        var tokensIn = 520;
        var tokensOut = 380;
        var cost = CalculateCost(tokensIn, tokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            TruthSourceVersionId = request.TruthSourceVersionId,
            Capability = AiCapabilities.GenerateIdeas,
            Provider = AiProviders.Mock,
            Model = model,
            PromptPolicyVersion = promptVersion,
            StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
            Confidence = 0.94,
            Rationale = responseData.ConciseRationale,
            LatencyMs = Math.Max(stopwatch.ElapsedMilliseconds, 30),
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            EstimatedCostUsd = cost,
            AcceptedState = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.AiRecommendations.Add(recommendation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AiCapabilityResult<GenerateIdeasResponse>(true, responseData, recommendation, null);
    }

    private static string Truncate(string value, int maxLen) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLen ? value : value[..maxLen] + "...";

    private static string BuildGenerateIdeasSystemPrompt(GenerateIdeasRequest request) =>
        $@"Eres el estratega creativo y director editorial de Content Factory para el canal '{request.ChannelName}' (Nicho: {request.ChannelNiche}, Idioma: {request.ChannelLanguage}).
Tu objetivo es generar {request.Count} ideas editoriales y ganchos de alto impacto para videos cortos (YouTube Shorts 30-60s) basándote ESTRICTAMENTE en los hechos y síntesis del TruthSource aprobado suministrado.
Reglas estrictas:
1. Cada idea debe tener un ángulo diferenciado (ej. Contraintuitivo, Prevención de Riesgo, Tutorial Paso a Paso, Análisis Realista).
2. 'hookStrategy' DEBE ser un patrón de interrupción potente para los primeros 0-3 segundos.
3. 'audienceValue' debe declarar explícitamente el beneficio o aprendizaje del espectador.
4. 'format' debe ser 'YouTube Short 30-60s'.
5. Respeta al 100% las restricciones 'doNotSayConstraints'.
6. Devuelve EXCLUSIVAMENTE un objeto JSON válido con la estructura:
{{
  ""ideas"": [
    {{
      ""title"": ""..."",
      ""angle"": ""..."",
      ""hookStrategy"": ""..."",
      ""audienceValue"": ""..."",
      ""format"": ""YouTube Short 30-60s"",
      ""intendedOutcome"": ""..."",
      ""freshnessClass"": ""Breaking | Timely | Evergreen"",
      ""priority"": ""Low | Normal | High | Urgent"",
      ""rationale"": ""...""
    }}
  ],
  ""conciseRationale"": ""...""
}}
NUNCA incluyas razonamiento privado ni bloques de pensamiento fuera del JSON.";

    private static string BuildGenerateIdeasUserPrompt(GenerateIdeasRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Canal: {request.ChannelName} ({request.ChannelLanguage}) - Nicho: {request.ChannelNiche}");
        sb.AppendLine($"TruthSource Resumen: {request.Summary}");
        sb.AppendLine("Ideas Clave:");
        foreach (var ki in request.KeyIdeas)
        {
            sb.AppendLine($"- {ki}");
        }
        sb.AppendLine("Afirmaciones Verificables:");
        foreach (var vc in request.VerifiableClaims)
        {
            sb.AppendLine($"- {vc.Claim} (Cita: {vc.SourceCitation})");
        }
        if (request.DoNotSayConstraints.Count > 0)
        {
            sb.AppendLine("Restricciones (Do NOT Say):");
            foreach (var sc in request.DoNotSayConstraints)
            {
                sb.AppendLine($"- {sc}");
            }
        }
        if (!string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            sb.AppendLine($"Audiencia objetivo: {request.TargetAudience}");
        }
        if (!string.IsNullOrWhiteSpace(request.FocusAngleStyle))
        {
            sb.AppendLine($"Estilo de ángulo preferido: {request.FocusAngleStyle}");
        }
        sb.AppendLine($"\nGenera {request.Count} propuestas creativas distintas en formato JSON.");
        return sb.ToString();
    }

    private static decimal CalculateCost(int tokensIn, int tokensOut)
    {
        // DeepSeek approx pricing: $0.14 / 1M in, $0.28 / 1M out
        var costIn = (decimal)tokensIn * 0.00000014m;
        var costOut = (decimal)tokensOut * 0.00000028m;
        return Math.Round(costIn + costOut, 6);
    }
}
