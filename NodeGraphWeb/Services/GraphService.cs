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

        // Add other methods as needed
    }
}
