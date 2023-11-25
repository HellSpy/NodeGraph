using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System;
using Microsoft.Msagl.Layout.MDS;
using System.Diagnostics;
using System.Drawing; // added this

public class FormVisualizer : Form
{
    private GViewer viewer;
    private CustomTooltipForm customTooltip; // Custom tooltip form
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

        customTooltip = new CustomTooltipForm();
        Console.WriteLine("Tooltip initialized."); // debugging 

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
            customTooltip.SetTooltipText(nodeUrlMap[dnode.Node.Id]);
            customTooltip.Location = new Point(Cursor.Position.X + 10, Cursor.Position.Y + 10);
            customTooltip.Show();
        }
        else
        {
            customTooltip.Hide();
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
                // Use StyleNode for styling linked nodes
                WebGraph.StyleNode(linkedNode.Url, viewer.Graph);

                // Create a directed edge
                var edge = viewer.Graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;

                // Add nodes to the nodeUrlMap so that that tooltip function can work
                if (!nodeUrlMap.ContainsKey(linkedNode.Url))
                {
                    nodeUrlMap[linkedNode.Url] = linkedNode.Url; // Or whatever value you want for the tooltip
                }
            }
        }

        // Copying nodes and edges to a new graph instance with applied MDS layout
        var newGraph = new Graph();
        foreach (var n in viewer.Graph.Nodes)
        {
            var newNode = WebGraph.StyleNode(n.Id, newGraph); // Apply styling while copying nodes
        }
        foreach (var e in viewer.Graph.Edges)
        {
            var newEdge = newGraph.AddEdge(e.Source, e.Target);
            newEdge.Attr.Color = e.Attr.Color;
            newEdge.Attr.ArrowheadAtTarget = e.Attr.ArrowheadAtTarget;
        }

        // Apply MDS layout settings
        var mdsLayout = new MdsLayoutSettings
        {
            // RemoveOverlaps = true, // Enable overlap removal
            // ScaleX = 1.0, // Set X scaling
            // ScaleY = 1.0, // Set Y scaling
            // PackingAspectRatio = 1.0, // Set packing aspect ratio
            // PivotNumber = 50 // Set the number of pivots
        };

        newGraph.LayoutAlgorithmSettings = mdsLayout;

        // Update the viewer with the new graph
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
