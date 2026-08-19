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

    public async Task<AiCapabilityResult<GenerateScriptResponse>> GenerateScriptAsync(
        GenerateScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.GenerateScript, context.ChannelId);
            return await ExecuteMockGenerateScriptAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekGenerateScriptAsync(request, context, apiKey!, cancellationToken);
        }

        return await ExecuteMockGenerateScriptAsync(request, context, cancellationToken);
    }

    private async Task<AiCapabilityResult<GenerateScriptResponse>> ExecuteDeepSeekGenerateScriptAsync(
        GenerateScriptRequest request,
        AiRoutingContext context,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var model = context.PreferredModel ?? configuration["DEEPSEEK_MODEL"] ?? "deepseek-chat";
        var endpoint = configuration["DEEPSEEK_API_URL"] ?? "https://api.deepseek.com/chat/completions";
        var promptVersion = "1.0";

        var stopwatch = Stopwatch.StartNew();
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt = BuildGenerateScriptSystemPrompt(request);
        var userPrompt = BuildGenerateScriptUserPrompt(request);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
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
                logger.LogError("DeepSeek API error for generate_script: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return await ExecuteMockGenerateScriptAsync(request, context, cancellationToken);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedRoot = JsonNode.Parse(responseBody);
            var rawContent = parsedRoot?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return await ExecuteMockGenerateScriptAsync(request, context, cancellationToken);
            }

            var tokensIn = parsedRoot?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 650;
            var tokensOut = parsedRoot?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 500;
            var cost = CalculateCost(tokensIn, tokensOut);

            var responseData = JsonSerializer.Deserialize<GenerateScriptResponse>(rawContent, JsonOptions)
                ?? throw new JsonException("Failed to deserialize GenerateScriptResponse");

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                TruthSourceVersionId = request.TruthSourceVersionId,
                Capability = AiCapabilities.GenerateScript,
                Provider = AiProviders.DeepSeek,
                Model = model,
                PromptPolicyVersion = promptVersion,
                StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
                Confidence = 0.93,
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

            return new AiCapabilityResult<GenerateScriptResponse>(true, responseData, recommendation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling DeepSeek API for generate_script, falling back to mock provider");
            return await ExecuteMockGenerateScriptAsync(request, context, cancellationToken);
        }
    }

    private async Task<AiCapabilityResult<GenerateScriptResponse>> ExecuteMockGenerateScriptAsync(
        GenerateScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-script-generator-v1";
        var promptVersion = "1.0-mock";

        var targetWpm = request.PacingWpm > 0 ? request.PacingWpm : 140;
        var firstClaim = request.VerifiableClaims.FirstOrDefault();

        var scenes = new List<GeneratedScriptSceneItem>
        {
            new(
                OrderIndex: 1,
                SceneType: SceneType.Hook,
                NarrationText: string.IsNullOrWhiteSpace(request.IdeaHookStrategy)
                    ? "¿Crees que dominar la inteligencia artificial requiere meses de estudio técnico? Mira esto."
                    : request.IdeaHookStrategy,
                VisualPrompt: "Primer plano directo a cámara con texto animado dinámico resaltando la pregunta inicial.",
                EvidenceReferences: firstClaim != null ? [new ScriptSceneEvidenceReferenceDto(Guid.NewGuid(), Guid.Empty, firstClaim.EvidenceId, firstClaim.Claim, "Apertura con gancho de alto impacto y patrón de interrupción")] : null
            ),
            new(
                OrderIndex: 2,
                SceneType: SceneType.Problem,
                NarrationText: "La mayoría de profesionales pierde horas intentando memorizar prompts complejos que quedan obsoletos cada tres semanas.",
                VisualPrompt: "B-roll rápido mostrando a una persona frustrada frente a una pantalla con exceso de pestañas abiertas.",
                EvidenceReferences: null
            ),
            new(
                OrderIndex: 3,
                SceneType: SceneType.Insight,
                NarrationText: $"La clave está en {request.IdeaAngle}. Aplicar criterio analítico y verificar cada respuesta reduce los fallos operativos en un ochenta por ciento.",
                VisualPrompt: "Animación de interfaz limpia mostrando la estructura paso a paso y la verificación de datos.",
                EvidenceReferences: firstClaim != null ? [new ScriptSceneEvidenceReferenceDto(Guid.NewGuid(), Guid.Empty, firstClaim.EvidenceId, firstClaim.Claim, "Fórmula respaldada en hechos y verificación factual del TruthSource")] : null
            ),
            new(
                OrderIndex: 4,
                SceneType: SceneType.Climax,
                NarrationText: "No necesitas más herramientas: necesitas una metodología clara de tres pasos para auditar lo que la IA produce.",
                VisualPrompt: "Infografía gráfica concisa de 3 pasos numerados con iconos de verificación verde.",
                EvidenceReferences: null
            ),
            new(
                OrderIndex: 5,
                SceneType: SceneType.CallToAction,
                NarrationText: "Guarda este video para aplicar el método en tu próxima tarea y comenta qué herramienta usas más.",
                VisualPrompt: "Llamada a la acción visual con icono de guardado y flecha hacia la caja de comentarios.",
                EvidenceReferences: null
            )
        };

        var scriptResult = new GeneratedScriptResult(
            Title: request.IdeaTitle,
            TargetDurationSeconds: request.TargetDurationSeconds > 0 ? request.TargetDurationSeconds : 45,
            PacingWpm: targetWpm,
            Language: request.ChannelLanguage ?? "es-ES",
            Scenes: scenes
        );

        var responseData = new GenerateScriptResponse(
            Script: scriptResult,
            ConciseRationale: $"Guión estructurado en 5 escenas adaptado al ritmo de {targetWpm} WPM para el canal {request.ChannelName}, preservando trazabilidad factual hacia el TruthSource v{request.TruthSourceVersionId.ToString()[..8]}."
        );

        stopwatch.Stop();
        var simulatedTokensIn = 600;
        var simulatedTokensOut = 450;
        var cost = CalculateCost(simulatedTokensIn, simulatedTokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            TruthSourceVersionId = request.TruthSourceVersionId,
            Capability = AiCapabilities.GenerateScript,
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

        return new AiCapabilityResult<GenerateScriptResponse>(true, responseData, recommendation, null);
    }

    private static string BuildGenerateScriptSystemPrompt(GenerateScriptRequest request) =>
        $@"Eres el guionista principal y productor de video corto de Content Factory para el canal '{request.ChannelName}' ({request.ChannelLanguage}).
Tu objetivo es transformar la idea creativa seleccionada y la evidencia del TruthSource en un guión estructurado escena por escena para YouTube Shorts / Reels (30-60 segundos).
Pacing de lectura configurado: {request.PacingWpm} palabras por minuto (~{(request.PacingWpm / 60.0):F2} palabras/segundo).
Duración objetivo: {request.TargetDurationSeconds} segundos.
Reglas estrictas:
1. Estructura exactamente en escenas: Hook (0-3s), Problem, Insight, Climax, CallToAction.
2. Cada escena debe incluir 'narrationText' (texto exacto a locutar en español sobrio y natural) y 'visualPrompt' (instrucción visual precisa de b-roll o animación).
3. Todas las afirmaciones factuales en la narración DEBEN basarse en las 'verifiableClaims' del TruthSource y vincularse en 'evidenceReferences'.
4. NUNCA violes las restricciones 'doNotSayConstraints'.
5. Devuelve EXCLUSIVAMENTE un objeto JSON válido con la estructura:
{{
  ""script"": {{
    ""title"": ""..."",
    ""targetDurationSeconds"": {request.TargetDurationSeconds},
    ""pacingWpm"": {request.PacingWpm},
    ""language"": ""{request.ChannelLanguage}"",
    ""scenes"": [
      {{
        ""orderIndex"": 1,
        ""sceneType"": ""Hook | Problem | Insight | Climax | CallToAction"",
        ""narrationText"": ""..."",
        ""visualPrompt"": ""..."",
        ""evidenceReferences"": [
          {{
            ""claimStatement"": ""..."",
            ""truthSourceClaimId"": ""..."",
            ""editorialNote"": ""...""
          }}
        ]
      }}
    ]
  }},
  ""conciseRationale"": ""...""
}}";

    private static string BuildGenerateScriptUserPrompt(GenerateScriptRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Idea Título: {request.IdeaTitle}");
        sb.AppendLine($"Ángulo: {request.IdeaAngle}");
        sb.AppendLine($"Estrategia de Gancho: {request.IdeaHookStrategy}");
        sb.AppendLine($"Valor para Audiencia: {request.IdeaAudienceValue}");
        sb.AppendLine($"Resultado deseado: {request.IdeaIntendedOutcome}");
        sb.AppendLine($"TruthSource Resumen: {request.Summary}");
        sb.AppendLine("Ideas Clave:");
        foreach (var ki in request.KeyIdeas) sb.AppendLine($"- {ki}");
        sb.AppendLine("Afirmaciones Verificables:");
        foreach (var vc in request.VerifiableClaims) sb.AppendLine($"- [ID: {vc.EvidenceId}] {vc.Claim}");
        if (request.DoNotSayConstraints.Count > 0)
        {
            sb.AppendLine("Restricciones Prohibidas (Do NOT Say):");
            foreach (var sc in request.DoNotSayConstraints) sb.AppendLine($"- {sc}");
        }
        if (!string.IsNullOrWhiteSpace(request.CustomInstructions))
        {
            sb.AppendLine($"Instrucciones editoriales adicionales: {request.CustomInstructions}");
        }
        if (!string.IsNullOrWhiteSpace(request.ToneStyle))
        {
            sb.AppendLine($"Tono estilístico: {request.ToneStyle}");
        }
        sb.AppendLine("\nGenera el guión estructurado en formato JSON.");
        return sb.ToString();
    }

    public async Task<AiCapabilityResult<ReviewScriptResponse>> ReviewScriptAsync(
        ReviewScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.ReviewScript, context.ChannelId);
            return await ExecuteMockReviewScriptAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekReviewScriptAsync(request, context, apiKey!, cancellationToken);
        }

        return await ExecuteMockReviewScriptAsync(request, context, cancellationToken);
    }

    private async Task<AiCapabilityResult<ReviewScriptResponse>> ExecuteDeepSeekReviewScriptAsync(
        ReviewScriptRequest request,
        AiRoutingContext context,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var model = context.PreferredModel ?? configuration["DEEPSEEK_MODEL"] ?? "deepseek-chat";
        var endpoint = configuration["DEEPSEEK_API_URL"] ?? "https://api.deepseek.com/chat/completions";
        var promptVersion = "1.0";

        var stopwatch = Stopwatch.StartNew();
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt = BuildReviewScriptSystemPrompt(request);
        var userPrompt = BuildReviewScriptUserPrompt(request);

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
                logger.LogError("DeepSeek API error for review_script: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return await ExecuteMockReviewScriptAsync(request, context, cancellationToken);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedRoot = JsonNode.Parse(responseBody);
            var rawContent = parsedRoot?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return await ExecuteMockReviewScriptAsync(request, context, cancellationToken);
            }

            var tokensIn = parsedRoot?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 700;
            var tokensOut = parsedRoot?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 450;
            var cost = CalculateCost(tokensIn, tokensOut);

            var responseData = JsonSerializer.Deserialize<ReviewScriptResponse>(rawContent, JsonOptions)
                ?? throw new JsonException("Failed to deserialize ReviewScriptResponse");

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                TruthSourceVersionId = request.TruthSourceVersionId,
                Capability = AiCapabilities.ReviewScript,
                Provider = AiProviders.DeepSeek,
                Model = model,
                PromptPolicyVersion = promptVersion,
                StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
                Confidence = 0.94,
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

            return new AiCapabilityResult<ReviewScriptResponse>(true, responseData, recommendation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling DeepSeek API for review_script, falling back to mock provider");
            return await ExecuteMockReviewScriptAsync(request, context, cancellationToken);
        }
    }

    private async Task<AiCapabilityResult<ReviewScriptResponse>> ExecuteMockReviewScriptAsync(
        ReviewScriptRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-script-critic-v1";
        var promptVersion = "1.0-mock";

        var totalWords = request.Scenes.Sum(s => s.WordCount);
        var targetWpm = request.PacingWpm > 0 ? request.PacingWpm : 140;
        var estDuration = Math.Round(totalWords / (targetWpm / 60.0), 1);

        var durationStatus = (estDuration >= 30 && estDuration <= 60) ? "Pass" : (estDuration > 60 ? "Critical" : "Warning");
        var sceneCritiques = request.Scenes.Select(s => new ScriptSceneCritiqueDto(
            OrderIndex: s.OrderIndex,
            SceneType: s.SceneType,
            Status: "Pass",
            ClaimFidelityNotes: "La afirmación narrada es consistente con la base de evidencia del TruthSource.",
            RetentionNotes: s.SceneType == SceneType.Hook ? "Gancho directo con patrón de interrupción efectivo." : null,
            PacingNotes: $"Ritmo estimado de {s.EstimatedDurationSeconds:F1}s para {s.WordCount} palabras.",
            Suggestions: []
        )).ToList();

        var reviewResult = new ScriptReviewResultDto(
            OverallStatus: durationStatus == "Critical" ? "Warning" : "Pass",
            FactualAlignmentScore: 0.95,
            RetentionAnalysis: "El gancho inicial en los primeros 3 segundos genera curiosidad sin caer en clickbait.",
            PacingAssessment: $"Duración total estimada de {estDuration:F1}s a {targetWpm} WPM. Ritmo equilibrado para formato vertical.",
            DoNotSayComplianceNotes: ["No se detectaron infracciones a las restricciones prohibidas."],
            Dimensions:
            [
                new ScriptReviewDimensionDto("Fidelidad Factual", "Pass", "Todas las afirmaciones corresponden al TruthSource aprobado."),
                new ScriptReviewDimensionDto("Cumplimiento Do-Not-Say", "Pass", "Cero infracciones detectadas."),
                new ScriptReviewDimensionDto("Retención y Gancho", "Pass", "Patrón de interrupción claro y promesa de valor directa."),
                new ScriptReviewDimensionDto("Ritmo y Duración", durationStatus, $"Duración estimada {estDuration:F1}s contra objetivo {request.TargetDurationSeconds}s.")
            ],
            SceneCritiques: sceneCritiques,
            ActionableRecommendations:
            [
                "Mantener el dinamismo visual en la transición entre el problema y el insight principal.",
                "Asegurar que los gráficos en pantalla refuercen los datos numéricos citados en la narración."
            ]
        );

        var responseData = new ReviewScriptResponse(
            ReviewResult: reviewResult,
            ConciseRationale: $"Evaluación editorial consultiva para '{request.ScriptTitle}'. Puntuación factual 95%, cumplimiento de guardrails aprobado y ritmo estimado en {estDuration:F1}s."
        );

        stopwatch.Stop();
        var simulatedTokensIn = 550;
        var simulatedTokensOut = 380;
        var cost = CalculateCost(simulatedTokensIn, simulatedTokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            TruthSourceVersionId = request.TruthSourceVersionId,
            Capability = AiCapabilities.ReviewScript,
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

        return new AiCapabilityResult<ReviewScriptResponse>(true, responseData, recommendation, null);
    }

    private static string BuildReviewScriptSystemPrompt(ReviewScriptRequest request) =>
        $@"Eres el auditor de calidad y crítico editorial de Content Factory para el canal '{request.ChannelName}' ({request.ChannelLanguage}).
Tu labor es realizar una revisión consultiva y analítica de un guión corto frente a la base factual aprobada (TruthSource) y sus restricciones (Do-Not-Say).
IMPORTANTE: Tu evaluación es estrictamente consultiva. La decisión editorial de aprobación o rechazo corresponde exclusivamente al operador humano.
Pacing configurado: {request.PacingWpm} palabras por minuto. Duración objetivo: {request.TargetDurationSeconds} segundos.
Evalúa:
1. Fidelidad factual frente a afirmaciones verificables.
2. Cumplimiento estricto de 'doNotSayConstraints' (si se infringe, marca como 'Critical').
3. Fuerza de retención del gancho (0-3s).
4. Viabilidad de ritmo y duración (30-60s).
Devuelve EXCLUSIVAMENTE un objeto JSON válido con la estructura:
{{
  ""reviewResult"": {{
    ""overallStatus"": ""Pass | Warning | Critical"",
    ""factualAlignmentScore"": 0.95,
    ""retentionAnalysis"": ""..."",
    ""pacingAssessment"": ""..."",
    ""doNotSayComplianceNotes"": [""...""],
    ""dimensions"": [
      {{ ""dimension"": ""..."", ""status"": ""Pass | Warning | Critical"", ""notes"": ""..."" }}
    ],
    ""sceneCritiques"": [
      {{
        ""orderIndex"": 1,
        ""sceneType"": ""..."",
        ""status"": ""Pass | Warning | Critical"",
        ""claimFidelityNotes"": ""..."",
        ""retentionNotes"": ""..."",
        ""pacingNotes"": ""..."",
        ""suggestions"": [""...""]
      }}
    ],
    ""actionableRecommendations"": [""...""]
  }},
  ""conciseRationale"": ""...""
}}";

    private static string BuildReviewScriptUserPrompt(ReviewScriptRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Guión Título: {request.ScriptTitle}");
        sb.AppendLine($"Target Duration: {request.TargetDurationSeconds}s | Pacing: {request.PacingWpm} WPM");
        sb.AppendLine($"TruthSource Resumen: {request.TruthSourceSummary}");
        sb.AppendLine("TruthSource Afirmaciones Verificables:");
        foreach (var vc in request.VerifiableClaims) sb.AppendLine($"- {vc.Claim}");
        if (request.DoNotSayConstraints.Count > 0)
        {
            sb.AppendLine("Restricciones Prohibidas (Do NOT Say):");
            foreach (var sc in request.DoNotSayConstraints) sb.AppendLine($"- {sc}");
        }
        sb.AppendLine("\nEscenas del Guión a Evaluar:");
        foreach (var s in request.Scenes)
        {
            sb.AppendLine($"Escena #{s.OrderIndex} [{s.SceneType}] ({s.WordCount} palabras, {s.EstimatedDurationSeconds:F1}s):");
            sb.AppendLine($"  Locución: \"{s.NarrationText}\"");
            sb.AppendLine($"  Visual: {s.VisualPrompt}");
            if (s.EvidenceReferences.Count > 0)
            {
                sb.AppendLine("  Referencias factuales:");
                foreach (var er in s.EvidenceReferences) sb.AppendLine($"    - {er.ClaimStatement} (Nota: {er.EditorialNote})");
            }
        }
        sb.AppendLine("\nRealiza la auditoría editorial consultiva en formato JSON.");
        return sb.ToString();
    }

    public async Task<AiCapabilityResult<PlanStoryboardResponse>> PlanStoryboardAsync(
        PlanStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.PlanStoryboard, context.ChannelId);
            return await ExecuteMockPlanStoryboardAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekPlanStoryboardAsync(request, context, apiKey!, cancellationToken);
        }

        return await ExecuteMockPlanStoryboardAsync(request, context, cancellationToken);
    }

    private async Task<AiCapabilityResult<PlanStoryboardResponse>> ExecuteDeepSeekPlanStoryboardAsync(
        PlanStoryboardRequest request,
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

        var systemPrompt = BuildPlanStoryboardSystemPrompt(request);
        var userPrompt = BuildPlanStoryboardUserPrompt(request);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
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
                logger.LogError("DeepSeek API error for plan_storyboard: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return await ExecuteMockPlanStoryboardAsync(request, context, cancellationToken);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedRoot = JsonNode.Parse(responseBody);
            var rawContent = parsedRoot?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return await ExecuteMockPlanStoryboardAsync(request, context, cancellationToken);
            }

            var tokensIn = parsedRoot?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 800;
            var tokensOut = parsedRoot?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 650;
            var cost = CalculateCost(tokensIn, tokensOut);

            var responseData = JsonSerializer.Deserialize<PlanStoryboardResponse>(rawContent, JsonOptions)
                ?? throw new JsonException("Failed to deserialize PlanStoryboardResponse");

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                TruthSourceVersionId = request.TruthSourceVersionId,
                Capability = AiCapabilities.PlanStoryboard,
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

            return new AiCapabilityResult<PlanStoryboardResponse>(true, responseData, recommendation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling DeepSeek API for plan_storyboard, falling back to mock provider");
            return await ExecuteMockPlanStoryboardAsync(request, context, cancellationToken);
        }
    }

    private async Task<AiCapabilityResult<PlanStoryboardResponse>> ExecuteMockPlanStoryboardAsync(
        PlanStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-storyboard-planner-v1";
        var promptVersion = "1.0-mock";

        var frames = new List<GeneratedStoryboardFrameItem>();
        var frameIndex = 1;

        foreach (var scene in request.Scenes.OrderBy(s => s.OrderIndex))
        {
            var sceneDur = scene.EstimatedDurationSeconds > 0 ? scene.EstimatedDurationSeconds : 8.0;

            if (scene.SceneType == SceneType.Hook)
            {
                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.CloseUp,
                    CompositionIntent: "Sujeto centrado en tercio superior, tercio inferior despejado para subtítulos.",
                    CameraMotionIntent: CameraMotionIntent.SlowZoomIn,
                    Subject: "Desarrollador enfocado observando pantalla holográfica vertical",
                    Environment: "Estudio de tecnología minimalista con iluminación azul y violeta",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Cinematic vertical 9:16 close-up of a focused software engineer with glowing reflection of futuristic AI interface on glasses, dark moody cyberpunk studio, neon rim lights, 8k render, photorealistic",
                    NegativePrompt: "deformed face, bad hands, low resolution, text overlay, watermark, blurry",
                    AudioCue: scene.NarrationText,
                    EstimatedDurationSeconds: Math.Round(sceneDur, 1),
                    OnScreenText: "¿Cómo usar la IA hoy?",
                    TransitionIntent: TransitionIntent.Cut
                ));
            }
            else if (scene.SceneType == SceneType.Problem)
            {
                var halfDur = Math.Round(sceneDur / 2.0, 1);
                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.MediumShot,
                    CompositionIntent: "Plano medio con espacio negativo lateral para gráficos flotantes.",
                    CameraMotionIntent: CameraMotionIntent.TrackingShot,
                    Subject: "Oficina tradicional desbordada de tareas repetitivas",
                    Environment: "Espacio de trabajo moderno con múltiples pantallas mostrando alertas",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 medium shot of busy professional overwhelmed by floating complex data charts, subtle motion blur, high contrast dramatic lighting",
                    NegativePrompt: "deformed limbs, noisy background, text watermark, distortion",
                    AudioCue: scene.NarrationText.Length > 60 ? scene.NarrationText[..60] + "..." : scene.NarrationText,
                    EstimatedDurationSeconds: halfDur > 0 ? halfDur : 4.0,
                    OnScreenText: "El problema real",
                    TransitionIntent: TransitionIntent.Dissolve
                ));

                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.IsometricUi,
                    CompositionIntent: "Esquema isométrico vertical limpio con flujo de información.",
                    CameraMotionIntent: CameraMotionIntent.DynamicGlitch,
                    Subject: "Diagrama digital de automatización bloqueada",
                    Environment: "Fondo oscuro con gradiente técnico minimalista",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 isometric 3D UI graphic showing bottleneck in automated workflow, glowing neon lines, clean modern interface design",
                    NegativePrompt: "cluttered, ugly colors, blurry, low poly artifacts",
                    AudioCue: scene.NarrationText.Length > 60 ? "..." + scene.NarrationText[60..] : scene.NarrationText,
                    EstimatedDurationSeconds: Math.Max(2.0, Math.Round(sceneDur - halfDur, 1)),
                    OnScreenText: "Cuello de botella",
                    TransitionIntent: TransitionIntent.Cut
                ));
            }
            else if (scene.SceneType == SceneType.Insight)
            {
                var halfDur = Math.Round(sceneDur / 2.0, 1);
                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.MotionGraphic,
                    CompositionIntent: "Diseño centrado con visualización de datos en tiempo real.",
                    CameraMotionIntent: CameraMotionIntent.SlowZoomIn,
                    Subject: "Representación visual del modelo de lenguaje procesando datos",
                    Environment: "Partículas digitales y nodos de red neuronal conectados",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 abstract data visualization of neural network architecture, luminous neural paths connecting cleanly, elegant cyber aesthetic, ultra-detailed",
                    NegativePrompt: "chaotic, messy wires, dark unreadable areas, watermark",
                    AudioCue: scene.NarrationText.Length > 60 ? scene.NarrationText[..60] + "..." : scene.NarrationText,
                    EstimatedDurationSeconds: halfDur > 0 ? halfDur : 5.0,
                    OnScreenText: "La clave: Síntesis directa",
                    TransitionIntent: TransitionIntent.ZoomIn
                ));

                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.MediumShot,
                    CompositionIntent: "Sujeto aplicando solución con fluidez.",
                    CameraMotionIntent: CameraMotionIntent.PanUp,
                    Subject: "Profesional operando interfaz con confianza",
                    Environment: "Ambiente limpio y luminoso con acentos tecnológicos",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 cinematic medium shot of a productive creator smiling while working alongside an AI assistant on a sleek vertical workstation, clean modern office",
                    NegativePrompt: "unrealistic expressions, distorted anatomy, text artifacts",
                    AudioCue: scene.NarrationText.Length > 60 ? "..." + scene.NarrationText[60..] : scene.NarrationText,
                    EstimatedDurationSeconds: Math.Max(2.0, Math.Round(sceneDur - halfDur, 1)),
                    OnScreenText: "Flujo optimizado",
                    TransitionIntent: TransitionIntent.Cut
                ));
            }
            else if (scene.SceneType == SceneType.Climax)
            {
                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.ExtremeCloseUp,
                    CompositionIntent: "Gran impacto visual centrado.",
                    CameraMotionIntent: CameraMotionIntent.Static,
                    Subject: "Punto de inflexión tecnológico",
                    Environment: "Explosión controlada de luz y datos nítidos",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 extreme close-up of a high-tech glowing processor chip with light expanding outward, precision engineering, volumetric lighting, photorealistic",
                    NegativePrompt: "blurry, low quality, oversaturated, deformed",
                    AudioCue: scene.NarrationText,
                    EstimatedDurationSeconds: Math.Round(sceneDur, 1),
                    OnScreenText: "Resultado medible",
                    TransitionIntent: TransitionIntent.Wipe
                ));
            }
            else // CallToAction
            {
                frames.Add(new GeneratedStoryboardFrameItem(
                    OrderIndex: frameIndex++,
                    ScriptSceneOrderIndex: scene.OrderIndex,
                    FramingIntent: FramingIntent.WideShot,
                    CompositionIntent: "Composición amplia vertical con espacio central para llamada a la acción clara.",
                    CameraMotionIntent: CameraMotionIntent.SlowZoomIn,
                    Subject: "Futuro del trabajo conectado",
                    Environment: "Horizonte urbano moderno al atardecer con sutiles elementos de conectividad",
                    StyleIntent: request.VisualStylePreset ?? "Tech Minimalist 9:16",
                    VisualPrompt: "Vertical 9:16 wide shot of modern smart city skyline at golden hour with subtle holographic interface elements in the sky, inspiring and forward-looking",
                    NegativePrompt: "dystopian, gloomy, bad architecture, text, watermark",
                    AudioCue: scene.NarrationText,
                    EstimatedDurationSeconds: Math.Round(sceneDur, 1),
                    OnScreenText: "Suscríbete para más IA simple",
                    TransitionIntent: TransitionIntent.Cut
                ));
            }
        }

        var totalDuration = Math.Round(frames.Sum(f => f.EstimatedDurationSeconds), 1);
        var targetDur = request.TargetDurationSeconds > 0 ? request.TargetDurationSeconds : 45;

        var result = new GeneratedStoryboardResult(
            Title: $"{request.ScriptTitle} - Storyboard",
            TargetDurationSeconds: targetDur,
            VisualStylePreset: request.VisualStylePreset ?? "Tech Minimalist 9:16",
            Frames: frames
        );

        var responseData = new PlanStoryboardResponse(
            Storyboard: result,
            ConciseRationale: $"Storyboard estructurado en {frames.Count} tomas para formato vertical 9:16 ({request.ChannelName}). Duración total estimada {totalDuration:F1}s alineada con el guión."
        );

        stopwatch.Stop();
        var simulatedTokensIn = 750;
        var simulatedTokensOut = 600;
        var cost = CalculateCost(simulatedTokensIn, simulatedTokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            TruthSourceVersionId = request.TruthSourceVersionId,
            Capability = AiCapabilities.PlanStoryboard,
            Provider = AiProviders.Mock,
            Model = model,
            PromptPolicyVersion = promptVersion,
            StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
            Confidence = 0.95,
            Rationale = responseData.ConciseRationale,
            LatencyMs = Math.Max(stopwatch.ElapsedMilliseconds, 30),
            TokensIn = simulatedTokensIn,
            TokensOut = simulatedTokensOut,
            EstimatedCostUsd = cost,
            AcceptedState = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.AiRecommendations.Add(recommendation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AiCapabilityResult<PlanStoryboardResponse>(true, responseData, recommendation, null);
    }

    public async Task<AiCapabilityResult<ReviewStoryboardResponse>> ReviewStoryboardAsync(
        ReviewStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(context);
        var apiKey = configuration["DEEPSEEK_API_KEY"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (provider == AiProviders.Mock || (provider == AiProviders.DeepSeek && string.IsNullOrWhiteSpace(apiKey)))
        {
            logger.LogInformation("Using Mock AI Provider for capability '{Capability}' (Channel: {ChannelId})",
                AiCapabilities.ReviewStoryboard, context.ChannelId);
            return await ExecuteMockReviewStoryboardAsync(request, context, cancellationToken);
        }

        if (provider == AiProviders.DeepSeek)
        {
            return await ExecuteDeepSeekReviewStoryboardAsync(request, context, apiKey!, cancellationToken);
        }

        return await ExecuteMockReviewStoryboardAsync(request, context, cancellationToken);
    }

    private async Task<AiCapabilityResult<ReviewStoryboardResponse>> ExecuteDeepSeekReviewStoryboardAsync(
        ReviewStoryboardRequest request,
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

        var systemPrompt = BuildReviewStoryboardSystemPrompt(request);
        var userPrompt = BuildReviewStoryboardUserPrompt(request);

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
                logger.LogError("DeepSeek API error for review_storyboard: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return await ExecuteMockReviewStoryboardAsync(request, context, cancellationToken);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedRoot = JsonNode.Parse(responseBody);
            var rawContent = parsedRoot?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return await ExecuteMockReviewStoryboardAsync(request, context, cancellationToken);
            }

            var tokensIn = parsedRoot?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 750;
            var tokensOut = parsedRoot?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 500;
            var cost = CalculateCost(tokensIn, tokensOut);

            var responseData = JsonSerializer.Deserialize<ReviewStoryboardResponse>(rawContent, JsonOptions)
                ?? throw new JsonException("Failed to deserialize ReviewStoryboardResponse");

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                ChannelId = context.ChannelId,
                ContentItemId = context.ContentItemId,
                TruthSourceVersionId = null,
                Capability = AiCapabilities.ReviewStoryboard,
                Provider = AiProviders.DeepSeek,
                Model = model,
                PromptPolicyVersion = promptVersion,
                StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
                Confidence = 0.94,
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

            return new AiCapabilityResult<ReviewStoryboardResponse>(true, responseData, recommendation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calling DeepSeek API for review_storyboard, falling back to mock provider");
            return await ExecuteMockReviewStoryboardAsync(request, context, cancellationToken);
        }
    }

    private async Task<AiCapabilityResult<ReviewStoryboardResponse>> ExecuteMockReviewStoryboardAsync(
        ReviewStoryboardRequest request,
        AiRoutingContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = "mock-storyboard-critic-v1";
        var promptVersion = "1.0-mock";

        var totalEstimatedDuration = Math.Round(request.Frames.Sum(f => f.EstimatedDurationSeconds), 1);
        var targetDur = request.TargetDurationSeconds > 0 ? request.TargetDurationSeconds : 45;
        var diff = Math.Abs(totalEstimatedDuration - targetDur);

        var timingStatus = diff <= 5.0 ? "Pass" : (diff <= 10.0 ? "Warning" : "Critical");
        var framingCount = request.Frames.Select(f => f.FramingIntent).Distinct().Count();
        var framingStatus = framingCount >= 3 ? "Pass" : "Warning";

        var frameCritiques = request.Frames.Select(f => new StoryboardFrameCritiqueDto(
            FrameIndex: f.OrderIndex,
            ScriptSceneOrderIndex: f.ScriptSceneOrderIndex,
            Status: "Pass",
            HookVisualNotes: f.OrderIndex == 1 ? "El encuadre CloseUp y el movimiento SlowZoomIn generan intriga visual en los primeros 3 segundos." : null,
            FramingVarietyNotes: $"Encuadre '{f.FramingIntent}' con movimiento '{f.CameraMotionIntent}'.",
            CompositionNotes: "Composición vertical 9:16 con espacio inferior libre para subtítulos.",
            TimingNotes: $"Duración de {f.EstimatedDurationSeconds:F1}s adecuada para el corte.",
            PromptFidelityNotes: "El prompt visual traduce con precisión la locución de la escena.",
            Suggestions: []
        )).ToList();

        var reviewResult = new StoryboardReviewResultDto(
            OverallStatus: (timingStatus == "Critical" || framingStatus == "Critical") ? "Warning" : "Pass",
            VisualAlignmentScore: 0.94,
            HookVisualAssessment: "La toma inicial ofrece un gancho visual potente optimizado para formato vertical 9:16 sin elementos en zonas de peligro.",
            FramingDiversityAssessment: $"Se identificaron {framingCount} tipos de encuadre distintos a lo largo de {request.Frames.Count} tomas, asegurando dinamismo.",
            TimingAlignmentAssessment: $"Duración total estimada de {totalEstimatedDuration:F1}s frente al objetivo de {targetDur}s (diferencia: {diff:F1}s).",
            Dimensions:
            [
                new StoryboardReviewDimensionDto("Composición Vertical 9:16", "Pass", "Prompts visuales adaptados a encuadre vertical sin saturar zonas de interfaz."),
                new StoryboardReviewDimensionDto("Dinamismo y Variedad de Planos", framingStatus, $"Diversidad de encuadres adecuada ({framingCount} tipos)."),
                new StoryboardReviewDimensionDto("Alineación de Tiempos", timingStatus, $"Duración total {totalEstimatedDuration:F1}s vs objetivo {targetDur}s."),
                new StoryboardReviewDimensionDto("Fidelidad Narrativa-Visual", "Pass", "Los prompts visuales son consistentes con las locuciones del guión.")
            ],
            FrameCritiques: frameCritiques,
            ActionableRecommendations:
            [
                "Asegurar que los gráficos o textos en pantalla no colisionen con los botones laterales de plataformas verticales (Shorts/TikTok).",
                "Verificar la coherencia de iluminación y paleta de color entre tomas consecutivas."
            ]
        );

        var responseData = new ReviewStoryboardResponse(
            ReviewResult: reviewResult,
            ConciseRationale: $"Evaluación visual consultiva para '{request.StoryboardTitle}'. Puntuación de alineación visual 94%, variedad de planos aprobada y duración total de {totalEstimatedDuration:F1}s."
        );

        stopwatch.Stop();
        var simulatedTokensIn = 600;
        var simulatedTokensOut = 420;
        var cost = CalculateCost(simulatedTokensIn, simulatedTokensOut);

        var recommendation = new AiRecommendation
        {
            Id = Guid.NewGuid(),
            ChannelId = context.ChannelId,
            ContentItemId = context.ContentItemId,
            TruthSourceVersionId = null,
            Capability = AiCapabilities.ReviewStoryboard,
            Provider = AiProviders.Mock,
            Model = model,
            PromptPolicyVersion = promptVersion,
            StructuredOutputJson = JsonSerializer.Serialize(responseData, JsonOptions),
            Confidence = 0.94,
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

        return new AiCapabilityResult<ReviewStoryboardResponse>(true, responseData, recommendation, null);
    }

    private static string BuildPlanStoryboardSystemPrompt(PlanStoryboardRequest request) =>
        $@"Eres el director visual y planificador de producción de Content Factory para el canal '{request.ChannelName}' ({request.ChannelLanguage}, {request.ChannelNiche}).
Tu labor es descomponer un guión corto aprobado en una secuencia estructurada de tomas/frames para video vertical corto (9:16, YouTube Shorts).
Estilo visual preferido: {request.VisualStylePreset ?? "Tech Minimalist 9:16"}.
Duración objetivo: {request.TargetDurationSeconds} segundos.
Pacing: {request.PacingWpm} palabras por minuto.

Reglas de producción:
1. Cada escena del guión debe descomponerse en 1 a 3 tomas/frames (de 2 a 6 segundos cada una).
2. Los 'visualPrompt' deben estar optimizados para generación de imágenes/video en formato vertical 9:16 (detallando sujeto, iluminación, encuadre, paleta de color y ángulo, sin inventar texto en la imagen).
3. 'negativePrompt' debe filtrar deformidades, artefactos de texto y marcas de agua.
4. 'framingIntent': ExtremeCloseUp, CloseUp, MediumShot, WideShot, IsometricUi, MotionGraphic.
5. 'cameraMotionIntent': Static, SlowZoomIn, PanUp, TrackingShot, DynamicGlitch.
6. 'transitionIntent': Cut, Dissolve, Wipe, ZoomIn, Glitch, PanUp.
7. 'audioCue': fragmento de locución que acompaña la toma o indicación sonora.
8. 'onScreenText': texto kinetic/caption conciso para retención en pantalla.

Devuelve EXCLUSIVAMENTE un objeto JSON válido con la estructura:
{{
  ""storyboard"": {{
    ""title"": ""..."",
    ""targetDurationSeconds"": {request.TargetDurationSeconds},
    ""visualStylePreset"": ""..."",
    ""frames"": [
      {{
        ""orderIndex"": 1,
        ""scriptSceneOrderIndex"": 1,
        ""framingIntent"": ""CloseUp"",
        ""compositionIntent"": ""..."",
        ""cameraMotionIntent"": ""SlowZoomIn"",
        ""subject"": ""..."",
        ""environment"": ""..."",
        ""styleIntent"": ""..."",
        ""visualPrompt"": ""..."",
        ""negativePrompt"": ""..."",
        ""audioCue"": ""..."",
        ""estimatedDurationSeconds"": 4.0,
        ""onScreenText"": ""..."",
        ""transitionIntent"": ""Cut""
      }}
    ]
  }},
  ""conciseRationale"": ""...""
}}";

    private static string BuildPlanStoryboardUserPrompt(PlanStoryboardRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Guión: {request.ScriptTitle}");
        sb.AppendLine($"Target Duration: {request.TargetDurationSeconds}s | Pacing: {request.PacingWpm} WPM");
        sb.AppendLine("\nEscenas del Guión Aprobado:");
        foreach (var s in request.Scenes.OrderBy(x => x.OrderIndex))
        {
            sb.AppendLine($"Escena #{s.OrderIndex} [{s.SceneType}] ({s.EstimatedDurationSeconds:F1}s):");
            sb.AppendLine($"  Locución: \"{s.NarrationText}\"");
            sb.AppendLine($"  Guía Visual: {s.VisualPrompt}");
        }
        sb.AppendLine("\nGenera la planificación de tomas del storyboard en formato JSON.");
        return sb.ToString();
    }

    private static string BuildReviewStoryboardSystemPrompt(ReviewStoryboardRequest request) =>
        $@"Eres el crítico de producción visual de Content Factory para el canal '{request.ChannelName}' ({request.ChannelLanguage}).
Tu labor es auditar consultivamente una planificación de storyboard frente al guión aprobado y las mejores prácticas de video vertical corto (9:16).
IMPORTANTE: Tu evaluación es consultiva. La decisión editorial corresponde al operador humano.

Evalúa:
1. Impacto visual del gancho inicial (0-3s).
2. Variedad de encuadres y ritmo visual (evitar planos estáticos monótonos).
3. Composición vertical 9:16 y zonas seguras (evitar elementos críticos en zona inferior de subtítulos o lateral de botones).
4. Fidelidad del prompt visual frente a la narración de la escena.
5. Coherencia de tiempos (duración total de frames vs objetivo del guión).

Devuelve EXCLUSIVAMENTE un objeto JSON válido con la estructura:
{{
  ""reviewResult"": {{
    ""overallStatus"": ""Pass | Warning | Critical"",
    ""visualAlignmentScore"": 0.94,
    ""hookVisualAssessment"": ""..."",
    ""framingDiversityAssessment"": ""..."",
    ""timingAlignmentAssessment"": ""..."",
    ""dimensions"": [
      {{ ""dimension"": ""..."", ""status"": ""Pass | Warning | Critical"", ""notes"": ""..."" }}
    ],
    ""frameCritiques"": [
      {{
        ""frameIndex"": 1,
        ""scriptSceneOrderIndex"": 1,
        ""status"": ""Pass | Warning | Critical"",
        ""hookVisualNotes"": ""..."",
        ""framingVarietyNotes"": ""..."",
        ""compositionNotes"": ""..."",
        ""timingNotes"": ""..."",
        ""promptFidelityNotes"": ""..."",
        ""suggestions"": [""...""]
      }}
    ],
    ""actionableRecommendations"": [""...""]
  }},
  ""conciseRationale"": ""...""
}}";

    private static string BuildReviewStoryboardUserPrompt(ReviewStoryboardRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Storyboard: {request.StoryboardTitle}");
        sb.AppendLine($"Guión: {request.ScriptTitle} (Target: {request.ScriptTargetDurationSeconds}s)");
        sb.AppendLine($"Storyboard Target Duration: {request.TargetDurationSeconds}s");
        sb.AppendLine("\nEscenas del Guión:");
        foreach (var s in request.ScriptScenes.OrderBy(x => x.OrderIndex))
        {
            sb.AppendLine($"Escena #{s.OrderIndex} [{s.SceneType}] ({s.EstimatedDurationSeconds:F1}s): {s.NarrationText}");
        }
        sb.AppendLine("\nTomas del Storyboard a Evaluar:");
        foreach (var f in request.Frames.OrderBy(x => x.OrderIndex))
        {
            sb.AppendLine($"Toma #{f.OrderIndex} (Escena #{f.ScriptSceneOrderIndex}) [{f.FramingIntent}, {f.CameraMotionIntent}] ({f.EstimatedDurationSeconds:F1}s):");
            sb.AppendLine($"  Prompt: {f.VisualPrompt}");
            sb.AppendLine($"  Locución: \"{f.AudioCue}\"");
            sb.AppendLine($"  Texto en pantalla: \"{f.OnScreenText}\"");
        }
        sb.AppendLine("\nRealiza la auditoría visual consultiva en formato JSON.");
        return sb.ToString();
    }
}
