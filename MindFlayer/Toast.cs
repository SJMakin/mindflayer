namespace MindFlayer;

public class Toast : Form
{
    System.Windows.Forms.Timer? timer;
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
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    private const int WS_EX_TOPMOST = 0x00000008;
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams createParams = base.CreateParams;
            createParams.ExStyle |= WS_EX_TOPMOST;
            return createParams;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        timer?.Stop();
        Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        int x = Screen.PrimaryScreen.WorkingArea.Right - Width;
        int y = Screen.PrimaryScreen.WorkingArea.Bottom - Height;
        Location = new Point(x, y);
    }

    public void SetText(string text, Color backColor)
    {
        _label.Text = text;
        _label.BackColor = backColor;
        Refresh();
    }

    public void UpdateThenClose(string text, Color backColor, int closeAfter)
    {
        SetText(text, backColor);
        timer = new () { Interval = closeAfter };
        timer.Tick += Timer_Tick;
        timer.Start();
    }
}
