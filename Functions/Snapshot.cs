using System;
using System.Collections.Generic;
using System.Xml.XPath;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using Websitewatcher.Models;

namespace Websitewatcher.Functions;

public class Snapshot(ILogger<Snapshot> logger)
{
    [Function(nameof(Snapshot))]
    [SqlOutput("dbo.Snapshots", "WebsiteWatcher")]
    public IEnumerable<SnapshotRecord> Run(
        [SqlTrigger("dbo.Websites", "WebsiteWatcher")]
        IReadOnlyList<SqlChange<Website>> changes,
        FunctionContext context)
    {
        var results = new List<SnapshotRecord>();

        foreach (var change in changes)
        {
            logger.LogInformation("Detected operation: {Operation}", change.Operation);

            if (change.Operation != SqlChangeOperation.Insert)
                continue;

            if (change.Item is null)
            {
                logger.LogWarning("Insert operation received with null Item. Skipping.");
                continue;
            }

            logger.LogInformation(
                "Operation details: ID - {Id} URL - {Url}",
                change.Item.Id,
                change.Item.Url
            );

            logger.LogInformation(
                "XPathExpression raw: [{XPath}]",
                change.Item.XPathExpression
            );

            HtmlDocument doc;
            try
            {
                var web = new HtmlWeb();
                doc = web.Load(change.Item.Url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load URL: {Url}", change.Item.Url);
                results.Add(new SnapshotRecord(change.Item.Id, "Failed to load URL"));
                continue;
            }

            string content;

            if (string.IsNullOrWhiteSpace(change.Item.XPathExpression))
            {
                content = "No XPathExpression provided";
            }
            else
            {
                try
                {
                    var node = doc.DocumentNode.SelectSingleNode(change.Item.XPathExpression);
                    content = node?.InnerText?.Trim() ?? "No content";
                }
                catch (XPathException ex)
                {
                    logger.LogError(ex, "Invalid XPathExpression: {XPath}", change.Item.XPathExpression);
                    content = "Invalid XPathExpression";
                }
            }

            logger.LogInformation("Fetched content: {Content}", content);

            results.Add(new SnapshotRecord(change.Item.Id, content));
        }

        return results;
    }
}

// Maps to dbo.Snapshots
public record SnapshotRecord(Guid Id, string Content);
