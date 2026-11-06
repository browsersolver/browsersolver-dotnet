using BrowserSolver;

using var client = new BrowserSolverClient(); // reads BROWSERSOLVER_API_KEY
var submitted = await client.WebsiteWebunlockerAsync(new WebsiteWebunlockerParams { Query = "https://example.com", IsAsync = true });
var job = await client.WaitForJobAsync(
    submitted.Json.GetProperty("job_id").GetString()!,
    onProgress: j => Console.WriteLine($"{j.GetProperty("status")} {j.GetProperty("progress")}%"));
Console.WriteLine(job.Json);
