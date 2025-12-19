using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Websitewatcher.Middleware;
using Websitewatcher.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureFunctionsWorkerDefaults(app =>
{
    app.UseWhen<SafeBrowsingMiddleware>(context =>
    {
        return context.FunctionDefinition.Name.Equals("Register", StringComparison.OrdinalIgnoreCase);
    });
});

builder.ConfigureServices(services =>
{
    services
        .AddApplicationInsightsTelemetryWorkerService()
        .AddSingleton<PdfCreatorService>()
        .ConfigureFunctionsApplicationInsights()
        .AddSingleton<SafeBrowsingService>();
});

builder.Build().Run();
