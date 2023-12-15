using System.Text.Json; // For JSON serialization
using NodeGraphWeb.Services;

namespace NodeGraphWeb.Services
{
    public class GraphService
    {
        private readonly DatabaseService _databaseService;

        public GraphService()
        {
            _databaseService = new DatabaseService();
        }

        public string GetGraphDataFromDatabase()
        {
            var nodes = _databaseService.FetchNodes();
            var edges = _databaseService.FetchEdges();

            var graphData = new { Nodes = nodes, Edges = edges };
            return JsonSerializer.Serialize(graphData);
        }
    }
}
