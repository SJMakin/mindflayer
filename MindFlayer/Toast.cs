namespace MindFlayer;

public class Toast : Form
{
    private System.Windows.Forms.Timer? _timer;
    private readonly Label _label;

    public Toast(string text)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        BackColor = Color.LightYellow;
        Size = new Size(300, 50);

        _label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 12, FontStyle.Regular)
        };
        Controls.Add(_label);    
        Show();
    }

    public sealed override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    protected override bool ShowWithoutActivation => true;

    private const int WsExTopmost = 0x00000008;
    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExTopmost;
            return createParams;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer?.Stop();
        Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var x = Screen.PrimaryScreen.WorkingArea.Right - Width;
        var y = Screen.PrimaryScreen.WorkingArea.Bottom - Height;
        Location = new Point(x, y);
    }

    private void SetText(string text, Color backColor)
    {
        _label.Text = text;
        _label.BackColor = backColor;
        Refresh();
    }

    public void UpdateThenClose(string text, Color backColor, int closeAfter)
    {
        SetText(text, backColor);
        _timer = new () { Interval = closeAfter };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }
}
