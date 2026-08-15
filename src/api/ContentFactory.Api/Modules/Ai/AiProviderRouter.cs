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

    private static decimal CalculateCost(int tokensIn, int tokensOut)
    {
        // DeepSeek approx pricing: $0.14 / 1M in, $0.28 / 1M out
        var costIn = (decimal)tokensIn * 0.00000014m;
        var costOut = (decimal)tokensOut * 0.00000028m;
        return Math.Round(costIn + costOut, 6);
    }
}
