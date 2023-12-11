using HtmlAgilityPack;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Msagl.Layout.MDS;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Msagl.Core.Routing;

public class WebGraph
{
    private readonly HtmlWeb web;
    public WebNode RootNode { get; private set; }
    public HashSet<string> VisitedDomains { get; private set; }


    public WebGraph()
    {
        web = new HtmlWeb();
        VisitedDomains = new HashSet<string>();
    }

    public void BuildGraph(string rootUrl)
    {
        RootNode = new WebNode(rootUrl);
        var visitedDomains = new HashSet<string>();
        FetchLinks(RootNode, visitedDomains);
    }

    // added parallel processessing
    public void FetchLinks(WebNode node, HashSet<string> visitedDomains)
    {
        try
        {
            // load the document from the URL
            var doc = web.Load(node.Url);
            var links = doc.DocumentNode.SelectNodes("//a[@href]"); // select all hyperlink elements

            if (links != null)
            {
                var hrefValues = links.Select(link => link.GetAttributeValue("href", string.Empty)) // extract hrefs and filter out empty and non-http links
                                      .Where(href => !string.IsNullOrEmpty(href) && href.StartsWith("http"))
                                      .ToList(); // Ensure that the collection is not modified during enumeration

                var newNodes = new ConcurrentBag<WebNode>(); // Thread-safe collection for new nodes

                // process links in parlalel
                Parallel.ForEach(hrefValues, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, hrefValue =>
                {
                    try
                    {
                        var domain = new Uri(hrefValue).Host;
                        if (!visitedDomains.Contains(domain))
                        {
                            lock (visitedDomains)
                            {
                                visitedDomains.Add(domain); // add new domain to visited list
                            }

                            var alreadyExists = false;
                            lock (node.LinkedNodes)
                            {
                                alreadyExists = node.LinkedNodes.Any(n => n.Url == hrefValue); // check if node already exists
                            }

                            if (!alreadyExists)
                            {
                                newNodes.Add(new WebNode(hrefValue)); // add new node to collection
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine("Error processing link: " + hrefValue + "\n" + innerEx.Message);
                    }
                });

                // Add the new nodes after the parallel processing
                foreach (var newNode in newNodes)
                {
                    lock (node.LinkedNodes)
                    {
                        node.LinkedNodes.Add(newNode);
                    }
                }

                /*foreach (var newNode in newNodes)
                {
                    if (!visitedDomains.Contains(newNode.Url))
                    {
                        FetchLinks(newNode, visitedDomains);
                    }
                }
                */
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


        // edge routing seetings that are applied to MDS Layout
        var edgeRouting = new EdgeRoutingSettings
        {
            EdgeRoutingMode = EdgeRoutingMode.SplineBundling
        };

        // use MDS layout settings
        var mdsLayout = new MdsLayoutSettings
        {
            EdgeRoutingSettings = edgeRouting
        };

        graph.LayoutAlgorithmSettings = mdsLayout;

        AddNodeToGraph(RootNode, graph, new HashSet<string>());

        ApplyClustering(graph);

        return graph;
    }

    private void AddNodeToGraph(WebNode node, Graph graph, HashSet<string> visitedDomains) // simplified this function
    {
        // check if the node's URL is already processed to avoid duplicate processing
        if (visitedDomains.Contains(node.Url)) return;

        // mark the current node's URL as visited
        visitedDomains.Add(node.Url);

        // When calling StyleNode, pass the number of linked nodes
        var msaglNode = StyleNode(node.Url, graph, node.LinkedNodes.Count);

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
    public static Node StyleNode(string url, Graph graph, int linkedNodeCount = 0) // Include linkedNodeCount as a parameter
    {
        Console.WriteLine($"Styling node: {url}, Linked Node Count: {linkedNodeCount}"); // debugging

        var msaglNode = graph.AddNode(url);
        Uri uri = new Uri(url);

        byte redShade = 0, greenShade = 0, blueShade = 0;

        // Define the thresholds
        const int maxLinkedNodesBlue = 10;
        const int maxLinkedNodesGreen = 21;
        const int maxLinkedNodesRed = 32;
        const int maxLinkedNodesPurple = 43;

        if (linkedNodeCount <= maxLinkedNodesBlue)
        {
            // Blue intensity for up to 10 linked nodes
            blueShade = (byte)(255 * Math.Min(linkedNodeCount / (double)maxLinkedNodesBlue, 1.0));
        }
        else if (linkedNodeCount <= maxLinkedNodesGreen)
        {
            // Green intensity for 11-21 linked nodes
            greenShade = (byte)(255 * Math.Min((linkedNodeCount - maxLinkedNodesBlue) / (double)(maxLinkedNodesGreen - maxLinkedNodesBlue), 1.0));
            blueShade = (byte)(255 - greenShade);
        }
        else if (linkedNodeCount <= maxLinkedNodesRed)
        {
            // Red intensity for 22-32 linked nodes
            redShade = (byte)(255 * Math.Min((linkedNodeCount - maxLinkedNodesGreen) / (double)(maxLinkedNodesRed - maxLinkedNodesGreen), 1.0));
            greenShade = (byte)(255 - redShade);
        }
        else if (linkedNodeCount <= maxLinkedNodesPurple)
        {
            // Purple intensity for 33-43 linked nodes
            redShade = (byte)(255 * Math.Min((linkedNodeCount - maxLinkedNodesRed) / (double)(maxLinkedNodesPurple - maxLinkedNodesRed), 1.0));
            blueShade = (byte)(255 - redShade); // Making purple by combining red and blue
        }
        else
        {
            // Purple color for more than 43 linked nodes
            // This can be adjusted as needed
            redShade = 255;
            greenShade = 0;
            blueShade = 255;
        }

        msaglNode.Attr.FillColor = new Color(redShade, greenShade, blueShade);

        msaglNode.Attr.Shape = Shape.Circle;
        msaglNode.Label.FontSize = 8; // add + linkedNodeCount to increase font size based on linkedNodeCount
        msaglNode.LabelText = uri.Host;
        msaglNode.Label.FontColor = Color.White;

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