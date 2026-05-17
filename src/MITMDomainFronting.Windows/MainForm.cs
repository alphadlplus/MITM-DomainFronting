using System.Diagnostics;

namespace MITMDomainFronting.Windows;

internal sealed class MainForm : Form
{
    private readonly XrayManager _xray = new();
    private readonly NetworkTrafficMonitor _traffic = new();
    private readonly System.Windows.Forms.Timer _trafficTimer = new();

    private readonly Label _statusDot = new();
    private readonly Label _statusText = new();
    private readonly Label _statusSubtext = new();
    private readonly Label _downloadSpeedLabel = new();
    private readonly Label _downloadTotalLabel = new();
    private readonly Label _uploadSpeedLabel = new();
    private readonly Label _uploadTotalLabel = new();
    private readonly Label _downloadBar = new();
    private readonly Label _uploadBar = new();
    private readonly RoundedButton _connectButton = new();

    private bool _busy;

    public MainForm()
    {
        Text = "DomainFront Control";
        ClientSize = new Size(720, 420);
        MinimumSize = new Size(720, 459);
        MaximumSize = new Size(720, 459);
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BackColor = Color.FromArgb(244, 247, 251);
        Font = new Font("Segoe UI", 9);

        AppPaths.EnsureDirectories();
        BuildLayout();

        _trafficTimer.Interval = 1000;
        _trafficTimer.Tick += (_, _) => UpdateTraffic();

        FormClosing += (_, _) =>
        {
            if (_xray.IsRunning || ProxyManager.IsEnabled)
            {
                ProxyManager.RestoreOrDisable();
                _xray.Stop();
            }
        };

        RefreshConnectionUi();
    }

    private void BuildLayout()
    {
        Controls.Add(MakeHeader());
        Controls.Add(MakeConnectionPanel());
        Controls.Add(MakeTrafficPanel());
        Controls.Add(MakeFooter());
    }

    private Control MakeHeader()
    {
        var header = new Panel
        {
            Location = new Point(24, 18),
            Size = new Size(672, 58),
            BackColor = BackColor,
        };

        var title = new Label
        {
            Text = "DomainFront Control",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(18, 29, 45),
            Location = new Point(0, 0),
            Size = new Size(320, 32),
        };

        var subtitle = new Label
        {
            Text = "One-click local Xray proxy with device-only certificate trust",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(94, 105, 123),
            Location = new Point(2, 34),
            Size = new Size(420, 20),
        };

        var badge = new RoundedPanel
        {
            Location = new Point(524, 8),
            Size = new Size(148, 36),
            Radius = 18,
            FillColor = Color.FromArgb(232, 238, 247),
        };

        _statusDot.Text = "●";
        _statusDot.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        _statusDot.ForeColor = Color.FromArgb(153, 162, 178);
        _statusDot.Location = new Point(14, 4);
        _statusDot.Size = new Size(22, 26);

        _statusText.Text = "Offline";
        _statusText.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _statusText.ForeColor = Color.FromArgb(53, 65, 84);
        _statusText.Location = new Point(38, 9);
        _statusText.Size = new Size(92, 18);

        badge.Controls.Add(_statusDot);
        badge.Controls.Add(_statusText);

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(badge);
        return header;
    }

    private Control MakeConnectionPanel()
    {
        var panel = new RoundedPanel
        {
            Location = new Point(24, 94),
            Size = new Size(318, 238),
            Radius = 18,
            FillColor = Color.White,
        };

        var accent = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(8, 238),
            BackColor = Color.FromArgb(16, 185, 129),
        };

        var title = new Label
        {
            Text = "Connection",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(18, 29, 45),
            Location = new Point(26, 24),
            Size = new Size(200, 26),
        };

        _statusSubtext.Text = "Ready";
        _statusSubtext.Font = new Font("Segoe UI", 9);
        _statusSubtext.ForeColor = Color.FromArgb(94, 105, 123);
        _statusSubtext.Location = new Point(27, 54);
        _statusSubtext.Size = new Size(252, 38);

        var stateBox = new RoundedPanel
        {
            Location = new Point(26, 100),
            Size = new Size(252, 48),
            Radius = 10,
            FillColor = Color.FromArgb(245, 248, 252),
        };

        var stateTitle = new Label
        {
            Text = "Local proxy",
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(94, 105, 123),
            Location = new Point(14, 7),
            Size = new Size(130, 16),
        };

        var stateValue = new Label
        {
            Text = "127.0.0.1:10808",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(18, 29, 45),
            Location = new Point(14, 22),
            Size = new Size(160, 20),
        };

        stateBox.Controls.Add(stateTitle);
        stateBox.Controls.Add(stateValue);

        _connectButton.Text = "Start Session";
        _connectButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _connectButton.Radius = 12;
        _connectButton.FillColor = Color.FromArgb(16, 185, 129);
        _connectButton.HoverColor = Color.FromArgb(5, 150, 105);
        _connectButton.PressedColor = Color.FromArgb(4, 120, 87);
        _connectButton.Location = new Point(26, 166);
        _connectButton.Size = new Size(252, 48);
        _connectButton.Click += async (_, _) => await ToggleConnectionAsync();

        panel.Controls.Add(accent);
        panel.Controls.Add(title);
        panel.Controls.Add(_statusSubtext);
        panel.Controls.Add(stateBox);
        panel.Controls.Add(_connectButton);
        return panel;
    }

    private Control MakeTrafficPanel()
    {
        var panel = new RoundedPanel
        {
            Location = new Point(362, 94),
            Size = new Size(334, 238),
            Radius = 18,
            FillColor = Color.White,
        };

        var title = new Label
        {
            Text = "Traffic",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(18, 29, 45),
            Location = new Point(24, 24),
            Size = new Size(200, 26),
        };

        var hint = new Label
        {
            Text = "Measured from Windows network counters while connected",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(123, 134, 152),
            Location = new Point(25, 52),
            Size = new Size(270, 18),
        };

        panel.Controls.Add(title);
        panel.Controls.Add(hint);
        panel.Controls.Add(MakeTrafficRow("Download", Color.FromArgb(37, 99, 235), new Point(24, 88), _downloadSpeedLabel, _downloadTotalLabel, _downloadBar));
        panel.Controls.Add(MakeTrafficRow("Upload", Color.FromArgb(245, 158, 11), new Point(24, 158), _uploadSpeedLabel, _uploadTotalLabel, _uploadBar));
        return panel;
    }

    private static Control MakeTrafficRow(
        string title,
        Color color,
        Point location,
        Label speedLabel,
        Label totalLabel,
        Label bar)
    {
        var row = new Panel
        {
            Location = location,
            Size = new Size(286, 54),
            BackColor = Color.White,
        };

        var icon = new Label
        {
            BackColor = color,
            Location = new Point(0, 7),
            Size = new Size(8, 40),
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(94, 105, 123),
            Location = new Point(18, 2),
            Size = new Size(100, 18),
        };

        speedLabel.Text = "0 B/s";
        speedLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        speedLabel.ForeColor = Color.FromArgb(18, 29, 45);
        speedLabel.Location = new Point(18, 18);
        speedLabel.Size = new Size(126, 28);

        totalLabel.Text = "Total: 0 B";
        totalLabel.Font = new Font("Segoe UI", 8);
        totalLabel.ForeColor = Color.FromArgb(123, 134, 152);
        totalLabel.TextAlign = ContentAlignment.MiddleRight;
        totalLabel.Location = new Point(150, 22);
        totalLabel.Size = new Size(132, 20);

        var track = new RoundedPanel
        {
            FillColor = Color.FromArgb(236, 241, 247),
            Radius = 4,
            Location = new Point(18, 47),
            Size = new Size(264, 6),
        };

        bar.BackColor = color;
        bar.Location = new Point(18, 47);
        bar.Size = new Size(1, 6);

        row.Controls.Add(icon);
        row.Controls.Add(titleLabel);
        row.Controls.Add(speedLabel);
        row.Controls.Add(totalLabel);
        row.Controls.Add(track);
        row.Controls.Add(bar);
        bar.BringToFront();
        return row;
    }

    private Control MakeFooter()
    {
        var panel = new Panel
        {
            Location = new Point(24, 350),
            Size = new Size(672, 48),
            BackColor = BackColor,
        };

        panel.Controls.Add(MakeFooterButton("Certificate", new Point(0, 4), async (_, _) => await RunSetupActionAsync(() => CertificateManager.InstallTrustedRootAsync())));
        panel.Controls.Add(MakeFooterButton("New CA", new Point(136, 4), async (_, _) => await RunSetupActionAsync(() => CertificateManager.GenerateAsync(_xray.XrayExePath))));
        panel.Controls.Add(MakeFooterButton("Remove CA", new Point(272, 4), async (_, _) => await RunSetupActionAsync(() => CertificateManager.RemoveTrustedRootAsync())));
        panel.Controls.Add(MakeFooterButton("Log", new Point(486, 4), (_, _) => OpenLog()));
        panel.Controls.Add(MakeFooterButton("Folder", new Point(580, 4), (_, _) => Process.Start(new ProcessStartInfo(AppPaths.DataDir) { UseShellExecute = true })));
        return panel;
    }

    private static Button MakeFooterButton(string text, Point location, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(text.Length > 8 ? 120 : 82, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(53, 65, 84),
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(219, 226, 237);
        button.Click += handler;
        return button;
    }

    private async Task ToggleConnectionAsync()
    {
        if (_busy)
        {
            return;
        }

        if (IsConnected() || IsBrokenProxyState())
        {
            await DisconnectAsync();
            return;
        }

        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        _busy = true;
        RefreshConnectionUi("Connecting...");

        try
        {
            if (!CertificateManager.HasCertificate)
            {
                RefreshConnectionUi("Generating a private CA for this device...");
                await CertificateManager.GenerateAsync(_xray.XrayExePath);
            }

            if (!CertificateManager.IsTrustedRoot)
            {
                var confirm = MessageBox.Show(
                    this,
                    "Windows needs to trust the local device certificate once. Continue?",
                    "Certificate setup",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information);

                if (confirm != DialogResult.OK)
                {
                    RefreshConnectionUi("Certificate setup cancelled");
                    return;
                }

                RefreshConnectionUi("Waiting for Windows certificate prompt...");
                await CertificateManager.InstallTrustedRootAsync();
            }

            RefreshConnectionUi("Starting local proxy...");
            _xray.Start();

            RefreshConnectionUi("Routing Windows traffic...");
            ProxyManager.Enable();

            _traffic.Start();
            _trafficTimer.Start();
            RefreshConnectionUi("Session active");
        }
        catch (Exception ex)
        {
            ProxyManager.RestoreOrDisable();
            _xray.Stop();
            _trafficTimer.Stop();
            ResetTrafficUi();
            RefreshConnectionUi("Could not connect");
            MessageBox.Show(this, ex.Message, "DomainFront Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            RefreshConnectionUi();
        }
    }

    private async Task DisconnectAsync()
    {
        _busy = true;
        RefreshConnectionUi("Disconnecting...");

        await Task.Run(() =>
        {
            ProxyManager.RestoreOrDisable();
            _xray.Stop();
        });

        _trafficTimer.Stop();
        _busy = false;
        RefreshConnectionUi("Ready");
    }

    private async Task RunSetupActionAsync(Func<Task> action)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        RefreshConnectionUi("Working...");

        try
        {
            await action();
            RefreshConnectionUi("Ready");
        }
        catch (Exception ex)
        {
            RefreshConnectionUi("Action failed");
            MessageBox.Show(this, ex.Message, "DomainFront Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            RefreshConnectionUi();
        }
    }

    private void RefreshConnectionUi(string? message = null)
    {
        var connected = IsConnected();
        var broken = IsBrokenProxyState();

        if (broken)
        {
            _statusDot.ForeColor = Color.FromArgb(239, 68, 68);
            _statusText.Text = "Attention";
            _statusSubtext.Text = message ?? "Proxy is enabled but Xray is stopped.";
            _connectButton.Text = "Fix Proxy";
            _connectButton.FillColor = Color.FromArgb(239, 68, 68);
            _connectButton.HoverColor = Color.FromArgb(220, 38, 38);
            _connectButton.PressedColor = Color.FromArgb(185, 28, 28);
        }
        else if (connected)
        {
            _statusDot.ForeColor = Color.FromArgb(16, 185, 129);
            _statusText.Text = "Connected";
            _statusSubtext.Text = message ?? "Local proxy is active.";
            _connectButton.Text = "Stop Session";
            _connectButton.FillColor = Color.FromArgb(18, 29, 45);
            _connectButton.HoverColor = Color.FromArgb(45, 59, 81);
            _connectButton.PressedColor = Color.FromArgb(10, 18, 30);
        }
        else
        {
            _statusDot.ForeColor = Color.FromArgb(153, 162, 178);
            _statusText.Text = "Offline";
            _statusSubtext.Text = message ?? "Ready";
            _connectButton.Text = "Start Session";
            _connectButton.FillColor = Color.FromArgb(16, 185, 129);
            _connectButton.HoverColor = Color.FromArgb(5, 150, 105);
            _connectButton.PressedColor = Color.FromArgb(4, 120, 87);
        }

        _connectButton.Enabled = !_busy;
        _connectButton.Invalidate();
    }

    private void UpdateTraffic()
    {
        var sample = _traffic.Sample();

        _downloadSpeedLabel.Text = FormatSpeed(sample.DownloadBytesPerSecond);
        _uploadSpeedLabel.Text = FormatSpeed(sample.UploadBytesPerSecond);
        _downloadTotalLabel.Text = "Total: " + FormatBytes(sample.TotalDownloadedBytes);
        _uploadTotalLabel.Text = "Total: " + FormatBytes(sample.TotalUploadedBytes);

        UpdateBar(_downloadBar, sample.DownloadBytesPerSecond);
        UpdateBar(_uploadBar, sample.UploadBytesPerSecond);
        RefreshConnectionUi();
    }

    private void ResetTrafficUi()
    {
        _downloadSpeedLabel.Text = "0 B/s";
        _uploadSpeedLabel.Text = "0 B/s";
        _downloadTotalLabel.Text = "Total: 0 B";
        _uploadTotalLabel.Text = "Total: 0 B";
        _downloadBar.Width = 1;
        _uploadBar.Width = 1;
    }

    private static void UpdateBar(Control bar, double bytesPerSecond)
    {
        const double twoMegabytes = 2 * 1024 * 1024;
        var percent = Math.Clamp(bytesPerSecond / twoMegabytes, 0.02, 1.0);
        bar.Width = (int)(264 * percent);
    }

    private bool IsConnected() => _xray.IsRunning && ProxyManager.IsEnabled;

    private bool IsBrokenProxyState() => ProxyManager.IsEnabled && !_xray.IsRunning;

    private static string FormatSpeed(double bytesPerSecond) =>
        FormatBytes((ulong)Math.Max(0, bytesPerSecond)) + "/s";

    private static string FormatBytes(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var index = 0;

        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return index == 0 ? $"{value:0} {units[index]}" : $"{value:0.##} {units[index]}";
    }

    private static void OpenLog()
    {
        if (!File.Exists(AppPaths.LogPath))
        {
            File.WriteAllText(AppPaths.LogPath, string.Empty);
        }

        Process.Start(new ProcessStartInfo(AppPaths.LogPath) { UseShellExecute = true });
    }
}

