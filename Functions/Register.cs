using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Websitewatcher.Models;

namespace Websitewatcher.Functions;

public class Register(ILogger<Register> logger)
{
    [Function(nameof(Register))]
    public async Task<RegisterResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext context)
    {
        logger.LogInformation("Register called.");

        // If middleware decided to block, return 400 with threats (and NO SQL insert)
        var blocked = context.Items.TryGetValue("Blocked", out var bObj) && bObj is bool b && b;

        if (blocked)
        {
            var error = context.Items.TryGetValue("BlockError", out var eObj) ? eObj?.ToString() : "Request blocked.";
            var threats = context.Items.TryGetValue("BlockThreats", out var tObj) && tObj is string[] arr ? arr : Array.Empty<string>();

            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error,
                threats
            });

            return new RegisterResult { Response = bad, Website = null };
        }

        // Use the body read by middleware (do NOT read req.Body again)
        if (!context.Items.TryGetValue("RawBody", out var rawObj) || rawObj is not string rawBody || string.IsNullOrWhiteSpace(rawBody))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Request body is missing or could not be read." });
            return new RegisterResult { Response = bad, Website = null };
        }

        Website? site;
        try
        {
            site = JsonSerializer.Deserialize<Website>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Invalid JSON body." });
            return new RegisterResult { Response = bad, Website = null };
        }

        // URL comes from middleware validation
        var url = context.Items.TryGetValue("ValidatedUrl", out var urlObj) ? urlObj?.ToString() : site?.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "You must specify the url" });
            return new RegisterResult { Response = bad, Website = null };
        }

        site ??= new Website();
        site.Id = Guid.NewGuid();
        site.Url = url;

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(site);

        // SQL insert happens only when Website != null
        return new RegisterResult { Response = ok, Website = site };
    }
}
