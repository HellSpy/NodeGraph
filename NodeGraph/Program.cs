using System;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WebGraph webGraph = new WebGraph();
        webGraph.BuildGraph("https://github.com");

        Graph graph = webGraph.Visualize();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new FormVisualizer(graph, webGraph));
    }
}
