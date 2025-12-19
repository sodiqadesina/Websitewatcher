using Google.Apis.Safebrowsing.v4;
using Google.Apis.Safebrowsing.v4.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace Websitewatcher.Services;

public class SafeBrowsingService(IConfiguration configuration)
{
    public async Task<(bool HasThreat, IReadOnlyList<string> Threats)> CheckAsync(string url)
    {
        var apiKey = configuration.GetValue<string>("GoogleSafeBrowsingApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, Array.Empty<string>());

        var initializer = new BaseClientService.Initializer { ApiKey = apiKey };
        using var safeBrowsing = new SafebrowsingService(initializer);

        var request = new GoogleSecuritySafebrowsingV4FindThreatMatchesRequest
        {
            Client = new GoogleSecuritySafebrowsingV4ClientInfo
            {
                ClientId = "websitewatcher",
                ClientVersion = "1.0"
            },
            ThreatInfo = new GoogleSecuritySafebrowsingV4ThreatInfo
            {
                ThreatTypes =
                [
                    "MALWARE",
                    "SOCIAL_ENGINEERING",
                    "UNWANTED_SOFTWARE",
                    "POTENTIALLY_HARMFUL_APPLICATION"
                ],
                PlatformTypes = ["ANY_PLATFORM"],
                ThreatEntryTypes = ["URL"],
                ThreatEntries =
                [
                    new GoogleSecuritySafebrowsingV4ThreatEntry { Url = url }
                ]
            }
        };

        var response = await safeBrowsing.ThreatMatches.Find(request).ExecuteAsync();

        if (response.Matches == null || response.Matches.Count == 0)
            return (false, Array.Empty<string>());

        var threats = response.Matches
            .Select(m => m.ThreatType)
            .Distinct()
            .ToList();

        return (true, threats);
    }
}

