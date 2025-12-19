using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Websitewatcher.Functions;
using Websitewatcher.Models;
using Websitewatcher.Services;

public class PdfCreator(
    ILogger<PdfCreator> logger,
    PdfCreatorService pdfCreatorService,
    SafeBrowsingService safeBrowsingService)
{
    [Function(nameof(PdfCreator))]
    public async Task Run([SqlTrigger("dbo.Websites", "WebsiteWatcher")] SqlChange<Website>[] changes)
    {
        foreach (var change in changes)
        {
            if (change.Operation != SqlChangeOperation.Insert) continue;

            var url = change.Item?.Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                logger.LogWarning("Insert received but Url is missing. WebsiteId={Id}", change.Item?.Id);
                continue;
            }

            // Await the SafeBrowsingService check
            var safeCheck = await safeBrowsingService.CheckAsync(url);
            if (safeCheck.HasThreat)
            {
                logger.LogWarning(
                    "Skipping PDF creation: unsafe URL. WebsiteId={Id} Threats={Threats}",
                    change.Item?.Id,
                    string.Join(", ", safeCheck.Threats)
                );
                continue;
            }

            var pdfStream = await pdfCreatorService.ConvertPageToPdfAsync(url);

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");
            var blobClient = new Azure.Storage.Blobs.BlobClient(
                connectionString,
                "pdfs",
                $"{change.Item!.Id}.pdf"
            );

            await blobClient.UploadAsync(pdfStream);
        }
    }
}
