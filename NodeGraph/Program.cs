using System;
using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack;

public class WebNode
{
    public string Url { get; set; }
    public List<WebNode> LinkedNodes { get; set; }

    public WebNode(string url)
    {
        Url = url;
        LinkedNodes = new List<WebNode>();
    }
}

public class WebGraph
{
    private readonly HtmlWeb web;
    public WebNode RootNode { get; private set; }

    public WebGraph()
    {
        web = new HtmlWeb();
    }

    public void BuildGraph(string rootUrl)
    {
        RootNode = new WebNode(rootUrl);
        FetchLinks(RootNode);
    }

    private void FetchLinks(WebNode node)
    {
        try
        {
            var doc = web.Load(node.Url);
            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var hrefValue = link.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(hrefValue) && hrefValue.StartsWith("http"))
                {
                    var newNode = new WebNode(hrefValue);
                    node.LinkedNodes.Add(newNode);
                    // Optionally, you can recursively call FetchLinks for newNode to build a deeper graph
                    // FetchLinks(newNode); 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching links from: " + node.Url + "\n" + ex.Message);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        WebGraph graph = new WebGraph();
        graph.BuildGraph("https://youtube.com"); // Replace with the desired URL

        // Example: Print all links from the root
        foreach (var link in graph.RootNode.LinkedNodes)
        {
            Console.WriteLine(link.Url);
        }
    }
}
