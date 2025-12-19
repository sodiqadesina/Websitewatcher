using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Websitewatcher.Services;

namespace Websitewatcher.Middleware;

public class SafeBrowsingMiddleware(SafeBrowsingService safeBrowsingService) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();
        if (req is null)
        {
            await next(context);
            return;
        }

        // Only run for Register
        if (!context.FunctionDefinition.Name.Equals("Register", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Read body ONCE here
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        context.Items["RawBody"] = body;

        var url = TryExtractUrl(body);
        context.Items["ValidatedUrl"] = url ?? "";

        // Basic validation
        if (string.IsNullOrWhiteSpace(url))
        {
            context.Items["Blocked"] = true;
            context.Items["BlockError"] = "You must specify the url";
            context.Items["BlockThreats"] = Array.Empty<string>();
            await next(context);
            return;
        }

        if (!IsValidUrl(url))
        {
            context.Items["Blocked"] = true;
            context.Items["BlockError"] = "The specified url is not valid";
            context.Items["BlockThreats"] = Array.Empty<string>();
            await next(context);
            return;
        }

        // Google Safe Browsing
        var safe = await safeBrowsingService.CheckAsync(url);
        if (safe.HasThreat)
        {
            context.Items["Blocked"] = true;
            context.Items["BlockError"] = "The specified url is not safe.";
            context.Items["BlockThreats"] = safe.Threats.ToArray();
            await next(context);
            return;
        }

        context.Items["Blocked"] = false;
        await next(context);
    }

    private static string? TryExtractUrl(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("url", out var p1)) return p1.GetString();
            if (root.TryGetProperty("Url", out var p2)) return p2.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
