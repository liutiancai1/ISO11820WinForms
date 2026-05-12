using ISO11820WinForms.Services;
using ISO11820WinForms.UI;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820WinForms.Forms.Controls
{
    public sealed class CalibrationView : UserControl
    {
        private readonly Dictionary<string, double> _surfaceTempData = new();
        private readonly Dictionary<int, double> _centerTempData = new();
        private readonly Dictionary<string, Label> _surfaceTempLabels = new();
        private readonly Dictionary<string, TextBox> _resultTextBoxes = new();
        private readonly Dictionary<int, TextBox> _centerTempTextBoxes = new();

        private Label _surfaceTempValue = null!;
        private Label _centerTempValue = null!;
        private Label _stableStatusValue = null!;
        private Label _surfaceProgressValue = null!;
        private Label _centerProgressValue = null!;
        private ComboBox _surfacePositionComboBox = null!;
        private ComboBox _centerPositionComboBox = null!;
        private PlotModel _centerChartModel = null!;
        private LineSeries _centerTempSeries = null!;
        private double _currentCalibrationTemperature;

        public event EventHandler? HistoryRequested;
        public event Action<string>? SystemMessageGenerated;

        public CalibrationView()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.AppBackground;
            Font = new Font("Microsoft YaHei", 10F, FontStyle.Regular, GraphicsUnit.Point);

            InitializeData();
            BuildLayout();
            SetCalibrationTemperature(0);
            UpdateSurfaceTempDisplay();
            UpdateCenterTempDisplay();
            UpdateProgress();
        }

        public void SetCalibrationTemperature(double temperature)
        {
            _currentCalibrationTemperature = temperature;
            string text = temperature.ToString("F1");
            _surfaceTempValue.Text = text;
            _centerTempValue.Text = text;
            UpdateStabilityIndicator(temperature);
        }

        private void InitializeData()
        {
            _surfaceTempData.Clear();
            foreach (var key in CalibrationCalculationService.SurfacePositions)
            {
                _surfaceTempData[key] = 0.0;
            }

            _centerTempData.Clear();
            foreach (var position in CalibrationCalculationService.CenterPositions)
            {
                _centerTempData[position] = 0.0;
            }
        }

        private void BuildLayout()
        {
            Controls.Clear();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(14),
                BackColor = UiTheme.AppBackground
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 49F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 51F));

            root.Controls.Add(CreateStatusBar(), 0, 0);
            root.Controls.Add(CreateSurfaceSection(), 0, 1);
            root.Controls.Add(CreateCenterSection(), 0, 2);

            Controls.Add(root);
        }

        private Control CreateStatusBar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            _surfaceTempValue = CreateMetricValueLabel();
            _stableStatusValue = CreateMetricValueLabel();
            _surfaceProgressValue = CreateMetricValueLabel();
            _centerProgressValue = CreateMetricValueLabel();

            layout.Controls.Add(CreateMetricBlock("炉壁校温", _surfaceTempValue, "℃"), 0, 0);
            layout.Controls.Add(CreateMetricBlock("稳定状态", _stableStatusValue), 1, 0);
            layout.Controls.Add(CreateMetricBlock("炉壁记录", _surfaceProgressValue), 2, 0);
            layout.Controls.Add(CreateMetricBlock("中心轴记录", _centerProgressValue), 3, 0);
            layout.Controls.Add(CreateHistoryBlock(), 4, 0);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateMetricBlock(string title, Label valueLabel, string suffix = "")
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 0, 8, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24
            };
            UiTheme.StyleMetricTitle(titleLabel);

            var valuePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            valuePanel.Controls.Add(valueLabel);
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                var suffixLabel = new Label
                {
                    Text = suffix,
                    AutoSize = true,
                    ForeColor = UiTheme.InkMuted,
                    Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point),
                    Margin = new Padding(4, 13, 0, 0)
                };
                valuePanel.Controls.Add(suffixLabel);
            }

            panel.Controls.Add(valuePanel);
            panel.Controls.Add(titleLabel);
            return panel;
        }

        private Control CreateHistoryBlock()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 12, 8, 12),
                BackColor = Color.Transparent
            };

            var button = new Button
            {
                Text = "历史记录",
                Name = "btnCalibrationHistory",
                Dock = DockStyle.Fill
            };
            UiTheme.StyleButton(button, ButtonTone.Secondary, compact: true);
            button.Click += (_, _) => HistoryRequested?.Invoke(this, EventArgs.Empty);

            panel.Controls.Add(button);
            return panel;
        }

        private Label CreateMetricValueLabel()
        {
            var label = new Label
            {
                AutoSize = false,
                Width = 118,
                Height = 38,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            UiTheme.StyleMetricValue(label, UiTheme.MetricGlow);
            return label;
        }

        private Control CreateSurfaceSection()
        {
            var shell = CreateSectionShell("炉壁温度均匀性校验", out var content);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateSurfaceControlPanel(), 0, 0);
            layout.Controls.Add(CreateSurfaceGrid(), 1, 0);
            layout.Controls.Add(CreateSurfaceResultsPanel(), 2, 0);
            content.Controls.Add(layout);

            return shell;
        }

        private Control CreateSurfaceControlPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(8),
                BackColor = UiTheme.Surface
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            panel.Controls.Add(CreateSmallLabel("炉壁点位"), 0, 0);

            _surfacePositionComboBox = new ComboBox
            {
                Name = "cmbSurfacePosition",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _surfacePositionComboBox.Items.AddRange(new object[]
            {
                "A-1 (0°)", "A-2 (120°)", "A-3 (240°)",
                "B-1 (0°)", "B-2 (120°)", "B-3 (240°)",
                "C-1 (0°)", "C-2 (120°)", "C-3 (240°)"
            });
            _surfacePositionComboBox.SelectedIndex = 0;
            panel.Controls.Add(_surfacePositionComboBox, 0, 1);

            var recordButton = new Button
            {
                Text = "记录",
                Name = "btnRecordSurface",
                Dock = DockStyle.Fill
            };
            UiTheme.StyleButton(recordButton, ButtonTone.Warning, compact: true);
            recordButton.Click += (_, _) => RecordSelectedSurfaceTemperature();
            panel.Controls.Add(recordButton, 0, 3);

            var calculateButton = new Button
            {
                Text = "计算",
                Name = "btnCalculateSurface",
                Dock = DockStyle.Fill
            };
            UiTheme.StyleButton(calculateButton, ButtonTone.Primary, compact: true);
            calculateButton.Click += (_, _) => CalculateSurfaceUniformity();
            panel.Controls.Add(calculateButton, 0, 4);

            var tempPanel = CreateInlineTemperatureBlock();
            panel.Controls.Add(tempPanel, 0, 5);

            return panel;
        }

        private Control CreateInlineTemperatureBlock()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(8, 4, 8, 4)
            };

            var label = new Label
            {
                Text = "当前校温",
                Dock = DockStyle.Left,
                Width = 80,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _centerTempValue = new Label
            {
                Text = "0.0",
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.MetricGlow,
                Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleRight
            };

            panel.Controls.Add(_centerTempValue);
            panel.Controls.Add(label);
            return panel;
        }

        private Control CreateSurfaceGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = UiTheme.GridLine,
                Margin = new Padding(8, 0, 8, 0)
            };

            for (int i = 0; i < 4; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            AddGridHeader(grid, 0, 0, "点位 / 温度(℃)");
            AddGridHeader(grid, 1, 0, "A 平面(30mm)");
            AddGridHeader(grid, 2, 0, "B 平面(0mm)");
            AddGridHeader(grid, 3, 0, "C 平面(-30mm)");

            AddGridHeader(grid, 0, 1, "第一点(0°)");
            AddSurfaceValueCell(grid, 1, 1, "A1");
            AddSurfaceValueCell(grid, 2, 1, "B1");
            AddSurfaceValueCell(grid, 3, 1, "C1");

            AddGridHeader(grid, 0, 2, "第二点(120°)");
            AddSurfaceValueCell(grid, 1, 2, "A2");
            AddSurfaceValueCell(grid, 2, 2, "B2");
            AddSurfaceValueCell(grid, 3, 2, "C2");

            AddGridHeader(grid, 0, 3, "第三点(240°)");
            AddSurfaceValueCell(grid, 1, 3, "A3");
            AddSurfaceValueCell(grid, 2, 3, "B3");
            AddSurfaceValueCell(grid, 3, 3, "C3");

            return grid;
        }

        private Control CreateSurfaceResultsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = UiTheme.Surface
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            for (int i = 0; i < 8; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }

            AddResultRow(panel, 0, "T.avg:", "TAvg", null, null);
            AddResultRow(panel, 1, "T.avg.axis1:", "TAvgAxis1", "T.avg.levela:", "TAvgLevelA");
            AddResultRow(panel, 2, "T.avg.axis2:", "TAvgAxis2", "T.avg.levelb:", "TAvgLevelB");
            AddResultRow(panel, 3, "T.avg.axis3:", "TAvgAxis3", "T.avg.levelc:", "TAvgLevelC");
            AddResultRow(panel, 4, "T.dev.axis1:", "TDevAxis1", "T.dev.levela:", "TDevLevelA");
            AddResultRow(panel, 5, "T.dev.axis2:", "TDevAxis2", "T.dev.levelb:", "TDevLevelB");
            AddResultRow(panel, 6, "T.dev.axis3:", "TDevAxis3", "T.dev.levelc:", "TDevLevelC");
            AddResultRow(panel, 7, "T.avg.dev.axis:", "TAvgDevAxis", "T.avg.dev.level:", "TAvgDevLevel");

            return panel;
        }

        private Control CreateCenterSection()
        {
            var shell = CreateSectionShell("中心轴温度分布", out var content);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateCenterControlPanel(), 0, 0);
            layout.Controls.Add(CreateCenterChartPanel(), 1, 0);
            layout.Controls.Add(CreateCenterRecordPanel(), 2, 0);
            content.Controls.Add(layout);

            return shell;
        }

        private Control CreateCenterControlPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(8),
                BackColor = UiTheme.Surface
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            panel.Controls.Add(CreateSmallLabel("中心点位"), 0, 0);

            _centerPositionComboBox = new ComboBox
            {
                Name = "cmbCenterPosition",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var position in CalibrationCalculationService.CenterPositions)
            {
                _centerPositionComboBox.Items.Add($"{position}(mm)");
            }
            _centerPositionComboBox.SelectedIndex = 0;
            panel.Controls.Add(_centerPositionComboBox, 0, 1);

            var recordButton = new Button
            {
                Text = "记录",
                Name = "btnRecordCenter",
                Dock = DockStyle.Fill
            };
            UiTheme.StyleButton(recordButton, ButtonTone.Warning, compact: true);
            recordButton.Click += (_, _) => RecordSelectedCenterTemperature();
            panel.Controls.Add(recordButton, 0, 3);

            var resetButton = new Button
            {
                Text = "重置",
                Name = "btnResetCenter",
                Dock = DockStyle.Fill
            };
            UiTheme.StyleButton(resetButton, ButtonTone.Neutral, compact: true);
            resetButton.Click += (_, _) => ResetCenterTemperatures();
            panel.Controls.Add(resetButton, 0, 4);

            return panel;
        }

        private Control CreateCenterChartPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 8, 0)
            };
            UiTheme.StylePlotHost(panel);

            InitializeCenterChart(panel);
            return panel;
        }

        private Control CreateCenterRecordPanel()
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                AutoScroll = true
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = CalibrationCalculationService.CenterPositions.Length,
                AutoSize = true,
                BackColor = UiTheme.Surface
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            int row = 0;
            foreach (var position in CalibrationCalculationService.CenterPositions)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
                table.Controls.Add(CreateResultLabel($"{position}(mm):"), 0, row);

                var textBox = CreateResultTextBox($"txtCenter{position}");
                _centerTempTextBoxes[position] = textBox;
                table.Controls.Add(textBox, 1, row);
                row++;
            }

            wrapper.Controls.Add(table);
            return wrapper;
        }

        private Control CreateSectionShell(string title, out Panel content)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(titleLabel, 0, 0);

            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            layout.Controls.Add(content, 0, 1);

            panel.Controls.Add(layout);
            return panel;
        }

        private void InitializeCenterChart(Control parent)
        {
            _centerChartModel = new PlotModel();

            _centerChartModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "位置(mm)",
                Minimum = 0,
                Maximum = 150,
                MajorStep = 10,
                MinorStep = 5,
                MajorGridlineStyle = LineStyle.Solid
            });

            _centerChartModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度(℃)",
                Minimum = 0,
                Maximum = 800,
                MajorStep = 100,
                MinorStep = 20,
                MajorGridlineStyle = LineStyle.Solid
            });

            _centerTempSeries = new LineSeries
            {
                Title = "记录值",
                Color = OxyColor.FromRgb(UiTheme.Accent.R, UiTheme.Accent.G, UiTheme.Accent.B),
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = OxyColor.FromRgb(UiTheme.Warning.R, UiTheme.Warning.G, UiTheme.Warning.B),
                StrokeThickness = 2.4
            };
            _centerChartModel.Series.Add(_centerTempSeries);
            UiTheme.StylePlot(_centerChartModel, "中心轴温度分布");

            var plotView = new PlotView
            {
                Dock = DockStyle.Fill,
                Model = _centerChartModel,
                BackColor = UiTheme.Surface
            };

            parent.Controls.Add(plotView);
        }

        private void RecordSelectedSurfaceTemperature()
        {
            if (_surfacePositionComboBox.SelectedItem == null)
            {
                ShowInfo("请先选择炉壁点位。");
                return;
            }

            string selectedPosition = _surfacePositionComboBox.SelectedItem.ToString() ?? string.Empty;
            string positionKey = selectedPosition.Substring(0, 3).Replace("-", "");
            _surfaceTempData[positionKey] = _currentCalibrationTemperature;

            UpdateSurfaceTempDisplay();
            UpdateProgress();
            SystemMessageGenerated?.Invoke($"已记录炉壁温度：{selectedPosition} = {_currentCalibrationTemperature:F1}℃");
        }

        private void CalculateSurfaceUniformity()
        {
            if (_surfaceTempData.Values.Any(value => value == 0.0))
            {
                ShowInfo("请先记录所有炉壁点位的温度。");
                return;
            }

            var result = CalibrationCalculationService.CalculateSurfaceUniformity(_surfaceTempData);
            DisplayUniformityResult(result);
            SystemMessageGenerated?.Invoke("温度均匀性计算完成。");
        }

        private void RecordSelectedCenterTemperature()
        {
            if (_centerPositionComboBox.SelectedItem == null)
            {
                ShowInfo("请先选择中心点位。");
                return;
            }

            string selectedPosition = _centerPositionComboBox.SelectedItem.ToString() ?? string.Empty;
            string positionText = selectedPosition.Replace("(mm)", "").Trim();
            if (!int.TryParse(positionText, out int position))
            {
                return;
            }

            _centerTempData[position] = _currentCalibrationTemperature;
            UpdateCenterTempDisplay();
            UpdateCenterChart();
            UpdateProgress();
            SystemMessageGenerated?.Invoke($"已记录中心轴温度：{position}(mm) = {_currentCalibrationTemperature:F1}℃");
        }

        private void ResetCenterTemperatures()
        {
            if (_centerTempData.Values.All(value => value == 0.0))
            {
                ShowInfo("没有需要重置的中心轴温度数据。");
                return;
            }

            var result = MessageBox.Show(
                "是否重置中心轴温度数据？",
                "重置确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (var position in CalibrationCalculationService.CenterPositions)
            {
                _centerTempData[position] = 0.0;
            }

            UpdateCenterTempDisplay();
            UpdateCenterChart();
            UpdateProgress();
            SystemMessageGenerated?.Invoke("已重置中心轴温度记录。");
        }

        private void UpdateSurfaceTempDisplay()
        {
            foreach (var item in _surfaceTempLabels)
            {
                item.Value.Text = _surfaceTempData[item.Key].ToString("F1");
            }
        }

        private void UpdateCenterTempDisplay()
        {
            foreach (var item in _centerTempTextBoxes)
            {
                item.Value.Text = _centerTempData[item.Key].ToString("F1");
            }
        }

        private void UpdateCenterChart()
        {
            _centerTempSeries.Points.Clear();

            foreach (var item in _centerTempData.Where(item => item.Value > 0).OrderBy(item => item.Key))
            {
                _centerTempSeries.Points.Add(new DataPoint(item.Key, item.Value));
            }

            _centerChartModel.InvalidatePlot(true);
        }

        private void UpdateProgress()
        {
            _surfaceProgressValue.Text = $"{_surfaceTempData.Count(item => item.Value > 0)}/9";
            _centerProgressValue.Text = $"{_centerTempData.Count(item => item.Value > 0)}/15";
        }

        private void UpdateStabilityIndicator(double temperature)
        {
            bool isStable = CalibrationCalculationService.IsTemperatureStable(temperature);
            _stableStatusValue.Text = isStable ? "稳定" : "未稳定";
            _stableStatusValue.ForeColor = isStable ? UiTheme.Success : UiTheme.Warning;
        }

        private void DisplayUniformityResult(CalibrationUniformityResult result)
        {
            SetResultText("TAvg", result.TAvg, false);
            SetResultText("TAvgAxis1", result.TAvgAxis1, false);
            SetResultText("TAvgAxis2", result.TAvgAxis2, false);
            SetResultText("TAvgAxis3", result.TAvgAxis3, false);
            SetResultText("TAvgLevelA", result.TAvgLevelA, false);
            SetResultText("TAvgLevelB", result.TAvgLevelB, false);
            SetResultText("TAvgLevelC", result.TAvgLevelC, false);
            SetResultText("TDevAxis1", result.TDevAxis1, true);
            SetResultText("TDevAxis2", result.TDevAxis2, true);
            SetResultText("TDevAxis3", result.TDevAxis3, true);
            SetResultText("TDevLevelA", result.TDevLevelA, true);
            SetResultText("TDevLevelB", result.TDevLevelB, true);
            SetResultText("TDevLevelC", result.TDevLevelC, true);
            SetResultText("TAvgDevAxis", result.TAvgDevAxis, true);
            SetResultText("TAvgDevLevel", result.TAvgDevLevel, true);
        }

        private void SetResultText(string key, double value, bool percent)
        {
            if (!_resultTextBoxes.TryGetValue(key, out var textBox))
            {
                return;
            }

            textBox.Text = percent ? $"{value:F2}%" : value.ToString("F2");
        }

        private void AddGridHeader(TableLayoutPanel grid, int column, int row, string text)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = UiTheme.GridHeader,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            grid.Controls.Add(label, column, row);
        }

        private void AddSurfaceValueCell(TableLayoutPanel grid, int column, int row, string key)
        {
            var label = new Label
            {
                Text = "0.0",
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.MetricGlow,
                Font = new Font("Consolas", 16F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            _surfaceTempLabels[key] = label;
            grid.Controls.Add(label, column, row);
        }

        private void AddResultRow(TableLayoutPanel panel, int row, string label1, string key1, string? label2, string? key2)
        {
            panel.Controls.Add(CreateResultLabel(label1), 0, row);
            var value1 = CreateResultTextBox($"txt{key1}");
            _resultTextBoxes[key1] = value1;
            panel.Controls.Add(value1, 1, row);

            if (label2 == null || key2 == null)
            {
                value1.Dock = DockStyle.Fill;
                panel.SetColumnSpan(value1, 3);
                return;
            }

            panel.Controls.Add(CreateResultLabel(label2), 2, row);
            var value2 = CreateResultTextBox($"txt{key2}");
            _resultTextBoxes[key2] = value2;
            panel.Controls.Add(value2, 3, row);
        }

        private Label CreateResultLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Ink,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2)
            };
        }

        private TextBox CreateResultTextBox(string name)
        {
            return new TextBox
            {
                Name = name,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = UiTheme.SurfaceRaised,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Margin = new Padding(2),
                Text = "0.0"
            };
        }

        private Label CreateSmallLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(
                FindForm(),
                message,
                "提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
