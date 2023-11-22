﻿using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.Generic;
using System.Windows.Forms;
using System;

public class FormVisualizer : Form
{
    private GViewer viewer;
    private ToolTip tooltip;
    private Dictionary<string, string> nodeUrlMap;
    private WebGraph webGraph;

    public FormVisualizer(Graph graph, WebGraph webGraph)
    {
        this.webGraph = webGraph;

        viewer = new GViewer
        {
            Graph = graph,
            Dock = DockStyle.Fill
        };

        tooltip = new ToolTip();
        nodeUrlMap = new Dictionary<string, string>();

        foreach (var node in graph.Nodes)
        {
            nodeUrlMap[node.Id] = node.Id;
        }

        viewer.MouseMove += Viewer_MouseMove;
        viewer.MouseClick += Viewer_MouseClick; // Ensure this is MouseClick
        Controls.Add(viewer);
    }

    private void Viewer_MouseMove(object sender, MouseEventArgs e)
    {
        if (viewer.GetObjectAt(e.X, e.Y) is DNode dnode && nodeUrlMap.ContainsKey(dnode.Node.Id))
        {
            tooltip.SetToolTip(viewer, nodeUrlMap[dnode.Node.Id]);
        }
        else
        {
            tooltip.SetToolTip(viewer, string.Empty);
        }
    }

    private bool IsValidUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
        catch
        {
            return false;
        }
    }

    private void Viewer_MouseClick(object sender, MouseEventArgs e)
    {
        // Get the object at the mouse click position
        var clickedObject = viewer.GetObjectAt(e.X, e.Y);
        if (clickedObject is DNode dnode && IsValidUrl(dnode.Node.Id))
        {
            Console.WriteLine("Node clicked: " + dnode.Node.Id);

            var node = new WebNode(dnode.Node.Id);
            webGraph.FetchLinks(node);

            Console.WriteLine("Fetched links for: " + dnode.Node.Id);

            // Now, update the graph with newly fetched links
            UpdateGraph(node);

            Console.WriteLine("Graph updated after clicking: " + dnode.Node.Id);
        }
        else
        {
            Console.WriteLine("Invalid URL or not a DNode: " + ((clickedObject as DNode)?.Node.Id ?? "None"));
        }
    }
    // i gave up so we will rebuild the entire graph with each update, which is going to cost more resources
    private void UpdateGraph(WebNode node)
    {
        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!viewer.Graph.NodeMap.ContainsKey(linkedNode.Url))
            {
                var linkedMsaglNode = viewer.Graph.AddNode(linkedNode.Url);
                linkedMsaglNode.Attr.FillColor = Color.LightGray; // Default color for linked nodes
                linkedMsaglNode.Attr.Shape = Shape.Circle;
                linkedMsaglNode.LabelText = ShortenUrl(linkedNode.Url); // Shortened label for linked nodes

                // Create a directed edge
                var edge = viewer.Graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = Color.Black;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }

        // Rebuild the entire graph with new nodes and edges
        var newGraph = new Graph();
        foreach (var n in viewer.Graph.Nodes)
        {
            var newNode = newGraph.AddNode(n.Id);
            newNode.Attr.FillColor = n.Attr.FillColor;
            newNode.Attr.Shape = n.Attr.Shape;
            newNode.LabelText = n.LabelText;
        }
        foreach (var e in viewer.Graph.Edges)
        {
            var newEdge = newGraph.AddEdge(e.Source, e.Target);
            newEdge.Attr.Color = e.Attr.Color;
            newEdge.Attr.ArrowheadAtTarget = e.Attr.ArrowheadAtTarget;
        }
        viewer.Graph = newGraph; // Assign the new graph to the viewer

        viewer.Invalidate();
        viewer.Refresh();
    }

    private string ShortenUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host; // Shortens to domain name
        }
        catch
        {
            return url; // Return the original URL if it's not a valid URI
        }
    }

}