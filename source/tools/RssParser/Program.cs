// See https://aka.ms/new-console-template for more information
    using System.Xml.Linq;

    string fileName = "abc.xml";
    var xdoc = XDocument.Load(fileName);
    XNamespace contentNs = @"http://purl.org/rss/1.0/modules/content/";
    XNamespace atomNs = @"http://www.w3.org/2005/Atom";

    if(xdoc?.Root?.Name.LocalName == "rss" || xdoc?.Root?.Name.LocalName == "channel")
    {
        var elements = xdoc?.Root?.Element("channel")?.Elements("item")?.Select(
        x => 
        new { 
            Title = x.Element("title")?.Value,
            Link = x.Element("link")?.Value,
            Published = x.Element("pubDate")?.Value,
            Description = x.Element("description")?.Value,
            Content = x?.Element(contentNs+"encoded")?.Value
        }).ToList();

        foreach (var title in elements!)
        {
            Console.WriteLine(title.Title);
            Console.WriteLine(title.Link);
            Console.WriteLine(title.Published);
            Console.WriteLine(title.Description);
            //Console.WriteLine(title.Content);
            Console.WriteLine();
        }
    }
    if(xdoc?.Root?.Name.LocalName == "feed")
    {
        var elements = xdoc?.Root?.Elements(atomNs+"entry")?.Select(
        x => 
        new { 
            Title = x.Element(atomNs+"title")?.Value,
            Link = x.Element(atomNs+"link")?.Attribute("href")?.Value,
            Published = x.Element(atomNs+"published")?.Value,
            Description = x.Element(atomNs+"summary")?.Value,
            Content = x?.Element(atomNs+"encoded")?.Value
        }).ToList();

        foreach (var title in elements!)
        {
            Console.WriteLine(title.Title);
            Console.WriteLine(title.Link);
            Console.WriteLine(title.Published);
            Console.WriteLine(title.Description);
            //Console.WriteLine(title.Content);
            Console.WriteLine();
        }
    }

    

    
    
