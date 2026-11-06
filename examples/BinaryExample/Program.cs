using BrowserSolver;

using var client = new BrowserSolverClient(); // reads BROWSERSOLVER_API_KEY
var response = await client.WebsiteScreenshotAsync(new WebsiteScreenshotParams { Url = "https://example.com", Format = "png" });
if (response.IsJson)
{
    Console.WriteLine(response.Json);
}
else
{
    await File.WriteAllBytesAsync("screenshot.png", response.Bytes);
    Console.WriteLine($"Saved screenshot.png ({response.ContentType}, {response.Bytes.Length} bytes)");
}
