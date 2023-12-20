using Microsoft.AspNetCore.Mvc;
using NodeGraphWeb.Services2;

namespace NodeGraphWeb.Controllers
{
    [ApiController]
    [Route("api/ExpandNodes")]
    public class ExpandNodesController : ControllerBase
    {
        private readonly GraphServiceBuild _graphService;

        public ExpandNodesController(GraphServiceBuild graphService)
        {
            _graphService = graphService;
        }

        [HttpGet("build")]
        public IActionResult BuildGraphFromUrl(string url)
        {
            var graph = _graphService.BuildGraph(url);
            var jsonData = _graphService.ConvertGraphToJson(graph);
            return Ok(jsonData);
        }
    }
}
