using System.Text.Json.Serialization;

namespace PeerCat;

/// <summary>
/// Generation mode
/// </summary>
public enum GenerationMode
{
    /// <summary>Production mode - uses credits</summary>
    [JsonPropertyName("production")]
    Production,
    /// <summary>Demo mode - free, returns placeholder images</summary>
    [JsonPropertyName("demo")]
    Demo
}

/// <summary>
/// Model information
/// </summary>
public record Model
{
    /// <summary>Model identifier</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Human-readable name</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Model description</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Model provider</summary>
    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    /// <summary>Maximum prompt length in characters</summary>
    [JsonPropertyName("maxPromptLength")]
    public int MaxPromptLength { get; init; }

    /// <summary>Output image format</summary>
    [JsonPropertyName("outputFormat")]
    public required string OutputFormat { get; init; }

    /// <summary>Output resolution</summary>
    [JsonPropertyName("outputResolution")]
    public required string OutputResolution { get; init; }

    /// <summary>Price in USD</summary>
    [JsonPropertyName("priceUsd")]
    public decimal PriceUsd { get; init; }
}

/// <summary>
/// Response containing available models
/// </summary>
public record ModelsResponse
{
    [JsonPropertyName("models")]
    public required IReadOnlyList<Model> Models { get; init; }
}

/// <summary>
/// Price information for a specific model
/// </summary>
public record ModelPrice
{
    /// <summary>Model identifier</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>Price in USD</summary>
    [JsonPropertyName("priceUsd")]
    public decimal PriceUsd { get; init; }

    /// <summary>Price in SOL</summary>
    [JsonPropertyName("priceSol")]
    public decimal PriceSol { get; init; }

    /// <summary>Price in SOL including slippage tolerance</summary>
    [JsonPropertyName("priceSolWithSlippage")]
    public decimal PriceSolWithSlippage { get; init; }
}

/// <summary>
/// Response containing pricing information
/// </summary>
public record PriceResponse
{
    /// <summary>Current SOL/USD price</summary>
    [JsonPropertyName("solPrice")]
    public decimal SolPrice { get; init; }

    /// <summary>Slippage tolerance (e.g., 0.02 = 2%)</summary>
    [JsonPropertyName("slippageTolerance")]
    public decimal SlippageTolerance { get; init; }

    /// <summary>Timestamp of price update</summary>
    [JsonPropertyName("updatedAt")]
    public required string UpdatedAt { get; init; }

    /// <summary>Treasury PDA address to send payments to</summary>
    [JsonPropertyName("treasury")]
    public required string Treasury { get; init; }

    /// <summary>Prices for each model</summary>
    [JsonPropertyName("models")]
    public required IReadOnlyList<ModelPrice> Models { get; init; }
}

/// <summary>
/// Parameters for image generation
/// </summary>
public record GenerateParams
{
    /// <summary>Text prompt for image generation (max 2000 characters)</summary>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    /// <summary>Model to use (default: stable-diffusion-xl)</summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; init; }

    /// <summary>Mode: production (default) or demo (free, placeholder images)</summary>
    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GenerationMode? Mode { get; init; }

    /// <summary>Additional model-specific options</summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Options { get; init; }
}

/// <summary>
/// Usage information from a generation
/// </summary>
public record GenerateUsage
{
    /// <summary>Credits used for this generation</summary>
    [JsonPropertyName("creditsUsed")]
    public decimal CreditsUsed { get; init; }

    /// <summary>Remaining credit balance</summary>
    [JsonPropertyName("balanceRemaining")]
    public decimal BalanceRemaining { get; init; }
}

/// <summary>
/// Result of an image generation
/// </summary>
public record GenerateResult
{
    /// <summary>Unique generation ID</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>URL to the generated image</summary>
    [JsonPropertyName("imageUrl")]
    public required string ImageUrl { get; init; }

    /// <summary>IPFS hash (if uploaded)</summary>
    [JsonPropertyName("ipfsHash")]
    public string? IpfsHash { get; init; }

    /// <summary>Model used</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>Mode used</summary>
    [JsonPropertyName("mode")]
    public GenerationMode Mode { get; init; }

    /// <summary>Usage information</summary>
    [JsonPropertyName("usage")]
    public required GenerateUsage Usage { get; init; }
}

/// <summary>
/// Account balance information
/// </summary>
public record Balance
{
    /// <summary>Current credit balance in USD</summary>
    [JsonPropertyName("credits")]
    public decimal Credits { get; init; }

    /// <summary>Total amount deposited</summary>
    [JsonPropertyName("totalDeposited")]
    public decimal TotalDeposited { get; init; }

    /// <summary>Total amount spent</summary>
    [JsonPropertyName("totalSpent")]
    public decimal TotalSpent { get; init; }

    /// <summary>Total amount withdrawn</summary>
    [JsonPropertyName("totalWithdrawn")]
    public decimal TotalWithdrawn { get; init; }

    /// <summary>Total number of generations</summary>
    [JsonPropertyName("totalGenerated")]
    public long TotalGenerated { get; init; }
}

/// <summary>
/// Parameters for fetching usage history
/// </summary>
public record HistoryParams
{
    /// <summary>Number of items to return (default: 50, max: 100)</summary>
    public int? Limit { get; init; }

    /// <summary>Pagination offset</summary>
    public int? Offset { get; init; }
}

/// <summary>
/// Status of a history item
/// </summary>
public enum HistoryStatus
{
    [JsonPropertyName("pending")]
    Pending,
    [JsonPropertyName("completed")]
    Completed,
    [JsonPropertyName("refunded")]
    Refunded
}

/// <summary>
/// A single usage history item
/// </summary>
public record HistoryItem
{
    /// <summary>Usage record ID</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>API endpoint called</summary>
    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; init; }

    /// <summary>Model used</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>Credits used</summary>
    [JsonPropertyName("creditsUsed")]
    public decimal CreditsUsed { get; init; }

    /// <summary>Request ID (for generation requests)</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }

    /// <summary>Status</summary>
    [JsonPropertyName("status")]
    public HistoryStatus Status { get; init; }

    /// <summary>Creation timestamp</summary>
    [JsonPropertyName("createdAt")]
    public required string CreatedAt { get; init; }

    /// <summary>Completion timestamp</summary>
    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; init; }
}

/// <summary>
/// Pagination information
/// </summary>
public record Pagination
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }
}

/// <summary>
/// Response containing usage history
/// </summary>
public record HistoryResponse
{
    /// <summary>Usage history items</summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<HistoryItem> Items { get; init; }

    /// <summary>Pagination info</summary>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }
}

/// <summary>
/// Parameters for creating an API key
/// </summary>
public record CreateKeyParams
{
    /// <summary>Optional name for the key</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>Message to sign</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Wallet signature (base58)</summary>
    [JsonPropertyName("signature")]
    public required string Signature { get; init; }

    /// <summary>Wallet public key (base58)</summary>
    [JsonPropertyName("publicKey")]
    public required string PublicKey { get; init; }
}

/// <summary>
/// Environment type for API keys
/// </summary>
public enum KeyEnvironment
{
    [JsonPropertyName("live")]
    Live,
    [JsonPropertyName("test")]
    Test
}

/// <summary>
/// API key information
/// </summary>
public record ApiKey
{
    /// <summary>Key ID</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Key name</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Key prefix (for display)</summary>
    [JsonPropertyName("keyPrefix")]
    public required string KeyPrefix { get; init; }

    /// <summary>Environment</summary>
    [JsonPropertyName("environment")]
    public KeyEnvironment Environment { get; init; }

    /// <summary>Rate limit tier</summary>
    [JsonPropertyName("rateLimitTier")]
    public required string RateLimitTier { get; init; }

    /// <summary>Creation timestamp</summary>
    [JsonPropertyName("createdAt")]
    public required string CreatedAt { get; init; }

    /// <summary>Last used timestamp</summary>
    [JsonPropertyName("lastUsedAt")]
    public string? LastUsedAt { get; init; }

    /// <summary>Whether the key has been revoked</summary>
    [JsonPropertyName("revoked")]
    public bool Revoked { get; init; }
}

/// <summary>
/// Result of creating an API key
/// </summary>
public record CreateKeyResult
{
    /// <summary>Key ID</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Full API key (only shown once!)</summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>Key prefix</summary>
    [JsonPropertyName("keyPrefix")]
    public required string KeyPrefix { get; init; }

    /// <summary>Key name</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Environment</summary>
    [JsonPropertyName("environment")]
    public KeyEnvironment Environment { get; init; }

    /// <summary>Creation timestamp</summary>
    [JsonPropertyName("createdAt")]
    public required string CreatedAt { get; init; }

    /// <summary>Warning message</summary>
    [JsonPropertyName("warning")]
    public required string Warning { get; init; }
}

/// <summary>
/// Response containing API keys
/// </summary>
public record KeysResponse
{
    [JsonPropertyName("keys")]
    public required IReadOnlyList<ApiKey> Keys { get; init; }
}

/// <summary>
/// Parameters for submitting a prompt for on-chain payment
/// </summary>
public record SubmitPromptParams
{
    /// <summary>Text prompt for image generation</summary>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    /// <summary>Model to use</summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; init; }

    /// <summary>Additional options</summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Options { get; init; }

    /// <summary>Callback URL for result notification</summary>
    [JsonPropertyName("callbackUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CallbackUrl { get; init; }
}

/// <summary>
/// Required payment amount in different units
/// </summary>
public record RequiredAmount
{
    /// <summary>Amount in SOL</summary>
    [JsonPropertyName("sol")]
    public decimal Sol { get; init; }

    /// <summary>Amount in lamports</summary>
    [JsonPropertyName("lamports")]
    public long Lamports { get; init; }

    /// <summary>Amount in USD</summary>
    [JsonPropertyName("usd")]
    public decimal Usd { get; init; }
}

/// <summary>
/// Result of submitting a prompt for on-chain payment
/// </summary>
public record PromptSubmission
{
    /// <summary>Submission ID</summary>
    [JsonPropertyName("submissionId")]
    public required string SubmissionId { get; init; }

    /// <summary>Prompt hash (for memo)</summary>
    [JsonPropertyName("promptHash")]
    public required string PromptHash { get; init; }

    /// <summary>Treasury address to send payment</summary>
    [JsonPropertyName("paymentAddress")]
    public required string PaymentAddress { get; init; }

    /// <summary>Required payment amount</summary>
    [JsonPropertyName("requiredAmount")]
    public required RequiredAmount RequiredAmount { get; init; }

    /// <summary>Memo to include in transaction</summary>
    [JsonPropertyName("memo")]
    public required string Memo { get; init; }

    /// <summary>Model to use</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>Slippage tolerance</summary>
    [JsonPropertyName("slippageTolerance")]
    public decimal SlippageTolerance { get; init; }

    /// <summary>Expiration timestamp</summary>
    [JsonPropertyName("expiresAt")]
    public required string ExpiresAt { get; init; }

    /// <summary>Payment instructions</summary>
    [JsonPropertyName("instructions")]
    public required Dictionary<string, string> Instructions { get; init; }
}

/// <summary>
/// Status of an on-chain generation
/// </summary>
public enum OnChainStatus
{
    [JsonPropertyName("pending")]
    Pending,
    [JsonPropertyName("processing")]
    Processing,
    [JsonPropertyName("completed")]
    Completed,
    [JsonPropertyName("failed")]
    Failed,
    [JsonPropertyName("refunded")]
    Refunded
}

/// <summary>
/// Status and result of an on-chain generation
/// </summary>
public record OnChainGenerationStatus
{
    /// <summary>Transaction signature</summary>
    [JsonPropertyName("txSignature")]
    public required string TxSignature { get; init; }

    /// <summary>Status</summary>
    [JsonPropertyName("status")]
    public OnChainStatus Status { get; init; }

    /// <summary>Model used</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>Creation timestamp</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; init; }

    /// <summary>Image URL (when completed)</summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; init; }

    /// <summary>IPFS hash (when completed)</summary>
    [JsonPropertyName("ipfsHash")]
    public string? IpfsHash { get; init; }

    /// <summary>Completion timestamp</summary>
    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; init; }

    /// <summary>Error message (when failed)</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Status message</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

// Internal types
internal record ApiErrorResponse
{
    [JsonPropertyName("error")]
    public required ApiErrorDetail Error { get; init; }
}

internal record ApiErrorDetail
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("param")]
    public string? Param { get; init; }
}

internal record SuccessResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}
