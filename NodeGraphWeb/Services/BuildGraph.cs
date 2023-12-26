using System.Text.Json; // For JSON serialization
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services2;

namespace NodeGraphWeb.Services2
{
    
    public class GraphServiceBuild
    {
        // Initialize _existingGraph as a new Graph instance or retrieve it from a persistent source
        private Graph _existingGraph = new Graph();

        public GraphServiceBuild()
        {
            // Initialization, if necessary
        }

        public Graph BuildGraph(string url)
        {
            WebGraph webGraph = new WebGraph();
            webGraph.BuildGraph(url);

            // Append new nodes to _existingGraph
            foreach (var node in webGraph.Visualize().Nodes)
            {
                if (!_existingGraph.Nodes.Any(n => n.Id == node.Id))
                {
                    _existingGraph.AddNode(node);
                }
            }

            // Append new edges to _existingGraph
            foreach (var edge in webGraph.Visualize().Edges)
            {
                if (!_existingGraph.Edges.Any(e => e.Source == edge.Source && e.Target == edge.Target))
                {
                    // Correcting the AddEdge method usage
                    _existingGraph.AddEdge(edge.SourceNode.Id, edge.TargetNode.Id);
                    // Optionally set other properties of the edge here
                }
            }

            return _existingGraph;
        }


        public string ConvertGraphToJson(Graph graph)
        {
            var nodes = graph.Nodes.Select(n => new {
                Id = n.Id, // this is the node id 
                Label = n.LabelText, // this is the text
                Color = $"#{n.Attr.FillColor.R:X2}{n.Attr.FillColor.G:X2}{n.Attr.FillColor.B:X2}", // this is the color 
                Size = n.Label.FontSize // this is the size 
            });
            var edges = graph.Edges.Select(e => new {
                source = e.SourceNode.Id,
                target = e.TargetNode.Id
            });


            var graphData = new { Nodes = nodes, Edges = edges };
            return System.Text.Json.JsonSerializer.Serialize(graphData);
        }
    }
}
