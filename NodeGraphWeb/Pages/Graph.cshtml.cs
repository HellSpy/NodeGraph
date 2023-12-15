using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services;

namespace NodeGraphWeb.Pages
{
    public class GraphModel : PageModel
    {
        private readonly GraphService _graphService;

        public string? GraphJsonData { get; private set; }

        public GraphModel(GraphService graphService)
        {
            _graphService = graphService;
        }

        public void OnGet()
        {
            // Fetch the graph data in JSON format from the database
            GraphJsonData = _graphService.GetGraphDataFromDatabase();
        }
    }
}
