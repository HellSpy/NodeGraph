using HtmlAgilityPack;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Msagl.Layout.Layered;

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

    public void FetchLinks(WebNode node)
    {
        try
        {
            var doc = web.Load(node.Url);
            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var hrefValue = link.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(hrefValue) && hrefValue.StartsWith("http"))
                {
                    if (!node.LinkedNodes.Any(n => n.Url == hrefValue)) // Check if the link is already added
                    {
                        var newNode = new WebNode(hrefValue);
                        node.LinkedNodes.Add(newNode);
                        // uncomment below line to recursively fetch links (can lead to large number of requests)
                        // FetchLinks(newNode); 
                    }
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

        // adjust layout settings as needed
        graph.Attr.LayerDirection = LayerDirection.LR; // From left to right
        graph.LayoutAlgorithmSettings.NodeSeparation = 70;
        graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.SplineBundling;

        AddNodeToGraph(RootNode, graph, new HashSet<string>());

        // optional: you can apply clustering here based on whatever criteria we make up
        ApplyClustering(graph);

        return graph;
    }

    private void AddNodeToGraph(WebNode node, Graph graph, HashSet<string> visited) //WARNING, RECURSION HAS BEEN OMITTED FROM THIS FUNCTION
    {
        if (visited.Contains(node.Url)) return;

        visited.Add(node.Url);
        var msaglNode = graph.AddNode(node.Url);

        // Customize the nodes based on domain
        Uri uri = new Uri(node.Url);
        if (uri.Host.Contains("youtube.com"))
        {
            msaglNode.Attr.FillColor = Color.Red;
            msaglNode.Attr.Shape = Shape.Diamond;
        }
        else
        {
            msaglNode.Attr.FillColor = Color.LightBlue;
            msaglNode.Attr.Shape = Shape.Box;
        }

        // Set node label with the shortened URL or title
        msaglNode.LabelText = uri.Host;

        // Iterate over linked nodes
        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!visited.Contains(linkedNode.Url))
            {
                var linkedMsaglNode = graph.AddNode(linkedNode.Url);
                linkedMsaglNode.Attr.FillColor = Color.LightGray; // Default color for linked nodes
                linkedMsaglNode.Attr.Shape = Shape.Circle;
                linkedMsaglNode.LabelText = new Uri(linkedNode.Url).Host; // Shortened label for linked nodes

                // Create a directed edge
                var edge = graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = Color.Black;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }
    }
    private void ApplyClustering(Graph graph)
    {
        // Basic clustering logic... (MOSTLY INCOMPLETE)
        var clusters = new Dictionary<string, Subgraph>();

        foreach (var node in graph.Nodes)
        {
            string clusterKey = GetClusterKey(node);
            if (!clusters.ContainsKey(clusterKey))
            {
                var subgraph = new Subgraph(clusterKey);
                clusters[clusterKey] = subgraph;
                graph.RootSubgraph.AddSubgraph(subgraph);
            }
            clusters[clusterKey].AddNode(node);
        }
    }
    private string GetClusterKey(Node node)
    {
        // Using the node's Id (which stores the URL) for clustering logic.
        // This example clusters by the first letter of the node's Id.
        // Ensure that the Id is not null or empty to avoid exceptions.
        if (!string.IsNullOrEmpty(node.Id) && node.Id.Length > 0)
        {
            return node.Id.Substring(0, 1).ToUpper(); // Use ToUpper() to standardize the cluster key if needed.
        }
        else
        {
            return string.Empty; // Or a default cluster key if the Id is not set or is empty.
        }
    }

}