using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace PeerCat.Tests;

public class PeerCatClientTests
{
    private const string TestApiKey = "pcat_test_abc123";
    private const string BaseUrl = "https://api.peerc.at/v1";

    private static PeerCatClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        return new PeerCatClient(new PeerCatOptions
        {
            ApiKey = TestApiKey,
            HttpClient = mockHttp.ToHttpClient()
        });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithApiKey_CreatesClient()
    {
        var client = new PeerCatClient(TestApiKey);
        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PeerCatClient(""));
    }

    [Fact]
    public void Constructor_WithOptions_CreatesClient()
    {
        var client = new PeerCatClient(new PeerCatOptions
        {
            ApiKey = TestApiKey,
            BaseUrl = "https://custom.api.com/v1",
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetries = 5
        });
        Assert.NotNull(client);
        client.Dispose();
    }

    #endregion

    #region Generate Tests

    [Fact]
    public async Task GenerateAsync_WithPrompt_ReturnsResult()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                id = "gen_123",
                imageUrl = "https://cdn.peerc.at/images/gen_123.png",
                model = "stable-diffusion-xl",
                mode = "production",
                usage = new { creditsUsed = 0.02m, balanceRemaining = 9.98m }
            }));

        using var client = CreateClient(mockHttp);
        var result = await client.GenerateAsync("A beautiful sunset");

        Assert.Equal("gen_123", result.Id);
        Assert.Equal("https://cdn.peerc.at/images/gen_123.png", result.ImageUrl);
        Assert.Equal("stable-diffusion-xl", result.Model);
        Assert.Equal(0.02m, result.Usage.CreditsUsed);
    }

    [Fact]
    public async Task GenerateAsync_WithParams_ReturnsResult()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                id = "gen_456",
                imageUrl = "https://cdn.peerc.at/images/gen_456.png",
                model = "imagen-3",
                mode = "demo",
                usage = new { creditsUsed = 0m, balanceRemaining = 10m }
            }));

        using var client = CreateClient(mockHttp);
        var result = await client.GenerateAsync(new GenerateParams
        {
            Prompt = "A mountain landscape",
            Model = "imagen-3",
            Mode = GenerationMode.Demo
        });

        Assert.Equal("gen_456", result.Id);
        Assert.Equal("imagen-3", result.Model);
        Assert.Equal(GenerationMode.Demo, result.Mode);
    }

    [Fact]
    public async Task GenerateAsync_WithNullPrompt_ThrowsArgumentException()
    {
        var mockHttp = new MockHttpMessageHandler();
        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.GenerateAsync(new GenerateParams { Prompt = "" }));
    }

    #endregion

    #region Models Tests

    [Fact]
    public async Task GetModelsAsync_ReturnsModels()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/models")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                models = new[]
                {
                    new
                    {
                        id = "stable-diffusion-xl",
                        name = "Stable Diffusion XL",
                        description = "High quality image generation",
                        provider = "stability",
                        maxPromptLength = 2000,
                        outputFormat = "png",
                        outputResolution = "1024x1024",
                        priceUsd = 0.02m
                    }
                }
            }));

        using var client = CreateClient(mockHttp);
        var models = await client.GetModelsAsync();

        Assert.Single(models);
        Assert.Equal("stable-diffusion-xl", models[0].Id);
        Assert.Equal("Stable Diffusion XL", models[0].Name);
        Assert.Equal(0.02m, models[0].PriceUsd);
    }

    #endregion

    #region Prices Tests

    [Fact]
    public async Task GetPricesAsync_ReturnsPrices()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/price")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                solPrice = 180.50m,
                slippageTolerance = 0.02m,
                updatedAt = "2024-01-15T10:30:00Z",
                models = new[]
                {
                    new
                    {
                        model = "stable-diffusion-xl",
                        priceUsd = 0.02m,
                        priceSol = 0.000111m,
                        priceSolWithSlippage = 0.000113m
                    }
                }
            }));

        using var client = CreateClient(mockHttp);
        var prices = await client.GetPricesAsync();

        Assert.Equal(180.50m, prices.SolPrice);
        Assert.Equal(0.02m, prices.SlippageTolerance);
        Assert.Single(prices.Models);
    }

    #endregion

    #region Balance Tests

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalance()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                credits = 10.50m,
                totalDeposited = 20m,
                totalSpent = 9.50m,
                totalWithdrawn = 0m,
                totalGenerated = 475L
            }));

        using var client = CreateClient(mockHttp);
        var balance = await client.GetBalanceAsync();

        Assert.Equal(10.50m, balance.Credits);
        Assert.Equal(20m, balance.TotalDeposited);
        Assert.Equal(9.50m, balance.TotalSpent);
        Assert.Equal(475, balance.TotalGenerated);
    }

    #endregion

    #region History Tests

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistory()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/history")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                items = new[]
                {
                    new
                    {
                        id = "use_123",
                        endpoint = "/v1/generate",
                        model = "stable-diffusion-xl",
                        creditsUsed = 0.02m,
                        requestId = "gen_123",
                        status = "completed",
                        createdAt = "2024-01-15T10:00:00Z",
                        completedAt = "2024-01-15T10:00:05Z"
                    }
                },
                pagination = new { total = 100, limit = 50, offset = 0, hasMore = true }
            }));

        using var client = CreateClient(mockHttp);
        var history = await client.GetHistoryAsync();

        Assert.Single(history.Items);
        Assert.Equal("use_123", history.Items[0].Id);
        Assert.Equal(HistoryStatus.Completed, history.Items[0].Status);
        Assert.True(history.Pagination.HasMore);
    }

    [Fact]
    public async Task GetHistoryAsync_WithPagination_IncludesQueryParams()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/history?limit=10&offset=20")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                items = Array.Empty<object>(),
                pagination = new { total = 100, limit = 10, offset = 20, hasMore = true }
            }));

        using var client = CreateClient(mockHttp);
        var history = await client.GetHistoryAsync(limit: 10, offset: 20);

        Assert.Equal(10, history.Pagination.Limit);
        Assert.Equal(20, history.Pagination.Offset);
    }

    #endregion

    #region API Key Tests

    [Fact]
    public async Task CreateKeyAsync_ReturnsKey()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/keys")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                id = "key_123",
                key = "pcat_live_abc123xyz",
                keyPrefix = "pcat_live_abc",
                name = "Production Key",
                environment = "live",
                createdAt = "2024-01-15T10:00:00Z",
                warning = "Store this key securely. It will not be shown again."
            }));

        using var client = CreateClient(mockHttp);
        var result = await client.CreateKeyAsync(new CreateKeyParams
        {
            Name = "Production Key",
            Message = "Sign this message",
            Signature = "sig123",
            PublicKey = "pubkey123"
        });

        Assert.Equal("key_123", result.Id);
        Assert.Equal("pcat_live_abc123xyz", result.Key);
        Assert.Equal(KeyEnvironment.Live, result.Environment);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsKeys()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/keys")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                keys = new[]
                {
                    new
                    {
                        id = "key_123",
                        name = "Production Key",
                        keyPrefix = "pcat_live_abc",
                        environment = "live",
                        rateLimitTier = "standard",
                        createdAt = "2024-01-15T10:00:00Z",
                        lastUsedAt = "2024-01-15T12:00:00Z",
                        revoked = false
                    }
                }
            }));

        using var client = CreateClient(mockHttp);
        var keys = await client.ListKeysAsync();

        Assert.Single(keys);
        Assert.Equal("key_123", keys[0].Id);
        Assert.False(keys[0].Revoked);
    }

    [Fact]
    public async Task RevokeKeyAsync_SendsDeleteRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/keys/key_123")
            .Respond("application/json", JsonSerializer.Serialize(new { success = true }));

        using var client = CreateClient(mockHttp);
        await client.RevokeKeyAsync("key_123");

        // If no exception, the test passes
    }

    [Fact]
    public async Task UpdateKeyNameAsync_SendsPatchRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/keys/key_123")
            .Respond("application/json", JsonSerializer.Serialize(new { success = true }));

        using var client = CreateClient(mockHttp);
        await client.UpdateKeyNameAsync("key_123", "New Key Name");

        // If no exception, the test passes
    }

    [Fact]
    public async Task UpdateKeyNameAsync_WithEmptyKeyId_ThrowsArgumentException()
    {
        var mockHttp = new MockHttpMessageHandler();
        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.UpdateKeyNameAsync("", "New Name"));
    }

    [Fact]
    public async Task UpdateKeyNameAsync_WithEmptyName_ThrowsArgumentException()
    {
        var mockHttp = new MockHttpMessageHandler();
        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.UpdateKeyNameAsync("key_123", ""));
    }

    #endregion

    #region On-Chain Tests

    [Fact]
    public async Task SubmitPromptAsync_ReturnsSubmission()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/prompts")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                submissionId = "sub_123",
                promptHash = "abc123def456",
                paymentAddress = "9JKi6Tr7JdsTJw1zNedF5vML9GpPnjHD9DWuZq1oE6nV",
                requiredAmount = new { sol = 0.000111m, lamports = 111000L, usd = 0.02m },
                memo = "PCAT:v1:sdxl:abc123def456",
                model = "stable-diffusion-xl",
                slippageTolerance = 0.02m,
                expiresAt = "2024-01-15T10:15:00Z",
                instructions = new Dictionary<string, string>
                {
                    ["1"] = "Send SOL to payment address",
                    ["2"] = "Include memo in transaction"
                }
            }));

        using var client = CreateClient(mockHttp);
        var result = await client.SubmitPromptAsync("A beautiful sunset");

        Assert.Equal("sub_123", result.SubmissionId);
        Assert.Equal("PCAT:v1:sdxl:abc123def456", result.Memo);
        Assert.Equal(0.000111m, result.RequiredAmount.Sol);
    }

    [Fact]
    public async Task GetGenerationStatusAsync_ReturnsStatus()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/generate/tx_signature_123")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                txSignature = "tx_signature_123",
                status = "completed",
                model = "stable-diffusion-xl",
                createdAt = "2024-01-15T10:00:00Z",
                imageUrl = "https://cdn.peerc.at/images/gen_123.png",
                ipfsHash = "QmXyz123",
                completedAt = "2024-01-15T10:00:10Z"
            }));

        using var client = CreateClient(mockHttp);
        var status = await client.GetGenerationStatusAsync("tx_signature_123");

        Assert.Equal("tx_signature_123", status.TxSignature);
        Assert.Equal(OnChainStatus.Completed, status.Status);
        Assert.Equal("https://cdn.peerc.at/images/gen_123.png", status.ImageUrl);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ApiError_ThrowsAuthenticationException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.Unauthorized, "application/json", JsonSerializer.Serialize(new
            {
                error = new
                {
                    type = "authentication_error",
                    code = "invalid_api_key",
                    message = "Invalid API key provided"
                }
            }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.GetBalanceAsync());
        Assert.Equal("invalid_api_key", ex.Code);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public async Task ApiError_ThrowsInsufficientCreditsException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(HttpStatusCode.PaymentRequired, "application/json", JsonSerializer.Serialize(new
            {
                error = new
                {
                    type = "insufficient_credits",
                    code = "insufficient_balance",
                    message = "Not enough credits"
                }
            }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<InsufficientCreditsException>(() =>
            client.GenerateAsync("test prompt"));
        Assert.Equal("insufficient_balance", ex.Code);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public async Task ApiError_ThrowsRateLimitException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", JsonSerializer.Serialize(new
            {
                error = new
                {
                    type = "rate_limit_error",
                    code = "rate_limit_exceeded",
                    message = "Too many requests"
                }
            }));

        using var client = new PeerCatClient(new PeerCatOptions
        {
            ApiKey = TestApiKey,
            MaxRetries = 0,
            HttpClient = mockHttp.ToHttpClient()
        });

        var ex = await Assert.ThrowsAsync<RateLimitException>(() =>
            client.GenerateAsync("test prompt"));
        Assert.Equal("rate_limit_exceeded", ex.Code);
        Assert.True(ex.IsRetryable);
    }

    [Fact]
    public async Task ApiError_ThrowsNotFoundException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/generate/invalid_tx")
            .Respond(HttpStatusCode.NotFound, "application/json", JsonSerializer.Serialize(new
            {
                error = new
                {
                    type = "not_found",
                    code = "generation_not_found",
                    message = "Generation not found"
                }
            }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            client.GetGenerationStatusAsync("invalid_tx"));
        Assert.Equal("generation_not_found", ex.Code);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public async Task ApiError_ThrowsInvalidRequestException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(new
            {
                error = new
                {
                    type = "invalid_request_error",
                    code = "invalid_prompt",
                    message = "Prompt is required",
                    param = "prompt"
                }
            }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<InvalidRequestException>(() =>
            client.GenerateAsync(new GenerateParams { Prompt = "test" }));
        Assert.Equal("invalid_prompt", ex.Code);
        Assert.Equal("prompt", ex.Param);
        Assert.False(ex.IsRetryable);
    }

    #endregion
}
