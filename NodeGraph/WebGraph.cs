using HtmlAgilityPack;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Msagl.Layout.MDS;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Msagl.Core.Routing;
using System.Data.SqlClient;
using System.Text;
using MySql.Data.MySqlClient;

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
        FetchLinks(RootNode, visitedDomains, 1); // depth limit of 1
    }

    // added parallel processessing
    public void FetchLinks(WebNode node, HashSet<string> visitedDomains, int depthLimit)
    {
        Console.WriteLine($"Fetching links for: {node.Url}, Depth Limit: {depthLimit}");

        if (depthLimit <= 0)
        {
            return; // Stop recursion when depth limit is reached
        }

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

                foreach (var newNode in newNodes)
                {
                    Console.WriteLine($"Processing new node: {newNode.Url}");

                    if (!visitedDomains.Contains(newNode.Url))
                    {
                        Console.WriteLine($"Making recursive call for: {newNode.Url}");
                        FetchLinks(newNode, visitedDomains, depthLimit - 1); // Recursive call with decremented depth limit
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

                // create a directed edge from the current node to the linked node
                var edge = graph.AddEdge(node.Url, linkedNode.Url);

                // customize the appearance of the edge
                edge.Attr.Color = Color.Black; // Set the color of the edge
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal; // Set the style of the arrowhead

                // recursively process linked nodes!!!!!!
                AddNodeToGraph(linkedNode, graph, visitedDomains);
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
        msaglNode.Label.FontSize = 8 + (linkedNodeCount * 0.5);
        msaglNode.LabelText = uri.Host;
        msaglNode.Label.FontColor = Color.White;

        return msaglNode;
    }
    public List<(WebNode Source, WebNode Target)> GetAllEdges()
    {
        var allEdges = new HashSet<(WebNode, WebNode)>();
        CollectAllEdges(RootNode, allEdges, new HashSet<WebNode>());
        return allEdges.ToList();
    }

    private void CollectAllEdges(WebNode node, HashSet<(WebNode, WebNode)> allEdges, HashSet<WebNode> visited)
    {
        if (!visited.Contains(node))
        {
            visited.Add(node);
            foreach (var linkedNode in node.LinkedNodes)
            {
                allEdges.Add((node, linkedNode));
                CollectAllEdges(linkedNode, allEdges, visited);
            }
        }
    }

    public async Task TransferToDatabase(Graph graph)
    {
        // Connection string - replace this with a secure method in production
        string connectionString = "server=193.203.166.22;user=u278723081_Avilin;database=u278723081_NodeGraph;port=3306;password=Csvma!l122mA";

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            // Batch insertion for nodes using the new format
            var nodeInsertCommand = new StringBuilder("INSERT INTO Nodes (Id, LabelText, Color) VALUES ");

            // Use the new format for nodes
            var nodes = graph.Nodes.Select(n => new {
                Id = n.Id,
                LabelText = n.LabelText,
                Color = $"#{n.Attr.FillColor.R:X2}{n.Attr.FillColor.G:X2}{n.Attr.FillColor.B:X2}"
            });

            foreach (var node in nodes)
            {
                Console.WriteLine($"Node - ID: {node.Id}, LabelText: {node.LabelText}, Color: {node.Color}");  // Debugging
                // Append each node's data to the command
                nodeInsertCommand.Append($"('{MySqlHelper.EscapeString(node.Id)}', '{MySqlHelper.EscapeString(node.LabelText)}', '{node.Color}'),");
            }

            // Remove the last comma and execute the command
            nodeInsertCommand.Length--;
            using (var cmd = new MySqlCommand(nodeInsertCommand.ToString(), connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Batch insertion for edges
            var edgeInsertCommand = new StringBuilder("INSERT INTO Edges (sourceId, targetId) VALUES ");
            foreach (var edge in GetAllEdges())
            {
                // Escape special characters to prevent SQL injection
                var sourceId = MySqlHelper.EscapeString(edge.Source.Url);
                var targetId = MySqlHelper.EscapeString(edge.Target.Url);

                edgeInsertCommand.Append($"('{sourceId}', '{targetId}'),");
                Console.WriteLine($"Edge - Source: {edge.Source.Url}, Target: {edge.Target.Url}");  // Debugging
            }
            edgeInsertCommand.Length--; // Remove the last comma
            using (var cmd = new MySqlCommand(edgeInsertCommand.ToString(), connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Print a success message to the console
            Console.WriteLine("Transfer to database successful.");
        }
    }
}