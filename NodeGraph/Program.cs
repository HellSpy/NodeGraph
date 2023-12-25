using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        // the path to appsettings.json
        string configPath = @"C:\Users\avili\source\repos\NodeGraph\NodeGraphWeb\appsettings.json";

        // Build the configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        // Pass the configuration to WebGraph
        WebGraph webGraph = new WebGraph(configuration);
        webGraph.BuildGraph("https://youtube.com");

        Graph graph = webGraph.Visualize();

        await webGraph.TransferToDatabase(graph);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Application.Run(new FormVisualizer(graph, webGraph)); // commented out as per your setup
    }
}
