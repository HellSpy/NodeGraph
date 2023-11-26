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


        //edge routing settings (which is applied to MdsLayout)
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


    /*      
        // uncomment this section if you just want the exact domain and not subdomains. make sure to replace if (TopDomains)
            right above special styling

        // Check if the domain is in the list of popular domains
        bool isPopularDomain = TopDomains.Domains.Any(d => string.Equals(d, uri.Host, StringComparison.OrdinalIgnoreCase));

        // Check if the URL is exactly the domain (without any additional path or query)
        bool isExactDomainMatch = (url.Equals($"http://{uri.Host}", StringComparison.OrdinalIgnoreCase) || url.Equals($"https://{uri.Host}", StringComparison.OrdinalIgnoreCase));

        if (isPopularDomain && isExactDomainMatch)
    */

        string domain = uri.Host.ToLower(); // Extract the domain from the URL and convert it to lower case for case-insensitive comparison

        // check for an exact & partial match in the list of top domains
        if (TopDomains.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)))
        {
            // Special styling for popular domains
            msaglNode.Attr.FillColor = Color.Moccasin;
            msaglNode.Label.FontSize = 35; // Larger font size for popular domains
            msaglNode.Attr.Shape = Shape.Circle;
        }
        else
        {
            // Default styling for other domains
            msaglNode.Attr.FillColor = Color.AliceBlue;
            msaglNode.Attr.Shape = Shape.Circle;
            msaglNode.Label.FontSize = 8;
        }

        /*  // Uncomment this entire code if you want to display domains and their first part (for example, youtube.com/about instead of youtube.com)
            // Set the label text to include the domain and the first segment of the path, if any
            string label = uri.Host;
            var pathSegments = uri.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length > 0)
            {
                label += "/" + pathSegments[0];
            }
            msaglNode.LabelText = label;
            return msaglNode;
        */

        msaglNode.LabelText = uri.Host; // Shortened label
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