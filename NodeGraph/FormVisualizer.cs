// use windows form to display the graph
// Honestly there's a better way of doing this
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Collections.Generic;
using System.Windows.Forms;

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
        viewer.ObjectUnderMouseCursorChanged += Viewer_MouseClick; // Changed to use ObjectUnderMouseCursorChanged
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

    private void Viewer_MouseClick(object sender, ObjectUnderMouseCursorChangedEventArgs e)
    {
        if (e.OldObject is DNode && e.NewObject is DNode dnode)
        {
            var node = new WebNode(dnode.Node.Id);
            webGraph.FetchLinks(node);
            // Call a method to update the graph here
        }
    }
}