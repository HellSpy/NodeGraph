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
        string configPath = @"../../../NodeGraphWeb/appsettings.json";

        // Get the full path to the appsettings.json file
        string fullPath = Path.GetFullPath(configPath);

        // Build the configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(fullPath)) // Set base path to the directory of appsettings.json
            .AddJsonFile(Path.GetFileName(fullPath), optional: false, reloadOnChange: true);

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
