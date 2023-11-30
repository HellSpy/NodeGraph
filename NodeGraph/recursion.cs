using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.MDS;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

public class RecursionForm : Form
{
    private System.Windows.Forms.Label label;
    private NumericUpDown depthNumericUpDown;
    private Button fetchButton;
    private WebGraph webGraph;
    private GViewer viewer; // Add GViewer field
    private Dictionary<string, string> nodeUrlMap; // Add Dictionary field

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
            Maximum = 10,
            Value = 1
        };

        fetchButton = new Button
        {
            Text = "Fetch Links",
            Location = new System.Drawing.Point(10, 40),
            Size = new System.Drawing.Size(100, 30)
        };
        fetchButton.Click += FetchButton_Click;

        Controls.Add(label);
        Controls.Add(depthNumericUpDown);
        Controls.Add(fetchButton);

        Text = "Recursion Depth";
        Size = new System.Drawing.Size(300, 120);
    }

    private void FetchButton_Click(object sender, EventArgs e)
    {
        int depth = (int)depthNumericUpDown.Value;

        if (depth > 0)
        {
            // Fetch links recursively up to the specified depth
            FetchLinksRecursively(webGraph.RootNode, depth);
        }

        // Add a message to indicate that the entire recursion is done
        Console.WriteLine("Recursion done for the entire process.");
        RefreshViewerGraph(); // Refresh the viewer's graph when recursion is done
        Close();
    }


    private void FetchLinksRecursively(WebNode node, int depth)
    {
        if (depth == 0)
        {
            Console.WriteLine("Reached recursion depth limit.");
            RefreshViewerGraph();
            return;
        }

        Console.WriteLine("Fetching links for: " + node.Url);

        webGraph.FetchLinks(node, webGraph.VisitedDomains);
        UpdateViewerGraph(node);

        foreach (var linkedNode in node.LinkedNodes)
        {
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
        // Refresh the viewer's graph
        viewer.Graph = viewer.Graph; // THIS IS HOW THE GRAPH IS REFRESHED, BY REASSIGNING THE SAME GRAPH
        viewer.NeedToCalculateLayout = true;
        viewer.Invalidate();
        viewer.Refresh();
    }
}
