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

                /* Uncomment this section for recursive link fetching
                foreach (var newNode in newNodes)
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
            // RemoveOverlaps = true, // Enable overlap removal
            // ScaleX = 1.0, // Set X scaling
            // ScaleY = 1.0, // Set Y scaling
            // PackingAspectRatio = 1.0, // Set packing aspect ratio
            // PivotNumber = 50 // Set the number of pivots
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

        // Calculate the color based on the number of linked nodes
        int maxLinkedNodes = 10; // You can adjust this based on your needs
        double intensity = Math.Min(linkedNodeCount / (double)maxLinkedNodes, 1.0);
        byte blueShade = (byte)(255 * intensity); // Ensure blueShade is a byte

        msaglNode.Attr.FillColor = new Color(0, 0, blueShade); // Darker blue for more connections

        msaglNode.Attr.Shape = Shape.Circle;
        msaglNode.Label.FontSize = 8; // You can adjust the font size as needed
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