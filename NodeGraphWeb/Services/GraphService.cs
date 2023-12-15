using Microsoft.Msagl.Drawing;
using NodeGraphWeb;

namespace NodeGraphWeb.Services
{
    public class GraphService
    {
        public GraphService()
        {
            // Initialization, if necessary
        }

        public Graph BuildGraph(string url)
        {
            WebGraph webGraph = new WebGraph();
            // Use methods from your NodeGraph project
            webGraph.BuildGraph(url);
            return webGraph.Visualize(); // Assuming this is how you build and visualize the graph
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



        // add other shit and methods here
    }
}
