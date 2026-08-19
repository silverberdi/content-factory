using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Modules.Ai;

public class MockVisualGenerationProvider(ILogger<MockVisualGenerationProvider> logger) : IVisualGenerationProvider
{
    public string ProviderName => "Mock";

    public IReadOnlyList<string> SupportedAssetTypes =>
    [
        AssetType.AiImage,
        AssetType.AiVideo,
        AssetType.GraphicOverlay
    ];

    public async Task<VisualGenerationResult> GenerateVisualAssetAsync(
        VisualGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Mock visual generation started for requirement {RequirementId}, candidate count {Count}",
            request.AssetRequirementId, request.CandidateCount);

        // Check provider asset type capability
        if (!SupportedAssetTypes.Contains(request.AssetType))
        {
            sw.Stop();
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "UNSUPPORTED_ASSET_TYPE",
                ErrorMessage: $"Mock provider does not support asset type '{request.AssetType}'.",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0m,
                ActualCostUsd: null
            );
        }

        // Simulate async processing
        await Task.Delay(200, cancellationToken);

        // Check failure triggers in prompt
        var promptLower = (request.VisualPrompt ?? "").ToLowerInvariant();

        if (promptLower.Contains("[mock:retryable-failure]") || promptLower.Contains("[mock:timeout]"))
        {
            sw.Stop();
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "PROVIDER_TRANSIENT_503",
                ErrorMessage: "Simulated transient HTTP 503 from mock visual generation provider.",
                IsRetryable: true,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0.001m,
                ActualCostUsd: null
            );
        }

        if (promptLower.Contains("[mock:action-required-failure]") || promptLower.Contains("[mock:invalid-workflow]"))
        {
            sw.Stop();
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "INVALID_WORKFLOW_CONFIGURATION",
                ErrorMessage: "Simulated invalid workflow template or missing credentials. Action required.",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0.001m,
                ActualCostUsd: null
            );
        }

        var outputs = new List<VisualGeneratedMediaOutput>();
        var candidateCount = Math.Clamp(request.CandidateCount, 1, 4);

        for (int i = 1; i <= candidateCount; i++)
        {
            // Deterministic seed derived strictly from requirement ID, variant index, and prompt
            var seed = Math.Abs(HashCode.Combine(request.AssetRequirementId, i, request.VisualPrompt)) % 900000 + 100000;
            var svgContent = GenerateSvgMedia(request, i, seed);
            var mediaBytes = Encoding.UTF8.GetBytes(svgContent);

            var paramSnapshot = JsonSerializer.Serialize(new
            {
                prompt = request.VisualPrompt,
                negativePrompt = request.NegativePrompt,
                aspectRatio = request.AspectRatio,
                style = request.StyleIntent,
                motion = request.MotionIntent,
                seed = seed,
                variant = i,
                model = "mock-flux-schnell-9x16"
            });

            outputs.Add(new VisualGeneratedMediaOutput(
                VariantIndex: i,
                MediaBytes: mediaBytes,
                ContentType: "image/svg+xml",
                FileExtension: "svg",
                Width: request.TargetWidth > 0 ? request.TargetWidth : 1080,
                Height: request.TargetHeight > 0 ? request.TargetHeight : 1920,
                DurationSeconds: request.TargetDurationSeconds,
                ProviderModelOrWorkflow: "mock-flux-schnell-9x16",
                GenerationParametersSnapshot: paramSnapshot
            ));
        }

        sw.Stop();
        var estimatedCost = 0.002m * candidateCount;

        return new VisualGenerationResult(
            Success: true,
            Outputs: outputs,
            ErrorCode: null,
            ErrorMessage: null,
            IsRetryable: false,
            ExecutionDurationMs: sw.ElapsedMilliseconds,
            EstimatedCostUsd: estimatedCost,
            ActualCostUsd: estimatedCost
        );
    }

    private static string GenerateSvgMedia(VisualGenerationRequest request, int variantIndex, int seed)
    {
        var sanitizedPrompt = System.Security.SecurityElement.Escape(
            request.VisualPrompt.Length > 160 ? request.VisualPrompt[..160] + "..." : request.VisualPrompt);
        var sanitizedStyle = System.Security.SecurityElement.Escape(request.StyleIntent ?? "Modern Editorial 9:16");
        var sanitizedAssetType = System.Security.SecurityElement.Escape(request.AssetType ?? "AiImage");

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 1080 1920"" width=""1080"" height=""1920"">
  <defs>
    <linearGradient id=""bgGrad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#090d16""/>
      <stop offset=""50%"" stop-color=""#131d33""/>
      <stop offset=""100%"" stop-color=""#050810""/>
    </linearGradient>
    <linearGradient id=""accentGrad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""#38bdf8""/>
      <stop offset=""100%"" stop-color=""#818cf8""/>
    </linearGradient>
    <filter id=""glow"" x=""-20%"" y=""-20%"" width=""140%"" height=""140%"">
      <feGaussianBlur stdDeviation=""40"" result=""blur"" />
      <feComposite in=""SourceGraphic"" in2=""blur"" operator=""over"" />
    </filter>
  </defs>

  <rect width=""1080"" height=""1920"" fill=""url(#bgGrad)""/>

  <!-- Subtle Cyber Grid -->
  <g opacity=""0.08"" stroke=""#38bdf8"" stroke-width=""2"">
    <line x1=""100"" y1=""0"" x2=""100"" y2=""1920""/>
    <line x1=""300"" y1=""0"" x2=""300"" y2=""1920""/>
    <line x1=""540"" y1=""0"" x2=""540"" y2=""1920""/>
    <line x1=""780"" y1=""0"" x2=""780"" y2=""1920""/>
    <line x1=""980"" y1=""0"" x2=""980"" y2=""1920""/>
    <line x1=""0"" y1=""300"" x2=""1080"" y2=""300""/>
    <line x1=""0"" y1=""600"" x2=""1080"" y2=""600""/>
    <line x1=""0"" y1=""960"" x2=""1080"" y2=""960""/>
    <line x1=""0"" y1=""1300"" x2=""1080"" y2=""1300""/>
    <line x1=""0"" y1=""1600"" x2=""1080"" y2=""1600""/>
  </g>

  <!-- Glowing Central Card -->
  <rect x=""90"" y=""280"" width=""900"" height=""1360"" rx=""48"" fill=""#111827"" fill-opacity=""0.75"" stroke=""#38bdf8"" stroke-width=""3"" stroke-opacity=""0.4"" filter=""url(#glow)""/>

  <!-- Top Badges -->
  <rect x=""140"" y=""340"" width=""280"" height=""60"" rx=""30"" fill=""#1e293b"" stroke=""#38bdf8"" stroke-width=""2""/>
  <text x=""280"" y=""378"" fill=""#38bdf8"" font-family=""sans-serif"" font-size=""24"" font-weight=""bold"" text-anchor=""middle"">CONTENT FACTORY</text>

  <rect x=""660"" y=""340"" width=""280"" height=""60"" rx=""30"" fill=""#1e293b"" stroke=""#818cf8"" stroke-width=""2""/>
  <text x=""800"" y=""378"" fill=""#818cf8"" font-family=""sans-serif"" font-size=""24"" font-weight=""bold"" text-anchor=""middle"">CANDIDATE #{variantIndex}</text>

  <!-- Center Visual Orb -->
  <circle cx=""540"" cy=""750"" r=""220"" fill=""url(#accentGrad)"" opacity=""0.15"" filter=""url(#glow)""/>
  <circle cx=""540"" cy=""750"" r=""160"" fill=""none"" stroke=""url(#accentGrad)"" stroke-width=""6"" stroke-dasharray=""16 8""/>
  <text x=""540"" y=""765"" fill=""#f8fafc"" font-family=""sans-serif"" font-size=""44"" font-weight=""800"" text-anchor=""middle"">{sanitizedAssetType.ToUpperInvariant()}</text>
  <text x=""540"" y=""820"" fill=""#94a3b8"" font-family=""sans-serif"" font-size=""26"" text-anchor=""middle"">9:16 VERTICAL (1080x1920)</text>

  <!-- Prompt Box -->
  <rect x=""140"" y=""1040"" width=""800"" height=""320"" rx=""24"" fill=""#0f172a"" fill-opacity=""0.9"" stroke=""#334155"" stroke-width=""2""/>
  <text x=""180"" y=""1090"" fill=""#38bdf8"" font-family=""sans-serif"" font-size=""22"" font-weight=""bold"">VISUAL PROMPT</text>
  <text x=""180"" y=""1140"" fill=""#e2e8f0"" font-family=""sans-serif"" font-size=""24"" width=""720"">
    <tspan x=""180"" dy=""0"">{sanitizedPrompt}</tspan>
  </text>
  
  <text x=""180"" y=""1280"" fill=""#94a3b8"" font-family=""sans-serif"" font-size=""20"">STYLE: {sanitizedStyle}</text>
  <text x=""180"" y=""1320"" fill=""#64748b"" font-family=""sans-serif"" font-size=""18"">SEED: {seed} | VARIANT {variantIndex}</text>

  <!-- Footer Info -->
  <text x=""540"" y=""1700"" fill=""#64748b"" font-family=""sans-serif"" font-size=""22"" text-anchor=""middle"">MOCK PRODUCTION PREVIEW | IMMUTABLE LINEAGE PRESERVED</text>
</svg>";
    }
}
