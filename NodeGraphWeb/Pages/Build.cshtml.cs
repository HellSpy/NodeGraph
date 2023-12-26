using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services2;

namespace NodeGraphWeb.Pages
{
    public class GraphModel2 : PageModel
    {
        private readonly GraphServiceBuild _graphService;

        // Static field to store the graph data
        private static Graph? CurrentGraph { get; set; }

        public string? GraphJsonData { get; private set; }

        public GraphModel2(GraphServiceBuild graphService)
        {
            _graphService = graphService;
        }

        [BindProperty]
        public string UrlInput { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Build a new graph from the input URL
            var newGraph = _graphService.BuildGraph(UrlInput);

            // Merge with the existing graph
            if (CurrentGraph == null)
            {
                CurrentGraph = newGraph;
            }
            else
            {
                MergeGraphs(CurrentGraph, newGraph);
            }

            GraphJsonData = _graphService.ConvertGraphToJson(CurrentGraph);
            return Page();
        }

        private void MergeGraphs(Graph existingGraph, Graph newGraph)
        {
            // Merge nodes
            foreach (var newNode in newGraph.Nodes)
            {
                if (!existingGraph.Nodes.Any(node => node.Id == newNode.Id))
                {
                    // Create a new node with the required properties
                    var addedNode = existingGraph.AddNode(newNode.Id);
                    addedNode.LabelText = newNode.LabelText;
                    addedNode.Attr.FillColor = new Microsoft.Msagl.Drawing.Color(
                        newNode.Attr.FillColor.A,
                        newNode.Attr.FillColor.R,
                        newNode.Attr.FillColor.G,
                        newNode.Attr.FillColor.B);
                    addedNode.Label.FontSize = newNode.Label.FontSize;
                }
            }

            // Merge edges
            foreach (var newEdge in newGraph.Edges)
            {
                if (!existingGraph.Edges.Any(edge => edge.SourceNode.Id == newEdge.SourceNode.Id && edge.TargetNode.Id == newEdge.TargetNode.Id))
                {
                    // Add edge by specifying source and target node IDs
                    existingGraph.AddEdge(newEdge.SourceNode.Id, newEdge.TargetNode.Id);
                }
            }
        }

        public IActionResult OnPostResetGraph()
        {
            ResetGraph();
            return new NoContentResult();
        }

        public void ResetGraph()
        {
            CurrentGraph = new Graph();
        }
    }
}
