using System;
using System.Windows.Forms;

public class AddUrlForm : Form
{
    private Label label;
    private TextBox urlTextBox;
    private Button addButton;

    public string EnteredUrl { get; private set; }

    public AddUrlForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        label = new Label
        {
            Text = "Enter a URL:",
            Location = new System.Drawing.Point(20, 20),
            AutoSize = true
        };

        urlTextBox = new TextBox
        {
            Location = new System.Drawing.Point(120, 20),
            Width = 300
        };

        addButton = new Button
        {
            Text = "Add",
            Location = new System.Drawing.Point(430, 20),
            Width = 75
        };

        addButton.Click += AddButton_Click;

        Controls.Add(label);
        Controls.Add(urlTextBox);
        Controls.Add(addButton);

        this.Text = "Add URL";
        this.ClientSize = new System.Drawing.Size(530, 80);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
    }

    private void AddButton_Click(object sender, EventArgs e)
    {
        EnteredUrl = urlTextBox.Text;
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
