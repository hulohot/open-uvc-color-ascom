using System;
using System.Windows.Forms;

namespace FibonaxUvcAscom;

internal sealed class SetupForm : Form
{
    private readonly NumericUpDown index = new() { Minimum = 0, Maximum = 32, Dock = DockStyle.Top };

    public SetupForm(int currentIndex)
    {
        Text = "Open UVC Color ASCOM Camera";
        Width = 390; Height = 150; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        var label = new Label { Text = "UVC camera index (as seen by Windows Media Foundation):", Dock = DockStyle.Top, AutoSize = true };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
        buttons.Controls.Add(ok); buttons.Controls.Add(new Button { Text = "Cancel", DialogResult = DialogResult.Cancel });
        index.Value = Math.Max(index.Minimum, Math.Min(index.Maximum, currentIndex));
        Controls.Add(index); Controls.Add(label); Controls.Add(buttons); AcceptButton = ok;
    }

    public int DeviceIndex => (int)index.Value;
}
