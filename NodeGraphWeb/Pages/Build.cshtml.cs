using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Msagl.Drawing;
using NodeGraphWeb.Services2;

namespace NodeGraphWeb.Pages
{
    public class GraphModel2 : PageModel
    {
        private readonly GraphServiceBuild _graphService;

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

            var graph = _graphService.BuildGraph(UrlInput);
            GraphJsonData = _graphService.ConvertGraphToJson(graph);
            return Page();
        }
    }
}
