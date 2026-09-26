using System.Drawing.Drawing2D;
using System.Globalization;
using UsdKrwWidget.Controls;
using UsdKrwWidget.Models;
using UsdKrwWidget.Services;

namespace UsdKrwWidget;

internal sealed class MainForm : Form
{
    private static readonly string[] Periods = ["1D", "1W", "1M", "6M", "1Y"];
    private const int CornerRadius = 14;

    private readonly NaverFinanceClient _client = new();
    private readonly RateCache _cache = new();
    private readonly List<RatePoint> _points = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 5 * 60 * 1000 };

    private readonly Label _pairLabel = new();
    private readonly Label _rateLabel = new();
    private readonly Label _changeLabel = new();
    private readonly Label _rangeLabel = new();
    private readonly Label _highLowLabel = new();
    private readonly Label _updatedLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Panel _chartCard = new();
    private readonly SparklinePanel _chart = new();
    private readonly Button _minimizeButton = new();
    private readonly Button _closeButton = new();
    private readonly Dictionary<string, Button> _periodButtons = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ToolTip _toolTip = new();
    private readonly ToolStripMenuItem _topMostItem = new("Always on top") { CheckOnClick = true };
    private readonly ToolStripMenuItem _startupItem = new("Start with Windows") { CheckOnClick = true };

    private AppSettings _settings = new();
    private string _period = "1D";
    private bool _allowExit;
    private bool _loadingSettings;
    private Point _dragStart;

    public MainForm()
    {
        Text = "USD/KRW Widget";
        ClientSize = new Size(316, 232);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(250, 252, 255);
        ForeColor = Color.FromArgb(35, 43, 52);
        Font = new Font("Segoe UI", 8.5f);
        ShowInTaskbar = false;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BuildUi();
        BuildMenusAndTray();
        UpdateRoundedRegion();
        PositionAtTopRight();

        EnableDragging(this);
        EnableDragging(_pairLabel);
        EnableDragging(_rateLabel);
        EnableDragging(_changeLabel);

        Paint += MainForm_Paint;
        Resize += (_, _) => UpdateRoundedRegion();
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        _refreshTimer.Tick += async (_, _) => await RefreshCurrentAsync();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CsDropShadow = 0x00020000;
            var cp = base.CreateParams;
            cp.ClassStyle |= CsDropShadow;
            return cp;
        }
    }

    private void BuildUi()
    {
        _pairLabel.Text = "USD/KRW · NAVER";
        _pairLabel.Font = new Font("Segoe UI Semibold", 9f);
        _pairLabel.ForeColor = Color.FromArgb(66, 77, 89);
        _pairLabel.Location = new Point(14, 11);
        _pairLabel.AutoSize = true;

        _statusLabel.Text = "● LIVE";
        _statusLabel.ForeColor = Color.FromArgb(22, 145, 86);
        _statusLabel.Font = new Font("Segoe UI Semibold", 7.4f);
        _statusLabel.Location = new Point(207, 12);
        _statusLabel.AutoSize = true;

        ConfigureWindowButton(_minimizeButton, "−", new Point(264, 6), isClose: false);
        ConfigureWindowButton(_closeButton, "×", new Point(289, 6), isClose: true);
        _minimizeButton.Click += (_, _) => HideWidget();
        _closeButton.Click += (_, _) => ExitApplication();
        _toolTip.SetToolTip(_minimizeButton, "Minimize to tray");
        _toolTip.SetToolTip(_closeButton, "Close");

        _rateLabel.Text = "—";
        _rateLabel.Font = new Font("Segoe UI Semibold", 23f);
        _rateLabel.ForeColor = Color.FromArgb(24, 31, 39);
        _rateLabel.Location = new Point(13, 35);
        _rateLabel.AutoSize = true;

        _changeLabel.Text = "Waiting for data";
        _changeLabel.Location = new Point(16, 73);
        _changeLabel.AutoSize = true;
        _changeLabel.Font = new Font("Segoe UI", 8.1f);

        var x = 119;
        foreach (var period in Periods)
        {
            var button = new Button { Text = period };
            button.SetBounds(x, 69, 34, 24);
            StyleSmallButton(button, selected: period == "1D");
            ApplyRoundedRegion(button, 6);
            button.Resize += (_, _) => ApplyRoundedRegion(button, 6);
            var captured = period;
            button.Click += (_, _) => SetPeriod(captured);
            _periodButtons[period] = button;
            Controls.Add(button);
            x += 37;
        }

        _chartCard.SetBounds(12, 100, 292, 83);
        _chartCard.BackColor = Color.White;
        _chartCard.Paint += ChartCard_Paint;
        _chartCard.Resize += (_, _) => ApplyRoundedRegion(_chartCard, 10);
        ApplyRoundedRegion(_chartCard, 10);

        _chart.SetBounds(7, 6, 278, 58);
        _chart.Font = new Font("Segoe UI", 7.5f);

        _rangeLabel.Text = "—";
        _rangeLabel.SetBounds(8, 65, 276, 14);
        _rangeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _rangeLabel.ForeColor = Color.FromArgb(107, 117, 128);
        _rangeLabel.Font = new Font("Segoe UI", 7.1f);

        _chartCard.Controls.Add(_chart);
        _chartCard.Controls.Add(_rangeLabel);

        _highLowLabel.Text = "H —   L —";
        _highLowLabel.Location = new Point(15, 190);
        _highLowLabel.AutoSize = true;
        _highLowLabel.ForeColor = Color.FromArgb(95, 106, 118);
        _highLowLabel.Font = new Font("Segoe UI Semibold", 7.4f);

        _updatedLabel.Text = "Updated —";
        _updatedLabel.SetBounds(205, 209, 96, 13);
        _updatedLabel.TextAlign = ContentAlignment.MiddleRight;
        _updatedLabel.ForeColor = Color.FromArgb(132, 141, 151);
        _updatedLabel.Font = new Font("Segoe UI", 7.1f);

        Controls.AddRange(new Control[]
        {
            _pairLabel, _statusLabel, _minimizeButton, _closeButton,
            _rateLabel, _changeLabel, _chartCard, _highLowLabel, _updatedLabel
        });
    }

    private void BuildMenusAndTray()
    {
        var widgetMenu = new ContextMenuStrip();
        widgetMenu.Items.Add("Refresh now", null, async (_, _) => await RefreshCurrentAsync());
        widgetMenu.Items.Add("Hide widget", null, (_, _) => HideWidget());
        widgetMenu.Items.Add(new ToolStripSeparator());

        _topMostItem.Checked = true;
        _topMostItem.CheckedChanged += (_, _) =>
        {
            if (_loadingSettings)
                return;

            TopMost = _topMostItem.Checked;
            _settings.AlwaysOnTop = TopMost;
            _ = SaveSettingsAsync();
        };
        widgetMenu.Items.Add(_topMostItem);

        _startupItem.Checked = StartupManager.IsEnabled();
        _startupItem.CheckedChanged += (_, _) =>
        {
            if (!_loadingSettings)
                SetStartup(_startupItem.Checked);
        };
        widgetMenu.Items.Add(_startupItem);

        widgetMenu.Items.Add(new ToolStripSeparator());
        widgetMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        ContextMenuStrip = widgetMenu;

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Show widget", null, (_, _) => ShowWidget());
        trayMenu.Items.Add("Refresh now", null, async (_, _) => await RefreshCurrentAsync());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon.Icon = SystemIcons.Application;
        _trayIcon.Text = "USD/KRW Widget";
        _trayIcon.Visible = true;
        _trayIcon.ContextMenuStrip = trayMenu;
        _trayIcon.DoubleClick += (_, _) => ShowWidget();
    }

    private static void ConfigureWindowButton(Button button, string text, Point location, bool isClose)
    {
        button.Text = text;
        button.SetBounds(location.X, location.Y, 22, 22);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = isClose
            ? Color.FromArgb(224, 75, 75)
            : Color.FromArgb(225, 231, 238);
        button.FlatAppearance.MouseOverBackColor = isClose
            ? Color.FromArgb(237, 92, 92)
            : Color.FromArgb(235, 239, 244);
        button.BackColor = Color.Transparent;
        button.ForeColor = isClose
            ? Color.FromArgb(112, 119, 128)
            : Color.FromArgb(105, 114, 124);
        button.Font = new Font("Segoe UI", isClose ? 10f : 11f, FontStyle.Regular);
        button.TabStop = false;
        button.Padding = Padding.Empty;
        button.TextAlign = ContentAlignment.MiddleCenter;
        ApplyRoundedRegion(button, 7);
        button.Resize += (_, _) => ApplyRoundedRegion(button, 7);
    }

    private static void StyleSmallButton(Button button, bool selected)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = selected
            ? Color.FromArgb(90, 132, 181)
            : Color.FromArgb(220, 226, 233);
        button.FlatAppearance.MouseOverBackColor = selected
            ? Color.FromArgb(218, 233, 250)
            : Color.FromArgb(246, 248, 251);
        button.BackColor = selected
            ? Color.FromArgb(226, 239, 253)
            : Color.White;
        button.ForeColor = selected
            ? Color.FromArgb(43, 91, 145)
            : Color.FromArgb(87, 98, 109);
        button.Font = new Font("Segoe UI Semibold", 7.3f);
        button.TabStop = false;
        button.Padding = Padding.Empty;
        button.TextAlign = ContentAlignment.MiddleCenter;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        _settings = await AppSettings.LoadAsync();
        RestoreWindowSettings();

        _points.AddRange(await _cache.LoadAsync());
        UpdatePeriodUi();

        await SyncHistoryAndCurrentAsync();
        _refreshTimer.Start();
    }

    private async Task SyncHistoryAndCurrentAsync()
    {
        try
        {
            _statusLabel.Text = "● SYNC";
            _statusLabel.ForeColor = Color.FromArgb(58, 111, 168);

            var history = await _client.GetDailyRatesAsync(400);
            MergePoints(history);
            await _cache.SaveAsync(_points);
            await RefreshCurrentAsync();
        }
        catch (Exception ex)
        {
            ShowOffline(ex.Message);
            UpdatePeriodUi();
        }
    }

    private async Task RefreshCurrentAsync()
    {
        try
        {
            var rate = await _client.GetCurrentRateAsync();
            var now = DateTime.Now;
            _points.Add(new RatePoint(now, rate));
            TrimAndDeduplicate();
            await _cache.SaveAsync(_points);

            _statusLabel.Text = "● NAVER";
            _statusLabel.ForeColor = Color.FromArgb(22, 145, 86);
            _updatedLabel.Text = $"Updated {now:HH:mm}";
            _trayIcon.Text = $"USD/KRW {rate:N2}";
            UpdatePeriodUi();
        }
        catch (Exception ex)
        {
            ShowOffline(ex.Message);
        }
    }

    private void SetPeriod(string period)
    {
        if (!Periods.Contains(period))
            period = "1D";

        _period = period;
        _settings.Period = period;
        UpdatePeriodUi();
        _ = SaveSettingsAsync();
    }

    private void MergePoints(IEnumerable<RatePoint> incoming)
    {
        _points.AddRange(incoming);
        TrimAndDeduplicate();
    }

    private void TrimAndDeduplicate()
    {
        var cutoff = DateTime.Now.AddDays(-400);
        var clean = _points
            .Where(p => p.Timestamp >= cutoff)
            .GroupBy(p => p.Timestamp)
            .Select(g => g.Last())
            .OrderBy(p => p.Timestamp)
            .ToList();

        _points.Clear();
        _points.AddRange(clean);
    }

    private void UpdatePeriodUi()
    {
        foreach (var pair in _periodButtons)
        {
            StyleSmallButton(pair.Value, pair.Key == _period);
            ApplyRoundedRegion(pair.Value, 6);
        }

        var now = DateTime.Now;
        var startDate = GetPeriodStart(now, _period);
        _rangeLabel.Text = FormatPeriodRange(startDate, now.Date, _period);

        if (_points.Count == 0)
            return;

        var periodPoints = _points
            .Where(p => p.Timestamp >= startDate)
            .OrderBy(p => p.Timestamp)
            .ToList();

        if (periodPoints.Count == 0)
            periodPoints = _points.TakeLast(Math.Min(50, _points.Count)).ToList();

        var latest = _points[^1].Rate;
        var first = periodPoints[0].Rate;
        var delta = latest - first;
        var percent = first == 0 ? 0 : delta / first * 100m;

        _rateLabel.Text = latest.ToString("N2", CultureInfo.InvariantCulture);
        var arrow = delta >= 0 ? "▲" : "▼";
        _changeLabel.Text = $"{arrow} {delta:+0.00;-0.00;0.00}  ({percent:+0.00;-0.00;0.00}%)  {_period}";
        _changeLabel.ForeColor = delta >= 0
            ? Color.FromArgb(22, 145, 86)
            : Color.FromArgb(211, 67, 67);

        var high = periodPoints.Max(p => p.Rate);
        var low = periodPoints.Min(p => p.Rate);
        _highLowLabel.Text = $"H  {high:N2}     L  {low:N2}";

        _chart.SetPoints(Downsample(periodPoints, 220));
    }

    private static DateTime GetPeriodStart(DateTime now, string period) => period switch
    {
        "1D" => now.Date,
        "1W" => now.Date.AddDays(-6),
        "1M" => now.Date.AddMonths(-1),
        "6M" => now.Date.AddMonths(-6),
        "1Y" => now.Date.AddYears(-1),
        _ => now.Date
    };

    private static string FormatPeriodRange(DateTime start, DateTime end, string period)
    {
        if (period == "1D")
            return end.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);

        if (start.Year == end.Year)
            return $"{start:yyyy.MM.dd}  —  {end:MM.dd}";

        return $"{start:yyyy.MM.dd}  —  {end:yyyy.MM.dd}";
    }

    private static IReadOnlyList<RatePoint> Downsample(IReadOnlyList<RatePoint> points, int maxPoints)
    {
        if (points.Count <= maxPoints)
            return points;

        var result = new List<RatePoint>(maxPoints);
        var step = (double)(points.Count - 1) / (maxPoints - 1);
        for (var i = 0; i < maxPoints; i++)
            result.Add(points[(int)Math.Round(i * step)]);
        return result;
    }

    private void ShowOffline(string detail)
    {
        _statusLabel.Text = "⚠ OFFLINE";
        _statusLabel.ForeColor = Color.FromArgb(188, 126, 34);
        _updatedLabel.Text = detail.Length > 23 ? detail[..23] + "…" : detail;
    }

    private void MainForm_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedRectPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var pen = new Pen(Color.FromArgb(205, 214, 224), 1f);
        e.Graphics.DrawPath(pen, path);
    }

    private void ChartCard_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedRectPath(new Rectangle(0, 0, _chartCard.Width - 1, _chartCard.Height - 1), 10);
        using var pen = new Pen(Color.FromArgb(221, 227, 234), 1f);
        e.Graphics.DrawPath(pen, path);
    }

    private void UpdateRoundedRegion() => ApplyRoundedRegion(this, CornerRadius);

    private static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        using var path = CreateRoundedRectPath(new Rectangle(0, 0, control.Width, control.Height), radius);
        var oldRegion = control.Region;
        control.Region = new Region(path);
        oldRegion?.Dispose();
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var diameter = Math.Max(2, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void EnableDragging(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _dragStart = e.Location;
        };

        control.MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            var screen = control.PointToScreen(e.Location);
            Location = new Point(screen.X - _dragStart.X, screen.Y - _dragStart.Y);
        };

        control.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            _settings.X = Left;
            _settings.Y = Top;
            _ = SaveSettingsAsync();
        };
    }

    private void RestoreWindowSettings()
    {
        _loadingSettings = true;
        try
        {
            _period = Periods.Contains(_settings.Period) ? _settings.Period : "1D";
            TopMost = _settings.AlwaysOnTop;
            _topMostItem.Checked = TopMost;

            var startupEnabled = StartupManager.IsEnabled();
            _startupItem.Checked = startupEnabled;
            _settings.StartWithWindows = startupEnabled;

            if (_settings.X is int x && _settings.Y is int y && IsVisibleOnAnyScreen(new Point(x, y)))
                Location = new Point(x, y);
            else
                PositionAtTopRight();
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void PositionAtTopRight()
    {
        var working = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(working.Right - Width - 14, working.Top + 14);
    }

    private static bool IsVisibleOnAnyScreen(Point location)
    {
        var testRect = new Rectangle(location, new Size(100, 50));
        return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(testRect));
    }

    private void SetStartup(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled);
            _settings.StartWithWindows = enabled;
            _ = SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            _loadingSettings = true;
            _startupItem.Checked = StartupManager.IsEnabled();
            _loadingSettings = false;
            MessageBox.Show(ex.Message, "USD/KRW Widget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void HideWidget() => Hide();

    private void ShowWidget()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        Close();
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideWidget();
            return;
        }

        _refreshTimer.Stop();
        _settings.X = Left;
        _settings.Y = Top;
        _settings.AlwaysOnTop = TopMost;
        _settings.Period = _period;
        await SaveSettingsAsync();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _toolTip.Dispose();
    }

    private Task SaveSettingsAsync() => _settings.SaveAsync();
}
