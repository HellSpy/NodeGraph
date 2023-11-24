using HtmlAgilityPack;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;

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
        var visitedDomains = new HashSet<string>();
        FetchLinks(RootNode, visitedDomains);
    }

    public void FetchLinks(WebNode node, HashSet<string> visitedDomains)
    {
        try
        {
            var doc = web.Load(node.Url);
            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var hrefValue = link.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(hrefValue) && hrefValue.StartsWith("http"))
                {
                    var domain = new Uri(hrefValue).Host;
                    if (!visitedDomains.Contains(domain)) // Check if the domain is not visited
                    {
                        visitedDomains.Add(domain); // Add domain to the visited list

                        if (!node.LinkedNodes.Any(n => n.Url == hrefValue)) // Check if the link is already added
                        {
                            var newNode = new WebNode(hrefValue);
                            node.LinkedNodes.Add(newNode);
                            // FetchLinks(newNode, visitedDomains); // Uncomment for recursive fetching
                        }
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

        // use MDS layout settings
        var mdsLayout = new MdsLayoutSettings();

        // You can adjust MDS layout settings here
        // For example:
        // mdsLayout.IterationLimit = 100;

        graph.LayoutAlgorithmSettings = mdsLayout;

        AddNodeToGraph(RootNode, graph, new HashSet<string>());

        // Apply clustering if needed
        ApplyClustering(graph);

        // here you can handle additional layout adjustments or post-processing, feel free to add any other optional stuff

        return graph;
    }

    private void AddNodeToGraph(WebNode node, Graph graph, HashSet<string> visitedDomains) // simplified this function
    {
        // check if the node's URL is already processed to avoid duplicate processing
        if (visitedDomains.Contains(node.Url)) return;

        // mark the current node's URL as visited
        visitedDomains.Add(node.Url);

        // apply styling to the current node and add it to the graph
        var msaglNode = StyleNode(node.Url, graph);

        // iterate over each linked node of the current node
        foreach (var linkedNode in node.LinkedNodes)
        {
            // check if the linked node is not already visited
            if (!visitedDomains.Contains(linkedNode.Url))
            {
                // apply styling to the linked node and add it to the graph
                StyleNode(linkedNode.Url, graph);

                // create a directed edge from the current node to the linked node
                var edge = graph.AddEdge(node.Url, linkedNode.Url);

                // customize the appearance of the edge
                edge.Attr.Color = Color.Black; // Set the color of the edge
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal; // Set the style of the arrowhead
            }
        }
    }

    // added a new function for styling the initial linked nodes
    public static Node StyleNode(string url, Graph graph) // set to public static so that it can be accessed by other files
    {
        var msaglNode = graph.AddNode(url);
        Uri uri = new Uri(url);

        // Check for an exact match in the list of top domains against the full URL
        if (TopDomains.Domains.Any(d => string.Equals(d, uri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            // Special styling for popular domains
            msaglNode.Attr.FillColor = Color.Red;
            msaglNode.Label.FontSize = 8; // larger font size for popular domains
            msaglNode.Attr.Shape = Shape.Circle;
        }
        else
        {
            // Default styling for other domains
            msaglNode.Attr.FillColor = Color.LightGray;
            msaglNode.Attr.Shape = Shape.Circle;
            msaglNode.Label.FontSize = 8;
        }

        msaglNode.LabelText = uri.Host; // shortened label
        return msaglNode;
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