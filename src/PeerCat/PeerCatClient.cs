using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PeerCat;

/// <summary>
/// Options for configuring the PeerCat client
/// </summary>
public record PeerCatOptions
{
    /// <summary>API key for authentication</summary>
    public required string ApiKey { get; init; }

    /// <summary>Base URL for the API (default: https://api.peerc.at/v1)</summary>
    public string BaseUrl { get; init; } = "https://api.peerc.at/v1";

    /// <summary>Request timeout (default: 30 seconds)</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum number of retries for failed requests (default: 3)</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Custom HttpClient (for testing or advanced scenarios)</summary>
    public HttpClient? HttpClient { get; init; }
}

/// <summary>
/// Client for the PeerCat AI image generation API
/// </summary>
public class PeerCatClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _maxRetries;
    private readonly bool _ownsHttpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Creates a new PeerCat client with the specified API key
    /// </summary>
    /// <param name="apiKey">Your PeerCat API key</param>
    public PeerCatClient(string apiKey) : this(new PeerCatOptions { ApiKey = apiKey })
    {
    }

    /// <summary>
    /// Creates a new PeerCat client with the specified options
    /// </summary>
    /// <param name="options">Client configuration options</param>
    public PeerCatClient(PeerCatOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("API key is required", nameof(options));

        _baseUrl = options.BaseUrl.TrimEnd('/');
        _maxRetries = options.MaxRetries;

        if (options.HttpClient != null)
        {
            _httpClient = options.HttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient { Timeout = options.Timeout };
            _ownsHttpClient = true;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("peercat-csharp/0.1.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    #region Image Generation

    /// <summary>
    /// Generate an image from a text prompt
    /// </summary>
    /// <param name="prompt">Text description of the image to generate</param>
    /// <param name="model">Model to use (optional)</param>
    /// <param name="mode">Generation mode (optional)</param>
    /// <param name="options">Additional model-specific options (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation result with image URL</returns>
    public Task<GenerateResult> GenerateAsync(
        string prompt,
        string? model = null,
        GenerationMode? mode = null,
        Dictionary<string, object>? options = null,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(new GenerateParams
        {
            Prompt = prompt,
            Model = model,
            Mode = mode,
            Options = options
        }, cancellationToken);
    }

    /// <summary>
    /// Generate an image from a text prompt
    /// </summary>
    /// <param name="request">Generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation result with image URL</returns>
    public Task<GenerateResult> GenerateAsync(
        GenerateParams request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt is required", nameof(request));

        return PostAsync<GenerateResult>("/generate", request, cancellationToken);
    }

    #endregion

    #region Models & Pricing

    /// <summary>
    /// Get available models
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available models</returns>
    public async Task<IReadOnlyList<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<ModelsResponse>("/models", cancellationToken);
        return response.Models;
    }

    /// <summary>
    /// Get current pricing for all models
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price information</returns>
    public Task<PriceResponse> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<PriceResponse>("/price", cancellationToken);
    }

    #endregion

    #region Account

    /// <summary>
    /// Get account balance
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance information</returns>
    public Task<Balance> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<Balance>("/balance", cancellationToken);
    }

    /// <summary>
    /// Get usage history
    /// </summary>
    /// <param name="limit">Number of items to return (max 100)</param>
    /// <param name="offset">Pagination offset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage history</returns>
    public Task<HistoryResponse> GetHistoryAsync(
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        if (offset.HasValue) query.Add($"offset={offset.Value}");

        var path = "/history";
        if (query.Count > 0) path += "?" + string.Join("&", query);

        return GetAsync<HistoryResponse>(path, cancellationToken);
    }

    #endregion

    #region API Keys

    /// <summary>
    /// Create a new API key
    /// </summary>
    /// <param name="request">Key creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created key (full key only shown once!)</returns>
    public Task<CreateKeyResult> CreateKeyAsync(
        CreateKeyParams request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateKeyResult>("/keys", request, cancellationToken);
    }

    /// <summary>
    /// List all API keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API keys</returns>
    public async Task<IReadOnlyList<ApiKey>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<KeysResponse>("/keys", cancellationToken);
        return response.Keys;
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    /// <param name="keyId">Key ID to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID is required", nameof(keyId));

        await DeleteAsync($"/keys/{Uri.EscapeDataString(keyId)}", cancellationToken);
    }

    /// <summary>
    /// Update an API key's name
    /// </summary>
    /// <param name="keyId">Key ID to update</param>
    /// <param name="name">New name for the key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateKeyNameAsync(string keyId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID is required", nameof(keyId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        await PatchAsync($"/keys/{Uri.EscapeDataString(keyId)}", new { name }, cancellationToken);
    }

    #endregion

    #region On-Chain Payments

    /// <summary>
    /// Submit a prompt for on-chain payment
    /// </summary>
    /// <param name="prompt">Text prompt</param>
    /// <param name="model">Model to use (optional)</param>
    /// <param name="options">Additional options (optional)</param>
    /// <param name="callbackUrl">Callback URL for result notification (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment submission details</returns>
    public Task<PromptSubmission> SubmitPromptAsync(
        string prompt,
        string? model = null,
        Dictionary<string, object>? options = null,
        string? callbackUrl = null,
        CancellationToken cancellationToken = default)
    {
        return SubmitPromptAsync(new SubmitPromptParams
        {
            Prompt = prompt,
            Model = model,
            Options = options,
            CallbackUrl = callbackUrl
        }, cancellationToken);
    }

    /// <summary>
    /// Submit a prompt for on-chain payment
    /// </summary>
    /// <param name="request">Submission parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment submission details</returns>
    public Task<PromptSubmission> SubmitPromptAsync(
        SubmitPromptParams request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt is required", nameof(request));

        return PostAsync<PromptSubmission>("/prompts", request, cancellationToken);
    }

    /// <summary>
    /// Get the status of an on-chain generation
    /// </summary>
    /// <param name="txSignature">Transaction signature</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation status</returns>
    public Task<OnChainGenerationStatus> GetGenerationStatusAsync(
        string txSignature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(txSignature))
            throw new ArgumentException("Transaction signature is required", nameof(txSignature));

        return GetAsync<OnChainGenerationStatus>(
            $"/generate/{Uri.EscapeDataString(txSignature)}",
            cancellationToken);
    }

    #endregion

    #region HTTP Methods

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync<T>(async () =>
        {
            var response = await _httpClient.GetAsync(_baseUrl + path, cancellationToken);
            return await HandleResponseAsync<T>(response, cancellationToken);
        }, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync<T>(async () =>
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_baseUrl + path, content, cancellationToken);
            return await HandleResponseAsync<T>(response, cancellationToken);
        }, cancellationToken);
    }

    private async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync<SuccessResponse>(async () =>
        {
            var response = await _httpClient.DeleteAsync(_baseUrl + path, cancellationToken);
            return await HandleResponseAsync<SuccessResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    private async Task PatchAsync(string path, object body, CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync<SuccessResponse>(async () =>
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, _baseUrl + path) { Content = content };
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<SuccessResponse>(response, cancellationToken);
        }, cancellationToken);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse rate limit headers (useful for both success and error cases)
        var rateLimitInfo = RateLimitInfo.FromHeaders(response.Headers);

        if (!response.IsSuccessStatusCode)
        {
            ApiErrorResponse? errorResponse = null;
            try
            {
                errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            }
            catch
            {
                // Ignore JSON parsing errors
            }

            if (errorResponse?.Error != null)
            {
                throw PeerCatException.FromApiError((int)response.StatusCode, errorResponse.Error, rateLimitInfo);
            }

            throw new PeerCatException(
                $"HTTP {(int)response.StatusCode}: {content}",
                "http_error",
                "unknown",
                null,
                (int)response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
        return result ?? throw new InvalidOperationException("Unexpected null response");
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (PeerCatException ex) when (ex.IsRetryable && attempt < _maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);

                if (ex is RateLimitException rle && rle.RetryAfter.HasValue)
                {
                    delay = TimeSpan.FromSeconds(rle.RetryAfter.Value);
                }

                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the client and releases resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client and releases resources
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
