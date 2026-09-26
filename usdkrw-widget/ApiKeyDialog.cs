namespace UsdKrwWidget;

internal sealed class ApiKeyDialog : Form
{
    private readonly TextBox _apiKeyBox = new();

    public string ApiKey => _apiKeyBox.Text.Trim();

    public ApiKeyDialog(string currentKey = "")
    {
        Text = "USD/KRW Widget Settings";
        ClientSize = new Size(420, 155);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);

        var title = new Label
        {
            Text = "Twelve Data API key",
            AutoSize = true,
            Location = new Point(18, 18),
            Font = new Font("Segoe UI Semibold", 10f)
        };

        var help = new Label
        {
            Text = "The key is saved to your Windows user environment, not to GitHub.",
            AutoSize = true,
            Location = new Point(18, 43),
            ForeColor = Color.DimGray
        };

        _apiKeyBox.SetBounds(18, 70, 384, 25);
        _apiKeyBox.Text = currentKey;
        _apiKeyBox.UseSystemPasswordChar = true;

        var show = new CheckBox
        {
            Text = "Show key",
            AutoSize = true,
            Location = new Point(18, 105)
        };
        show.CheckedChanged += (_, _) => _apiKeyBox.UseSystemPasswordChar = !show.Checked;

        var save = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK
        };
        save.SetBounds(246, 104, 75, 28);

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        cancel.SetBounds(327, 104, 75, 28);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange(new Control[] { title, help, _apiKeyBox, show, save, cancel });
    }
}
