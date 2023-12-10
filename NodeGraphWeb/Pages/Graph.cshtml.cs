using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services;

namespace NodeGraphWeb.Pages
{
    public class GraphModel : PageModel
    {
        private readonly GraphService _graphService;

        public Graph? GraphData { get; private set; } // Make it nullable

        public GraphModel(GraphService graphService)
        {
            _graphService = graphService;
        }

        public void OnGet()
        {
            GraphData = _graphService.BuildGraph("https://github.com");
        }
    }
}
