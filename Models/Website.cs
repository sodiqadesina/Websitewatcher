using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Websitewatcher.Models;

public class Website
{
    public Guid Id { get; set; }
    public string? Url { get; set; }
    public string? XPathExpression { get; set; }
}