using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace PeerCat.Tests;

/// <summary>
/// Error scenario and resilience tests for the PeerCat C# SDK.
/// These tests cover edge cases, network failures, malformed responses,
/// and retry/rate-limit behavior to ensure SDK robustness.
/// </summary>
public class ErrorScenariosTests
{
    private const string TestApiKey = "pcat_test_error_scenarios";
    private const string BaseUrl = "https://api.peerc.at/v1";

    private static PeerCatClient CreateClient(MockHttpMessageHandler mockHttp, int maxRetries = 0)
    {
        return new PeerCatClient(new PeerCatOptions
        {
            ApiKey = TestApiKey,
            HttpClient = mockHttp.ToHttpClient(),
            MaxRetries = maxRetries
        });
    }

    // ============ Malformed Response Tests ============

    [Fact]
    public async Task MalformedJsonResponse_ThrowsException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", "not valid json {");

        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetBalanceAsync());
    }

    [Fact]
    public async Task MalformedJsonErrorResponse_ThrowsException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "invalid error json");

        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetBalanceAsync());
    }

    [Fact]
    public async Task EmptyResponseBody_ThrowsException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", "");

        using var client = CreateClient(mockHttp);

        // Should throw, not crash with null reference
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetBalanceAsync());
    }

    [Fact]
    public async Task ErrorResponseWithoutErrorWrapper_Throws()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.InternalServerError, "application/json",
                JsonSerializer.Serialize(new { message = "Something went wrong" }));

        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetBalanceAsync());
    }

    // ============ HTTP Status Code Tests ============

    [Fact]
    public async Task Http403Forbidden_ThrowsAuthenticationException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.Forbidden, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "authentication_error",
                        code = "forbidden",
                        message = "Access denied"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.GetBalanceAsync());
        Assert.Equal("forbidden", ex.Code);
    }

    [Fact]
    public async Task Http404NotFound_ThrowsNotFoundException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/generate/invalid_tx")
            .Respond(HttpStatusCode.NotFound, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "not_found",
                        code = "resource_not_found",
                        message = "Generation not found"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.GetGenerationStatusAsync("invalid_tx"));
        Assert.Equal("resource_not_found", ex.Code);
    }

    [Fact]
    public async Task Http502BadGateway_ThrowsPeerCatException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.BadGateway, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "server_error",
                        code = "bad_gateway",
                        message = "Bad gateway"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<PeerCatException>(() => client.GetBalanceAsync());
        Assert.Equal("server_error", ex.Type);
    }

    [Fact]
    public async Task Http503ServiceUnavailable_ThrowsPeerCatException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "server_error",
                        code = "service_unavailable",
                        message = "Service temporarily unavailable"
                    }
                }));

        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAsync<PeerCatException>(() => client.GetBalanceAsync());
    }

    [Fact]
    public async Task Http504GatewayTimeout_ThrowsPeerCatException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.GatewayTimeout, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "server_error",
                        code = "gateway_timeout",
                        message = "Gateway timeout"
                    }
                }));

        using var client = CreateClient(mockHttp);

        await Assert.ThrowsAsync<PeerCatException>(() => client.GetBalanceAsync());
    }

    // ============ Retry Behavior Tests ============

    [Fact]
    public async Task NoRetry4xxErrors_DoesNotRetry()
    {
        var callCount = 0;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(req =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            type = "authentication_error",
                            code = "invalid_api_key",
                            message = "Invalid API key"
                        }
                    }), System.Text.Encoding.UTF8, "application/json")
                };
            });

        using var client = CreateClient(mockHttp, maxRetries: 3);

        await Assert.ThrowsAsync<AuthenticationException>(() => client.GetBalanceAsync());
        Assert.Equal(1, callCount);
    }

    // ============ Rate Limit Tests ============

    [Fact]
    public async Task RateLimitError_ThrowsRateLimitException()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(req =>
            {
                var response = new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            type = "rate_limit_error",
                            code = "rate_limit_exceeded",
                            message = "Rate limit exceeded"
                        }
                    }), System.Text.Encoding.UTF8, "application/json")
                };
                response.Headers.Add("X-RateLimit-Limit", "1000");
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("Retry-After", "60");
                return response;
            });

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GetBalanceAsync());

        Assert.Equal(1000, ex.RateLimitInfo?.Limit);
        Assert.Equal(0, ex.RateLimitInfo?.Remaining);
        Assert.Equal(60, ex.RetryAfter);
    }

    // ============ Error Property Tests ============

    [Fact]
    public async Task ErrorProperties_IncludesAllDetails()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(HttpStatusCode.Unauthorized, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "authentication_error",
                        code = "invalid_api_key",
                        message = "Invalid API key"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.GetBalanceAsync());

        Assert.Equal("invalid_api_key", ex.Code);
        Assert.Equal("authentication_error", ex.Type);
        Assert.Contains("Invalid API key", ex.Message);
    }

    [Fact]
    public async Task ErrorWithParam_IncludesParam()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "invalid_request_error",
                        code = "invalid_param",
                        message = "Model not found",
                        param = "model"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var ex = await Assert.ThrowsAsync<InvalidRequestException>(() =>
            client.GenerateAsync(new GenerateParams { Prompt = "test", Model = "invalid" }));

        Assert.Equal("model", ex.Param);
    }

    // ============ Edge Case Tests ============

    [Fact]
    public async Task VeryLongPrompt_HandledCorrectly()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        type = "invalid_request_error",
                        code = "prompt_too_long",
                        message = "Prompt exceeds maximum length",
                        param = "prompt"
                    }
                }));

        using var client = CreateClient(mockHttp);

        var longPrompt = new string('x', 10000);

        var ex = await Assert.ThrowsAsync<InvalidRequestException>(() =>
            client.GenerateAsync(new GenerateParams { Prompt = longPrompt }));

        Assert.Equal("prompt_too_long", ex.Code);
    }

    [Fact]
    public async Task SpecialCharactersInPrompt_SentCorrectly()
    {
        string? receivedPrompt = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(req =>
            {
                var body = req.Content!.ReadAsStringAsync().Result;
                var doc = JsonDocument.Parse(body);
                receivedPrompt = doc.RootElement.GetProperty("prompt").GetString();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        id = "gen_123",
                        imageUrl = "https://example.com/image.png",
                        model = "stable-diffusion-xl",
                        mode = "production",
                        usage = new { creditsUsed = 0.05m, balanceRemaining = 9.95m }
                    }), System.Text.Encoding.UTF8, "application/json")
                };
            });

        using var client = CreateClient(mockHttp);

        var specialPrompt = "Test with \"quotes\" and <tags> and émojis 🎨";

        await client.GenerateAsync(new GenerateParams { Prompt = specialPrompt });

        Assert.Equal(specialPrompt, receivedPrompt);
    }

    [Fact]
    public async Task UnicodeInPrompt_SentCorrectly()
    {
        string? receivedPrompt = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/generate")
            .Respond(req =>
            {
                var body = req.Content!.ReadAsStringAsync().Result;
                var doc = JsonDocument.Parse(body);
                receivedPrompt = doc.RootElement.GetProperty("prompt").GetString();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        id = "gen_123",
                        imageUrl = "https://example.com/image.png",
                        model = "stable-diffusion-xl",
                        mode = "production",
                        usage = new { creditsUsed = 0.05m, balanceRemaining = 9.95m }
                    }), System.Text.Encoding.UTF8, "application/json")
                };
            });

        using var client = CreateClient(mockHttp);

        var unicodePrompt = "日本語テスト 中文测试 한국어테스트";

        await client.GenerateAsync(new GenerateParams { Prompt = unicodePrompt });

        Assert.Equal(unicodePrompt, receivedPrompt);
    }

    [Fact]
    public async Task ExtraFieldsInResponse_IgnoredGracefully()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", """
            {
                "credits": 10.50,
                "totalDeposited": 50.00,
                "totalSpent": 39.50,
                "totalWithdrawn": 0.00,
                "totalGenerated": 100,
                "unexpectedField": "should be ignored",
                "anotherUnknown": {"nested": "data"}
            }
            """);

        using var client = CreateClient(mockHttp);

        var balance = await client.GetBalanceAsync();

        Assert.Equal(10.50m, balance.Credits);
    }

    [Fact]
    public async Task VeryLargeNumericValues_HandleCorrectly()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                credits = 999999999.99m,
                totalDeposited = 1000000000m,
                totalSpent = 0.000001m,
                totalWithdrawn = 0m,
                totalGenerated = 9007199254740991L
            }));

        using var client = CreateClient(mockHttp);

        var result = await client.GetBalanceAsync();

        Assert.Equal(999999999.99m, result.Credits);
        Assert.Equal(9007199254740991L, result.TotalGenerated);
    }

    [Fact]
    public async Task ZeroCredits_HandleCorrectly()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                credits = 0m,
                totalDeposited = 0m,
                totalSpent = 0m,
                totalWithdrawn = 0m,
                totalGenerated = 0L
            }));

        using var client = CreateClient(mockHttp);

        var result = await client.GetBalanceAsync();

        Assert.Equal(0m, result.Credits);
    }

    // ============ API Key Tests ============

    [Fact]
    public async Task ApiKeyInAuthorizationHeader_SentCorrectly()
    {
        string? authHeader = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/balance")
            .Respond(req =>
            {
                authHeader = req.Headers.Authorization?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        credits = 10m,
                        totalDeposited = 50m,
                        totalSpent = 40m,
                        totalWithdrawn = 0m,
                        totalGenerated = 100L
                    }), System.Text.Encoding.UTF8, "application/json")
                };
            });

        using var client = new PeerCatClient(new PeerCatOptions
        {
            ApiKey = "pcat_live_test123",
            HttpClient = mockHttp.ToHttpClient()
        });

        await client.GetBalanceAsync();

        Assert.Equal("Bearer pcat_live_test123", authHeader);
    }
}
