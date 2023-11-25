using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System;
using Microsoft.Msagl.Layout.MDS;
using System.Diagnostics;

public class FormVisualizer : Form
{
    private GViewer viewer;
    private ToolTip tooltip;
    private Dictionary<string, string> nodeUrlMap;
    private WebGraph webGraph;
    private HashSet<string> visitedDomains; // Added to track visited domains

    private Stopwatch stopwatch = new Stopwatch(); // for performance measurement

    // yeah this looks like shit cause theres a duplicate color class cause of the OutsideAreaBrush implementation im sorry
    // im gonna revise this at some point anyway and move a lot of the code from here to WebGraph eventually
    public Microsoft.Msagl.Drawing.Color defaultNodeColor = Microsoft.Msagl.Drawing.Color.LightGray;
    public Microsoft.Msagl.Drawing.Color defaultEdgeColor = Microsoft.Msagl.Drawing.Color.Black;

    public FormVisualizer(Graph graph, WebGraph webGraph)
    {
        this.Text = "NodeGraph -- Double click on any node to begin"; // sets the title of the form
        this.Icon = NodeGraph.Properties.Resources.icon; // ok i was gonna put this in the InitializeComponent function but it didnt work >_< thats why its up here

        this.WindowState = FormWindowState.Maximized; // starts the program in maximized view

        this.webGraph = webGraph;

        visitedDomains = new HashSet<string>(); // Initialize the visited domains HashSet

        viewer = new GViewer
        {
            Graph = graph, // should still work fine with MDS layout settings...
            Dock = DockStyle.Fill,
            OutsideAreaBrush = Brushes.White // by defualt, the window has a bunch of grey space which looks ugly so i set it to 
        };

        tooltip = new ToolTip();
        nodeUrlMap = new Dictionary<string, string>();

        foreach (var node in graph.Nodes)
        {
            nodeUrlMap[node.Id] = node.Id;
        }

        viewer.MouseMove += Viewer_MouseMove;
        viewer.MouseDoubleClick += Viewer_MouseDoubleClick; // Changed to MouseDoubleClick so that you can double click duh
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

    private void Viewer_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        // Get the object at the mouse click position
        var clickedObject = viewer.GetObjectAt(e.X, e.Y);
        if (clickedObject is DNode dnode && IsValidUrl(dnode.Node.Id))
        {
            Console.WriteLine("Node clicked: " + dnode.Node.Id);

            stopwatch.Reset(); // resets the stopwatch & removes the previous value (in case it's not zero)
            stopwatch.Start(); // begins the stopwatch so we can see how long it takes to generate the graph

            var node = new WebNode(dnode.Node.Id);
            webGraph.FetchLinks(node, visitedDomains);

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
        Console.WriteLine("Generating graph...");

        // add new nodes and edges to the existing graph
        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!viewer.Graph.NodeMap.ContainsKey(linkedNode.Url))
            {
                var linkedMsaglNode = viewer.Graph.AddNode(linkedNode.Url);
                linkedMsaglNode.Attr.FillColor = defaultNodeColor; // default color for linked nodes
                linkedMsaglNode.Attr.Shape = Shape.Circle;
                linkedMsaglNode.LabelText = ShortenUrl(linkedNode.Url); // shortened label for linked nodes

                // Create a directed edge
                var edge = viewer.Graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = defaultEdgeColor;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }

        // creating a new graph instance to apply the MDS layout
        var newGraph = new Graph();
        foreach (var n in viewer.Graph.Nodes)
        {
            var newNode = newGraph.AddNode(n.Id);
            
            newNode.Attr.FillColor = n.Attr.FillColor;
            //newNode.Attr.AddStyle()
            newNode.Attr.Shape = n.Attr.Shape;
            newNode.LabelText = n.LabelText;
        }
        foreach (var e in viewer.Graph.Edges)
        {
            var newEdge = newGraph.AddEdge(e.Source, e.Target);
            newEdge.Attr.Color = e.Attr.Color;
            newEdge.Attr.ArrowheadAtTarget = e.Attr.ArrowheadAtTarget;
        }

        // apply MDS layout settings to the new graph
        var mdsLayout = new MdsLayoutSettings();
        // adjust MDS layout settings here if needed
        newGraph.LayoutAlgorithmSettings = mdsLayout;

        // assign the new graph to the viewer and refresh the layout
        viewer.Graph = newGraph;
        viewer.NeedToCalculateLayout = true;
        viewer.Invalidate();
        viewer.Refresh();

        stopwatch.Stop();

        this.Text = "NodeGraph -- Generated in " + stopwatch.ElapsedMilliseconds.ToString() + " ms";
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

    private void InitializeComponent()
    {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormVisualizer));
            this.SuspendLayout();
            // 
            // FormVisualizer
            // 
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "FormVisualizer";
            this.ResumeLayout(false);

    }
}
