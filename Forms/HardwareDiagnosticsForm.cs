using ISO11820WinForms.Core;
using ISO11820WinForms.Services;
using ISO11820WinForms.UI;
using ISO11820WinForms.Utilities;

namespace ISO11820WinForms.Forms
{
    public sealed class HardwareDiagnosticsForm : Form
    {
        private readonly DaqWorker? _daqWorker;
        private readonly TestMaster1? _testMaster;
        private readonly HardwareDiagnosticService _diagnosticService = new();
        private readonly TextBox _resultTextBox;
        private readonly Label _modeLabel;
        private readonly Button _probeAllButton;
        private readonly Button _probePidButton;
        private readonly Button _probeSensorButton;

        public HardwareDiagnosticsForm(DaqWorker? daqWorker, TestMaster1? testMaster)
        {
            _daqWorker = daqWorker;
            _testMaster = testMaster;

            Text = "硬件诊断";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, 430);
            MinimumSize = new Size(480, 360);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            UiTheme.ApplyFormTheme(this, dialog: true);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(14),
                BackColor = UiTheme.AppBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            _modeLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Padding = new Padding(12, 8, 12, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 3)
            };

            _probeAllButton = CreateActionButton("测试全部");
            _probePidButton = CreateActionButton("测试 PID");
            _probeSensorButton = CreateActionButton("测试 ADAM");
            buttons.Controls.Add(_probeAllButton);
            buttons.Controls.Add(_probePidButton);
            buttons.Controls.Add(_probeSensorButton);

            _resultTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.Ink,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                WordWrap = true
            };

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.SurfaceStrongAlt,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Padding = new Padding(12, 0, 12, 0),
                Text = "测试会复用系统现有串口访问逻辑；真实硬件模式下可能等待串口超时。",
                TextAlign = ContentAlignment.MiddleLeft
            };

            root.Controls.Add(_modeLabel, 0, 0);
            root.Controls.Add(buttons, 0, 1);
            root.Controls.Add(_resultTextBox, 0, 2);
            root.Controls.Add(hint, 0, 3);
            Controls.Add(root);

            _probeAllButton.Click += async (_, _) => await RunProbeAsync(() =>
                _diagnosticService.ProbeAll(_daqWorker, _testMaster?.Manipulator));
            _probePidButton.Click += async (_, _) => await RunProbeAsync(() =>
                new[] { _diagnosticService.ProbePid(_testMaster?.Manipulator) });
            _probeSensorButton.Click += async (_, _) => await RunProbeAsync(() =>
                new[] { _diagnosticService.ProbeSensor(_daqWorker) });

            RefreshSummary();
        }

        private Button CreateActionButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 118,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                Tag = ButtonTone.Secondary
            };
            UiTheme.StyleButton(button, ButtonTone.Secondary);
            button.Height = 34;
            button.MinimumSize = new Size(96, 34);
            return button;
        }

        private async Task RunProbeAsync(Func<IReadOnlyList<HardwareDiagnosticResult>> probe)
        {
            SetButtonsEnabled(false);
            _resultTextBox.Text = "正在测试...\r\n";

            try
            {
                var results = await Task.Run(probe);
                _resultTextBox.Text = FormatResults(results);
                RefreshSummary();
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"诊断执行失败：{ex.Message}";
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void RefreshSummary()
        {
            var pid = _testMaster?.Manipulator;
            var pidMode = pid?.IsSimulationMode == true;
            var sensorMode = _daqWorker?.IsSimulationMode == true;
            var mode = pidMode || sensorMode ? "仿真模式" : "真实硬件模式";
            var pidState = pid?.IsConnected == true ? "已连接" : "未连接";
            var sensorState = _daqWorker?.IsSensorConnected == true ? "已连接" : "未连接";

            _modeLabel.Text =
                $"{mode}\r\nPID：{pid?.PidPortName ?? ConfigurationHelper.GetPidPort()} / {pidState}    " +
                $"ADAM：{_daqWorker?.SensorPortName ?? ConfigurationHelper.GetSensorPort()} / {sensorState}";
        }

        private static string FormatResults(IReadOnlyList<HardwareDiagnosticResult> results)
        {
            return string.Join(
                Environment.NewLine + Environment.NewLine,
                results.Select(result =>
                    $"{result.DeviceName} [{(result.Success ? "成功" : "失败")}]\r\n" +
                    $"端口：{result.PortName}\r\n" +
                    $"详情：{result.Detail}" +
                    (string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? string.Empty
                        : $"\r\n错误：{result.ErrorMessage}")));
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _probeAllButton.Enabled = enabled;
            _probePidButton.Enabled = enabled;
            _probeSensorButton.Enabled = enabled;
        }
    }
}
