# PeerCat .NET SDK

Official .NET SDK for the [PeerCat](https://peerc.at) AI image generation API.

[![NuGet](https://img.shields.io/nuget/v/PeerCat.svg)](https://www.nuget.org/packages/PeerCat)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0-blue)](https://dotnet.microsoft.com/)

## Installation

```bash
dotnet add package PeerCat
```

Or via Package Manager:

```powershell
Install-Package PeerCat
```

## Quick Start

```csharp
using PeerCat;

// Create client with your API key
var client = new PeerCatClient("pcat_live_your_api_key");

// Generate an image
var result = await client.GenerateAsync("A beautiful sunset over mountains");

Console.WriteLine($"Image URL: {result.ImageUrl}");
Console.WriteLine($"Credits used: {result.Usage.CreditsUsed}");
```

## Features

- Full async/await support
- Automatic retry with exponential backoff
- Typed error handling with specific exception types
- Support for .NET 6.0, 7.0, 8.0, and .NET Standard 2.0
- Demo mode for testing without spending credits
- On-chain SOL payment support

## Configuration

```csharp
var client = new PeerCatClient(new PeerCatOptions
{
    ApiKey = "pcat_live_your_api_key",
    BaseUrl = "https://api.peerc.at/v1",  // Optional
    Timeout = TimeSpan.FromSeconds(60),    // Optional, default: 30s
    MaxRetries = 5                          // Optional, default: 3
});
```

## API Methods

### Image Generation

```csharp
// Simple generation
var result = await client.GenerateAsync("A cat wearing a hat");

// With options
var result = await client.GenerateAsync(new GenerateParams
{
    Prompt = "A cat wearing a hat",
    Model = "imagen-3",
    Mode = GenerationMode.Production,
    Options = new Dictionary<string, object>
    {
        ["negative_prompt"] = "blurry, low quality"
    }
});
```

### Demo Mode

Test the API without spending credits:

```csharp
var result = await client.GenerateAsync(
    prompt: "Test prompt",
    mode: GenerationMode.Demo
);
// Returns placeholder image, no credits charged
```

### Models & Pricing

```csharp
// Get available models
var models = await client.GetModelsAsync();
foreach (var model in models)
{
    Console.WriteLine($"{model.Id}: {model.Name} - ${model.PriceUsd}");
}

// Get current prices with SOL conversion
var prices = await client.GetPricesAsync();
Console.WriteLine($"SOL/USD: {prices.SolPrice}");
```

### Account Management

```csharp
// Get balance
var balance = await client.GetBalanceAsync();
Console.WriteLine($"Credits: ${balance.Credits}");
Console.WriteLine($"Total generated: {balance.TotalGenerated}");

// Get usage history
var history = await client.GetHistoryAsync(limit: 10);
foreach (var item in history.Items)
{
    Console.WriteLine($"{item.CreatedAt}: {item.Endpoint} - ${item.CreditsUsed}");
}
```

### API Key Management

```csharp
// List keys
var keys = await client.ListKeysAsync();

// Revoke a key
await client.RevokeKeyAsync("key_id_to_revoke");
```

### On-Chain Payments

Pay with SOL instead of API credits:

```csharp
// Submit prompt to get payment instructions
var submission = await client.SubmitPromptAsync("A mountain landscape");

Console.WriteLine($"Payment address: {submission.PaymentAddress}");
Console.WriteLine($"Amount: {submission.RequiredAmount.Sol} SOL");
Console.WriteLine($"Memo: {submission.Memo}");

// After sending SOL transaction, check status
var status = await client.GetGenerationStatusAsync(txSignature);
if (status.Status == OnChainStatus.Completed)
{
    Console.WriteLine($"Image: {status.ImageUrl}");
}
```

## Error Handling

The SDK provides typed exceptions for different error conditions:

```csharp
try
{
    var result = await client.GenerateAsync("prompt");
}
catch (AuthenticationException ex)
{
    // Invalid or missing API key (401)
    Console.WriteLine($"Auth error: {ex.Message}");
}
catch (InsufficientCreditsException ex)
{
    // Not enough credits (402)
    Console.WriteLine($"Need more credits: {ex.Message}");
}
catch (RateLimitException ex)
{
    // Too many requests (429)
    Console.WriteLine($"Rate limited, retry after: {ex.RetryAfter}s");
}
catch (InvalidRequestException ex)
{
    // Bad request parameters (400)
    Console.WriteLine($"Invalid param '{ex.Param}': {ex.Message}");
}
catch (NotFoundException ex)
{
    // Resource not found (404)
    Console.WriteLine($"Not found: {ex.Message}");
}
catch (PeerCatException ex)
{
    // Other API errors
    Console.WriteLine($"Error ({ex.Code}): {ex.Message}");

    // Check if retryable
    if (ex.IsRetryable)
    {
        // Retry logic
    }
}
```

## Dependency Injection

Register the client with ASP.NET Core DI:

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<PeerCatClient>(sp =>
    new PeerCatClient(Configuration["PeerCat:ApiKey"]!));

// Or with options
services.AddSingleton<PeerCatClient>(sp =>
    new PeerCatClient(new PeerCatOptions
    {
        ApiKey = Configuration["PeerCat:ApiKey"]!,
        Timeout = TimeSpan.FromSeconds(60)
    }));
```

## Cancellation Support

All async methods support cancellation:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    var result = await client.GenerateAsync("prompt", cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request cancelled");
}
```

## Custom HttpClient

For advanced scenarios, you can provide your own HttpClient:

```csharp
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-Custom-Header", "value");

var client = new PeerCatClient(new PeerCatOptions
{
    ApiKey = "your_key",
    HttpClient = httpClient
});
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Links

- [PeerCat Website](https://peerc.at)
- [API Documentation](https://docs.peerc.at)
- [GitHub Repository](https://github.com/peercat/peercat-sdk-csharp)
