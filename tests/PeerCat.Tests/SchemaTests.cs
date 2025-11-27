using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace PeerCat.Tests;

/// <summary>
/// Schema validation tests to ensure SDK types match OpenAPI specification.
/// These tests validate:
/// 1. All required fields are present in response types
/// 2. Field types match the OpenAPI schema
/// 3. JSON property names match OpenAPI property names
/// 4. Enum values match OpenAPI enum definitions
/// </summary>
public class SchemaTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    #region Model Schema Tests

    [Fact]
    public void Model_HasAllRequiredFields()
    {
        // OpenAPI required: id, name, description, provider, maxPromptLength, outputFormat, outputResolution, priceUsd
        var requiredFields = new[]
        {
            "id", "name", "description", "provider", "maxPromptLength", "outputFormat", "outputResolution", "priceUsd"
        };

        AssertHasJsonProperties(typeof(Model), requiredFields);
    }

    [Fact]
    public void Model_DeserializesCorrectly()
    {
        var json = """
        {
            "id": "stable-diffusion-xl",
            "name": "Stable Diffusion XL",
            "description": "High quality image generation",
            "provider": "stability",
            "maxPromptLength": 2000,
            "outputFormat": "png",
            "outputResolution": "1024x1024",
            "priceUsd": 0.28
        }
        """;

        var model = JsonSerializer.Deserialize<Model>(json, JsonOptions);

        Assert.NotNull(model);
        Assert.Equal("stable-diffusion-xl", model.Id);
        Assert.Equal("Stable Diffusion XL", model.Name);
        Assert.Equal(2000, model.MaxPromptLength);
        Assert.Equal(0.28m, model.PriceUsd);
    }

    #endregion

    #region Balance Schema Tests

    [Fact]
    public void Balance_HasAllRequiredFields()
    {
        var requiredFields = new[]
        {
            "credits", "totalDeposited", "totalSpent", "totalWithdrawn", "totalGenerated"
        };

        AssertHasJsonProperties(typeof(Balance), requiredFields);
    }

    [Fact]
    public void Balance_DeserializesCorrectly()
    {
        var json = """
        {
            "credits": 10.50,
            "totalDeposited": 50.00,
            "totalSpent": 39.50,
            "totalWithdrawn": 0.00,
            "totalGenerated": 100
        }
        """;

        var balance = JsonSerializer.Deserialize<Balance>(json, JsonOptions);

        Assert.NotNull(balance);
        Assert.Equal(10.50m, balance.Credits);
        Assert.Equal(100, balance.TotalGenerated);
    }

    #endregion

    #region GenerateResult Schema Tests

    [Fact]
    public void GenerateResult_HasAllRequiredFields()
    {
        var requiredFields = new[] { "id", "imageUrl", "model", "mode", "usage" };

        AssertHasJsonProperties(typeof(GenerateResult), requiredFields);
    }

    [Fact]
    public void GenerateResult_DeserializesProductionMode()
    {
        var json = """
        {
            "id": "gen_123",
            "imageUrl": "https://cdn.peerc.at/images/gen_123.png",
            "ipfsHash": "QmXyz123",
            "model": "stable-diffusion-xl",
            "mode": "production",
            "usage": {
                "creditsUsed": 0.28,
                "balanceRemaining": 9.72
            }
        }
        """;

        var result = JsonSerializer.Deserialize<GenerateResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(GenerationMode.Production, result.Mode);
        Assert.Equal("QmXyz123", result.IpfsHash);
    }

    [Fact]
    public void GenerateResult_DeserializesDemoModeWithNullIpfs()
    {
        var json = """
        {
            "id": "demo_123",
            "imageUrl": "https://cdn.peerc.at/demo/placeholder.png",
            "ipfsHash": null,
            "model": "stable-diffusion-xl",
            "mode": "demo",
            "usage": {
                "creditsUsed": 0,
                "balanceRemaining": 10
            }
        }
        """;

        var result = JsonSerializer.Deserialize<GenerateResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(GenerationMode.Demo, result.Mode);
        Assert.Null(result.IpfsHash);
    }

    #endregion

    #region PriceResponse Schema Tests

    [Fact]
    public void PriceResponse_HasAllRequiredFields_IncludingTreasury()
    {
        // Treasury is now required per OpenAPI spec (our fix)
        var requiredFields = new[]
        {
            "solPrice", "slippageTolerance", "updatedAt", "treasury", "models"
        };

        AssertHasJsonProperties(typeof(PriceResponse), requiredFields);
    }

    [Fact]
    public void PriceResponse_DeserializesWithTreasury()
    {
        var json = """
        {
            "solPrice": 185.50,
            "slippageTolerance": 0.05,
            "updatedAt": "2024-01-15T12:00:00Z",
            "treasury": "9JKi6Tr7JdsTJw1zNedF5vML9GpPnjHD9DWuZq1oE6nV",
            "models": [
                {
                    "model": "stable-diffusion-xl",
                    "priceUsd": 0.28,
                    "priceSol": 0.00151,
                    "priceSolWithSlippage": 0.00159
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<PriceResponse>(json, JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(185.50m, response.SolPrice);
        Assert.Equal("9JKi6Tr7JdsTJw1zNedF5vML9GpPnjHD9DWuZq1oE6nV", response.Treasury);
        Assert.Single(response.Models);
    }

    #endregion

    #region HistoryItem Schema Tests

    [Fact]
    public void HistoryItem_HasAllRequiredFields()
    {
        var requiredFields = new[] { "id", "endpoint", "creditsUsed", "status", "createdAt" };

        AssertHasJsonProperties(typeof(HistoryItem), requiredFields);
    }

    [Fact]
    public void HistoryItem_DeserializesCompleted()
    {
        var json = """
        {
            "id": "use_123",
            "endpoint": "/v1/generate",
            "model": "stable-diffusion-xl",
            "creditsUsed": 0.28,
            "requestId": "gen_123",
            "status": "completed",
            "createdAt": "2024-01-15T10:00:00Z",
            "completedAt": "2024-01-15T10:00:05Z"
        }
        """;

        var item = JsonSerializer.Deserialize<HistoryItem>(json, JsonOptions);

        Assert.NotNull(item);
        Assert.Equal(HistoryStatus.Completed, item.Status);
        Assert.NotNull(item.CompletedAt);
    }

    [Fact]
    public void HistoryItem_DeserializesPendingWithNullFields()
    {
        var json = """
        {
            "id": "use_456",
            "endpoint": "/v1/generate",
            "model": null,
            "creditsUsed": 0,
            "requestId": null,
            "status": "pending",
            "createdAt": "2024-01-15T10:00:00Z",
            "completedAt": null
        }
        """;

        var item = JsonSerializer.Deserialize<HistoryItem>(json, JsonOptions);

        Assert.NotNull(item);
        Assert.Equal(HistoryStatus.Pending, item.Status);
        Assert.Null(item.Model);
        Assert.Null(item.CompletedAt);
    }

    #endregion

    #region ApiKey Schema Tests

    [Fact]
    public void ApiKey_HasAllRequiredFields()
    {
        var requiredFields = new[]
        {
            "id", "keyPrefix", "environment", "rateLimitTier", "createdAt", "revoked"
        };

        AssertHasJsonProperties(typeof(ApiKey), requiredFields);
    }

    #endregion

    #region OnChainGenerationStatus Schema Tests

    [Fact]
    public void OnChainGenerationStatus_HasRequiredFields()
    {
        var requiredFields = new[] { "txSignature", "status" };

        AssertHasJsonProperties(typeof(OnChainGenerationStatus), requiredFields);
    }

    [Fact]
    public void OnChainGenerationStatus_DeserializesCompleted()
    {
        var json = """
        {
            "txSignature": "txSig123abc",
            "status": "completed",
            "model": "stable-diffusion-xl",
            "createdAt": "2024-01-15T10:00:00Z",
            "imageUrl": "https://cdn.peerc.at/images/gen_123.png",
            "ipfsHash": "QmXyz123",
            "completedAt": "2024-01-15T10:00:10Z"
        }
        """;

        var status = JsonSerializer.Deserialize<OnChainGenerationStatus>(json, JsonOptions);

        Assert.NotNull(status);
        Assert.Equal(OnChainStatus.Completed, status.Status);
        Assert.NotNull(status.ImageUrl);
    }

    [Fact]
    public void OnChainGenerationStatus_DeserializesPendingMinimal()
    {
        var json = """
        {
            "txSignature": "txSig456def",
            "status": "pending"
        }
        """;

        var status = JsonSerializer.Deserialize<OnChainGenerationStatus>(json, JsonOptions);

        Assert.NotNull(status);
        Assert.Equal(OnChainStatus.Pending, status.Status);
        Assert.Null(status.ImageUrl);
    }

    #endregion

    #region Enum Tests

    [Theory]
    [InlineData("\"production\"", GenerationMode.Production)]
    [InlineData("\"demo\"", GenerationMode.Demo)]
    public void GenerationMode_DeserializesCorrectly(string json, GenerationMode expected)
    {
        var mode = JsonSerializer.Deserialize<GenerationMode>(json, JsonOptions);
        Assert.Equal(expected, mode);
    }

    [Theory]
    [InlineData("\"pending\"", HistoryStatus.Pending)]
    [InlineData("\"completed\"", HistoryStatus.Completed)]
    [InlineData("\"refunded\"", HistoryStatus.Refunded)]
    public void HistoryStatus_DeserializesCorrectly(string json, HistoryStatus expected)
    {
        var status = JsonSerializer.Deserialize<HistoryStatus>(json, JsonOptions);
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData("\"live\"", KeyEnvironment.Live)]
    [InlineData("\"test\"", KeyEnvironment.Test)]
    public void KeyEnvironment_DeserializesCorrectly(string json, KeyEnvironment expected)
    {
        var env = JsonSerializer.Deserialize<KeyEnvironment>(json, JsonOptions);
        Assert.Equal(expected, env);
    }

    [Theory]
    [InlineData("\"pending\"", OnChainStatus.Pending)]
    [InlineData("\"processing\"", OnChainStatus.Processing)]
    [InlineData("\"completed\"", OnChainStatus.Completed)]
    [InlineData("\"failed\"", OnChainStatus.Failed)]
    [InlineData("\"refunded\"", OnChainStatus.Refunded)]
    public void OnChainStatus_DeserializesCorrectly(string json, OnChainStatus expected)
    {
        var status = JsonSerializer.Deserialize<OnChainStatus>(json, JsonOptions);
        Assert.Equal(expected, status);
    }

    #endregion

    #region Contract Tests

    [Fact]
    public void Contract_PriceResponse_HasTreasuryField()
    {
        // This test ensures our fix is in place - treasury must be accessible
        var response = new PriceResponse
        {
            SolPrice = 185.50m,
            SlippageTolerance = 0.05m,
            UpdatedAt = "2024-01-15T12:00:00Z",
            Treasury = "9JKi6Tr7JdsTJw1zNedF5vML9GpPnjHD9DWuZq1oE6nV",
            Models = Array.Empty<ModelPrice>().ToList().AsReadOnly()
        };

        Assert.NotEmpty(response.Treasury);
    }

    [Fact]
    public void Contract_GenerateUsage_HasRequiredFields()
    {
        var usage = new GenerateUsage
        {
            CreditsUsed = 0.28m,
            BalanceRemaining = 9.72m
        };

        Assert.True(usage.CreditsUsed >= 0);
        Assert.True(usage.BalanceRemaining >= 0);
    }

    [Fact]
    public void Contract_RequiredAmount_HasAllFields()
    {
        var amount = new RequiredAmount
        {
            Sol = 0.00151m,
            Lamports = 1510000,
            Usd = 0.28m
        };

        Assert.True(amount.Sol > 0);
        Assert.True(amount.Lamports > 0);
        Assert.True(amount.Usd > 0);
    }

    #endregion

    #region Helper Methods

    private static void AssertHasJsonProperties(Type type, string[] expectedJsonNames)
    {
        var properties = type.GetProperties();
        var jsonNames = new HashSet<string>();

        foreach (var prop in properties)
        {
            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr != null)
            {
                jsonNames.Add(jsonAttr.Name);
            }
            else
            {
                // Fall back to camelCase conversion of property name
                var name = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
                jsonNames.Add(name);
            }
        }

        foreach (var expected in expectedJsonNames)
        {
            Assert.True(
                jsonNames.Contains(expected),
                $"Type {type.Name} is missing required JSON property: {expected}. Available: {string.Join(", ", jsonNames)}"
            );
        }
    }

    #endregion
}
