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
            return webGraph.Visualize(); // call the visualize function
        }

        public string ConvertGraphToJson(Graph graph)
        {
            var nodes = graph.Nodes.Select(n => new {
                Id = n.Id,
                Label = n.LabelText,
                Color = $"#{n.Attr.FillColor.R:X2}{n.Attr.FillColor.G:X2}{n.Attr.FillColor.B:X2}"
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
