using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services;

namespace NodeGraphWeb.Pages
{
    public class GraphModel2 : PageModel
    {
        private readonly GraphService _graphService;

        public string? GraphJsonData { get; private set; }

        public GraphModel2(GraphService graphService2)
        {
            _graphService2 = graphService2;
        }

        public void OnGet()
        {
            //Fetch the graph data in JSON format from the database
            GraphJsonData = _graphService.GetGraphDataFromDatabase();

            /* // previous code for fetching directly from backend
            var graph = _graphService.BuildGraph("https://github.com");
            GraphJsonData = _graphService.ConvertGraphToJson(graph);
            */
        }
    }
}
