using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Forms;
using System.Resources.Extensions;
using HtmlAgilityPack; // We will use  the HtmlAgilityPack library as it contains the crawler we need to extract HTML elements
using Microsoft.Msagl.Drawing; // This library is important for visualization
using Microsoft.Msagl.GraphViewerGdi; // and so is this one


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
                    // uncomment below line to recursively fetch links (can lead to large number of requests)
                    // FetchLinks(newNode); 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching links from: " + node.Url + "\n" + ex.Message);
        }
    }

    public Graph Visualize()
    {
        var graph = new Graph("webgraph");
        AddNodeToGraph(RootNode, graph, new HashSet<string>());
        return graph;
    }

    private void AddNodeToGraph(WebNode node, Graph graph, HashSet<string> visited)
    {
        if (visited.Contains(node.Url)) return;

        visited.Add(node.Url);
        graph.AddNode(node.Url);

        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!visited.Contains(linkedNode.Url))
            {
                graph.AddNode(linkedNode.Url);
                graph.AddEdge(node.Url, linkedNode.Url);
                // again, uncomment  below line to recursively add nodes (may lead to a very large graph) lol
                // AddNodeToGraph(linkedNode, graph, visited);
            }
        }
    }
}

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WebGraph webGraph = new WebGraph();
        webGraph.BuildGraph("https://youtube.com"); // this is the URL we can set as our initial input

        // graph code to visualize
        Graph graph = webGraph.Visualize();

        // We would have to create a windows form app to visualize the graph
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1(graph));
    }
}

// use windows form to display the graph
// Honestly there's a better way of doing this
public class Form1 : Form
{
    public Form1(Graph graph)
    {
        var viewer = new GViewer
        {
            Graph = graph,
            Dock = DockStyle.Fill
        };
        Controls.Add(viewer);
    }
}