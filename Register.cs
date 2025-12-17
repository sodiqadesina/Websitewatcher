using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Websitewatcher;

public class Register(ILogger<Register> logger)
{
    private readonly ILogger<Register> _logger = logger;

    [Function(nameof(Register))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}


public class Website { 

    public Guid Id { get; set; }
    public string Url { get; set; }
    public string? XPathExpression { get; set; }
}