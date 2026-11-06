using BrowserSolver;

using var client = new BrowserSolverClient(); // reads BROWSERSOLVER_API_KEY
try
{
    var response = await client.RequestAsync("GET", "/v1/usage");
    Console.WriteLine(response.Text);
}
catch (BrowserSolverApiException exception)
{
    Console.Error.WriteLine($"HTTP {exception.StatusCode}: {exception.Message}");
}
