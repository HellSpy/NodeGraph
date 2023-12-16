using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        WebGraph webGraph = new WebGraph();
        webGraph.BuildGraph("https://youtube.com");

        Graph graph = webGraph.Visualize();

        await webGraph.TransferToDatabase(graph);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Application.Run(new FormVisualizer(graph, webGraph)); // comment this out as to not use formvisualizer.cs
    }
}
