# BrowserSolver .NET SDK

The official .NET SDK for the [BrowserSolver](https://www.browsersolver.com) web unlocker API.

Unlock, render, crawl and screenshot any website — anti-bot bypass and technology detection through one API.

The SDK exposes **9 typed async methods**, one per documented endpoint (with `required` properties checked at compile time), plus a generic `RequestAsync`.

## Requirements

- .NET 8 or newer

## Installation

```sh
dotnet add package BrowserSolver
```

## Get your API key

[Sign up for BrowserSolver](https://app.browsersolver.com/register) or sign in, then copy your API key from the [dashboard](https://app.browsersolver.com). `new BrowserSolverClient()` reads the `BROWSERSOLVER_API_KEY` environment variable.

## Quick start

```csharp
using BrowserSolver;

using var client = new BrowserSolverClient();
var response = await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com" });
Console.WriteLine(response.Json);
```

Parameter classes mirror the [API reference](https://app.browsersolver.com/openapi). Use `response.Deserialize<MyType>()` to map the JSON onto your own types.

## Binary responses

Endpoints that return a file (image, PDF…) give you the bytes; `IsJson` tells both cases apart:

```csharp
var response = await client.WebsiteScreenshotAsync(new WebsiteScreenshotParams { Url = "https://example.com", Format = "png" });
if (!response.IsJson)
{
    await File.WriteAllBytesAsync("screenshot.png", response.Bytes);
}
```

## Async jobs

Any call accepts `is_async: true` (useful past the 60-second server limit): the response is then a `job_id`. `WaitForJobAsync` polls `GET /v1/jobs/{id}` until a terminal status (`CompletedJob`, `Error`, `NotFound`, `BadRequest`):

```csharp
var submitted = await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com", IsAsync = true });
var job = await client.WaitForJobAsync(
    submitted.Json.GetProperty("job_id").GetString()!,
    onProgress: j => Console.WriteLine($"{j.GetProperty("status")} {j.GetProperty("progress")}%"));
```

## GET or POST

Methods use GET when the endpoint supports it (query string). POST sends the same parameters as JSON:

```csharp
await client.WebsiteCrawlerAsync(new WebsiteCrawlerParams { Query = "https://books.toscrape.com/" }, new RequestOptions { Method = "POST" });
```

## Any endpoint

```csharp
var response = await client.RequestAsync("GET", "/v1/usage");
```

## Errors

```csharp
try
{
    await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com" });
}
catch (BrowserSolverApiException exception)
{
    Console.Error.WriteLine($"{exception.StatusCode}: {exception.Message}");
}
```

| Exception | When |
| --- | --- |
| `BrowserSolverConfigurationException` | Missing API key, invalid options or unsupported method. |
| `BrowserSolverSerializationException` | A required parameter is missing or a value can't be sent. |
| `BrowserSolverTransportException` | Network failure or timeout. |
| `BrowserSolverApiException` | Non-2xx status (`StatusCode`, `ErrorMessage`, `ResponseBody`, `RawResponse`). |
| `BrowserSolverResponseDecodingException` | A success response could not be decoded. |

All of them derive from `BrowserSolverException`. Caller cancellation surfaces as `OperationCanceledException`.

## Retries

Requests are retried up to 3 times after a network error, a `408`, `500`, `502`, `503` or `504`, or a rate limit — which the API returns as `401` (or `429`) with a `Rate limit exceeded` message. Per-key quota errors (`…: quota daily`) are not retried, nor are `400`, `402`, `404` or client-side timeouts. The delay starts at 1 second and grows 1.5× per retry (capped at 30 seconds); a `Retry-After` header wins.

```csharp
using var client = new BrowserSolverClient(new BrowserSolverClientOptions { MaxRetries = 5, RetryDelay = TimeSpan.FromMilliseconds(500) });
await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com" }, new RequestOptions { MaxRetries = 0 }); // one request without retries
```

## Batch requests

`BatchAsync` runs a function per item with at most 10 in flight (`concurrency`) and returns one `BatchResult` per item, in input order. A failing item never stops the others.

```csharp
string[] inputs = { "https://example.com", "https://www.browsersolver.com" };
var results = await client.BatchAsync(inputs, (value, ct) =>
    client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = value }, cancellationToken: ct), concurrency: 5);
foreach (var result in results)
{
    if (result.Ok)
    {
        Console.WriteLine(result.Value!.Json);
    }
    else
    {
        Console.Error.WriteLine($"{inputs[result.Index]}: {result.Error!.Message}");
    }
}
```

| Constant | Default | Description |
| --- | --- | --- |
| `BrowserSolverClient.DefaultConcurrency` | `10` | Tasks run at the same time by batch |
| `BrowserSolverClient.DefaultTimeout` | `TimeSpan.FromSeconds(120)` | Request timeout |
| `BrowserSolverClient.MaxRetries` | `3` | Retry attempts after a network error, a 408/5xx or a rate limit |
| `BrowserSolverClient.RetryBackoffFactor` | `1.5` | Exponential backoff multiplier |
| `BrowserSolverClient.DefaultRetryDelay` | `TimeSpan.FromSeconds(1)` | Delay before the first retry |

## Configuration

```csharp
using var client = new BrowserSolverClient(new BrowserSolverClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("BROWSERSOLVER_API_KEY"),
    Timeout = TimeSpan.FromSeconds(60), // default 120 s
    HttpClient = myHttpClient,           // optional, you keep ownership
});
```

## Endpoints

9 methods, generated from the [OpenAPI document](https://app.browsersolver.com/openapi).

| Method | HTTP | Description |
| --- | --- | --- |
| `JobsAsync` | GET `/v1/jobs/{id}` | Poll the status, progress (0–100) and result of a job started with is_async=true. Polling is free. Poll until status is terminal: Completed… |
| `StatsAsync` | GET `/v1/stats` | Infrastructure health for an engine or endpoint over the last hour. Not scoped to your account or tenant. Enabled per account: HTTP 403 mea… |
| `UsageAsync` | GET `/v1/usage` | Retrieve real-time usage statistics and credit balance for your BrowserSolver account. |
| `WebsiteAntibotAsync` | GET `/v1/website/antibot` | Single-pass scan that names the bot-protection vendor in front of a URL so you can choose the right BrowserSolver fetch tier next. |
| `WebsiteCrawlerAsync` | GET / POST `/v1/website/crawler` | Fast HTTP pull of public HTML with no browser session. Ideal when the first response already contains the content you need. |
| `WebsiteRenderingAsync` | GET `/v1/website/rendering` | Run a managed browser session against a public URL and collect the HTML after client-side scripts finish. Prefer Crawler for static markup;… |
| `WebsiteScreenshotAsync` | GET / POST `/v1/website/screenshot` | Turn any public URL into a PNG, JPEG, WebP, or PDF asset rendered by BrowserSolver. No local Chromium fleet to operate. |
| `WebsiteTechnologyAsync` | GET / POST `/v1/website/technology` | Map CMS, frameworks, analytics, CDN, and security tooling exposed by a public URL. Choose simple lists or expert metadata with confidence s… |
| `WebsiteWebunlockerAsync` | GET / POST `/v1/website/webunlocker` | BrowserSolver's highest HTML fetch tier for retail and marketplace hosts on your account whitelist. Solves persistent bot challenges that s… |

## All BrowserSolver SDKs

| SDK | Package | Install |
| --- | --- | --- |
| Node.js | [`@browsersolver/sdk`](https://www.npmjs.com/package/@browsersolver/sdk) | `npm install @browsersolver/sdk` |
| Python | [`browsersolver`](https://pypi.org/project/browsersolver/) | `pip install browsersolver` |
| Go | [`browsersolver-go`](https://pkg.go.dev/github.com/browsersolver/browsersolver-go) | `go get github.com/browsersolver/browsersolver-go` |
| .NET | [`BrowserSolver`](https://www.nuget.org/packages/BrowserSolver) | `dotnet add package BrowserSolver` |
| Ruby | [`browsersolver`](https://rubygems.org/gems/browsersolver) | `gem install browsersolver` |
| PHP | [`browsersolver/sdk`](https://packagist.org/packages/browsersolver/sdk) | `composer require browsersolver/sdk` |
| Java | [`browsersolver-java`](https://github.com/browsersolver/browsersolver-java) | build from source, see its README |
| CLI | [`@browsersolver/cli`](https://www.npmjs.com/package/@browsersolver/cli) | `npm install -g @browsersolver/cli` |
| MCP server | [`@browsersolver/mcp`](https://www.npmjs.com/package/@browsersolver/mcp) | `npx -y @browsersolver/mcp` |

## License

MIT — see [LICENSE](LICENSE).
