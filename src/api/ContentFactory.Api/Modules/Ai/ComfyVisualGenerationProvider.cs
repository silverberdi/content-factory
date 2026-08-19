using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ContentFactory.Api.Modules.Content;

namespace ContentFactory.Api.Modules.Ai;

public class ComfyVisualGenerationProvider(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ComfyVisualGenerationProvider> logger) : IVisualGenerationProvider
{
    public string ProviderName => "Comfy";

    public IReadOnlyList<string> SupportedAssetTypes =>
    [
        AssetType.AiImage
    ];

    private readonly string _endpoint = configuration["COMFY_ENDPOINT"] ?? configuration["Comfy:Endpoint"] ?? "http://127.0.0.1:8188";
    private readonly string _defaultWorkflow = configuration["COMFY_WORKFLOW_TEMPLATE"] ?? configuration["Comfy:WorkflowTemplate"] ?? "flux_schnell_vertical_9x16";

    public async Task<VisualGenerationResult> GenerateVisualAssetAsync(
        VisualGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Comfy visual generation starting for Requirement {ReqId} via endpoint {Endpoint}",
            request.AssetRequirementId, _endpoint);

        if (!SupportedAssetTypes.Contains(request.AssetType))
        {
            sw.Stop();
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "UNSUPPORTED_ASSET_TYPE",
                ErrorMessage: $"Comfy visual provider does not support asset type '{request.AssetType}'.",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0m,
                ActualCostUsd: null
            );
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var candidateCount = Math.Clamp(request.CandidateCount, 1, 4);
            var outputs = new List<VisualGeneratedMediaOutput>();

            for (int i = 1; i <= candidateCount; i++)
            {
                var seed = RandomNumberGenerator.GetInt32(100000, 99999999);
                var promptPayload = BuildComfyPromptPayload(request, seed);

                var promptResponse = await client.PostAsJsonAsync($"{_endpoint.TrimEnd('/')}/prompt", new { prompt = promptPayload }, cancellationToken);

                if (!promptResponse.IsSuccessStatusCode)
                {
                    var errorBody = await promptResponse.Content.ReadAsStringAsync(cancellationToken);
                    sw.Stop();
                    var is5xx = (int)promptResponse.StatusCode >= 500;
                    return new VisualGenerationResult(
                        Success: false,
                        Outputs: [],
                        ErrorCode: is5xx ? "COMFY_SERVER_ERROR" : "COMFY_BAD_REQUEST",
                        ErrorMessage: $"Comfy returned HTTP {(int)promptResponse.StatusCode}: {errorBody}",
                        IsRetryable: is5xx,
                        ExecutionDurationMs: sw.ElapsedMilliseconds,
                        EstimatedCostUsd: 0.005m,
                        ActualCostUsd: null
                    );
                }

                var promptResult = await promptResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                var promptId = promptResult.TryGetProperty("prompt_id", out var pidElem) ? pidElem.GetString() : null;

                if (string.IsNullOrWhiteSpace(promptId))
                {
                    sw.Stop();
                    return new VisualGenerationResult(
                        Success: false,
                        Outputs: [],
                        ErrorCode: "COMFY_MISSING_PROMPT_ID",
                        ErrorMessage: "Comfy did not return a valid prompt_id in response.",
                        IsRetryable: true,
                        ExecutionDurationMs: sw.ElapsedMilliseconds,
                        EstimatedCostUsd: null,
                        ActualCostUsd: null
                    );
                }

                // Poll history for outputs
                var mediaBytes = await PollAndDownloadComfyOutputAsync(client, promptId, cancellationToken);

                var paramSnapshot = JsonSerializer.Serialize(new
                {
                    prompt = request.VisualPrompt,
                    negativePrompt = request.NegativePrompt,
                    aspectRatio = request.AspectRatio,
                    style = request.StyleIntent,
                    seed = seed,
                    variant = i,
                    workflow = _defaultWorkflow,
                    promptId = promptId
                });

                outputs.Add(new VisualGeneratedMediaOutput(
                    VariantIndex: i,
                    MediaBytes: mediaBytes,
                    ContentType: "image/png",
                    FileExtension: "png",
                    Width: request.TargetWidth > 0 ? request.TargetWidth : 1080,
                    Height: request.TargetHeight > 0 ? request.TargetHeight : 1920,
                    DurationSeconds: request.TargetDurationSeconds,
                    ProviderModelOrWorkflow: _defaultWorkflow,
                    GenerationParametersSnapshot: paramSnapshot
                ));
            }

            sw.Stop();
            var cost = 0.005m * candidateCount;

            return new VisualGenerationResult(
                Success: true,
                Outputs: outputs,
                ErrorCode: null,
                ErrorMessage: null,
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: cost,
                ActualCostUsd: cost
            );
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "HTTP connection error connecting to Comfy at {Endpoint}", _endpoint);
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "COMFY_CONNECTION_FAILED",
                ErrorMessage: $"Cannot connect to Comfy execution server at {_endpoint}: {ex.Message}",
                IsRetryable: true,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: null,
                ActualCostUsd: null
            );
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Comfy request timed out after {Elapsed}ms", sw.ElapsedMilliseconds);
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "COMFY_TIMEOUT",
                ErrorMessage: "Comfy generation timed out awaiting worker completion.",
                IsRetryable: true,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: null,
                ActualCostUsd: null
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Unexpected error during Comfy generation");
            return new VisualGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "COMFY_UNEXPECTED_ERROR",
                ErrorMessage: $"Unexpected error in Comfy provider: {ex.Message}",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: null,
                ActualCostUsd: null
            );
        }
    }

    private static Dictionary<string, object> BuildComfyPromptPayload(VisualGenerationRequest request, int seed)
    {
        var width = request.TargetWidth > 0 ? request.TargetWidth : 1080;
        var height = request.TargetHeight > 0 ? request.TargetHeight : 1920;

        return new Dictionary<string, object>
        {
            ["3"] = new
            {
                inputs = new
                {
                    seed = seed,
                    steps = 20,
                    cfg = 7.0,
                    sampler_name = "euler",
                    scheduler = "normal",
                    denoise = 1.0,
                    model = new object[] { "4", 0 },
                    positive = new object[] { "6", 0 },
                    negative = new object[] { "7", 0 },
                    latent_image = new object[] { "5", 0 }
                },
                class_type = "KSampler"
            },
            ["4"] = new
            {
                inputs = new { unet_name = "flux1-schnell.sft", weight_dtype = "default" },
                class_type = "UNETLoader"
            },
            ["5"] = new
            {
                inputs = new { width = width, height = height, batch_size = 1 },
                class_type = "EmptyLatentImage"
            },
            ["6"] = new
            {
                inputs = new { text = $"{request.VisualPrompt}, {request.StyleIntent}, 9:16 vertical framing, high resolution", clip = new object[] { "4", 1 } },
                class_type = "CLIPTextEncode"
            },
            ["7"] = new
            {
                inputs = new { text = request.NegativePrompt ?? "blurry, low quality, deformed, extra limbs, bad anatomy, text, watermark", clip = new object[] { "4", 1 } },
                class_type = "CLIPTextEncode"
            },
            ["8"] = new
            {
                inputs = new { samples = new object[] { "3", 0 }, vae = new object[] { "4", 2 } },
                class_type = "VAEDecode"
            },
            ["9"] = new
            {
                inputs = new { filename_prefix = "ContentFactory_Visual", images = new object[] { "8", 0 } },
                class_type = "SaveImage"
            }
        };
    }

    private async Task<byte[]> PollAndDownloadComfyOutputAsync(HttpClient client, string promptId, CancellationToken cancellationToken)
    {
        // Poll /history/{promptId}
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(1000, cancellationToken);

            var historyResponse = await client.GetAsync($"{_endpoint.TrimEnd('/')}/history/{promptId}", cancellationToken);
            if (historyResponse.IsSuccessStatusCode)
            {
                var historyJson = await historyResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                if (historyJson.TryGetProperty(promptId, out var jobHistory) &&
                    jobHistory.TryGetProperty("outputs", out var outputsElem))
                {
                    foreach (var outputProperty in outputsElem.EnumerateObject())
                    {
                        if (outputProperty.Value.TryGetProperty("images", out var imagesArr) && imagesArr.GetArrayLength() > 0)
                        {
                            var firstImage = imagesArr[0];
                            var filename = firstImage.GetProperty("filename").GetString();
                            var subfolder = firstImage.TryGetProperty("subfolder", out var sf) ? sf.GetString() : "";
                            var type = firstImage.TryGetProperty("type", out var tp) ? tp.GetString() : "output";

                            var viewUrl = $"{_endpoint.TrimEnd('/')}/view?filename={Uri.EscapeDataString(filename ?? "")}&subfolder={Uri.EscapeDataString(subfolder ?? "")}&type={Uri.EscapeDataString(type ?? "output")}";
                            var imageResponse = await client.GetAsync(viewUrl, cancellationToken);
                            if (imageResponse.IsSuccessStatusCode)
                            {
                                return await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                            }
                        }
                    }
                }
            }
        }

        throw new TimeoutException($"Timed out waiting for Comfy outputs for prompt {promptId}.");
    }
}
