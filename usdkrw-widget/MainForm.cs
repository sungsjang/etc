using System.Globalization;
using UsdKrwWidget.Controls;
using UsdKrwWidget.Models;
using UsdKrwWidget.Services;

namespace UsdKrwWidget;

internal sealed class MainForm : Form
{
    private readonly TwelveDataClient _client;
    private readonly RateCache _cache = new();
    private readonly List<RatePoint> _points = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 5 * 60 * 1000 };

    private readonly Label _pairLabel = new();
    private readonly Label _rateLabel = new();
    private readonly Label _changeLabel = new();
    private readonly Label _highLowLabel = new();
    private readonly Label _updatedLabel = new();
    private readonly Label _statusLabel = new();
    private readonly SparklinePanel _chart = new();
    private readonly Button _dayButton = new();
    private readonly Button _weekButton = new();

    private bool _showWeek;
    private Point _dragStart;

    public MainForm()
    {
        Text = "USD/KRW Widget";
        ClientSize = new Size(330, 238);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(22, 24, 28);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9f);
        ShowInTaskbar = true;

        var working = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(working.Right - Width - 16, working.Top + 16);

        BuildUi();
        EnableDragging(this);
        EnableDragging(_pairLabel);
        EnableDragging(_rateLabel);
        EnableDragging(_changeLabel);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshCurrentAsync());
        menu.Items.Add("Always on top", null, (_, _) => TopMost = !TopMost);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Close());
        ContextMenuStrip = menu;

        try
        {
            _client = new TwelveDataClient();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "⚠ API key missing";
            _statusLabel.ForeColor = Color.Goldenrod;
            _updatedLabel.Text = ex.Message;
            _client = null!;
        }

        Load += MainForm_Load;
        _refreshTimer.Tick += async (_, _) => await RefreshCurrentAsync();
    }

    private void BuildUi()
    {
        _pairLabel.Text = "USD/KRW";
        _pairLabel.Font = new Font("Segoe UI Semibold", 10f);
        _pairLabel.Location = new Point(14, 10);
        _pairLabel.AutoSize = true;

        _statusLabel.Text = "● LIVE";
        _statusLabel.ForeColor = Color.FromArgb(70, 200, 120);
        _statusLabel.Location = new Point(263, 11);
        _statusLabel.AutoSize = true;

        _rateLabel.Text = "—";
        _rateLabel.Font = new Font("Segoe UI Semibold", 27f);
        _rateLabel.Location = new Point(12, 35);
        _rateLabel.AutoSize = true;

        _changeLabel.Text = "Waiting for data";
        _changeLabel.Location = new Point(17, 82);
        _changeLabel.AutoSize = true;

        _dayButton.Text = "1D";
        _dayButton.SetBounds(248, 78, 32, 25);
        StyleSmallButton(_dayButton, selected: true);
        _dayButton.Click += (_, _) => { _showWeek = false; UpdatePeriodUi(); };

        _weekButton.Text = "1W";
        _weekButton.SetBounds(284, 78, 32, 25);
        StyleSmallButton(_weekButton, selected: false);
        _weekButton.Click += (_, _) => { _showWeek = true; UpdatePeriodUi(); };

        _chart.SetBounds(14, 110, 302, 76);
        _chart.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

        _highLowLabel.Text = "H —    L —";
        _highLowLabel.Location = new Point(16, 193);
        _highLowLabel.AutoSize = true;
        _highLowLabel.ForeColor = Color.Silver;

        _updatedLabel.Text = "Updated —";
        _updatedLabel.Location = new Point(209, 213);
        _updatedLabel.AutoSize = true;
        _updatedLabel.ForeColor = Color.Gray;

        Controls.AddRange(new Control[]
        {
            _pairLabel, _statusLabel, _rateLabel, _changeLabel,
            _dayButton, _weekButton, _chart, _highLowLabel, _updatedLabel
        });
    }

    private static void StyleSmallButton(Button button, bool selected)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = selected ? Color.FromArgb(62, 68, 78) : Color.FromArgb(35, 38, 44);
        button.ForeColor = Color.WhiteSmoke;
        button.Font = new Font("Segoe UI Semibold", 8f);
        button.TabStop = false;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        _points.AddRange(await _cache.LoadAsync());
        UpdatePeriodUi();

        if (_client is null)
            return;

        try
        {
            _statusLabel.Text = "● SYNC";
            var history = await _client.GetRecentFiveMinuteRatesAsync();
            MergePoints(history);
            await RefreshCurrentAsync();
            _refreshTimer.Start();
        }
        catch (Exception ex)
        {
            ShowOffline(ex.Message);
            UpdatePeriodUi();
            _refreshTimer.Start();
        }
    }

    private async Task RefreshCurrentAsync()
    {
        if (_client is null)
            return;

        try
        {
            var rate = await _client.GetCurrentRateAsync();
            var now = DateTime.Now;
            _points.Add(new RatePoint(now, rate));
            TrimAndDeduplicate();
            await _cache.SaveAsync(_points);

            _statusLabel.Text = "● LIVE";
            _statusLabel.ForeColor = Color.FromArgb(70, 200, 120);
            _updatedLabel.Text = $"Updated {now:HH:mm}";
            UpdatePeriodUi();
        }
        catch (Exception ex)
        {
            ShowOffline(ex.Message);
        }
    }

    private void MergePoints(IEnumerable<RatePoint> incoming)
    {
        _points.AddRange(incoming);
        TrimAndDeduplicate();
    }

    private void TrimAndDeduplicate()
    {
        var cutoff = DateTime.Now.AddDays(-30);
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
        StyleSmallButton(_dayButton, !_showWeek);
        StyleSmallButton(_weekButton, _showWeek);

        if (_points.Count == 0)
            return;

        var now = DateTime.Now;
        var cutoff = _showWeek ? now.AddDays(-7) : now.Date;
        var period = _points.Where(p => p.Timestamp >= cutoff).OrderBy(p => p.Timestamp).ToList();
        if (period.Count == 0)
            period = _points.TakeLast(Math.Min(50, _points.Count)).ToList();

        var latest = _points[^1].Rate;
        var first = period[0].Rate;
        var delta = latest - first;
        var percent = first == 0 ? 0 : delta / first * 100m;

        _rateLabel.Text = latest.ToString("N2", CultureInfo.InvariantCulture);
        var arrow = delta >= 0 ? "▲" : "▼";
        _changeLabel.Text = $"{arrow} {delta:+0.00;-0.00;0.00}  ({percent:+0.00;-0.00;0.00}%)  {(_showWeek ? "1W" : "Today")}";
        _changeLabel.ForeColor = delta >= 0
            ? Color.FromArgb(70, 200, 120)
            : Color.FromArgb(235, 95, 95);

        var high = period.Max(p => p.Rate);
        var low = period.Min(p => p.Rate);
        _highLowLabel.Text = $"H {high:N2}    L {low:N2}";

        _chart.SetPoints(Downsample(period, 260));
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
        _statusLabel.ForeColor = Color.Goldenrod;
        _updatedLabel.Text = detail.Length > 25 ? detail[..25] + "…" : detail;
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
    }
}
