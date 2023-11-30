using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System;
using Microsoft.Msagl.Layout.MDS;
using System.Diagnostics;
using Microsoft.Msagl.Core.Routing;

public class FormVisualizer : Form
{
    private GViewer viewer;
    private CustomTooltipForm customTooltip; // Custom tooltip form
    private Dictionary<string, string> nodeUrlMap;
    private WebGraph webGraph;
    private HashSet<string> visitedDomains; // Added to track visited domains

    private AddUrlForm addUrlForm; // for the box to add more URLs

    private Stopwatch stopwatch = new Stopwatch(); // for performance measurement

    // yeah this looks like shit cause theres a duplicate color class cause of the OutsideAreaBrush implementation im sorry
    // im gonna revise this at some point anyway and move a lot of the code from here to WebGraph eventually
    public Microsoft.Msagl.Drawing.Color defaultNodeColor = Microsoft.Msagl.Drawing.Color.LightGray;
    public Microsoft.Msagl.Drawing.Color defaultEdgeColor = Microsoft.Msagl.Drawing.Color.Black;

    public FormVisualizer(Graph graph, WebGraph webGraph)
    {

        // Create a menu strip (new toolbar) comment out this entire code if you want the default toolbar back.
        // Move this code downwards to the bottom of the formvisualizer if you want the default toolbar and this menu to exist.
        var menuStrip = new MenuStrip();
        var fileMenuItem = new ToolStripMenuItem("File");
        var addUrlMenuItem = new ToolStripMenuItem("Add URL");
        addUrlMenuItem.Click += AddUrlMenuItem_Click;
        fileMenuItem.DropDownItems.Add(addUrlMenuItem);
        menuStrip.Items.Add(fileMenuItem);

        var recursionMenuItem = new ToolStripMenuItem("Recursion");
        recursionMenuItem.Click += RecursionMenuItem_Click;
        fileMenuItem.DropDownItems.Add(recursionMenuItem); // Add the "Recursion" menu item

        this.Controls.Add(menuStrip); // this is the end of the menu strip


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
        viewer.MouseLeave += Viewer_MouseLeave; // remove tooltip when mouse leaves
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
    private void Viewer_MouseLeave(object sender, EventArgs e)
    {
        customTooltip.Hide();
    }

    // i gave up so we will rebuild the entire graph with each update, which is going to cost more resources
    private void UpdateGraph(WebNode node)
    {
        Console.WriteLine("Generating graph...");

        var linkedNodeCounts = new Dictionary<string, int>();

        // Initialize counts for existing nodes
        foreach (var n in viewer.Graph.Nodes)
        {
            linkedNodeCounts[n.Id] = 0;
        }

        // Recalculate counts based on existing edges
        foreach (var e in viewer.Graph.Edges)
        {
            linkedNodeCounts[e.Source]++;
            linkedNodeCounts[e.Target]++;
        }

        // Add the new nodes and edges
        foreach (var linkedNode in node.LinkedNodes)
        {
            if (!viewer.Graph.NodeMap.ContainsKey(linkedNode.Url))
            {
                // Initialize the count for new nodes
                linkedNodeCounts[linkedNode.Url] = 1; // As it's a new node, start with 1

                // Create a directed edge
                var edge = viewer.Graph.AddEdge(node.Url, linkedNode.Url);
                edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
                edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;

                // Increment linked node counts for both nodes in the edge
                linkedNodeCounts[node.Url]++;
            }
        }

        var newGraph = new Graph();

        // Apply styling to all nodes with updated linkedNodeCounts
        foreach (var nodeId in linkedNodeCounts.Keys)
        {
            WebGraph.StyleNode(nodeId, newGraph, linkedNodeCounts[nodeId]);
        }

        // Copy the edges to the new graph
        foreach (var e in viewer.Graph.Edges)
        {
            var newEdge = newGraph.AddEdge(e.Source, e.Target);
            newEdge.Attr.Color = e.Attr.Color;
            newEdge.Attr.ArrowheadAtTarget = e.Attr.ArrowheadAtTarget;
        }

        // edge routing seetings that are applied to MDS Layout
        var edgeRouting = new EdgeRoutingSettings
        {
            EdgeRoutingMode = EdgeRoutingMode.SplineBundling
        };

        // Apply MDS layout settings
        var mdsLayout = new MdsLayoutSettings
        {
            EdgeRoutingSettings = edgeRouting
        };

        nodeUrlMap.Clear(); // this is to fix the tooltip
        foreach (var n in newGraph.Nodes)
        {
            nodeUrlMap[n.Id] = n.Id;
        }

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

    private void ShowAddUrlForm()
    {
        addUrlForm = new AddUrlForm();
        var result = addUrlForm.ShowDialog();

        if (result == DialogResult.OK)
        {
            string enteredUrl = addUrlForm.EnteredUrl;

            // Check if the entered URL is valid before processing
            if (!string.IsNullOrEmpty(enteredUrl) && IsValidUrl(enteredUrl))
            {
                var newNode = new WebNode(enteredUrl);
                webGraph.FetchLinks(newNode, visitedDomains);
                UpdateGraph(newNode);
            }
            else
            {
                MessageBox.Show("Invalid URL. Please enter a valid URL.");
            }
        }

        addUrlForm.Dispose();
    }

    private void AddUrlMenuItem_Click(object sender, EventArgs e)
    {
        ShowAddUrlForm();
    }

    private void RecursionMenuItem_Click(object sender, EventArgs e)
    {
        // Open the recursion form and pass viewer and nodeUrlMap
        var recursionForm = new RecursionForm(webGraph, viewer, nodeUrlMap);
        recursionForm.ShowDialog();
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