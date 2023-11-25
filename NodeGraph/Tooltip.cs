using System.Windows.Forms;
using System.Drawing;

public class CustomTooltipForm : Form
{
    private Label label;

    public CustomTooltipForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.label = new Label();
        this.SuspendLayout();

        // Label setup
        this.label.Dock = DockStyle.Fill;
        this.label.TextAlign = ContentAlignment.MiddleCenter;
        this.label.AutoSize = true;

        // Form setup
        this.AutoScaleMode = AutoScaleMode.None;
        this.ClientSize = new Size(200, 50);
        this.ControlBox = false;
        this.Controls.Add(this.label);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Name = "CustomTooltipForm";
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.TopMost = true;

        this.ResumeLayout(false);
    }

    public void SetTooltipText(string text)
    {
        this.label.Text = text;
        // this will adjust the width of the tooltip box to accomodate for the text size
        this.label.Size = TextRenderer.MeasureText(this.label.Text, this.label.Font);
        this.ClientSize = new Size(this.label.Width + 20, this.label.Height + 20); // Add some padding

    }
}
