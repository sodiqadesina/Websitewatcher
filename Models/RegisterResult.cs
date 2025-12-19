using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json.Serialization;

namespace Websitewatcher.Models;

public class RegisterResult
{
    [HttpResult] // <— IMPORTANT: tells Functions this is the HTTP response

    public HttpResponseData? Response { get; set; }

    [SqlOutput("dbo.Websites", "WebsiteWatcher")]
    public Website? Website { get; set; }
}
