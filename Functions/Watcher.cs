using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using Websitewatcher.Services;

namespace Websitewatcher.Functions;

public class Watcher(ILogger<Watcher> logger, PdfCreatorService pdfCreatorService)
{

    private const string SqlInputQuery = @"SELECT w.Id, w.Url, w.XPathExpression, s.Content AS LatestContent
                                            FROM dbo.Websites w
                                            LEFT JOIN dbo.Snapshots s ON w.Id = s.Id
                                            WHERE s.Timestamp = (SELECT MAX(Timestamp) FROM dbo.Snapshots WHERE Id = w.Id)";

    [Function(nameof(Watcher))]
    [SqlOutput("dbo.Snapshots", "WebsiteWatcher")]
    public async Task<SnapshotRecord> Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer,
        [SqlInput(SqlInputQuery, "WebsiteWatcher")] IReadOnlyList<WebsiteModel> websites)
    {
        logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        SnapshotRecord? result = null;

        foreach (var website in websites)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(website.Url);

            var divWithContent = doc.DocumentNode.SelectSingleNode(website.XPathExpression);

            var content = divWithContent != null ? divWithContent.InnerText.Trim() : "No content found";


            var contentHasChanged = content != website.LatestContent;

            if (contentHasChanged)
            {
                logger.LogInformation("Content has changed for Website ID: {Id}", website.Id);

                var newPdf = await pdfCreatorService.ConvertPageToPdfAsync(website.Url);
                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");

                var blobClient = new Azure.Storage.Blobs.BlobClient(
                    connectionString,
                    "pdfs",
                    $"{website.Id}-{DateTime.UtcNow:MMddyyyyhhmmss}.pdf"

                );

                await blobClient.UploadAsync(newPdf);

                logger.LogInformation("Uploaded new PDF for Website ID: {Id}", website.Id);

                result = new SnapshotRecord(website.Id, content);
            }
            else
            {
                logger.LogInformation("No change in content for Website ID: {Id}", website.Id);


            }
        }

        return result!;

    }




    public class WebsiteModel
    {
        public Guid Id { get; set; }
        public string? Url { get; set; }
        public string? XPathExpression { get; set; }

        public string? LatestContent { get; set; }
    }
}