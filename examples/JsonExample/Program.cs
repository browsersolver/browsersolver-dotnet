using BrowserSolver;

using var client = new BrowserSolverClient(); // reads BROWSERSOLVER_API_KEY
var response = await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com" });
Console.WriteLine(response.Json);
