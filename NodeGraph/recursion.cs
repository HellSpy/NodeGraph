using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.MDS;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

public class RecursionForm : Form
{
    private System.Windows.Forms.Label label;
    private NumericUpDown depthNumericUpDown;
    private Button fetchButton;
    private WebGraph webGraph;
    private GViewer viewer; // Add GViewer field
    private Dictionary<string, string> nodeUrlMap; // Add Dictionary field

    private Button stopButton; // New stop button
    private bool stopRecursion = false; // Flag to control recursion

    public RecursionForm(WebGraph webGraph, GViewer viewer, Dictionary<string, string> nodeUrlMap)
    {
        this.webGraph = webGraph;
        this.viewer = viewer;
        this.nodeUrlMap = nodeUrlMap;

        // Initialize viewer's graph with MDS layout settings
        viewer.Graph = new Graph();

        // Set the layout algorithm to MDS (Multidimensional Scaling)
        viewer.Graph.LayoutAlgorithmSettings = new MdsLayoutSettings
        {
            EdgeRoutingSettings = new EdgeRoutingSettings
            {
                EdgeRoutingMode = EdgeRoutingMode.SplineBundling
            }
        };

        label = new System.Windows.Forms.Label
        {
            Text = "Enter recursion depth:",
            Location = new System.Drawing.Point(10, 10),
            AutoSize = true
        };

        depthNumericUpDown = new NumericUpDown
        {
            Location = new System.Drawing.Point(150, 10),
            Minimum = 1,
            Maximum = 100,
            Value = 1
        };

        fetchButton = new Button
        {
            Text = "Fetch Links",
            Location = new System.Drawing.Point(10, 40),
            Size = new System.Drawing.Size(100, 30)
        };
        fetchButton.Click += FetchButton_Click;

        stopButton = new Button
        {
            Text = "Stop Recursion",
            Location = new System.Drawing.Point(120, 40),
            Size = new System.Drawing.Size(100, 30)
        };
        stopButton.Click += StopButton_Click;

        Controls.Add(label);
        Controls.Add(depthNumericUpDown);
        Controls.Add(fetchButton);
        Controls.Add(stopButton);

        Text = "Recursion Depth";
        Size = new System.Drawing.Size(300, 120);
    }

    private void StopButton_Click(object sender, EventArgs e)
    {
        stopRecursion = true; // Set the flag to true to stop recursion
    }

    private async void FetchButton_Click(object sender, EventArgs e)
    {
        int depth = (int)depthNumericUpDown.Value;

        if (depth > 0)
        {
            // Fetch links recursively up to the specified depth
            await Task.Run(() => FetchLinksRecursively(webGraph.RootNode, depth));
        }

        // Add a message to indicate that the entire recursion is done
        Console.WriteLine("Recursion done for the entire process.");
        RefreshViewerGraph(); // Refresh the viewer's graph when recursion is done
        Close();
    }


    private void FetchLinksRecursively(WebNode node, int depth)
    {
        if (stopRecursion || depth == 0)
        {
            Console.WriteLine("Reached recursion depth limit.");
            RefreshViewerGraph();
            return;
        }

        Console.WriteLine("Fetching links for: " + node.Url);

        webGraph.FetchLinks(node, webGraph.VisitedDomains, 1);
        UpdateViewerGraph(node);

        foreach (var linkedNode in node.LinkedNodes)
        {
            // Check again for stop condition or depth limit before making a recursive call
            if (stopRecursion || depth <= 1)
            {
                Console.WriteLine("Stopping recursion early due to stop condition or depth limit.");
                break;
            }

            Console.WriteLine("Recursing into: " + linkedNode.Url);
            FetchLinksRecursively(linkedNode, depth - 1);
        }
    }

    private void UpdateViewerGraph(WebNode node)
    {
        var linkedNodeCounts = CalculateLinkedNodeCounts();

        // Restyle and add the current node if not already present
        if (!viewer.Graph.NodeMap.ContainsKey(node.Url))
        {
            // Ensure the node is in the linkedNodeCounts dictionary
            int count = linkedNodeCounts.ContainsKey(node.Url) ? linkedNodeCounts[node.Url] : 0;
            WebGraph.StyleNode(node.Url, viewer.Graph, count);
            if (!nodeUrlMap.ContainsKey(node.Url))
            {
                nodeUrlMap[node.Url] = node.Url;
            }
        }

        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!viewer.Graph.NodeMap.ContainsKey(linkedNode.Url))
            {
                // Ensure the linkedNode is in the linkedNodeCounts dictionary
                int linkedCount = linkedNodeCounts.ContainsKey(linkedNode.Url) ? linkedNodeCounts[linkedNode.Url] : 0;
                WebGraph.StyleNode(linkedNode.Url, viewer.Graph, linkedCount);

                // Create a directed edge
                var edge = viewer.Graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;

                if (!nodeUrlMap.ContainsKey(linkedNode.Url))
                {
                    nodeUrlMap[linkedNode.Url] = linkedNode.Url;
                }
            }
        }
    }


    private Dictionary<string, int> CalculateLinkedNodeCounts()
    {
        var linkedNodeCounts = new Dictionary<string, int>();

        // Initialize counts for all nodes
        foreach (var n in viewer.Graph.Nodes)
        {
            linkedNodeCounts[n.Id] = 0;
        }

        // Increment counts based on existing edges
        foreach (var e in viewer.Graph.Edges)
        {
            if (linkedNodeCounts.ContainsKey(e.Source))
                linkedNodeCounts[e.Source]++;
            if (linkedNodeCounts.ContainsKey(e.Target))
                linkedNodeCounts[e.Target]++;
        }

        return linkedNodeCounts;
    }

    private void RefreshViewerGraph()
    {
        var linkedNodeCounts = CalculateLinkedNodeCounts();

        // Reapply styles to nodes based on the new data
        foreach (var node in viewer.Graph.Nodes)
        {
            ReapplyStylesToNode(node, linkedNodeCounts[node.Id]);
        }

        // Refresh the viewer's graph
        viewer.Graph = viewer.Graph;
        viewer.NeedToCalculateLayout = true;
        viewer.Invalidate();
        viewer.Refresh();
    }
    private void ReapplyStylesToNode(Node node, int linkedNodeCount)
    {
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

        node.Attr.FillColor = new Color(redShade, greenShade, blueShade); // apply the gradient coloring

        node.Label.FontSize = 8; // add + linkedNodeCount to increase font size based on linkedNodeCount
        node.Label.FontColor = Color.White;
    }
}
