using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using TestServer.Models;
using ISO11820WinForms.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using ISO11820WinForms.Core;
using ISO11820WinForms.Forms.Controls;
using ISO11820WinForms.Services;
using ISO11820WinForms.Global;
using Serilog;
using Microsoft.EntityFrameworkCore;
using ISO11820WinForms.Utilities;
using ISO11820WinForms.UI;

namespace ISO11820WinForms.Forms
{
    public partial class MainForm : Form
    {
        private Operator _currentUser;
        private ToolStripMenuItem? _selectedMenuItem;
        private bool _showSystemMessages = true;
        private const string StandardTestModeText = "标准模式";
        private const string FixedDurationTestModeText = "固定时长";
        private const int DefaultTargetDurationSeconds = 3600;

        // 图表相关字段
        private PlotView? _chartView;
        private PlotModel? _chartModel;
        private LineSeries? _seriesTF1;  // 炉内温度1（蓝色）
        private LineSeries? _seriesTF2;  // 炉内温度2（红色）
        private LineSeries? _seriesTS;   // 表面温度（绿色）
        private LineSeries? _seriesTC;   // 中心温度（黄色）

        private int _dataPointCount = 0;  // 图表时间计数器（按 TestMaster 每秒广播累加）
        private const int CHART_TIME_WINDOW_SECONDS = 600;  // 图表显示最近10分钟
        private const int MAX_DATA_POINTS = CHART_TIME_WINDOW_SECONDS;
        private const double Y_AXIS_MAX = 800;     // Y轴最大值

        // DaqWorker实例（假设已经在全局上下文中）
        private DaqWorker? _daqWorker;

        // TestMaster1实例（一号试验炉控制器）
        // Requirement 1.1, 1.2, 1.4: 订阅TestMaster1事件以接收实时数据和状态更新
        private TestMaster1? _testMaster1;

        // 系统消息和实时数据相关字段
        private const int MAX_REALTIME_DATA_ROWS = 75;  // 最多保留75条（60秒 ÷ 0.8秒）

        // 导出服务实例
        private ExportService _exportService = new ExportService();

        // 样品试验主页布局和状态区
        private Panel? _connectionStatusPanel;
        private Panel? _operationStatusPanel;
        private Label? _lblSharedPortLed;
        private Label? _lblSharedPortTitle;
        private Label? _lblSharedPortSummary;
        private Label? _lblSharedPortDetail;
        private Label? _lblPidConnectionState;
        private Label? _lblPidConnectionDetail;
        private Label? _lblAdamConnectionState;
        private Label? _lblAdamConnectionDetail;
        private Label? _lblLastHandshake;
        private Label? _lblMessageRowCount;
        private Label? _lblMessageRefresh;
        private Label? _lblMessagePortState;
        private Label? _lblHardwareModeSummary;
        private Label? _lblMainStatusPill;
        private Label? _lblMainStatusTarget;
        private Label? _lblStateProductId;
        private Label? _lblStateTestId;
        private Label? _lblRuntimeHandshake;
        private Label? _lblRuntimeSampleRate;
        private Label? _lblRuntimeRecordState;
        private Label? _lblRuntimeAlarmState;
        private Label?[]? _recentStatusTimeLabels;
        private Label?[]? _recentStatusMessageLabels;
        private ComboBox? _comboMainTestMode;
        private TextBox? _txtMainDurationMinutes;
        private Label? _lblMainDurationUnit;
        private Label? _lblMainTestModeHint;
        private bool _isSyncingMainTestModeControls;
        private bool _sampleStaticLayoutHooked;
        private bool _metricDisplayResizeHooked;
        private System.Windows.Forms.Timer? _connectionStatusTimer;

        // 系统校验界面控件字段
        private Label? _lblTempA1, _lblTempA2, _lblTempA3;
        private Label? _lblTempB1, _lblTempB2, _lblTempB3;
        private Label? _lblTempC1, _lblTempC2, _lblTempC3;
        private TextBox? _txtTAvg, _txtTAvgAxis1, _txtTAvgAxis2, _txtTAvgAxis3;
        private TextBox? _txtTAvgLevela, _txtTAvgLevelb, _txtTAvgLevelc;
        private TextBox? _txtTDevAxis1, _txtTDevAxis2, _txtTDevAxis3;
        private TextBox? _txtTDevLevela, _txtTDevLevelb, _txtTDevLevelc;
        private TextBox? _txtTAvgDevAxis, _txtTAvgDevLevel;
        private Dictionary<int, TextBox> _centerTempTextBoxes = new Dictionary<int, TextBox>();
        private CalibrationView? _calibrationView;

        public MainForm(Operator user)
        {
            InitializeComponent();
            _currentUser = user;

            this.Text = "建筑材料不燃性试验系统";
            lblSystemName.Text = "建筑材料不燃性试验系统 版本 3.0";

            // 加载应用程序图标
            LoadApplicationIcon();

            // 隐藏TabControl的标签页
            this.tabControl1.ItemSize = new System.Drawing.Size(0, 1);
            this.tabControl1.SizeMode = TabSizeMode.Fixed;

            // 默认选中第一个
            SetSelectedMenuItem(样品试验ToolStripMenuItem, 0);

            // 初始化温度曲线图表
            InitializeChart();

            // 从全局上下文获取DaqWorker实例并订阅事件（标准单例访问模式）
            SetDaqWorker(SystemContext.Current.Daq);

            // 从全局上下文获取TestMaster1实例并订阅事件
            // Requirement 1.1, 1.2, 1.4: 订阅TestMaster1事件以接收实时数据和状态更新
            SetTestMaster1(SystemContext.Current.Master1);

            // 初始化系统消息和实时数据表格
            InitializeMessageTables();

            // 添加欢迎消息
            AppendSystemMessage("系统已启动，操作员: " + _currentUser.Userid);
            UpdateButtonStates(MasterStatus.Idle);
            ReportHardwareStartupStatus();
            if (_testMaster1 != null && !_testMaster1.IsFlameDetectionEnabled)
            {
                AppendSystemMessage("当前版本未启用火焰自动检测，火焰时间和持续时间请在试验结束后手工录入。");
            }

            // 初始化系统校验视图
            InitializeCalibrationView();

            // 初始化试验报告视图
            InitializeReportView();

            // 初始化记录查询视图
            InitializeQueryView();

            // 应用统一工业风主题
            InitializeButtonHoverEffects();
            ApplyIndustrialTheme();
        }

        /*
         * 功能: 加载应用程序图标
         */
        private void LoadApplicationIcon()
        {
            var icon = ResourceHelper.LoadApplicationIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        private void ApplyIndustrialTheme()
        {
            UiTheme.ApplyFormTheme(this);
            UiTheme.ApplyToControlTree(this);

            BackColor = UiTheme.AppBackground;
            menuStrip1.Height = 58;
            menuStrip1.Padding = new Padding(18, 9, 18, 9);
            panel1.Height = 50;
            panel1.BackColor = UiTheme.SurfaceStrongAlt;
            panelSample.BackColor = UiTheme.AppBackground;
            panelCalibration.BackColor = UiTheme.AppBackground;
            panelReport.BackColor = UiTheme.AppBackground;
            panelQuery.BackColor = UiTheme.AppBackground;
            tabPage样品试验.BackColor = UiTheme.AppBackground;
            tabPage系统校验.BackColor = UiTheme.AppBackground;
            tabPage试验报告.BackColor = UiTheme.AppBackground;
            tabPage记录查询.BackColor = UiTheme.AppBackground;

            UiTheme.StyleMenuStrip(menuStrip1);
            UiTheme.StyleMenuStrip(menuStrip3, compact: true);
            lblSystemName.Enabled = true;
            lblSystemName.ForeColor = UiTheme.TitleInk;
            lblSystemName.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold, GraphicsUnit.Point);
            lblSystemName.Padding = new Padding(18, 8, 18, 8);

            panelOperations.BackColor = UiTheme.Surface;
            panelOperations.Padding = new Padding(12, 8, 12, 8);
            panelOperations.Height = 76;

            panelMessageBottom.BackColor = UiTheme.SurfaceStrongAlt;
            panelMessageBottom.Padding = new Padding(10, 8, 10, 8);
            panelMessageBottom.Height = 52;

            messagePanel.BackColor = UiTheme.Surface;
            messagePanel.BorderStyle = BorderStyle.None;
            chartPanel.BackColor = UiTheme.Surface;
            chartPanel.BorderStyle = BorderStyle.None;
            panelDataDisplay.BackColor = UiTheme.Surface;
            panelDataDisplay.BorderStyle = BorderStyle.None;

            StylePrimaryButtons();
            LayoutOperationButtons();
            ConfigureSampleDashboardLayout();
            BuildMetricDisplay();
            StyleDataTables();
            ApplyChartTheme();
            ApplyMessageToggleButtonState();
            StartConnectionStatusTimer();
            UpdateConnectionStatus();
            SetSelectedMenuItem(_selectedMenuItem ?? 样品试验ToolStripMenuItem, tabControl1.SelectedIndex);
        }

        private void ConfigureSampleDashboardLayout()
        {
            panelSample.SuspendLayout();

            panelOperations.Dock = DockStyle.None;
            panelOperations.Margin = Padding.Empty;
            panelDataDisplay.Dock = DockStyle.None;
            panelDataDisplay.AutoScroll = true;
            panelDataDisplay.Margin = Padding.Empty;
            panelDataDisplay.Paint -= PanelDataDisplay_Paint;

            chartPanel.Dock = DockStyle.None;
            chartPanel.Margin = Padding.Empty;

            _connectionStatusPanel?.Dispose();
            _connectionStatusPanel = CreateConnectionStatusPanel();
            _connectionStatusPanel.Dock = DockStyle.None;
            _connectionStatusPanel.Margin = Padding.Empty;

            panelMessageBottom.Dock = DockStyle.None;
            panelMessageBottom.Height = 50;
            panelMessageBottom.Margin = Padding.Empty;
            ConfigureMessageHeader();

            messagePanel.Dock = DockStyle.None;
            messagePanel.Margin = Padding.Empty;
            messagePanel.Padding = Padding.Empty;

            panelSample.Controls.Remove(panelOperations);
            panelSample.Controls.Remove(panelDataDisplay);
            panelSample.Controls.Remove(chartPanel);
            panelSample.Controls.Remove(panelMessageBottom);
            panelSample.Controls.Remove(messagePanel);
            panelSample.Controls.Remove(_connectionStatusPanel);
            panelSample.Controls.Add(panelOperations);
            panelSample.Controls.Add(panelDataDisplay);
            panelSample.Controls.Add(chartPanel);
            panelSample.Controls.Add(_connectionStatusPanel);
            panelSample.Controls.Add(panelMessageBottom);
            panelSample.Controls.Add(messagePanel);
            panelOperations.BringToFront();

            if (!_sampleStaticLayoutHooked)
            {
                panelSample.Resize += (_, _) => ApplySampleStaticLayout();
                _sampleStaticLayoutHooked = true;
            }

            ApplySampleStaticLayout();

            panelSample.ResumeLayout();
        }

        private void ApplySampleStaticLayout()
        {
            if (_connectionStatusPanel == null)
            {
                return;
            }

            const int pad = 12;
            const int gap = 10;
            const int operationHeight = 76;
            const int leftWidth = 300;
            const int rightWidth = 500;
            const int messageHeight = 300;
            const int messageHeaderHeight = 50;

            var width = panelSample.ClientSize.Width;
            var height = panelSample.ClientSize.Height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            panelOperations.SetBounds(0, 0, width, operationHeight);

            var messageTop = Math.Max(operationHeight + gap, height - pad - messageHeight);
            var messageWidth = Math.Max(360, width - pad * 2);
            panelMessageBottom.SetBounds(pad, messageTop, messageWidth, messageHeaderHeight);
            messagePanel.SetBounds(pad, messageTop + messageHeaderHeight, messageWidth, Math.Max(72, height - pad - messageTop - messageHeaderHeight));

            var mainTop = operationHeight + gap;
            var mainHeight = Math.Max(260, messageTop - gap - mainTop);
            var rightX = Math.Max(pad + leftWidth + gap + 360 + gap, width - pad - rightWidth);
            var chartX = pad + leftWidth + gap;
            var chartWidth = Math.Max(360, rightX - gap - chartX);

            panelDataDisplay.SetBounds(pad, mainTop, leftWidth, mainHeight);
            chartPanel.SetBounds(chartX, mainTop, chartWidth, mainHeight);
            _connectionStatusPanel.SetBounds(rightX, mainTop, rightWidth, mainHeight);

            PositionOperationControls();
            PositionConnectionStatusControls();
            ResizeMetricCards();
        }

        private void ConfigureMessageHeader()
        {
            panelMessageBottom.SuspendLayout();
            panelMessageBottom.Controls.Clear();
            panelMessageBottom.BackColor = UiTheme.SurfaceStrongAlt;
            panelMessageBottom.Padding = new Padding(10, 6, 10, 0);

            var buttonHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 300,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Tag = "theme-skip"
            };

            btnSystemMessage.Text = "系统信息";
            btnRealTimeData.Text = "实时数据";
            btnSystemMessage.Margin = new Padding(0, 0, 8, 0);
            btnRealTimeData.Margin = Padding.Empty;
            buttonHost.Controls.Add(btnSystemMessage);
            buttonHost.Controls.Add(btnRealTimeData);

            var metaHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 430,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = new Padding(0, 9, 0, 0),
                Tag = "theme-skip"
            };

            _lblMessageRowCount = CreateMessageMetaLabel("最近 0 条", 120);
            _lblMessageRefresh = CreateMessageMetaLabel("刷新 0.8s", 120);
            _lblMessagePortState = CreateMessageMetaLabel("COM9 --", 150);
            metaHost.Controls.Add(_lblMessageRowCount);
            metaHost.Controls.Add(_lblMessageRefresh);
            metaHost.Controls.Add(_lblMessagePortState);

            panelMessageBottom.Controls.Add(metaHost);
            panelMessageBottom.Controls.Add(buttonHost);
            panelMessageBottom.ResumeLayout();
        }

        private Label CreateMessageMetaLabel(string text, int width)
        {
            return new Label
            {
                AutoSize = false,
                Width = width,
                Height = 24,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 12, 0),
                Tag = "theme-skip"
            };
        }

        private Panel CreateConnectionStatusPanel()
        {
            var panel = new Panel
            {
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(12),
                Tag = "theme-skip"
            };

            var sharedCard = CreateSharedPortCard();
            var stateCard = CreateTestStateCard();
            var summaryCard = CreateRuntimeSummaryCard();
            var recentCard = CreateRecentStatusCard();
            var note = CreateConnectionNote();

            sharedCard.Name = "SharedPortCard";
            stateCard.Name = "TestStateCard";
            summaryCard.Name = "RuntimeSummaryCard";
            recentCard.Name = "RecentStatusCard";
            note.Name = "ConnectionNote";

            panel.Controls.Add(sharedCard);
            panel.Controls.Add(stateCard);
            panel.Controls.Add(summaryCard);
            panel.Controls.Add(recentCard);
            panel.Controls.Add(note);
            panel.AutoScroll = false;

            panel.Resize += (_, _) => PositionConnectionStatusControls();
            return panel;
        }

        private void PositionConnectionStatusControls()
        {
            if (_connectionStatusPanel == null)
            {
                return;
            }

            var useLooseLayout = _connectionStatusPanel.ClientSize.Height >= 720;
            var pad = useLooseLayout ? 12 : 8;
            var baseGap = useLooseLayout ? 12 : 5;
            var width = Math.Max(260, _connectionStatusPanel.ClientSize.Width - pad * 2);
            var availableHeight = Math.Max(0, _connectionStatusPanel.ClientSize.Height - pad * 2);

            var sharedHeight = useLooseLayout ? 148 : 118;
            var stateHeight = useLooseLayout ? 220 : 190;
            var summaryHeight = useLooseLayout ? 150 : 136;
            var recentHeight = useLooseLayout ? 150 : 108;
            var noteHeight = useLooseLayout ? 56 : 42;
            var gap = baseGap;

            var baseTotal = sharedHeight + stateHeight + summaryHeight + recentHeight + noteHeight + gap * 4;
            var extra = Math.Max(0, availableHeight - baseTotal);
            if (extra > 0)
            {
                var sharedExtra = Math.Min(18, extra * 8 / 100);
                var stateExtra = Math.Min(52, extra * 20 / 100);
                var summaryExtra = Math.Min(76, extra * 28 / 100);
                var recentExtra = Math.Min(80, extra * 30 / 100);
                var noteExtra = Math.Min(12, extra * 4 / 100);

                sharedHeight += sharedExtra;
                stateHeight += stateExtra;
                summaryHeight += summaryExtra;
                recentHeight += recentExtra;
                noteHeight += noteExtra;

                var usedExtra = sharedExtra + stateExtra + summaryExtra + recentExtra + noteExtra;
                gap += Math.Min(28, Math.Max(0, extra - usedExtra) / 4);
            }

            var sharedCard = _connectionStatusPanel.Controls["SharedPortCard"];
            var stateCard = _connectionStatusPanel.Controls["TestStateCard"];
            var summaryCard = _connectionStatusPanel.Controls["RuntimeSummaryCard"];
            var recentCard = _connectionStatusPanel.Controls["RecentStatusCard"];
            var note = _connectionStatusPanel.Controls["ConnectionNote"];

            var y = pad;
            sharedCard?.SetBounds(pad, y, width, sharedHeight);
            y += sharedHeight + gap;
            stateCard?.SetBounds(pad, y, width, stateHeight);
            y += stateHeight + gap;
            summaryCard?.SetBounds(pad, y, width, summaryHeight);
            y += summaryHeight + gap;
            recentCard?.SetBounds(pad, y, width, recentHeight);
            y += recentHeight + gap;
            note?.SetBounds(pad, y, width, noteHeight);
        }

        private Control CreateConnectionHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            var title = new Label
            {
                Text = "设备连接",
                Dock = DockStyle.Left,
                Width = 150,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            var subtitle = new Label
            {
                Text = "串口共用检测",
                Dock = DockStyle.Right,
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            header.Controls.Add(subtitle);
            header.Controls.Add(title);
            return header;
        }

        private Control CreateSharedPortCard()
        {
            var card = CreateDashboardCard();
            card.Dock = DockStyle.None;
            card.Margin = Padding.Empty;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(12, 0, 12, 0),
                BackColor = UiTheme.SurfaceStrong,
                Tag = "theme-skip"
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSharedPortLed = new Label
            {
                Text = "●",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = UiTheme.Success,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            _lblSharedPortTitle = new Label
            {
                Text = "COM9 共口",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            _lblSharedPortSummary = new Label
            {
                Text = "已连接",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UiTheme.Success,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            header.Controls.Add(_lblSharedPortLed, 0, 0);
            header.Controls.Add(_lblSharedPortTitle, 1, 0);
            header.Controls.Add(_lblSharedPortSummary, 2, 0);

            var devices = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(14, 6, 14, 1),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            devices.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            devices.Controls.Add(CreateConnectionDeviceColumn("PID 控制器", "COM9 / 站号 2", out _lblPidConnectionState, out _lblPidConnectionDetail), 0, 0);
            devices.Controls.Add(CreateConnectionDeviceColumn("ADAM 采集", "COM9 / 站号 1", out _lblAdamConnectionState, out _lblAdamConnectionDetail), 1, 0);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(14, 0, 14, 4),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _lblSharedPortDetail = CreateFlowLabel("COM9 分时访问", UiTheme.InkMuted, 8F, FontStyle.Bold);
            _lblLastHandshake = CreateFlowLabel("--", UiTheme.InkMuted, 8F, FontStyle.Bold);
            _lblLastHandshake.TextAlign = ContentAlignment.MiddleRight;
            footer.Controls.Add(_lblSharedPortDetail, 0, 0);
            footer.Controls.Add(_lblLastHandshake, 1, 0);

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(devices, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            card.Controls.Add(layout);
            return card;
        }

        private Control CreateConnectionDeviceColumn(
            string title,
            string detail,
            out Label stateLabel,
            out Label detailLabel)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 0, 8, 0),
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

            var titleLabel = CreateFlowLabel(title, UiTheme.Ink, 8.4F, FontStyle.Bold);
            detailLabel = CreateFlowLabel(detail, UiTheme.InkMuted, 7.8F, FontStyle.Regular);
            stateLabel = CreateFlowLabel("● 正常", UiTheme.Success, 8F, FontStyle.Bold);

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(detailLabel, 0, 1);
            layout.Controls.Add(stateLabel, 0, 2);
            return layout;
        }

        private Control CreateDeviceBlock(
            string title,
            string subtitle,
            out Label stateLabel,
            out Label detailLabel)
        {
            var block = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 1, 0),
                Padding = new Padding(10, 5, 10, 5),
                BackColor = UiTheme.Surface,
                Tag = "theme-skip"
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            detailLabel = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Top,
                Height = 16,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 7.8F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            stateLabel = new Label
            {
                Text = "正常",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Success,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            block.Controls.Add(stateLabel);
            block.Controls.Add(detailLabel);
            block.Controls.Add(titleLabel);
            return block;
        }

        private Label CreateStaticText(
            string text,
            int x,
            int y,
            int width,
            int height,
            Color color,
            float size,
            FontStyle style)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = color,
                Font = new Font("Microsoft YaHei", size, style, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };
        }

        private Label CreateFlowLabel(
            string text,
            Color color,
            float size,
            FontStyle style,
            ContentAlignment align = ContentAlignment.MiddleLeft)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = align,
                ForeColor = color,
                Font = new Font("Microsoft YaHei", size, style, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };
        }

        private TableLayoutPanel CreateTwoColumnInfoRow(string label, string value)
        {
            var valueLabel = CreateFlowLabel(value, UiTheme.Ink, 8.5F, FontStyle.Bold);
            return CreateValueRow(label, valueLabel);
        }

        private TableLayoutPanel CreateValueRow(string label, Control valueControl)
        {
            var row = CreateInfoRowBase(columnCount: 2);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.Controls.Add(CreateFlowLabel(label, UiTheme.InkMuted, 8F, FontStyle.Bold), 0, 0);
            row.Controls.Add(valueControl, 1, 0);
            return row;
        }

        private TableLayoutPanel CreateStationInfoRow()
        {
            var row = CreateInfoRowBase(columnCount: 3);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.Controls.Add(CreateFlowLabel("站号", UiTheme.InkMuted, 8F, FontStyle.Bold), 0, 0);
            row.Controls.Add(CreateFlowLabel($"PID {ConfigurationHelper.GetPidStationNumber()}", UiTheme.Ink, 8.5F, FontStyle.Bold), 1, 0);
            row.Controls.Add(CreateFlowLabel($"ADAM {ConfigurationHelper.GetSensorStationNumber()}", UiTheme.Ink, 8.5F, FontStyle.Bold), 2, 0);
            return row;
        }

        private TableLayoutPanel CreateEditorRow(string label, Control editor)
        {
            var row = CreateInfoRowBase(columnCount: 2);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editor.Margin = Padding.Empty;
            row.Controls.Add(CreateFlowLabel(label, UiTheme.InkMuted, 8F, FontStyle.Bold), 0, 0);
            row.Controls.Add(editor, 1, 0);
            return row;
        }

        private TableLayoutPanel CreateInfoRowBase(int columnCount)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columnCount,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return row;
        }

        private Control CreateTestStateCard()
        {
            var card = CreateDashboardCard();
            card.Dock = DockStyle.None;
            card.Margin = Padding.Empty;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Margin = Padding.Empty,
                Padding = new Padding(14, 6, 14, 6),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(CreateFlowLabel("试验状态", UiTheme.Ink, 10.5F, FontStyle.Bold), 0, 0);
            layout.Controls.Add(CreateTwoColumnInfoRow("目标温度", $"{ConfigurationHelper.GetPidTemperature():F1} ℃"), 0, 1);
            layout.Controls.Add(CreateStationInfoRow(), 0, 2);

            var mode = CreateMainTestModeComboBox();
            mode.Dock = DockStyle.Fill;
            layout.Controls.Add(CreateEditorRow("试验模式", mode), 0, 3);

            var duration = CreateMainDurationEditor();
            duration.Dock = DockStyle.Fill;
            layout.Controls.Add(CreateEditorRow("试验时长", duration), 0, 4);

            _lblStateProductId = CreateFlowLabel("--", UiTheme.Ink, 8.2F, FontStyle.Bold);
            layout.Controls.Add(CreateValueRow("样品编号", _lblStateProductId), 0, 5);
            _lblStateTestId = CreateFlowLabel("--", UiTheme.Ink, 8.2F, FontStyle.Bold);
            layout.Controls.Add(CreateValueRow("样品标识", _lblStateTestId), 0, 6);

            _lblMainTestModeHint = new Label
            {
                Text = "请先新建本次试验",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.TopLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };
            layout.Controls.Add(_lblMainTestModeHint, 0, 7);
            card.Controls.Add(layout);

            SyncMainTestModeControls(TryGetCurrentTestData(), _testMaster1?.Status ?? MasterStatus.Idle);
            return card;
        }

        private Control CreateRuntimeSummaryCard()
        {
            var card = CreateDashboardCard();
            card.Dock = DockStyle.None;
            card.Margin = Padding.Empty;

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 10),
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            grid.Controls.Add(CreateRuntimeTile("连接握手", "重试", out _lblRuntimeHandshake), 0, 0);
            grid.Controls.Add(CreateRuntimeTile("采样节拍", "0.8s", out _lblRuntimeSampleRate), 1, 0);
            grid.Controls.Add(CreateRuntimeTile("记录状态", "未记录", out _lblRuntimeRecordState), 0, 1);
            grid.Controls.Add(CreateRuntimeTile("报警状态", "正常", out _lblRuntimeAlarmState), 1, 1);

            card.Controls.Add(grid);
            card.Controls.Add(CreateDashboardSectionTitle("运行摘要", "随结果刷新"));
            return card;
        }

        private Control CreateRecentStatusCard()
        {
            var card = CreateDashboardCard();
            card.Dock = DockStyle.None;
            card.Margin = Padding.Empty;
            _recentStatusTimeLabels = new Label?[3];
            _recentStatusMessageLabels = new Label?[3];

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8),
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
                grid.Controls.Add(CreateRecentStatusRow(i), 0, i);
            }

            card.Controls.Add(grid);
            card.Controls.Add(CreateDashboardSectionTitle("最近状态", "最近 3 条"));
            UpdateRecentStatusEvents();
            return card;
        }

        private Panel CreateDashboardSectionTitle(string title, string meta)
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(12, 0, 10, 0),
                Tag = "theme-skip"
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Left,
                Width = 180,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            var metaLabel = new Label
            {
                Text = meta,
                Dock = DockStyle.Right,
                Width = 120,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            header.Controls.Add(metaLabel);
            header.Controls.Add(titleLabel);
            return header;
        }

        private Panel CreateRuntimeTile(string title, string value, out Label valueLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(8, 2, 8, 2),
                BackColor = UiTheme.SurfaceRaised,
                Tag = "theme-skip"
            };
            panel.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                panel.ClientRectangle,
                UiTheme.GridLine,
                ButtonBorderStyle.Solid);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 7.4F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            valueLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Consolas", 9.8F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(valueLabel, 0, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateRecentStatusRow(int index)
        {
            var row = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Tag = "theme-skip"
            };
            row.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                row.ClientRectangle,
                Color.Transparent,
                0,
                ButtonBorderStyle.None,
                Color.Transparent,
                0,
                ButtonBorderStyle.None,
                Color.Transparent,
                0,
                ButtonBorderStyle.None,
                UiTheme.GridLine,
                1,
                ButtonBorderStyle.Solid);

            var time = new Label
            {
                Text = "--:--:--",
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Consolas", 8.2F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            var message = new Label
            {
                Text = "暂无状态",
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Ink,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _recentStatusTimeLabels![index] = time;
            _recentStatusMessageLabels![index] = message;
            layout.Controls.Add(time, 0, 0);
            layout.Controls.Add(message, 1, 0);
            row.Controls.Add(layout);
            return row;
        }

        private ComboBox CreateMainTestModeComboBox()
        {
            _comboMainTestMode = new ComboBox
            {
                Dock = DockStyle.None,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.White,
                ForeColor = UiTheme.Ink,
                Margin = new Padding(0, 2, 0, 2),
                Tag = "theme-skip"
            };
            _comboMainTestMode.Items.Add(StandardTestModeText);
            _comboMainTestMode.Items.Add(FixedDurationTestModeText);
            _comboMainTestMode.SelectedItem = StandardTestModeText;
            _comboMainTestMode.SelectedIndexChanged += (_, _) => ApplyMainTestModeSelection(showWarning: false);
            return _comboMainTestMode;
        }

        private Control CreateMainDurationEditor()
        {
            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = Padding.Empty,
                Tag = "theme-skip"
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _txtMainDurationMinutes = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = (DefaultTargetDurationSeconds / 60).ToString(),
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = UiTheme.Ink,
                Margin = new Padding(0, 2, 8, 2),
                Tag = "theme-skip"
            };
            _txtMainDurationMinutes.Leave += (_, _) => ApplyMainTestModeSelection(showWarning: false);
            _txtMainDurationMinutes.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ApplyMainTestModeSelection(showWarning: false);
                    e.SuppressKeyPress = true;
                }
            };

            _lblMainDurationUnit = new Label
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Text = "分钟",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            editor.Controls.Add(_txtMainDurationMinutes, 0, 0);
            editor.Controls.Add(_lblMainDurationUnit, 1, 0);
            return editor;
        }

        private Control CreateConnectionNote()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.None,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 7, 10, 7),
                BackColor = UiTheme.SurfaceStrongAlt,
                Tag = "theme-skip"
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblHardwareModeSummary = new Label
            {
                Name = "HardwareModeSummaryLabel",
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0),
                Text = "当前：--",
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };

            var button = new Button
            {
                Name = "HardwareDiagnosticsButton",
                Dock = DockStyle.Fill,
                Text = "诊断",
                Tag = ButtonTone.Secondary
            };
            UiTheme.StyleButton(button, ButtonTone.Secondary);
            button.MinimumSize = new Size(72, 26);
            button.Height = 28;
            button.Padding = new Padding(8, 0, 8, 0);
            button.Click += (_, _) => OpenHardwareDiagnostics();

            var switchButton = new Button
            {
                Name = "HardwareModeSwitchButton",
                Dock = DockStyle.Fill,
                Text = "切换模式",
                Tag = ButtonTone.Neutral
            };
            UiTheme.StyleButton(switchButton, ButtonTone.Neutral);
            switchButton.MinimumSize = new Size(88, 26);
            switchButton.Height = 28;
            switchButton.Padding = new Padding(6, 0, 6, 0);
            switchButton.Click += (_, _) => SwitchHardwareMode();

            panel.Controls.Add(_lblHardwareModeSummary, 0, 0);
            panel.Controls.Add(button, 1, 0);
            panel.Controls.Add(switchButton, 2, 0);
            return panel;
        }

        private void OpenHardwareDiagnostics()
        {
            using var form = new HardwareDiagnosticsForm(_daqWorker, _testMaster1);
            form.ShowDialog(this);
            UpdateConnectionStatus();
        }

        private void SwitchHardwareMode()
        {
            try
            {
                var service = new SimulationModeConfigService();
                var currentSimulationMode = service.IsSimulationModeEnabled();
                var targetSimulationMode = !currentSimulationMode;
                var targetText = targetSimulationMode ? "仿真模式" : "真实硬件模式";

                var message =
                    $"将运行模式切换为“{targetText}”。\n\n" +
                    "切换会写入 appsettings.json，重启程序后生效。\n" +
                    "当前已经初始化的串口和仿真器不会在运行中重建。\n\n" +
                    "是否继续？";

                if (!ExceptionHandler.Confirm(message, "切换运行模式"))
                {
                    return;
                }

                service.SetSimulationMode(targetSimulationMode);
                var successMessage = $"运行模式已切换为“{targetText}”，请重启程序后生效。";
                AppendSystemMessage(successMessage);
                if (_lblHardwareModeSummary != null)
                {
                    _lblHardwareModeSummary.Text = $"重启后：{targetText}";
                    _lblHardwareModeSummary.ForeColor = UiTheme.Warning;
                }

                ExceptionHandler.ShowInfo(successMessage, "切换完成");
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "切换运行模式失败，请检查 appsettings.json 是否可写。", "切换运行模式");
            }
        }

        private Panel CreateDashboardCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.None,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = UiTheme.Surface,
                BorderStyle = BorderStyle.None,
                Tag = "theme-skip"
            };
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                UiTheme.Border,
                ButtonBorderStyle.Solid);
            return card;
        }

        private Label CreateDetailLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 7.8F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
        }

        private Label CreateMutedLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
        }

        private Label CreateStrongLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 8.4F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
        }

        private void StartConnectionStatusTimer()
        {
            if (_connectionStatusTimer == null)
            {
                _connectionStatusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                _connectionStatusTimer.Tick += (_, _) => UpdateConnectionStatus();
            }

            _connectionStatusTimer.Start();
        }

        private void UpdateConnectionStatus()
        {
            var pidPort = _testMaster1?.Manipulator?.PidPortName ?? ConfigurationHelper.GetPidPort();
            var adamPort = _daqWorker?.SensorPortName ?? ConfigurationHelper.GetSensorPort();
            var pidOk = _testMaster1?.Manipulator?.IsConnected == true;
            var adamOk = _daqWorker?.IsSensorConnected == true;
            var samePort = ConfigurationHelper.AreSameSerialPort(pidPort, adamPort);
            var connectionOk = pidOk && adamOk;
            var simulationMode = _testMaster1?.Manipulator?.IsSimulationMode == true
                || _daqWorker?.IsSimulationMode == true;
            var warningColor = samePort ? UiTheme.Danger : UiTheme.Warning;
            var summaryColor = connectionOk ? UiTheme.Success : warningColor;
            var portTitle = samePort ? $"{pidPort} 共口" : $"{pidPort}/{adamPort}";

            if (_lblSharedPortTitle != null)
            {
                _lblSharedPortTitle.Text = portTitle;
            }

            if (_lblSharedPortSummary != null)
            {
                _lblSharedPortSummary.Text = simulationMode
                    ? "仿真模式"
                    : connectionOk
                    ? "已连接"
                    : samePort ? "连接异常" : "端口异常";
                _lblSharedPortSummary.ForeColor = summaryColor;
            }

            if (_lblHardwareModeSummary != null)
            {
                _lblHardwareModeSummary.Text = simulationMode ? "当前：仿真模式" : "当前：真实硬件";
                _lblHardwareModeSummary.ForeColor = simulationMode ? UiTheme.Warning : UiTheme.InkMuted;
            }

            if (_lblSharedPortLed != null)
            {
                _lblSharedPortLed.ForeColor = summaryColor;
            }

            if (_lblPidConnectionState != null)
            {
                _lblPidConnectionState.Text = pidOk ? "● 正常" : "● 未连接";
                _lblPidConnectionState.ForeColor = pidOk ? UiTheme.Success : UiTheme.Danger;
            }

            if (_lblPidConnectionDetail != null)
            {
                _lblPidConnectionDetail.Text = $"{pidPort} / 站号 {ConfigurationHelper.GetPidStationNumber()}";
            }

            if (_lblAdamConnectionState != null)
            {
                _lblAdamConnectionState.Text = adamOk ? "● 正常" : "● 未连接";
                _lblAdamConnectionState.ForeColor = adamOk ? UiTheme.Success : UiTheme.Danger;
            }

            if (_lblAdamConnectionDetail != null)
            {
                _lblAdamConnectionDetail.Text = $"{adamPort} / 站号 {ConfigurationHelper.GetSensorStationNumber()}";
            }

            if (_lblSharedPortDetail != null)
            {
                _lblSharedPortDetail.Text = simulationMode
                    ? "离线仿真，不访问真实串口"
                    : samePort ? "共口分时访问" : "双串口访问";
            }

            if (_lblLastHandshake != null)
            {
                _lblLastHandshake.Text = simulationMode ? "仿真" : connectionOk ? "正常" : "重试";
                _lblLastHandshake.ForeColor = connectionOk ? UiTheme.InkMuted : UiTheme.Danger;
            }

            if (_lblMessagePortState != null)
            {
                _lblMessagePortState.Text = $"{(samePort ? pidPort : "串口")} {(simulationMode ? "仿真" : connectionOk ? "正常" : "异常")}";
                _lblMessagePortState.ForeColor = connectionOk ? UiTheme.InkMuted : summaryColor;
            }

            if (_lblRuntimeHandshake != null)
            {
                _lblRuntimeHandshake.Text = simulationMode ? "仿真" : connectionOk ? "OK" : "重试";
                _lblRuntimeHandshake.ForeColor = connectionOk ? UiTheme.Success : warningColor;
            }

            if (_lblRuntimeSampleRate != null)
            {
                _lblRuntimeSampleRate.Text = "0.8s";
                _lblRuntimeSampleRate.ForeColor = UiTheme.TitleInk;
            }

            if (_lblRuntimeAlarmState != null)
            {
                _lblRuntimeAlarmState.Text = simulationMode ? "仿真" : connectionOk ? "正常" : "异常";
                _lblRuntimeAlarmState.ForeColor = connectionOk ? UiTheme.Success : summaryColor;
            }

            if (_lblRuntimeRecordState != null)
            {
                var status = _testMaster1?.Status ?? MasterStatus.Idle;
                _lblRuntimeRecordState.Text = GetRecordStateText(status);
                _lblRuntimeRecordState.ForeColor = GetRecordStateColor(status);
            }
        }

        private void StylePrimaryButtons()
        {
            UiTheme.StyleButton(btnNewTest, ButtonTone.Warning, compact: true);
            UiTheme.StyleButton(btnOpenRecord, ButtonTone.Secondary, compact: true);
            UiTheme.StyleButton(btnStopRecord, ButtonTone.Danger, compact: true);
            UiTheme.StyleButton(btnRecordLogs, ButtonTone.Neutral, compact: true);
            UiTheme.StyleButton(btnParamSettings, ButtonTone.Neutral, compact: true);
            UiTheme.StyleButton(btnStartHeating, ButtonTone.Primary, compact: true);
            UiTheme.StyleButton(btnStopHeating, ButtonTone.Danger, compact: true);

            UiTheme.StyleButton(btnCalculate, ButtonTone.Primary);
            UiTheme.StyleButton(btnRecordSurface, ButtonTone.Warning);
            UiTheme.StyleButton(btnResetCenter, ButtonTone.Neutral);
            UiTheme.StyleButton(btnRecordCenter, ButtonTone.Warning);

            UiTheme.StyleButton(btnReportQuery, ButtonTone.Primary);
            UiTheme.StyleButton(btnReportReset, ButtonTone.Neutral);
            UiTheme.StyleButton(btnReportExportExcel, ButtonTone.Warning);
            UiTheme.StyleButton(btnReportExportPdf, ButtonTone.Secondary);

            UiTheme.StyleButton(btnQuerySearch, ButtonTone.Primary);
            UiTheme.StyleButton(btnQueryReset, ButtonTone.Neutral);
            UiTheme.StyleButton(btnQueryViewDetails, ButtonTone.Secondary);
            UiTheme.StyleButton(btnQueryExportExcel, ButtonTone.Warning);
            UiTheme.StyleButton(btnQueryExportCsv, ButtonTone.Warning);
            UiTheme.StyleButton(btnQuerySummaryReport, ButtonTone.Primary);
        }

        private void LayoutOperationButtons()
        {
            var buttons = new[]
            {
                btnNewTest,
                btnOpenRecord,
                btnStopRecord,
                btnRecordLogs,
                btnParamSettings,
                btnStartHeating,
                btnStopHeating
            };

            panelOperations.SuspendLayout();
            panelOperations.Controls.Clear();

            foreach (var button in buttons)
            {
                button.Size = new Size(
                    Math.Max(button.Width, button.MinimumSize.Width),
                    Math.Max(button.Height, button.MinimumSize.Height));
                button.Dock = DockStyle.None;
                button.Margin = Padding.Empty;
                panelOperations.Controls.Add(button);
            }

            _operationStatusPanel = (Panel)CreateOperationStatusPanel();
            panelOperations.Controls.Add(_operationStatusPanel);
            panelOperations.Height = 76;
            panelOperations.Resize -= PanelOperations_Resize;
            panelOperations.Resize += PanelOperations_Resize;
            PositionOperationControls();
            panelOperations.ResumeLayout();
            UpdateMainStatusPanel(_testMaster1?.Status ?? MasterStatus.Idle);
        }

        private void PanelOperations_Resize(object? sender, EventArgs e)
        {
            PositionOperationControls();
        }

        private void PositionOperationControls()
        {
            const int buttonHeight = 46;
            var x = 12;
            var y = Math.Max(10, (panelOperations.ClientSize.Height - buttonHeight) / 2);
            var buttons = new[]
            {
                btnNewTest,
                btnOpenRecord,
                btnStopRecord,
                btnRecordLogs,
                btnParamSettings,
                btnStartHeating,
                btnStopHeating
            };

            foreach (var button in buttons)
            {
                button.SetBounds(x, y, button.Width, buttonHeight);
                x += button.Width + 10;
            }

            if (_operationStatusPanel != null)
            {
                var statusWidth = 260;
                _operationStatusPanel.SetBounds(
                    Math.Max(x + 10, panelOperations.ClientSize.Width - statusWidth - 12),
                    11,
                    statusWidth,
                    54);
            }
        }

        private Control CreateOperationStatusPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.None,
                BackColor = UiTheme.SurfaceStrongAlt,
                Tag = "theme-skip"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(18, 0, 14, 0),
                Margin = Padding.Empty,
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblMainStatusPill = new Label
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 13, 12, 13),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = UiTheme.SuccessSoft,
                ForeColor = UiTheme.Success,
                Tag = "theme-skip"
            };

            _lblMainStatusTarget = new Label
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Tag = "theme-skip"
            };
            layout.Controls.Add(_lblMainStatusPill, 0, 0);
            layout.Controls.Add(_lblMainStatusTarget, 1, 0);
            panel.Controls.Add(layout);
            return panel;
        }

        private void UpdateMainStatusPanel(MasterStatus status)
        {
            if (_lblMainStatusPill != null)
            {
                var color = GetStatusColor(status);
                _lblMainStatusPill.Text = GetStatusText(status);
                _lblMainStatusPill.ForeColor = color;
                _lblMainStatusPill.BackColor = status == MasterStatus.Exception
                    ? Color.FromArgb(247, 226, 222)
                    : status == MasterStatus.Preparing || status == MasterStatus.Complete
                        ? Color.FromArgb(247, 236, 213)
                        : UiTheme.SuccessSoft;
            }

            if (_lblMainStatusTarget != null)
            {
                _lblMainStatusTarget.Text = $"目标 {ConfigurationHelper.GetPidTemperature():F1} ℃";
            }

            if (_lblRuntimeRecordState != null)
            {
                _lblRuntimeRecordState.Text = GetRecordStateText(status);
                _lblRuntimeRecordState.ForeColor = GetRecordStateColor(status);
            }
        }

        private static string GetStatusText(MasterStatus status)
        {
            return status switch
            {
                MasterStatus.Preparing => "升温中",
                MasterStatus.Ready => "可记录",
                MasterStatus.Recording => "记录中",
                MasterStatus.Complete => "待保存",
                MasterStatus.Exception => "异常",
                _ => "未开始"
            };
        }

        private static string GetRecordStateText(MasterStatus status)
        {
            return status switch
            {
                MasterStatus.Ready => "可记录",
                MasterStatus.Recording => "记录中",
                MasterStatus.Complete => "待保存",
                MasterStatus.Exception => "异常",
                _ => "未记录"
            };
        }

        private static Color GetStatusColor(MasterStatus status)
        {
            return status switch
            {
                MasterStatus.Preparing => UiTheme.Warning,
                MasterStatus.Complete => UiTheme.Warning,
                MasterStatus.Exception => UiTheme.Danger,
                _ => UiTheme.Success
            };
        }

        private static Color GetRecordStateColor(MasterStatus status)
        {
            return status switch
            {
                MasterStatus.Recording => UiTheme.Success,
                MasterStatus.Ready => UiTheme.Success,
                MasterStatus.Complete => UiTheme.Warning,
                MasterStatus.Exception => UiTheme.Danger,
                _ => UiTheme.InkMuted
            };
        }

        private void StyleDataTables()
        {
            UiTheme.StyleDataGridView(dgvSystemMessage);
            UiTheme.StyleDataGridView(dgvRealTimeData);
            StyleDashboardMessageTables();
            UiTheme.StyleDataGridView(dgvSurfaceTemp);
            UiTheme.StyleDataGridView(dgvReportData);
            UiTheme.StyleDataGridView(dgvQueryData);
        }

        private void StyleDashboardMessageTables()
        {
            var numberFont = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            var textFont = new Font("Microsoft YaHei", 10F, FontStyle.Regular, GraphicsUnit.Point);

            foreach (var grid in new[] { dgvSystemMessage, dgvRealTimeData })
            {
                grid.BorderStyle = BorderStyle.None;
                grid.BackgroundColor = UiTheme.Surface;
                grid.ColumnHeadersHeight = 34;
                grid.RowTemplate.Height = 32;
                grid.AllowUserToResizeRows = false;
                grid.AllowUserToResizeColumns = false;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
                grid.ScrollBars = ScrollBars.Both;
                grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }

            if (dgvSystemMessage.Columns.Contains("Time"))
            {
                dgvSystemMessage.Columns["Time"].Width = 130;
                dgvSystemMessage.Columns["Time"].DefaultCellStyle.Font = numberFont;
            }

            if (dgvSystemMessage.Columns.Contains("Content"))
            {
                dgvSystemMessage.Columns["Content"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvSystemMessage.Columns["Content"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvSystemMessage.Columns["Content"].DefaultCellStyle.Font = textFont;
            }

            dgvRealTimeData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            SetColumnWidth(dgvRealTimeData, "Timer", 130);
            SetColumnWidth(dgvRealTimeData, "Temp1", 140);
            SetColumnWidth(dgvRealTimeData, "Temp2", 140);
            SetColumnWidth(dgvRealTimeData, "TempSurface", 140);
            SetColumnWidth(dgvRealTimeData, "TempCenter", 140);
            SetColumnWidth(dgvRealTimeData, "TempDrift", 140);
            foreach (DataGridViewColumn column in dgvRealTimeData.Columns)
            {
                column.DefaultCellStyle.Font = numberFont;
            }
        }

        private static void SetColumnWidth(DataGridView grid, string columnName, int width)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].Width = width;
            }
        }

        private void ApplyChartTheme()
        {
            UiTheme.StylePlotHost(chartPanel);
            if (_chartModel != null)
            {
                UiTheme.StylePlot(_chartModel, "实时温度趋势");
                if (_seriesTF1 != null)
                {
                    _seriesTF1.Color = OxyColor.FromRgb(74, 120, 168);
                }

                if (_seriesTF2 != null)
                {
                    _seriesTF2.Color = OxyColor.FromRgb(176, 113, 84);
                }

                if (_seriesTS != null)
                {
                    _seriesTS.Color = OxyColor.FromRgb(79, 143, 118);
                }

                if (_seriesTC != null)
                {
                    _seriesTC.Color = OxyColor.FromRgb(154, 115, 53);
                }

                _chartModel.InvalidatePlot(false);
            }

            if (_chartView != null)
            {
                _chartView.BackColor = UiTheme.Surface;
            }

            if (_centerChartModel != null)
            {
                UiTheme.StylePlot(_centerChartModel, "中心轴温度分布");
                _centerChartModel.InvalidatePlot(false);
            }

            if (_centerChartView != null)
            {
                _centerChartView.BackColor = UiTheme.Surface;
            }
        }

        private void BuildMetricDisplay()
        {
            panelDataDisplay.SuspendLayout();
            panelDataDisplay.Controls.Clear();
            panelDataDisplay.Padding = new Padding(14);
            lblTempRise.Text = $"温度漂移 ({TestMaster.TemperatureDriftUnitText})";

            var y = 12;
            panelDataDisplay.Controls.Add(CreateMetricHeader(12, y));
            y += 42;
            panelDataDisplay.Controls.Add(CreateSectionLabel("计时", 12, y));
            y += 30;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblTime, dataTime, UiTheme.Accent, true, 72, 12, y));
            y += 80;
            panelDataDisplay.Controls.Add(CreateSectionLabel("炉内温度", 12, y));
            y += 30;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblTemp1, dataTemp1, Color.FromArgb(74, 120, 168), height: 64, x: 12, y: y));
            y += 72;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblTemp2, dataTemp2, Color.FromArgb(176, 113, 84), height: 64, x: 12, y: y));
            y += 76;
            panelDataDisplay.Controls.Add(CreateSectionLabel("试样温度", 12, y));
            y += 30;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblSurfaceTemp, dataSurfaceTemp, UiTheme.Success, height: 64, x: 12, y: y));
            y += 72;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblCenterTemp, dataCenterTemp, UiTheme.MetricGlow, height: 64, x: 12, y: y));
            y += 72;
            panelDataDisplay.Controls.Add(CreateMetricCard(lblTempRise, dataTempRise, UiTheme.MetricGlow, height: 64, x: 12, y: y));

            if (!_metricDisplayResizeHooked)
            {
                panelDataDisplay.Resize += (_, _) => ResizeMetricCards();
                _metricDisplayResizeHooked = true;
            }

            ResizeMetricCards();
            panelDataDisplay.ResumeLayout();
            panelDataDisplay.Invalidate();
        }

        private Panel CreateMetricHeader(int x, int y)
        {
            var header = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(260, 30),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };

            var title = new Label
            {
                Dock = DockStyle.Left,
                Width = 160,
                ForeColor = UiTheme.TitleInk,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "实时监测",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            var refresh = new Label
            {
                Dock = DockStyle.Right,
                Width = 80,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "1 秒刷新",
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            header.Controls.Add(refresh);
            header.Controls.Add(title);
            return header;
        }

        private Panel CreateSectionLabel(string text, int x, int y)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(260, 26),
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0),
                Tag = "theme-skip"
            };

            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold, GraphicsUnit.Point),
                BackColor = Color.Transparent
            };

            panel.Controls.Add(label);
            return panel;
        }

        private Panel CreateMetricCard(Label titleLabel, Label valueLabel, Color accentColor, bool emphasize = false, int height = 70, int x = 0, int y = 0)
        {
            UiTheme.StyleMetricTitle(titleLabel);
            UiTheme.StyleMetricValue(valueLabel, accentColor, emphasize);
            titleLabel.Height = emphasize ? 22 : 20;
            valueLabel.Height = emphasize ? 36 : 32;
            valueLabel.Font = emphasize
                ? new Font("Consolas", 18F, FontStyle.Bold, GraphicsUnit.Point)
                : new Font("Consolas", 16F, FontStyle.Bold, GraphicsUnit.Point);

            var accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = accentColor,
                Tag = "theme-skip"
            };

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = emphasize ? new Padding(14, 6, 12, 6) : new Padding(14, 5, 12, 5),
                BackColor = Color.Transparent,
                Tag = "theme-skip"
            };
            titleLabel.Dock = DockStyle.Top;
            valueLabel.Dock = DockStyle.Fill;
            content.Controls.Add(valueLabel);
            content.Controls.Add(titleLabel);

            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(260, height),
                Dock = DockStyle.None,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = UiTheme.SurfaceRaised,
                BorderStyle = BorderStyle.None,
                Tag = "theme-skip"
            };

            card.Controls.Add(content);
            card.Controls.Add(accentBar);
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                UiTheme.Border,
                ButtonBorderStyle.Solid);
            return card;
        }

        private void ResizeMetricCards()
        {
            if (panelDataDisplay.Controls.Count == 0)
            {
                return;
            }

            var width = Math.Max(220, panelDataDisplay.ClientSize.Width - 24);
            foreach (Control control in panelDataDisplay.Controls)
            {
                control.Width = width;
            }
        }

        private void ApplyMessageToggleButtonState()
        {
            UiTheme.StyleButton(btnSystemMessage, _showSystemMessages ? ButtonTone.Primary : ButtonTone.Neutral, compact: true);
            UiTheme.StyleButton(btnRealTimeData, _showSystemMessages ? ButtonTone.Neutral : ButtonTone.Primary, compact: true);
            btnSystemMessage.MinimumSize = new Size(136, 38);
            btnSystemMessage.Size = new Size(136, 38);
            btnRealTimeData.MinimumSize = new Size(136, 38);
            btnRealTimeData.Size = new Size(136, 38);
            UpdateMessageTableMeta();
        }

        /*
         * 功能: 初始化按钮悬停效果
         */
        private void InitializeButtonHoverEffects()
        {
            // 工业风主题统一使用 FlatAppearance 处理按钮交互，这里不再绑定旧配色事件。
        }

        /*
         * 功能: 为按钮添加悬停效果
         */
        private void AddButtonHoverEffect(Button button, Color normalColor, Color hoverColor)
        {
            UiTheme.StyleButton(button, ButtonTone.Neutral, compact: true);
        }

        /*
         * 功能: 绘制数据显示面板（包含炉子图片）
         */
        private void PanelDataDisplay_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                // 尝试加载炉子图片
                var furnaceImage = ResourceHelper.LoadFurnaceImage();
                
                if (furnaceImage != null)
                {
                    using (furnaceImage)
                    {
                        // 在面板底部绘制小尺寸炉体图标，作为工业仪表盘背景装饰
                        int imageWidth = 112;
                        int imageHeight = 112;
                        int x = (panelDataDisplay.Width - imageWidth) / 2;
                        int y = panelDataDisplay.Height - imageHeight - 18;
                        
                        e.Graphics.DrawImage(furnaceImage, x, y, imageWidth, imageHeight);
                    }
                }
                else
                {
                    // 如果图片不存在，绘制一个简单的炉子示意图
                    DrawFurnacePlaceholder(e.Graphics);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "绘制炉子图片失败");
                // 绘制占位符
                DrawFurnacePlaceholder(e.Graphics);
            }
        }

        /*
         * 功能: 绘制炉子占位符
         */
        private void DrawFurnacePlaceholder(Graphics g)
        {
            try
            {
                int width = 104;
                int height = 104;
                int x = (panelDataDisplay.Width - width) / 2;
                int y = panelDataDisplay.Height - height - 24;

                using (var pen = new Pen(UiTheme.Border, 2))
                {
                    g.DrawRectangle(pen, x, y, width, height);
                }

                using (var brush = new SolidBrush(UiTheme.GridHeader))
                {
                    g.FillRectangle(brush, x + 8, y + 8, width - 16, height - 16);
                }

                using (var font = new Font("Microsoft YaHei", 10F, FontStyle.Bold))
                using (var brush = new SolidBrush(UiTheme.VideoLabel))
                {
                    var text = "试验炉";
                    var textSize = g.MeasureString(text, font);
                    var textX = x + (width - textSize.Width) / 2;
                    var textY = y + (height - textSize.Height) / 2;
                    g.DrawString(text, font, brush, textX, textY);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"绘制炉子占位符失败: {ex.Message}");
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem clickedItem)
            {
                int tabIndex = clickedItem.Text switch
                {
                    "样品试验" => 0,
                    "系统校验" => 1,
                    "试验报告" => 2,
                    "记录查询" => 3,
                    _ => 0
                };

                SetSelectedMenuItem(clickedItem, tabIndex);
            }
        }

        private void SetSelectedMenuItem(ToolStripMenuItem selectedItem, int tabIndex)
        {
            if (_selectedMenuItem != null)
            {
                _selectedMenuItem.BackColor = Color.Transparent;
                _selectedMenuItem.ForeColor = UiTheme.Ink;
            }

            tabControl1.SelectedIndex = tabIndex;

            selectedItem.BackColor = UiTheme.Accent;
            selectedItem.ForeColor = Color.White;

            _selectedMenuItem = selectedItem;
        }

        private void btnNewTest_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击新建试验按钮");
                var currentMaster = _testMaster1 ?? SystemContext.Current.Master1;
                if (!CanCreateNewTest(currentMaster?.Status ?? MasterStatus.Idle, currentMaster?.GetActiveTestOrNull()))
                {
                    ExceptionHandler.ShowWarning("当前试验已完成但尚未保存，请先点击“记录试后数据”并生成报告。", "请先保存当前试验");
                    return;
                }

                using (NewTestForm newTestForm = new NewTestForm())
                {
                    if (newTestForm.ShowDialog(this) == DialogResult.OK)
                    {
                        // 重置温度曲线图表（标准重置模式）
                        ResetChart();

                        // 清空系统消息和实时数据表格
                        ClearMessageTables();
                                        
                        // 重置 DaqWorker计时器
                        _daqWorker?.ResetTimer();

                        // 添加系统消息（与Web版一致，只显示在系统消息中）
                        var testMaster = SystemContext.Current.Masters.DictTestMaster[0];
                        var testData = testMaster.GetTestData();
                        if (testData != null)
                        {
                            Log.Information("新建试验成功，样品编号: {ProductId}, 样品标识: {TestId}", 
                                testData.Productid, testData.Testid);
                            AppendSystemMessage($"创建新试验成功。样品编号: [{testData.Productid}], 样品标识: [{testData.Testid}]");
                            SyncMainTestModeControls(testData, _testMaster1?.Status ?? SystemContext.Current.Master1?.Status ?? MasterStatus.Idle);
                        }
                    }
                    else
                    {
                        Log.Information("用户取消新建试验");
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "打开新建试验窗口失败", "btnNewTest_Click");
            }
        }

        /*
         * 功能: 开始升温按钮点击事件
         */
        private async void btnStartHeating_Click(object sender, EventArgs e)
        {
            // 禁用按钮防止重复点击
            btnStartHeating.Enabled = false;

            try
            {
                using (var progress = new ProgressIndicator(this, "正在启动加热..."))
                {
                    try
                    {
                        Log.Information("用户点击开始升温按钮");

                        var session = GetSessionOrWarn();
                        if (session == null)
                        {
                            return;
                        }

                        if (!EnsureHardwareReadyForStart("开始升温", requireSensor: true, requirePid: true))
                        {
                            return;
                        }

                        var result = await session.StartHeatingAsync();

                        if (result.Success)
                        {
                            Log.Information("试验装置开始加热成功");
                            AppendSystemMessage("试验装置开始加热。");
                        }
                        else
                        {
                            Log.Warning("试验装置开始加热失败: {Message}", result.Message);
                            ExceptionHandler.ShowWarning("通信异常，炉温加热未能启动。\n请检查设备连接。");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.HandleHardwareException(ex, "加热控制器");
                    }
                }
            }
            finally
            {
                // 恢复按钮状态
                btnStartHeating.Enabled = true;
            }
        }

        /*
         * 功能: 停止升温按钮点击事件
         */
        private void btnStopHeating_Click(object sender, EventArgs e)
        {
            try
            {
                // 添加确认对话框
                if (!ExceptionHandler.Confirm("确定要停止升温吗？", "停止升温确认"))
                {
                    return;
                }

                Log.Information("用户点击停止升温按钮");

                var session = GetSessionOrWarn();
                if (session == null)
                {
                    return;
                }

                var result = session.StopHeating();

                if (result.Success)
                {
                    Log.Information("试验装置停止加热成功");
                    AppendSystemMessage("试验装置已停止加热。");
                }
                else
                {
                    Log.Warning("试验装置停止加热失败: {Message}", result.Message);
                    ExceptionHandler.ShowWarning("通信异常，试验装置未能停止加热。\n请检查设备连接。");
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleHardwareException(ex, "加热控制器");
            }
        }

        /*
         * 功能: 开始记录按钮点击事件（标准分层架构）
         */
        private void btnOpenRecord_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击开始记录按钮");

                var session = GetSessionOrWarn();
                if (session == null)
                {
                    return;
                }

                if (!ValidateAndApplyMainTestModeForStart())
                {
                    return;
                }

                if (!EnsureHardwareReadyForStart("开始记录", requireSensor: true, requirePid: true))
                {
                    return;
                }

                var result = session.StartRecording();

                if (result.Success)
                {
                    var testData = TryGetCurrentTestData();
                    if (testData == null)
                    {
                        ExceptionHandler.ShowWarning("试验控制器尚未接收试验样品信息，请先新建本次试验。", "无法开始记录");
                        return;
                    }

                    Log.Information("开始记录试验数据成功，样品编号: {ProductId}, 样品标识: {TestId}", 
                        testData.Productid, testData.Testid);
                    AppendSystemMessage($"开始记录试验数据。样品编号: [{testData.Productid}], 样品标识: [{testData.Testid}]");
                }
                else
                {
                    Log.Warning("开始记录失败: {Message}", result.Message);
                    ExceptionHandler.ShowWarning(result.Message, "无法开始记录");
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleHardwareException(ex, "数据采集设备");
            }
        }

        /*
         * 功能: 停止记录按钮点击事件
         */
        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            try
            {
                // 添加确认对话框
                if (!ExceptionHandler.Confirm("确定要停止记录吗？\n\n停止后将无法继续记录当前试验数据。", "停止记录确认"))
                {
                    return;
                }

                Log.Information("用户点击停止记录按钮");

                var session = GetSessionOrWarn();
                if (session == null)
                {
                    return;
                }

                var result = session.StopRecording();

                if (result.Success)
                {
                    Log.Information("停止记录成功");
                    AppendSystemMessage("计时结束。");
                }
                else
                {
                    Log.Warning("停止记录失败: {Message}", result.Message);
                    ExceptionHandler.ShowWarning(result.Message, "无法停止记录");
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleHardwareException(ex, "数据采集设备");
            }
        }

        /*
         * 功能: 试验记录按钮点击事件（完成试验流程）
         * 说明: 与原Web项目保持一致，完整执行试验后期处理流程：
         *       1. 设置试验后数据（现象编码、火焰时间、残余质量）
         *       2. 执行试验后期处理（保存数据到数据库、生成报告文件）
         *       3. 清空本次试验数据缓存
         */
        private async void btnRecordLogs_Click(object sender, EventArgs e)
        {
            try
            {
                var session = GetSessionOrWarn();
                if (session == null)
                {
                    return;
                }

                var testMaster = SystemContext.Current.Master1;
                if (testMaster == null)
                {
                    ExceptionHandler.ShowWarning("一号炉控制器尚未初始化，请先完成系统初始化。", "系统未就绪");
                    return;
                }

                var testData = testMaster.GetTestData();

                // 判断是否已新建试验（业务规则验证）
                if (testData == null)
                {
                    ExceptionHandler.ShowWarning("试验控制器尚未接收试验样品信息，请先新建本次试验。", "无法记录试验数据");
                    return;
                }

                if (!CanPostTestRecord(testMaster.Status, testData))
                {
                    ExceptionHandler.ShowWarning("本次试验尚未完成，请等待试验自动达到终止条件后再保存试验记录。", "无法记录试验数据");
                    return;
                }

                // 打开试验记录对话框
                using (TestPhenoForm testPhenoForm = new TestPhenoForm())
                {
                    if (testPhenoForm.ShowDialog(this) == DialogResult.OK)
                    {
                        AppendSystemMessage($"试验记录数据已设置。现象编码: [{testPhenoForm.PhenoCode}], 残余质量: [{testPhenoForm.PostWeight}g]");

                        CommandResult result;
                        using (var progress = new ProgressIndicator(this, "正在保存试验数据并生成报告..."))
                        {
                            result = await session.SubmitPostTestAsync(
                                testPhenoForm.PhenoCode,
                                testPhenoForm.FlameTime,
                                testPhenoForm.FlameDuration,
                                testPhenoForm.PostWeight);

                            if (!result.Success)
                            {
                                Log.Warning("保存试验记录失败: {Message}", result.Message);
                                ExceptionHandler.ShowWarning(result.Message, "无法记录试验数据");
                                return;
                            }
                        }

                        testMaster.ResetTestData();
                        UpdateButtonStates(testMaster.Status);

                        if (result.ReportGenerated)
                        {
                            Log.Information("试验完成，数据已保存，试验包已生成: {PackagePath}, Excel={ExcelPath}", result.TestPackagePath, result.ExcelReportPath);
                            AppendSystemMessage(!string.IsNullOrWhiteSpace(result.TestPackagePath)
                                ? $"试验已完成，试验包已生成: {result.TestPackagePath}"
                                : $"试验已完成，Excel报告已生成: {result.ExcelReportPath}");
                            ShowGeneratedTestPackage(result);
                        }
                        else
                        {
                            Log.Warning("试验完成，数据已保存，但报告未生成: {Message}", result.Message);
                            AppendSystemMessage(result.Message);
                            ExceptionHandler.ShowWarning(result.Message, "报告生成提示");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "完成试验流程失败");
                ExceptionHandler.HandleDatabaseException(ex, "保存试验记录");
            }
        }

        private SampleTestSessionService? GetSessionOrWarn()
        {
            var session = SystemContext.Current.Session;
            if (session != null)
            {
                return session;
            }

            const string message = "会话服务尚未初始化，请先完成系统初始化。";
            Log.Warning(message);
            AppendSystemMessage(message);
            ExceptionHandler.ShowWarning(message, "系统未就绪");
            return null;
        }

        private void ShowGeneratedTestPackage(CommandResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.TestPackagePath) && Directory.Exists(result.TestPackagePath))
            {
                var message = $"试验数据已保存，试验包已生成。\n\n试验包：{result.TestPackagePath}\n\n是否打开试验包文件夹？";
                if (ExceptionHandler.Confirm(message, "试验完成"))
                {
                    OpenFolder(result.TestPackagePath);
                }

                return;
            }

            ExceptionHandler.ShowSuccess($"试验数据已保存，Excel报告已生成。\n\nExcel路径：{result.ExcelReportPath}");
        }

        private static void OpenFolder(string folderPath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "打开试验包文件夹失败: {FolderPath}", folderPath);
                ExceptionHandler.ShowWarning($"试验包已生成，但打开文件夹失败。\n\n试验包：{folderPath}", "打开试验包");
            }
        }

        private static bool CanPostTestRecord(MasterStatus status, Testmaster? testData)
        {
            if (testData == null)
            {
                return false;
            }

            return status == MasterStatus.Complete || testData.Totaltesttime > 0;
        }

        private static bool CanStartRecord(MasterStatus status, Testmaster? testData)
        {
            return status == MasterStatus.Ready
                && testData != null
                && !HasUnsavedCompletedTest(testData);
        }

        private static string? GetStatusMessage(MasterStatus oldStatus, MasterStatus newStatus, Testmaster? testData)
        {
            if (oldStatus == newStatus)
            {
                return null;
            }

            if (HasUnsavedCompletedTest(testData) && newStatus == MasterStatus.Complete)
            {
                return "试验已完成，请点击“试验记录”保存并生成报告。";
            }

            if (HasUnsavedCompletedTest(testData)
                && (newStatus == MasterStatus.Preparing || newStatus == MasterStatus.Ready))
            {
                return null;
            }

            return newStatus switch
            {
                MasterStatus.Idle => "系统空闲",
                MasterStatus.Preparing => "正在升温准备中...",
                MasterStatus.Ready => "已达到试验条件，可以开始记录",
                MasterStatus.Recording => "正在记录试验数据...",
                MasterStatus.Complete => "试验已完成",
                MasterStatus.Exception => "系统异常",
                _ => $"状态: {newStatus}"
            };
        }

        private static bool HasUnsavedCompletedTest(Testmaster? testData)
        {
            return testData != null
                && testData.Totaltesttime > 0
                && !string.Equals(testData.Flag, "10000000", StringComparison.Ordinal);
        }

        private static bool CanCreateNewTest(MasterStatus status, Testmaster? testData)
        {
            if (testData == null)
            {
                return true;
            }

            bool isCompletedOrRecorded = status == MasterStatus.Complete || testData.Totaltesttime > 0;
            bool isSaved = string.Equals(testData.Flag, "10000000", StringComparison.Ordinal);
            return !isCompletedOrRecorded || isSaved;
        }

        private static bool TryApplyMainPageTestMode(
            Testmaster? testData,
            string modeText,
            string durationMinutesText,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (testData == null)
            {
                errorMessage = "请先新建本次试验。";
                return false;
            }

            if (string.Equals(modeText.Trim(), FixedDurationTestModeText, StringComparison.Ordinal))
            {
                if (!int.TryParse(durationMinutesText.Trim(), out int minutes) || minutes <= 0)
                {
                    errorMessage = "固定时长模式下，试验时长必须是大于 0 的整数分钟。";
                    return false;
                }

                testData.UseFixedDuration = true;
                testData.TargetDurationSeconds = minutes * 60;
                return true;
            }

            testData.UseFixedDuration = false;
            testData.TargetDurationSeconds = DefaultTargetDurationSeconds;
            return true;
        }

        private void ApplyMainTestModeSelection(bool showWarning)
        {
            if (_isSyncingMainTestModeControls)
            {
                return;
            }

            var testData = TryGetCurrentTestData();
            var status = _testMaster1?.Status ?? SystemContext.Current.Master1?.Status ?? MasterStatus.Idle;
            var modeText = _comboMainTestMode?.SelectedItem?.ToString() ?? StandardTestModeText;
            var durationText = _txtMainDurationMinutes?.Text ?? (DefaultTargetDurationSeconds / 60).ToString();

            if (!TryApplyMainPageTestMode(testData, modeText, durationText, out string errorMessage))
            {
                SetMainTestModeHint(errorMessage, isError: true);
                if (showWarning && !string.IsNullOrWhiteSpace(errorMessage))
                {
                    ExceptionHandler.ShowWarning(errorMessage, "试验时长设置");
                }

                UpdateMainTestModeControlState(testData, status);
                return;
            }

            UpdateMainTestModeControlState(testData, status);
        }

        private bool ValidateAndApplyMainTestModeForStart()
        {
            var testData = TryGetCurrentTestData();
            var modeText = _comboMainTestMode?.SelectedItem?.ToString()
                ?? (testData?.UseFixedDuration == true ? FixedDurationTestModeText : StandardTestModeText);
            var durationText = _txtMainDurationMinutes?.Text
                ?? ((testData?.TargetDurationSeconds > 0 ? testData.TargetDurationSeconds : DefaultTargetDurationSeconds) / 60).ToString();

            if (!TryApplyMainPageTestMode(testData, modeText, durationText, out string errorMessage))
            {
                ExceptionHandler.ShowWarning(errorMessage, "无法开始记录");
                return false;
            }

            SyncMainTestModeControls(testData, _testMaster1?.Status ?? SystemContext.Current.Master1?.Status ?? MasterStatus.Idle);
            return true;
        }

        private void SyncMainTestModeControls(Testmaster? testData, MasterStatus status)
        {
            if (_comboMainTestMode == null || _txtMainDurationMinutes == null)
            {
                return;
            }

            _isSyncingMainTestModeControls = true;
            try
            {
                var targetSeconds = testData?.TargetDurationSeconds > 0
                    ? testData.TargetDurationSeconds
                    : DefaultTargetDurationSeconds;

                _comboMainTestMode.SelectedItem = testData?.UseFixedDuration == true
                    ? FixedDurationTestModeText
                    : StandardTestModeText;
                _txtMainDurationMinutes.Text = Math.Max(1, targetSeconds / 60).ToString();
                UpdateMainTestModeControlState(testData, status);
            }
            finally
            {
                _isSyncingMainTestModeControls = false;
            }
        }

        private void UpdateMainTestModeControlState(Testmaster? testData, MasterStatus status)
        {
            var hasTest = testData != null;
            var isRecording = status == MasterStatus.Recording;
            var hasRecordedData = testData?.Totaltesttime > 0 || status == MasterStatus.Complete;
            var isFixedDuration = _comboMainTestMode?.SelectedItem?.ToString() == FixedDurationTestModeText;
            var canEdit = hasTest && !isRecording && !hasRecordedData;

            if (_comboMainTestMode != null)
            {
                _comboMainTestMode.Enabled = canEdit;
            }

            if (_txtMainDurationMinutes != null)
            {
                _txtMainDurationMinutes.Enabled = canEdit && isFixedDuration;
            }

            if (_lblMainDurationUnit != null)
            {
                _lblMainDurationUnit.Enabled = canEdit && isFixedDuration;
            }

            if (_lblStateProductId != null)
            {
                _lblStateProductId.Text = hasTest ? testData!.Productid : "--";
            }

            if (_lblStateTestId != null)
            {
                _lblStateTestId.Text = hasTest ? testData!.Testid : "--";
            }

            if (!hasTest)
            {
                SetMainTestModeHint("请先新建本次试验", isError: false);
            }
            else if (isRecording)
            {
                SetMainTestModeHint("正在记录，试验模式已锁定", isError: false);
            }
            else if (hasRecordedData)
            {
                SetMainTestModeHint("本次试验已有记录，试验模式已锁定", isError: false);
            }
            else if (isFixedDuration)
            {
                SetMainTestModeHint("到达指定分钟数后直接完成", isError: false);
            }
            else
            {
                SetMainTestModeHint("按标准时间点和终止条件判断", isError: false);
            }
        }

        private void SetMainTestModeHint(string text, bool isError)
        {
            if (_lblMainTestModeHint == null)
            {
                return;
            }

            _lblMainTestModeHint.Text = text;
            _lblMainTestModeHint.ForeColor = isError ? UiTheme.Danger : UiTheme.InkMuted;
        }

        /*
         * 功能: 参数设置按钮点击事件
         */
        private async void btnSetParam_Click(object sender, EventArgs e)
        {
            try
            {
                // 从全局上下文获取TestMaster实例
                var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                // 获取当前设备参数
                var apparatus = await testMaster.GetApparatusParam();

                // 创建参数设置对话框
                SetParamForm setParamForm;
                if (apparatus != null)
                {
                    // 如果设备参数存在，使用现有参数初始化对话框
                    setParamForm = new SetParamForm(
                        apparatus.Innernumber,
                        apparatus.Apparatusname,
                        apparatus.Checkdatef,
                        apparatus.Checkdatet,
                        apparatus.Pidport,
                        apparatus.Constpower ?? 0);
                }
                else
                {
                    // 如果设备参数不存在，使用默认值
                    setParamForm = new SetParamForm();
                }

                // 显示对话框
                using (setParamForm)
                {
                    if (setParamForm.ShowDialog(this) == DialogResult.OK)
                    {
                        // 禁用按钮防止重复点击
                        btnParamSettings.Enabled = false;
                        this.Cursor = Cursors.WaitCursor;

                        // 调用TestMaster的SetApparatusParam方法保存设备参数
                        var success = await testMaster.SetApparatusParam(
                            setParamForm.ApparatusId,
                            setParamForm.ApparatusName,
                            setParamForm.CheckDateFrom,
                            setParamForm.CheckDateTo,
                            setParamForm.PidPort,
                            setParamForm.ConstPower);

                        if (success)
                        {
                            // 添加系统消息
                            AppendSystemMessage($"设备参数已保存。设备编号: [{setParamForm.ApparatusId}], 设备名称: [{setParamForm.ApparatusName}]。当前按单串口共口模式处理，PID/功率端口都会使用 [{setParamForm.PidPort}]，重启软件后生效。");
                            ExceptionHandler.ShowSuccess("设备参数已成功保存。当前按单串口共口模式处理，重启软件后生效。");
                        }
                        else
                        {
                            ExceptionHandler.ShowWarning("保存设备参数失败，请检查数据库连接。");
                        }

                        // 恢复按钮状态
                        btnParamSettings.Enabled = true;
                        this.Cursor = Cursors.Default;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "设置设备参数");
                
                // 恢复按钮状态
                btnParamSettings.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private void 导出温度曲线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportChartToImage();
        }

        private void 批量导出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换到记录查询页面
            SetSelectedMenuItem(记录查询ToolStripMenuItem, 3);
            
            // 调用批量导出功能
            BatchExportTestData();
        }

        private void 日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户打开日志查看器");
                using (var logViewer = new LogViewerForm())
                {
                    logViewer.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开日志查看器失败");
                MessageBox.Show($"打开日志查看器失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("建筑材料不燃性试验系统 版本 3.0\n\n符合ISO 11820标准",
                "关于系统",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var master = _testMaster1 ?? SystemContext.Current.Master1;
            var confirmMessage = "确定要退出系统吗？\n\n退出后所有未保存的数据将丢失。";

            if (master != null && master.Status == MasterStatus.Recording)
            {
                confirmMessage = "1号试验装置正在试验中，继续退出将导致数据丢失，是否继续？";
            }

            if (!ExceptionHandler.Confirm(confirmMessage, "退出确认"))
            {
                e.Cancel = true;
            }
            else
            {
                // 取消订阅DaqWorker事件
                if (_daqWorker != null)
                {
                    _daqWorker.SensorDataReceived -= OnSensorDataReceived;
                }

                // 取消订阅TestMaster1事件
                // Requirement 1.1, 1.2, 1.4: 清理事件订阅
                if (_testMaster1 != null)
                {
                    _testMaster1.DataBroadcast -= OnTestMasterDataBroadcast;
                    _testMaster1.StateChanged -= OnTestMasterStateChanged;
                    if (_testMaster1.IsFlameDetectionEnabled)
                    {
                        _testMaster1.FlameDetected -= OnTestMasterFlameDetected;
                    }
                }

                _connectionStatusTimer?.Stop();
                _connectionStatusTimer?.Dispose();
                _connectionStatusTimer = null;
                
                Log.Information("用户退出系统");
            }
        }

        #region 温度曲线图表功能

        /*
         * 功能: 设置DaqWorker实例并订阅事件
         */
        public void SetDaqWorker(DaqWorker daqWorker)
        {
            _daqWorker = daqWorker;

            // 订阅传感器数据事件
            if (_daqWorker != null)
            {
                _daqWorker.SensorDataReceived += OnSensorDataReceived;
            }
        }

        /*
         * 功能: 设置TestMaster1实例并订阅事件
         * Requirement 1.1, 1.2, 1.4: 订阅TestMaster1事件以接收实时数据和状态更新
         */
        public void SetTestMaster1(TestMaster1? testMaster1)
        {
            _testMaster1 = testMaster1;

            if (_testMaster1 != null)
            {
                // Requirement 1.1: 订阅DataBroadcast事件以接收实时传感器数据
                _testMaster1.DataBroadcast += OnTestMasterDataBroadcast;
                
                // Requirement 1.2: 订阅StateChanged事件以接收状态变更通知
                _testMaster1.StateChanged += OnTestMasterStateChanged;
                
                if (_testMaster1.IsFlameDetectionEnabled)
                {
                    _testMaster1.FlameDetected += OnTestMasterFlameDetected;
                }
            }

            SyncMainTestModeControls(TryGetCurrentTestData(), _testMaster1?.Status ?? MasterStatus.Idle);
        }

        /*
         * 功能: 处理TestMaster1的数据广播事件
         * Requirement 1.1, 1.4: 在Recording状态下以1秒间隔接收数据更新
         * 注意：图表数据也从此处更新（使用Modbus数据），而非DaqWorker
         */
        private void OnTestMasterDataBroadcast(object? sender, DataBroadcastEventArgs e)
        {
            // 跨线程调用
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTestMasterDataBroadcast(sender, e)));
                return;
            }

            try
            {
                // 更新左侧温度数据显示（从TestMaster获取的PID控制器温度）
                if (e.SensorData != null)
                {
                    dataTemp1.Text = e.SensorData.Temp1.ToString("F1");
                    dataTemp2.Text = e.SensorData.Temp2.ToString("F1");
                    dataSurfaceTemp.Text = e.SensorData.TempSuf.ToString("F1");
                    dataCenterTemp.Text = e.SensorData.TempCen.ToString("F1");

                    // 更新温度曲线图表（由 TestMaster 广播驱动）
                    UpdateChartFromModbus(e.SensorData);
                }

                // 更新温度漂移显示
                if (e.CalculateData != null)
                {
                    dataTempRise.Text = e.CalculateData.TempDriftMean.ToString("F1");
                }

                // 处理控制器消息
                if (e.Messages != null && e.Messages.Count > 0)
                {
                    foreach (var msg in e.Messages)
                    {
                        AppendSystemMessage(msg.Message);
                    }
                }

                // 如果检测到火焰，更新UI显示
                if (e.FlameDetected)
                {
                    Log.Information("DataBroadcast: 检测到火焰事件，时间={FlameTime}s, 持续={Duration}s", 
                        e.FlameDetectedTime, e.FlameDuration);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理DataBroadcast事件失败");
            }
        }

        /*
         * 功能: 使用 TestMaster 广播数据更新温度曲线图表
         * 说明: 图表时间轴跟随状态机每秒广播推进，覆盖升温和记录两个阶段
         */
        private void UpdateChartFromModbus(SensorDataCatch sensorData)
        {
            try
            {
                if (_seriesTF1 == null || _seriesTF2 == null || _seriesTS == null || _seriesTC == null || _chartModel == null)
                {
                    Log.Warning("图表更新失败: 图表组件未初始化");
                    return;
                }

                // TestMaster 状态机每秒广播一次，直接用计数器表示图表时间轴。
                double xValue = _dataPointCount;

                // 添加数据点到曲线
                _seriesTF1.Points.Add(new DataPoint(xValue, sensorData.Temp1));
                _seriesTF2.Points.Add(new DataPoint(xValue, sensorData.Temp2));
                _seriesTS.Points.Add(new DataPoint(xValue, sensorData.TempSuf));
                _seriesTC.Points.Add(new DataPoint(xValue, sensorData.TempCen));

                _dataPointCount++;

                // 滚动显示：超过10分钟(600秒)时移除最旧数据
                if (_dataPointCount > MAX_DATA_POINTS)
                {
                    _seriesTF1.Points.RemoveAt(0);
                    _seriesTF2.Points.RemoveAt(0);
                    _seriesTS.Points.RemoveAt(0);
                    _seriesTC.Points.RemoveAt(0);

                    // 动态调整X轴范围
                    var xAxis = _chartModel.Axes[0] as LinearAxis;
                    if (xAxis != null && _seriesTF1.Points.Count > 0)
                    {
                        var firstPoint = _seriesTF1.Points[0];
                        xAxis.Minimum = firstPoint.X;
                        xAxis.Maximum = firstPoint.X + CHART_TIME_WINDOW_SECONDS;
                    }
                }

                // 刷新图表显示
                _chartModel.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新Modbus图表数据失败");
            }
        }

        /*
         * 功能: 处理TestMaster1的状态变更事件
         * Requirement 1.2: 接收状态变更通知并更新UI
         */
        private void OnTestMasterStateChanged(object? sender, StateChangedEventArgs e)
        {
            // 跨线程调用
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTestMasterStateChanged(sender, e)));
                return;
            }

            try
            {
                // 记录状态变更
                Log.Information("TestMaster状态变更: {OldStatus} -> {NewStatus}", 
                    e.OldStatus, e.NewStatus);

                // 根据新状态更新UI
                var testData = TryGetCurrentTestData();
                var statusMessage = GetStatusMessage(e.OldStatus, e.NewStatus, testData);

                // 添加系统消息
                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                    AppendSystemMessage(statusMessage);
                }

                // 更新按钮状态（根据当前状态启用/禁用相关按钮）
                UpdateButtonStates(e.NewStatus);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理StateChanged事件失败");
            }
        }

        /*
         * 功能: 处理TestMaster1的火焰检测事件
         * Requirement 1.3: 接收火焰检测通知并更新UI
         */
        private void OnTestMasterFlameDetected(object? sender, FlameEventArgs e)
        {
            // 跨线程调用
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTestMasterFlameDetected(sender, e)));
                return;
            }

            try
            {
                // 记录火焰事件
                Log.Warning("检测到持续火焰事件: 时间={Time}, 持续时间={Duration}秒", 
                    e.Time.ToString("HH:mm:ss"), e.Duration);

                // 添加系统消息（醒目提示）
                AppendSystemMessage($"⚠️ 检测到持续火焰！起火时间: {e.Time:HH:mm:ss}, 持续时间: {e.Duration}秒");

                // 可以在这里添加声音提示或其他警告
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理FlameDetected事件失败");
            }
        }

        private void ReportHardwareStartupStatus()
        {
            if (_daqWorker != null && !_daqWorker.IsSensorConnected)
            {
                AppendSystemMessage($"传感器采集未连接，当前串口: {_daqWorker.SensorPortName}。系统已启动，但实时采集不可用。");
            }

            if (_testMaster1 != null && !_testMaster1.Manipulator.IsConnected)
            {
                AppendSystemMessage($"PID 控制器未连接，当前串口: {_testMaster1.Manipulator.PidPortName}。纯硬件模式下，请先接好硬件再开始升温或记录。");
            }
        }

        private bool EnsureHardwareReadyForStart(string actionName, bool requireSensor, bool requirePid)
        {
            var missingParts = new List<string>();

            if (requireSensor && (_daqWorker == null || !_daqWorker.IsSensorConnected))
            {
                var sensorPort = _daqWorker?.SensorPortName ?? "未配置";
                missingParts.Add($"采集串口 {sensorPort}");
            }

            if (requirePid && (_testMaster1 == null || !_testMaster1.Manipulator.IsConnected))
            {
                var pidPort = _testMaster1?.Manipulator.PidPortName ?? "未配置";
                missingParts.Add($"PID 串口 {pidPort}");
            }

            if (missingParts.Count == 0)
            {
                return true;
            }

            var message = $"{actionName}前检测到硬件未连接：{string.Join("、", missingParts)}。请先连接硬件后再试。";
            Log.Warning(message);
            AppendSystemMessage(message);
            ExceptionHandler.ShowWarning(message, "硬件未连接");
            return false;
        }

        /*
         * 功能: 根据TestMaster状态更新按钮启用状态
         */
        private void UpdateButtonStates(MasterStatus status)
        {
            try
            {
                var testData = TryGetCurrentTestData();

                switch (status)
                {
                    case MasterStatus.Idle:
                        btnStartHeating.Enabled = true;
                        btnStopHeating.Enabled = false;
                        btnNewTest.Enabled = true;
                        btnOpenRecord.Enabled = false;
                        btnStopRecord.Enabled = false;
                        break;

                    case MasterStatus.Preparing:
                        btnStartHeating.Enabled = false;
                        btnStopHeating.Enabled = true;
                        btnNewTest.Enabled = true;
                        btnOpenRecord.Enabled = false;
                        btnStopRecord.Enabled = false;
                        break;

                    case MasterStatus.Ready:
                        btnStartHeating.Enabled = false;
                        btnStopHeating.Enabled = true;
                        btnNewTest.Enabled = true;
                        btnOpenRecord.Enabled = true;
                        btnStopRecord.Enabled = false;
                        break;

                    case MasterStatus.Recording:
                        btnStartHeating.Enabled = false;
                        btnStopHeating.Enabled = false;
                        btnNewTest.Enabled = false;
                        btnOpenRecord.Enabled = false;
                        btnStopRecord.Enabled = true;
                        break;

                    case MasterStatus.Complete:
                        btnStartHeating.Enabled = false;
                        btnStopHeating.Enabled = true;
                        btnNewTest.Enabled = true;
                        btnOpenRecord.Enabled = false;
                        btnStopRecord.Enabled = false;
                        break;

                    case MasterStatus.Exception:
                        btnStartHeating.Enabled = false;
                        btnStopHeating.Enabled = true;
                        btnNewTest.Enabled = false;
                        btnOpenRecord.Enabled = false;
                        btnStopRecord.Enabled = false;
                        break;
                }

                btnOpenRecord.Enabled = btnOpenRecord.Enabled && CanStartRecord(status, testData);
                btnRecordLogs.Enabled = CanPostTestRecord(status, testData);
                btnNewTest.Enabled = btnNewTest.Enabled && CanCreateNewTest(status, testData);
                UpdateMainStatusPanel(status);
                SyncMainTestModeControls(testData, status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新按钮状态失败");
            }
        }

        private static Testmaster? TryGetCurrentTestData()
        {
            return SystemContext.Current.Master1?.GetTestData();
        }

        /*
         * 功能: 初始化温度曲线图表
         */
        private void InitializeChart()
        {
            try
            {
                // 创建PlotModel
                _chartModel = new PlotModel
                {
                    Title = "实时温度趋势",
                    Background = OxyColors.Transparent,
                    TitleFontSize = 12,
                    PlotMargins = new OxyThickness(48, 12, 16, 34),
                    Padding = new OxyThickness(2, 2, 2, 2)
                };

                // 配置X轴（时间轴）
                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "时间(s)",
                    Minimum = 0,
                    Maximum = CHART_TIME_WINDOW_SECONDS,
                    MajorStep = 60,  // 每60秒一个主刻度
                    MinorStep = 10,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineColor = OxyColor.FromRgb(UiTheme.GridLine.R, UiTheme.GridLine.G, UiTheme.GridLine.B),
                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColor.FromRgb(153, 153, 153),
                    TicklineColor = OxyColor.FromRgb(136, 136, 136),
                    TextColor = OxyColor.FromRgb(UiTheme.InkMuted.R, UiTheme.InkMuted.G, UiTheme.InkMuted.B),
                    TitleColor = OxyColor.FromRgb(UiTheme.TitleInk.R, UiTheme.TitleInk.G, UiTheme.TitleInk.B),
                    FontSize = 10,
                    TitleFontSize = 11,
                    AxisTitleDistance = 12
                };
                _chartModel.Axes.Add(xAxis);

                // 配置Y轴（温度轴）
                var yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "温度(℃)",
                    Minimum = 0,
                    Maximum = Y_AXIS_MAX,
                    MajorStep = 100,
                    MinorStep = 20,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineColor = OxyColor.FromRgb(UiTheme.GridLine.R, UiTheme.GridLine.G, UiTheme.GridLine.B)
                };
                _chartModel.Axes.Add(yAxis);

                // 创建4个数据线系列
                _seriesTF1 = new LineSeries
                {
                    Title = "TF1(炉内温度1)",
                    Color = OxyColor.FromRgb(74, 120, 168),
                    StrokeThickness = 2.0,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Solid
                };
                _chartModel.Series.Add(_seriesTF1);

                _seriesTF2 = new LineSeries
                {
                    Title = "TF2(炉内温度2)",
                    Color = OxyColor.FromRgb(176, 113, 84),
                    StrokeThickness = 2.0,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Solid
                };
                _chartModel.Series.Add(_seriesTF2);

                _seriesTS = new LineSeries
                {
                    Title = "TS(表面温度)",
                    Color = OxyColor.FromRgb(79, 143, 118),
                    StrokeThickness = 2.0,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Solid
                };
                _chartModel.Series.Add(_seriesTS);

                _seriesTC = new LineSeries
                {
                    Title = "TC(中心温度)",
                    Color = OxyColor.FromRgb(154, 115, 53),
                    StrokeThickness = 2.0,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Solid
                };
                _chartModel.Series.Add(_seriesTC);

                // 创建PlotView控件
                _chartView = new PlotView
                {
                    Model = _chartModel,
                    Dock = DockStyle.Fill,
                    BackColor = UiTheme.Surface
                };

                // 添加到chartPanel
                chartPanel.Controls.Clear();
                chartPanel.Controls.Add(_chartView);

                _dataPointCount = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图表初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 处理传感器数据更新事件 (DaqWorker/ADAM协议)
         * 注意：温度显示和图表由 OnTestMasterDataBroadcast 更新（使用Modbus数据）
         *       此处只更新计时器、校准温度和实时数据表格
         */
        private void OnSensorDataReceived(object? sender, SensorDataEventArgs e)
        {
            // 跨线程调用
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSensorDataReceived(sender, e)));
                return;
            }

            try
            {
                // 更新计时器显示
                dataTime.Text = e.Timer.ToString("F1");
                
                // 注意：炉内温度、表面温度、中心温度、图表由 TestMaster1 的 DataBroadcast 事件更新
                // 此处不再更新，避免与 Modbus 读取的温度冲突

                // 更新校温热电偶温度显示（用于系统校验，来自 ADAM 模块）
                dataCaliTemp.Text = e.TempCalibration.ToString("F1");
                if (_centerCalibrationTempLabel != null)
                {
                    _centerCalibrationTempLabel.Text = e.TempCalibration.ToString("F1");
                }
                _calibrationView?.SetCalibrationTemperature(e.TempCalibration);
                
                // 更新校准温度稳定状态视觉指示
                // 根据 Requirements 4.3: 当温度在 750±5°C (745-755°C) 范围内时显示稳定状态
                UpdateCalibrationStabilityIndicator(e.TempCalibration);

                // 添加实时数据到表格（标准WinForms数据绑定模式）
                AppendRealTimeData(e);

                // 注意：图表更新已移至 OnTestMasterDataBroadcast -> UpdateChartFromModbus
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理传感器数据失败: {ex.Message}");
            }
        }

        /*
         * 功能: 重置图表（开始新试验时调用）
         */
        private void ResetChart()
        {
            try
            {
                if (_seriesTF1 != null && _seriesTF2 != null && _seriesTS != null && _seriesTC != null && _chartModel != null)
                {
                    _seriesTF1.Points.Clear();
                    _seriesTF2.Points.Clear();
                    _seriesTS.Points.Clear();
                    _seriesTC.Points.Clear();
                    _dataPointCount = 0;

                    // 重置X轴范围
                    var xAxis = _chartModel.Axes[0] as LinearAxis;
                    if (xAxis != null)
                    {
                        xAxis.Minimum = 0;
                        xAxis.Maximum = CHART_TIME_WINDOW_SECONDS;
                    }

                    _chartModel.InvalidatePlot(true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置图表失败: {ex.Message}");
            }
        }

        #endregion

        #region 系统消息和实时数据功能

        /*
         * 功能: 初始化系统消息和实时数据表格
         */
        private void InitializeMessageTables()
        {
            try
            {
                // 配置系统消息DataGridView
                dgvSystemMessage.AutoGenerateColumns = false;
                dgvSystemMessage.AllowUserToAddRows = false;
                dgvSystemMessage.AllowUserToDeleteRows = false;
                dgvSystemMessage.ReadOnly = true;
                dgvSystemMessage.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvSystemMessage.RowHeadersVisible = false;
                dgvSystemMessage.BackgroundColor = Color.White;
                dgvSystemMessage.BorderStyle = BorderStyle.None;

                // 添加列：时间
                var colTime = new DataGridViewTextBoxColumn
                {
                    Name = "Time",
                    HeaderText = "时间",
                    DataPropertyName = "Time",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F)
                    }
                };
                dgvSystemMessage.Columns.Add(colTime);

                // 添加列：消息内容
                var colContent = new DataGridViewTextBoxColumn
                {
                    Name = "Content",
                    HeaderText = "消息内容",
                    DataPropertyName = "Content",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleLeft,
                        Font = new Font("Arial", 9F)
                    }
                };
                dgvSystemMessage.Columns.Add(colContent);

                // 配置实时数据DataGridView
                dgvRealTimeData.AutoGenerateColumns = false;
                dgvRealTimeData.AllowUserToAddRows = false;
                dgvRealTimeData.AllowUserToDeleteRows = false;
                dgvRealTimeData.ReadOnly = true;
                dgvRealTimeData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvRealTimeData.RowHeadersVisible = false;
                dgvRealTimeData.BackgroundColor = Color.White;
                dgvRealTimeData.BorderStyle = BorderStyle.None;

                // 添加列：计时(s)
                var colTimer = new DataGridViewTextBoxColumn
                {
                    Name = "Timer",
                    HeaderText = "计时(s)",
                    DataPropertyName = "Timer",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTimer);

                // 添加列：炉内温度1(℃)
                var colTemp1 = new DataGridViewTextBoxColumn
                {
                    Name = "Temp1",
                    HeaderText = "炉内温度1(℃)",
                    DataPropertyName = "Temp1",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTemp1);

                // 添加列：炉内温度2(℃)
                var colTemp2 = new DataGridViewTextBoxColumn
                {
                    Name = "Temp2",
                    HeaderText = "炉内温度2(℃)",
                    DataPropertyName = "Temp2",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTemp2);

                // 添加列：表面温度(℃)
                var colTempSurface = new DataGridViewTextBoxColumn
                {
                    Name = "TempSurface",
                    HeaderText = "表面温度(℃)",
                    DataPropertyName = "TempSurface",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTempSurface);

                // 添加列：中心温度(℃)
                var colTempCenter = new DataGridViewTextBoxColumn
                {
                    Name = "TempCenter",
                    HeaderText = "中心温度(℃)",
                    DataPropertyName = "TempCenter",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTempCenter);

                // 添加列：温度漂移(℃)
                var colTempDrift = new DataGridViewTextBoxColumn
                {
                    Name = "TempDrift",
                    HeaderText = $"温度漂移({TestMaster.TemperatureDriftUnitText})",
                    DataPropertyName = "TempDrift",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9F),
                        Format = "F1"
                    }
                };
                dgvRealTimeData.Columns.Add(colTempDrift);
            }
            catch (Exception ex)
            {                MessageBox.Show($"初始化数据表格失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 添加一条系统消息（标准WinForms插入模式）
         */
        private void AppendSystemMessage(string message)
        {
            try
            {
                // 在表格顶部插入新行（index: 0）
                dgvSystemMessage.Rows.Insert(0, DateTime.Now.ToString("HH:mm:ss"), message);

                // 滚动到最新消息（顶部）
                if (dgvSystemMessage.Rows.Count > 0)
                {
                    dgvSystemMessage.FirstDisplayedScrollingRowIndex = 0;
                }

                UpdateMessageTableMeta();
                UpdateRecentStatusEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加系统消息失败: {ex.Message}");
            }
        }

        /*
         * 功能: 添加一条实时数据（标准WinForms数据滚动模式）
         */
        private void AppendRealTimeData(SensorDataEventArgs data)
        {
            try
            {
                // 在表格顶部插入新行（index: 0）
                dgvRealTimeData.Rows.Insert(0, 
                    data.Timer, 
                    data.Temp1, 
                    data.Temp2, 
                    data.TempSurface, 
                    data.TempCenter, 
                    data.TempDrift);

                // 如果超过60秒，移除末尾行（保持最近60秒数据）
                if (data.Timer > 60 && dgvRealTimeData.Rows.Count > MAX_REALTIME_DATA_ROWS)
                {
                    dgvRealTimeData.Rows.RemoveAt(dgvRealTimeData.Rows.Count - 1);
                }

                // 滚动到最新数据（顶部）
                if (dgvRealTimeData.Rows.Count > 0)
                {
                    dgvRealTimeData.FirstDisplayedScrollingRowIndex = 0;
                }

                UpdateMessageTableMeta();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加实时数据失败: {ex.Message}");
            }
        }

        /*
         * 功能: 切换到系统消息显示
         */
        private void btnSystemMessage_Click(object sender, EventArgs e)
        {
            _showSystemMessages = true;
            ApplyMessageToggleButtonState();
            dgvSystemMessage.Visible = true;
            dgvRealTimeData.Visible = false;
            UpdateMessageTableMeta();
        }

        /*
         * 功能: 切换到实时数据显示
         */
        private void btnRealTimeData_Click(object sender, EventArgs e)
        {
            _showSystemMessages = false;
            ApplyMessageToggleButtonState();
            dgvRealTimeData.Visible = true;
            dgvSystemMessage.Visible = false;
            UpdateMessageTableMeta();
        }

        private void UpdateMessageTableMeta()
        {
            if (_lblMessageRowCount != null)
            {
                var rows = _showSystemMessages ? dgvSystemMessage.Rows.Count : dgvRealTimeData.Rows.Count;
                _lblMessageRowCount.Text = $"最近 {rows} 条";
            }

            if (_lblMessageRefresh != null)
            {
                _lblMessageRefresh.Text = _showSystemMessages ? "系统消息" : "刷新 0.8s";
            }
        }

        private void UpdateRecentStatusEvents()
        {
            if (_recentStatusTimeLabels == null || _recentStatusMessageLabels == null)
            {
                return;
            }

            if (dgvSystemMessage == null)
            {
                return;
            }

            for (int i = 0; i < _recentStatusTimeLabels.Length; i++)
            {
                var hasRow = i < dgvSystemMessage.Rows.Count;
                var time = hasRow ? dgvSystemMessage.Rows[i].Cells["Time"].Value?.ToString() : "--:--:--";
                var message = hasRow ? dgvSystemMessage.Rows[i].Cells["Content"].Value?.ToString() : "暂无状态";

                if (_recentStatusTimeLabels[i] != null)
                {
                    _recentStatusTimeLabels[i]!.Text = time ?? "--:--:--";
                }

                if (_recentStatusMessageLabels[i] != null)
                {
                    _recentStatusMessageLabels[i]!.Text = message ?? "暂无状态";
                }
            }
        }

        /*
         * 功能: 清空系统消息和实时数据表格（新建试验时调用）
         */
        private void ClearMessageTables()
        {
            try
            {
                dgvSystemMessage.Rows.Clear();
                dgvRealTimeData.Rows.Clear();
                UpdateMessageTableMeta();
                UpdateRecentStatusEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清空数据表格失败: {ex.Message}");
            }
        }

        #endregion

        #region 系统校验功能

        // 系统校验相关字段
        private Dictionary<string, double> _surfaceTempData = new Dictionary<string, double>();
        private Dictionary<int, double> _centerTempData = new Dictionary<int, double>();
        private PlotView? _centerChartView;
        private PlotModel? _centerChartModel;
        private LineSeries? _centerTempSeries;
        private Label? _centerCalibrationTempLabel;

        /*
         * 功能: 初始化系统校验视图（采用三列布局，模仿原Web项目）
         */
        private void InitializeCalibrationView()
        {
            try
            {
                panelCalibration.Controls.Clear();
                panelCalibration.BackColor = UiTheme.AppBackground;
                panelCalibration.Padding = new Padding(0);

                _calibrationView = new CalibrationView
                {
                    Dock = DockStyle.Fill
                };
                _calibrationView.HistoryRequested += (_, _) => ShowCalibrationHistoryDialog();
                _calibrationView.SystemMessageGenerated += AppendSystemMessage;

                panelCalibration.Controls.Add(_calibrationView);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "初始化系统校验视图失败");
                MessageBox.Show($"初始化系统校验视图失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 创建炉壁温度校验面板（三列布局）
         */
        private Panel CreateSurfaceCalibrationPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15),
                Margin = new Padding(5)
            };

            // 创建三列布局
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽：20% - 40% - 40%
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 左侧区域：控制区
            var leftPanel = CreateSurfaceLeftPanel();
            tlp.Controls.Add(leftPanel, 0, 0);

            // 中间区域：温度网格表格
            var middlePanel = CreateSurfaceMiddlePanel();
            tlp.Controls.Add(middlePanel, 1, 0);

            // 右侧区域：计算结果
            var rightPanel = CreateSurfaceRightPanel();
            tlp.Controls.Add(rightPanel, 2, 0);

            panel.Controls.Add(tlp);
            return panel;
        }

        /*
         * 功能: 创建炉壁温度校验左侧控制面板
         */
        private Panel CreateSurfaceLeftPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            int yPos = 10;

            // 校温热电偶温度显示
            var lblCaliTempLabel = new Label
            {
                Text = "校温热电偶温度(℃)",
                Location = new Point(10, yPos),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(65, 105, 225),
                ForeColor = Color.White,
                Font = new Font("Arial", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblCaliTempLabel);

            dataCaliTemp = new Label
            {
                Text = "0.0",
                Location = new Point(10, yPos + 35),
                Size = new Size(160, 40),
                BackColor = Color.Black,
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(dataCaliTemp);

            yPos += 90;

            // 炉壁点位选择
            var lblPosition = new Label
            {
                Text = "炉壁点位:",
                Location = new Point(10, yPos),
                Size = new Size(80, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            panel.Controls.Add(lblPosition);

            cmbSurfacePosition = new ComboBox
            {
                Location = new Point(10, yPos + 30),
                Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            InitializeSurfacePositionComboBox();
            panel.Controls.Add(cmbSurfacePosition);

            yPos += 70;

            // 计算按钮
            btnCalculate = new Button
            {
                Text = "计算",
                Location = new Point(10, yPos),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(100, 150, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCalculate.FlatAppearance.BorderSize = 0;
            btnCalculate.Click += btnCalculate_Click;
            panel.Controls.Add(btnCalculate);

            yPos += 45;

            // 记录按钮
            btnRecordSurface = new Button
            {
                Text = "记录",
                Location = new Point(10, yPos),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(150, 100, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRecordSurface.FlatAppearance.BorderSize = 0;
            btnRecordSurface.Click += btnRecordSurface_Click;
            panel.Controls.Add(btnRecordSurface);

            return panel;
        }

        /*
         * 功能: 创建炉壁温度校验中间面板（4x4网格表格）
         */
        private Panel CreateSurfaceMiddlePanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // 创建4x4网格表格
            var tlpGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.Gray
            };

            // 设置列宽和行高
            for (int i = 0; i < 4; i++)
            {
                tlpGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                tlpGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            // 第一行：表头
            AddGridHeader(tlpGrid, 0, 0, "点位 / 温度(℃)");
            AddGridHeader(tlpGrid, 1, 0, "A 平面(30mm)");
            AddGridHeader(tlpGrid, 2, 0, "B 平面(0mm)");
            AddGridHeader(tlpGrid, 3, 0, "C 平面(-30mm)");

            // 第二行：第一点(0°)
            AddGridHeader(tlpGrid, 0, 1, "第一点(0°)");
            _lblTempA1 = AddGridCell(tlpGrid, 1, 1, "0.0");
            _lblTempB1 = AddGridCell(tlpGrid, 2, 1, "0.0");
            _lblTempC1 = AddGridCell(tlpGrid, 3, 1, "0.0");

            // 第三行：第二点(120°)
            AddGridHeader(tlpGrid, 0, 2, "第二点(120°)");
            _lblTempA2 = AddGridCell(tlpGrid, 1, 2, "0.0");
            _lblTempB2 = AddGridCell(tlpGrid, 2, 2, "0.0");
            _lblTempC2 = AddGridCell(tlpGrid, 3, 2, "0.0");

            // 第四行：第三点(240°)
            AddGridHeader(tlpGrid, 0, 3, "第三点(240°)");
            _lblTempA3 = AddGridCell(tlpGrid, 1, 3, "0.0");
            _lblTempB3 = AddGridCell(tlpGrid, 2, 3, "0.0");
            _lblTempC3 = AddGridCell(tlpGrid, 3, 3, "0.0");

            panel.Controls.Add(tlpGrid);
            return panel;
        }

        /*
         * 功能: 添加网格表头单元格
         */
        private void AddGridHeader(TableLayoutPanel tlp, int col, int row, string text)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(66, 96, 231),
                ForeColor = Color.White,
                Font = new Font("Arial", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            tlp.Controls.Add(label, col, row);
        }

        /*
         * 功能: 添加网格数据单元格
         */
        private Label AddGridCell(TableLayoutPanel tlp, int col, int row, string text)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            tlp.Controls.Add(label, col, row);
            return label;
        }

        /*
         * 功能: 创建炉壁温度校验右侧面板（计算结果）
         */
        private Panel CreateSurfaceRightPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // 创建结果显示表格（4列布局：标签-值-标签-值）
            var tlpResults = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 8,
                AutoSize = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽
            tlpResults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlpResults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpResults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlpResults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            // 设置行高
            for (int i = 0; i < 8; i++)
            {
                tlpResults.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            }

            int row = 0;

            // 第一行：T.avg（跨两列）
            AddResultLabel(tlpResults, 0, row, "T.avg:");
            _txtTAvg = AddResultTextBox(tlpResults, 1, row);
            tlpResults.SetColumnSpan(_txtTAvg, 3);
            row++;

            // 第二行：T.avg.axis1 和 T.avg.levela
            AddResultLabel(tlpResults, 0, row, "T.avg.axis1:");
            _txtTAvgAxis1 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.avg.levela:");
            _txtTAvgLevela = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第三行：T.avg.axis2 和 T.avg.levelb
            AddResultLabel(tlpResults, 0, row, "T.avg.axis2:");
            _txtTAvgAxis2 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.avg.levelb:");
            _txtTAvgLevelb = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第四行：T.avg.axis3 和 T.avg.levelc
            AddResultLabel(tlpResults, 0, row, "T.avg.axis3:");
            _txtTAvgAxis3 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.avg.levelc:");
            _txtTAvgLevelc = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第五行：T.dev.axis1 和 T.dev.levela
            AddResultLabel(tlpResults, 0, row, "T.dev.axis1:");
            _txtTDevAxis1 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.dev.levela:");
            _txtTDevLevela = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第六行：T.dev.axis2 和 T.dev.levelb
            AddResultLabel(tlpResults, 0, row, "T.dev.axis2:");
            _txtTDevAxis2 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.dev.levelb:");
            _txtTDevLevelb = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第七行：T.dev.axis3 和 T.dev.levelc
            AddResultLabel(tlpResults, 0, row, "T.dev.axis3:");
            _txtTDevAxis3 = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.dev.levelc:");
            _txtTDevLevelc = AddResultTextBox(tlpResults, 3, row);
            row++;

            // 第八行：T.avg.dev.axis 和 T.avg.dev.level
            AddResultLabel(tlpResults, 0, row, "T.avg.dev.axis:");
            _txtTAvgDevAxis = AddResultTextBox(tlpResults, 1, row);
            AddResultLabel(tlpResults, 2, row, "T.avg.dev.level:");
            _txtTAvgDevLevel = AddResultTextBox(tlpResults, 3, row);

            panel.Controls.Add(tlpResults);
            return panel;
        }

        /*
         * 功能: 添加结果标签
         */
        private void AddResultLabel(TableLayoutPanel tlp, int col, int row, string text)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Margin = new Padding(2)
            };
            tlp.Controls.Add(label, col, row);
        }

        /*
         * 功能: 添加结果文本框
         */
        private TextBox AddResultTextBox(TableLayoutPanel tlp, int col, int row)
        {
            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
                Font = new Font("Arial", 9F),
                BackColor = Color.White,
                Margin = new Padding(2)
            };
            tlp.Controls.Add(textBox, col, row);
            return textBox;
        }

        /*
         * 功能: 创建中心轴温度校验面板（三列布局）
         */
        private Panel CreateCenterCalibrationPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15),
                Margin = new Padding(5)
            };

            // 创建三列布局
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽：20% - 50% - 30%
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 左侧区域：控制区
            var leftPanel = CreateCenterLeftPanel();
            tlp.Controls.Add(leftPanel, 0, 0);

            // 中间区域：温度分布图表
            var middlePanel = CreateCenterMiddlePanel();
            tlp.Controls.Add(middlePanel, 1, 0);

            // 右侧区域：15个点位温度值
            var rightPanel = CreateCenterRightPanel();
            tlp.Controls.Add(rightPanel, 2, 0);

            panel.Controls.Add(tlp);
            return panel;
        }

        /*
         * 功能: 创建中心轴温度校验左侧控制面板
         */
        private Panel CreateCenterLeftPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            int yPos = 10;

            // 校温热电偶温度显示（复用）
            var lblCaliTempLabel2 = new Label
            {
                Text = "校温热电偶温度(℃)",
                Location = new Point(10, yPos),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(65, 105, 225),
                ForeColor = Color.White,
                Font = new Font("Arial", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblCaliTempLabel2);

            _centerCalibrationTempLabel = new Label
            {
                Text = "0.0",
                Location = new Point(10, yPos + 35),
                Size = new Size(160, 40),
                BackColor = Color.Black,
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(_centerCalibrationTempLabel);

            yPos += 90;

            // 中心点位选择
            var lblPosition = new Label
            {
                Text = "中心点位:",
                Location = new Point(10, yPos),
                Size = new Size(80, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            panel.Controls.Add(lblPosition);

            cmbCenterPosition = new ComboBox
            {
                Location = new Point(10, yPos + 30),
                Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            InitializeCenterPositionComboBox();
            panel.Controls.Add(cmbCenterPosition);

            yPos += 70;

            // 重置按钮
            btnResetCenter = new Button
            {
                Text = "重置",
                Location = new Point(10, yPos),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetCenter.FlatAppearance.BorderSize = 0;
            btnResetCenter.Click += btnResetCenter_Click;
            panel.Controls.Add(btnResetCenter);

            yPos += 45;

            // 记录按钮
            btnRecordCenter = new Button
            {
                Text = "记录",
                Location = new Point(10, yPos),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(150, 100, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRecordCenter.FlatAppearance.BorderSize = 0;
            btnRecordCenter.Click += btnRecordCenter_Click;
            panel.Controls.Add(btnRecordCenter);

            return panel;
        }

        /*
         * 功能: 创建中心轴温度校验中间面板（图表）
         */
        private Panel CreateCenterMiddlePanel()
        {
            panelCenterChart = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 初始化中心轴温度图表
            InitializeCenterChart();

            return panelCenterChart;
        }

        /*
         * 功能: 创建中心轴温度校验右侧面板（15个点位温度值）
         */
        private Panel CreateCenterRightPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // 创建温度值显示表格（2列布局：标签-值）
            var tlpTemps = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 15,
                AutoSize = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽
            tlpTemps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tlpTemps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            // 设置行高
            for (int i = 0; i < 15; i++)
            {
                tlpTemps.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            }

            // 添加15个点位（从145mm到5mm）
            int row = 0;
            for (int pos = 145; pos >= 5; pos -= 10)
            {
                AddResultLabel(tlpTemps, 0, row, $"{pos}(mm):");
                var txtTemp = AddResultTextBox(tlpTemps, 1, row);
                _centerTempTextBoxes[pos] = txtTemp;
                row++;
            }

            panel.Controls.Add(tlpTemps);
            return panel;
        }



        /*
         * 功能: 初始化炉壁点位选择器
         */
        private void InitializeSurfacePositionComboBox()
        {
            cmbSurfacePosition.Items.Clear();
            
            // A平面(+30mm)
            cmbSurfacePosition.Items.Add("A-1 (0°)");
            cmbSurfacePosition.Items.Add("A-2 (120°)");
            cmbSurfacePosition.Items.Add("A-3 (240°)");
            
            // B平面(0mm)
            cmbSurfacePosition.Items.Add("B-1 (0°)");
            cmbSurfacePosition.Items.Add("B-2 (120°)");
            cmbSurfacePosition.Items.Add("B-3 (240°)");
            
            // C平面(-30mm)
            cmbSurfacePosition.Items.Add("C-1 (0°)");
            cmbSurfacePosition.Items.Add("C-2 (120°)");
            cmbSurfacePosition.Items.Add("C-3 (240°)");
            
            // 默认选中第一项
            if (cmbSurfacePosition.Items.Count > 0)
            {
                cmbSurfacePosition.SelectedIndex = 0;
            }
        }

        /*
         * 功能: 初始化中心点位选择器
         */
        private void InitializeCenterPositionComboBox()
        {
            cmbCenterPosition.Items.Clear();
            
            // 添加中心轴点位（从145mm到5mm，间隔10mm）
            for (int i = 145; i >= 5; i -= 10)
            {
                cmbCenterPosition.Items.Add($"{i}(mm)");
            }
            
            // 默认选中第一项
            if (cmbCenterPosition.Items.Count > 0)
            {
                cmbCenterPosition.SelectedIndex = 0;
            }
        }

        /*
         * 功能: 初始化炉壁温度数据表格
         */
        private void InitializeSurfaceTempDataGrid()
        {
            dgvSurfaceTemp.AutoGenerateColumns = false;
            dgvSurfaceTemp.AllowUserToAddRows = false;
            dgvSurfaceTemp.AllowUserToDeleteRows = false;
            dgvSurfaceTemp.ReadOnly = true;
            dgvSurfaceTemp.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSurfaceTemp.RowHeadersVisible = false;
            dgvSurfaceTemp.BackgroundColor = Color.White;
            dgvSurfaceTemp.BorderStyle = BorderStyle.FixedSingle;
            dgvSurfaceTemp.ColumnHeadersHeight = 35;
            dgvSurfaceTemp.RowTemplate.Height = 40;

            // 清空现有列
            dgvSurfaceTemp.Columns.Clear();

            // 添加列：点位/温度
            var colPosition = new DataGridViewTextBoxColumn
            {
                Name = "Position",
                HeaderText = "点位/温度(℃)",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(65, 105, 225),
                    ForeColor = Color.White
                }
            };
            dgvSurfaceTemp.Columns.Add(colPosition);

            // 添加列：A平面(30mm)
            var colLevelA = new DataGridViewTextBoxColumn
            {
                Name = "LevelA",
                HeaderText = "A平面(30mm)",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 12F, FontStyle.Bold),
                    BackColor = Color.Black,
                    ForeColor = Color.FromArgb(255, 255, 0)
                }
            };
            dgvSurfaceTemp.Columns.Add(colLevelA);

            // 添加列：B平面(0mm)
            var colLevelB = new DataGridViewTextBoxColumn
            {
                Name = "LevelB",
                HeaderText = "B平面(0mm)",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 12F, FontStyle.Bold),
                    BackColor = Color.Black,
                    ForeColor = Color.FromArgb(255, 255, 0)
                }
            };
            dgvSurfaceTemp.Columns.Add(colLevelB);

            // 添加列：C平面(-30mm)
            var colLevelC = new DataGridViewTextBoxColumn
            {
                Name = "LevelC",
                HeaderText = "C平面(-30mm)",
                Width = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 12F, FontStyle.Bold),
                    BackColor = Color.Black,
                    ForeColor = Color.FromArgb(255, 255, 0)
                }
            };
            dgvSurfaceTemp.Columns.Add(colLevelC);

            // 设置列标题样式
            dgvSurfaceTemp.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSurfaceTemp.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(65, 105, 225);
            dgvSurfaceTemp.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSurfaceTemp.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9F, FontStyle.Bold);

            // 添加三行数据（第一点、第二点、第三点）
            dgvSurfaceTemp.Rows.Add("第一点(0°)", "100", "100", "100");
            dgvSurfaceTemp.Rows.Add("第二点(120°)", "100", "100", "100");
            dgvSurfaceTemp.Rows.Add("第三点(240°)", "100", "100", "100");
        }

        /*
         * 功能: 初始化中心轴温度图表
         */
        private void InitializeCenterChart()
        {
            try
            {
                // 创建PlotModel
                _centerChartModel = new PlotModel
                {
                    Title = "中心轴温度分布",
                    Background = OxyColor.FromRgb(UiTheme.SurfaceRaised.R, UiTheme.SurfaceRaised.G, UiTheme.SurfaceRaised.B),
                    TitleFontSize = 14
                };

                // 配置X轴（位置轴）
                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "位置(mm)",
                    Minimum = 0,
                    Maximum = 150,
                    MajorStep = 10,
                    MinorStep = 5,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineColor = OxyColor.FromRgb(224, 218, 209)
                };
                _centerChartModel.Axes.Add(xAxis);

                // 配置Y轴（温度轴）
                var yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "温度(℃)",
                    Minimum = 0,
                    Maximum = 800,
                    MajorStep = 100,
                    MinorStep = 20,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineColor = OxyColor.FromRgb(224, 218, 209)
                };
                _centerChartModel.Axes.Add(yAxis);

                // 创建数据线系列
                _centerTempSeries = new LineSeries
                {
                    Title = "中心轴温度",
                    Color = OxyColor.FromRgb(83, 147, 245),
                    StrokeThickness = 2.6,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = OxyColor.FromRgb(83, 147, 245)
                };
                _centerChartModel.Series.Add(_centerTempSeries);

                // 创建PlotView控件
                _centerChartView = new PlotView
                {
                    Model = _centerChartModel,
                    Dock = DockStyle.Fill,
                    BackColor = UiTheme.SurfaceRaised
                };

                // 添加到panelCenterChart
                panelCenterChart.Controls.Clear();
                panelCenterChart.Controls.Add(_centerChartView);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "初始化中心轴温度图表失败");
                MessageBox.Show($"初始化中心轴温度图表失败: {ex.Message}", 
                    "错误", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 初始化炉壁温度数据字典
         */
        private void InitializeSurfaceTempData()
        {
            _surfaceTempData.Clear();
            _surfaceTempData["A1"] = 0.0;
            _surfaceTempData["A2"] = 0.0;
            _surfaceTempData["A3"] = 0.0;
            _surfaceTempData["B1"] = 0.0;
            _surfaceTempData["B2"] = 0.0;
            _surfaceTempData["B3"] = 0.0;
            _surfaceTempData["C1"] = 0.0;
            _surfaceTempData["C2"] = 0.0;
            _surfaceTempData["C3"] = 0.0;
        }

        /*
         * 功能: 初始化中心轴温度数据字典
         */
        private void InitializeCenterTempData()
        {
            _centerTempData.Clear();
            for (int i = 145; i >= 5; i -= 10)
            {
                _centerTempData[i] = 0.0;
            }
        }

        // 校准温度稳定状态常量
        private const double CALIBRATION_TARGET_TEMP = 750.0;  // 目标温度 750°C
        private const double CALIBRATION_TOLERANCE = 5.0;      // 允许偏差 ±5°C

        /*
         * 功能: 更新校准温度稳定状态视觉指示
         * 说明: 根据 Requirements 4.3，当温度在 750±5°C (745-755°C) 范围内时显示稳定状态
         */
        private void UpdateCalibrationStabilityIndicator(double temperature)
        {
            // 判断温度是否在目标范围内 (745°C - 755°C)
            bool isStable = IsTemperatureInCalibrationRange(temperature);

            // 更新校准温度显示的视觉指示
            if (dataCaliTemp != null)
            {
                if (isStable)
                {
                    // 稳定状态：绿色背景，表示温度在目标范围内
                    dataCaliTemp.BackColor = Color.FromArgb(40, 167, 69);  // 绿色
                    dataCaliTemp.ForeColor = Color.White;
                }
                else
                {
                    // 非稳定状态：黑色背景，黄色文字（默认样式）
                    dataCaliTemp.BackColor = Color.Black;
                    dataCaliTemp.ForeColor = Color.Yellow;
                }
            }
        }

        /*
         * 功能: 判断温度是否在校准目标范围内
         * 说明: 根据 Property 3，温度范围判断结果应等于 (745 ≤ T ≤ 755)
         */
        public static bool IsTemperatureInCalibrationRange(double temperature)
        {
            double lowerBound = CALIBRATION_TARGET_TEMP - CALIBRATION_TOLERANCE;  // 745°C
            double upperBound = CALIBRATION_TARGET_TEMP + CALIBRATION_TOLERANCE;  // 755°C
            return temperature >= lowerBound && temperature <= upperBound;
        }

        /*
         * 功能: 初始化结果显示区域
         */
        private void InitializeResultsDisplay()
        {
            // 初始化炉壁温度均匀性结果显示
            InitializeSurfaceResultsPanel();

            // 初始化中心轴温度记录显示
            InitializeCenterResultsPanel();
        }

        /*
         * 功能: 初始化炉壁温度均匀性结果显示面板
         */
        private void InitializeSurfaceResultsPanel()
        {
            grpSurfaceResults.Controls.Clear();

            // 创建一个TableLayoutPanel来组织结果显示
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(8, 5, 8, 5),
                AutoScroll = false,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // 设置行高
            for (int i = 0; i < 8; i++)
            {
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }

            // 添加结果标签和文本框
            string[] labels = new string[]
            {
                "T.avg:", "T.avg.axis1:", "T.avg.levela:",
                "T.avg.axis2:", "T.avg.levelb:", "T.avg.axis3:",
                "T.avg.levelc:", "T.dev.axis1:", "T.dev.levela:",
                "T.dev.axis2:", "T.dev.levelb:", "T.dev.axis3:",
                "T.dev.levelc:", "T.avg.dev.axis:", "T.avg.dev.level:"
            };

            int row = 0;
            int col = 0;
            foreach (var labelText in labels)
            {
                var label = new Label
                {
                    Text = labelText,
                    TextAlign = ContentAlignment.MiddleRight,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 7.5F, FontStyle.Regular, GraphicsUnit.Point),
                    Margin = new Padding(2)
                };
                tableLayout.Controls.Add(label, col, row);
                col++;

                var textBox = new TextBox
                {
                    Name = "txt" + labelText.Replace(".", "").Replace(":", ""),
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 8F, FontStyle.Regular, GraphicsUnit.Point),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
                    Margin = new Padding(2)
                };
                tableLayout.Controls.Add(textBox, col, row);
                col++;

                if (col >= 4)
                {
                    col = 0;
                    row++;
                }
            }

            grpSurfaceResults.Controls.Add(tableLayout);
        }

        /*
         * 功能: 初始化中心轴温度记录显示面板
         */
        private void InitializeCenterResultsPanel()
        {
            grpCenterResults.Controls.Clear();

            // 创建一个TableLayoutPanel来组织温度记录显示
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(8, 5, 8, 5),
                AutoScroll = false,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 设置列宽
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // 设置行高
            for (int i = 0; i < 8; i++)
            {
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }

            // 添加温度记录标签和文本框（从145mm到5mm）
            int row = 0;
            int col = 0;
            for (int i = 145; i >= 5; i -= 10)
            {
                var label = new Label
                {
                    Text = $"{i}(mm):",
                    TextAlign = ContentAlignment.MiddleRight,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 7.5F, FontStyle.Regular, GraphicsUnit.Point),
                    Margin = new Padding(2)
                };
                tableLayout.Controls.Add(label, col, row);
                col++;

                var textBox = new TextBox
                {
                    Name = $"txtCenter{i}",
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 8F, FontStyle.Regular, GraphicsUnit.Point),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
                    Text = "0.0",
                    Margin = new Padding(2)
                };
                tableLayout.Controls.Add(textBox, col, row);
                col++;

                if (col >= 4)
                {
                    col = 0;
                    row++;
                }
            }

            grpCenterResults.Controls.Add(tableLayout);
        }

        /*
         * 功能: 记录炉壁温度按钮点击事件
         */
        private void btnRecordSurface_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前选中的点位
                if (cmbSurfacePosition.SelectedIndex < 0)
                {
                    MessageBox.Show("请先选择炉壁点位。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string selectedPosition = cmbSurfacePosition.SelectedItem.ToString() ?? "";
                
                // 解析点位（例如："A-1 (0°)" -> "A1"）
                string positionKey = selectedPosition.Substring(0, 3).Replace("-", "");

                // 获取当前校温热电偶温度（从dataCaliTemp标签）
                if (double.TryParse(dataCaliTemp.Text, out double temperature))
                {
                    // 保存到数据字典
                    _surfaceTempData[positionKey] = temperature;

                    // 更新表格显示
                    UpdateSurfaceTempDataGrid();

                    Log.Information("记录炉壁温度：点位={Position}, 温度={Temperature}℃", positionKey, temperature);
                    AppendSystemMessage($"已记录炉壁温度：{selectedPosition} = {temperature:F1}℃");
                }
                else
                {
                    MessageBox.Show("无法读取校温热电偶温度。",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "记录炉壁温度失败");
                MessageBox.Show($"记录炉壁温度失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 更新炉壁温度数据表格显示（更新网格Label）
         */
        private void UpdateSurfaceTempDataGrid()
        {
            try
            {
                // 更新网格Label控件
                if (_lblTempA1 != null) _lblTempA1.Text = _surfaceTempData["A1"].ToString("F1");
                if (_lblTempA2 != null) _lblTempA2.Text = _surfaceTempData["A2"].ToString("F1");
                if (_lblTempA3 != null) _lblTempA3.Text = _surfaceTempData["A3"].ToString("F1");

                if (_lblTempB1 != null) _lblTempB1.Text = _surfaceTempData["B1"].ToString("F1");
                if (_lblTempB2 != null) _lblTempB2.Text = _surfaceTempData["B2"].ToString("F1");
                if (_lblTempB3 != null) _lblTempB3.Text = _surfaceTempData["B3"].ToString("F1");

                if (_lblTempC1 != null) _lblTempC1.Text = _surfaceTempData["C1"].ToString("F1");
                if (_lblTempC2 != null) _lblTempC2.Text = _surfaceTempData["C2"].ToString("F1");
                if (_lblTempC3 != null) _lblTempC3.Text = _surfaceTempData["C3"].ToString("F1");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新炉壁温度数据表格失败");
            }
        }

        /*
         * 功能: 计算按钮点击事件
         */
        private async void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击计算按钮，开始计算温度均匀性");

                // 检查是否所有点位都已记录温度
                bool allRecorded = true;
                foreach (var key in _surfaceTempData.Keys)
                {
                    if (_surfaceTempData[key] == 0.0)
                    {
                        allRecorded = false;
                        break;
                    }
                }

                if (!allRecorded)
                {
                    MessageBox.Show("请先记录所有炉壁点位的温度。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 计算温度均匀性
                var results = CalculateTemperatureUniformity();

                // 显示计算结果
                DisplayUniformityResults(results);

                Log.Information("温度均匀性计算完成");
                AppendSystemMessage("温度均匀性计算完成。");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "计算温度均匀性失败");
                MessageBox.Show($"计算温度均匀性失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                // 恢复按钮状态
                btnCalculate.Enabled = true;
            }
        }

        /*
         * 功能: 计算温度均匀性
         * 返回: 包含所有计算结果的字典
         */
        private Dictionary<string, double> CalculateTemperatureUniformity()
        {
            var results = new Dictionary<string, double>();

            // 获取所有温度值
            double a1 = _surfaceTempData["A1"];
            double a2 = _surfaceTempData["A2"];
            double a3 = _surfaceTempData["A3"];
            double b1 = _surfaceTempData["B1"];
            double b2 = _surfaceTempData["B2"];
            double b3 = _surfaceTempData["B3"];
            double c1 = _surfaceTempData["C1"];
            double c2 = _surfaceTempData["C2"];
            double c3 = _surfaceTempData["C3"];

            // 计算总平均温度 T.avg
            double t_avg = (a1 + a2 + a3 + b1 + b2 + b3 + c1 + c2 + c3) / 9.0;
            results["T.avg"] = t_avg;

            // 计算各轴平均温度
            double t_avg_axis1 = (a1 + b1 + c1) / 3.0;  // 第一轴(0°)
            double t_avg_axis2 = (a2 + b2 + c2) / 3.0;  // 第二轴(120°)
            double t_avg_axis3 = (a3 + b3 + c3) / 3.0;  // 第三轴(240°)
            results["T.avg.axis1"] = t_avg_axis1;
            results["T.avg.axis2"] = t_avg_axis2;
            results["T.avg.axis3"] = t_avg_axis3;

            // 计算各平面平均温度
            double t_avg_levela = (a1 + a2 + a3) / 3.0;  // A平面(+30mm)
            double t_avg_levelb = (b1 + b2 + b3) / 3.0;  // B平面(0mm)
            double t_avg_levelc = (c1 + c2 + c3) / 3.0;  // C平面(-30mm)
            results["T.avg.levela"] = t_avg_levela;
            results["T.avg.levelb"] = t_avg_levelb;
            results["T.avg.levelc"] = t_avg_levelc;

            // 计算各轴温度偏差（相对偏差，单位：%）
            // 公式：t_dev = 100 * |t_avg_axis - t_avg| / t_avg
            double t_dev_axis1 = 100 * Math.Abs(t_avg_axis1 - t_avg) / t_avg;
            double t_dev_axis2 = 100 * Math.Abs(t_avg_axis2 - t_avg) / t_avg;
            double t_dev_axis3 = 100 * Math.Abs(t_avg_axis3 - t_avg) / t_avg;
            results["T.dev.axis1"] = t_dev_axis1;
            results["T.dev.axis2"] = t_dev_axis2;
            results["T.dev.axis3"] = t_dev_axis3;

            // 计算各平面温度偏差（相对偏差，单位：%）
            // 公式：t_dev = 100 * |t_avg_level - t_avg| / t_avg
            double t_dev_levela = 100 * Math.Abs(t_avg_levela - t_avg) / t_avg;
            double t_dev_levelb = 100 * Math.Abs(t_avg_levelb - t_avg) / t_avg;
            double t_dev_levelc = 100 * Math.Abs(t_avg_levelc - t_avg) / t_avg;
            results["T.dev.levela"] = t_dev_levela;
            results["T.dev.levelb"] = t_dev_levelb;
            results["T.dev.levelc"] = t_dev_levelc;

            // 计算轴向平均偏差
            double t_avg_dev_axis = (t_dev_axis1 + t_dev_axis2 + t_dev_axis3) / 3.0;
            results["T.avg.dev.axis"] = t_avg_dev_axis;

            // 计算平面平均偏差
            double t_avg_dev_level = (t_dev_levela + t_dev_levelb + t_dev_levelc) / 3.0;
            results["T.avg.dev.level"] = t_avg_dev_level;

            return results;
        }

        /*
         * 功能: 显示温度均匀性计算结果
         */
        private void DisplayUniformityResults(Dictionary<string, double> results)
        {
            try
            {
                // 直接更新TextBox控件
                if (_txtTAvg != null && results.ContainsKey("T.avg"))
                    _txtTAvg.Text = results["T.avg"].ToString("F2");

                if (_txtTAvgAxis1 != null && results.ContainsKey("T.avg.axis1"))
                    _txtTAvgAxis1.Text = results["T.avg.axis1"].ToString("F2");
                if (_txtTAvgAxis2 != null && results.ContainsKey("T.avg.axis2"))
                    _txtTAvgAxis2.Text = results["T.avg.axis2"].ToString("F2");
                if (_txtTAvgAxis3 != null && results.ContainsKey("T.avg.axis3"))
                    _txtTAvgAxis3.Text = results["T.avg.axis3"].ToString("F2");

                if (_txtTAvgLevela != null && results.ContainsKey("T.avg.levela"))
                    _txtTAvgLevela.Text = results["T.avg.levela"].ToString("F2");
                if (_txtTAvgLevelb != null && results.ContainsKey("T.avg.levelb"))
                    _txtTAvgLevelb.Text = results["T.avg.levelb"].ToString("F2");
                if (_txtTAvgLevelc != null && results.ContainsKey("T.avg.levelc"))
                    _txtTAvgLevelc.Text = results["T.avg.levelc"].ToString("F2");

                if (_txtTDevAxis1 != null && results.ContainsKey("T.dev.axis1"))
                    _txtTDevAxis1.Text = results["T.dev.axis1"].ToString("F2") + "%";
                if (_txtTDevAxis2 != null && results.ContainsKey("T.dev.axis2"))
                    _txtTDevAxis2.Text = results["T.dev.axis2"].ToString("F2") + "%";
                if (_txtTDevAxis3 != null && results.ContainsKey("T.dev.axis3"))
                    _txtTDevAxis3.Text = results["T.dev.axis3"].ToString("F2") + "%";

                if (_txtTDevLevela != null && results.ContainsKey("T.dev.levela"))
                    _txtTDevLevela.Text = results["T.dev.levela"].ToString("F2") + "%";
                if (_txtTDevLevelb != null && results.ContainsKey("T.dev.levelb"))
                    _txtTDevLevelb.Text = results["T.dev.levelb"].ToString("F2") + "%";
                if (_txtTDevLevelc != null && results.ContainsKey("T.dev.levelc"))
                    _txtTDevLevelc.Text = results["T.dev.levelc"].ToString("F2") + "%";

                if (_txtTAvgDevAxis != null && results.ContainsKey("T.avg.dev.axis"))
                    _txtTAvgDevAxis.Text = results["T.avg.dev.axis"].ToString("F2") + "%";
                if (_txtTAvgDevLevel != null && results.ContainsKey("T.avg.dev.level"))
                    _txtTAvgDevLevel.Text = results["T.avg.dev.level"].ToString("F2") + "%";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "显示温度均匀性计算结果失败");
            }
        }

        /*
         * 功能: 从TextBox名称获取结果键名
         */
        private string GetResultKeyFromTextBoxName(string textBoxName)
        {
            // 将TextBox名称转换为结果键名
            // 例如: "txtTavg" -> "T.avg"
            var mapping = new Dictionary<string, string>
            {
                { "txtTavg", "T.avg" },
                { "txtTavgaxis1", "T.avg.axis1" },
                { "txtTavglevela", "T.avg.levela" },
                { "txtTavgaxis2", "T.avg.axis2" },
                { "txtTavglevelb", "T.avg.levelb" },
                { "txtTavgaxis3", "T.avg.axis3" },
                { "txtTavglevelc", "T.avg.levelc" },
                { "txtTdevaxis1", "T.dev.axis1" },
                { "txtTdevlevela", "T.dev.levela" },
                { "txtTdevaxis2", "T.dev.axis2" },
                { "txtTdevlevelb", "T.dev.levelb" },
                { "txtTdevaxis3", "T.dev.axis3" },
                { "txtTdevlevelc", "T.dev.levelc" },
                { "txtTavgdevaxis", "T.avg.dev.axis" },
                { "txtTavgdevlevel", "T.avg.dev.level" }
            };

            return mapping.ContainsKey(textBoxName) ? mapping[textBoxName] : string.Empty;
        }

        /*
         * 功能: 记录中心轴温度按钮点击事件
         */
        private void btnRecordCenter_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前选中的点位
                if (cmbCenterPosition.SelectedIndex < 0)
                {
                    MessageBox.Show("请先选择中心点位。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string selectedPosition = cmbCenterPosition.SelectedItem.ToString() ?? "";
                
                // 解析点位（例如："145(mm)" -> 145）
                string positionStr = selectedPosition.Replace("(mm)", "").Trim();
                if (int.TryParse(positionStr, out int position))
                {
                    // 获取当前校温热电偶温度（从dataCaliTemp标签）
                    if (double.TryParse(dataCaliTemp.Text, out double temperature))
                    {
                        // 保存到数据字典
                        _centerTempData[position] = temperature;

                        // 更新中心轴温度记录显示
                        UpdateCenterTempDisplay();

                        // 更新中心轴温度图表
                        UpdateCenterChart();

                        Log.Information("记录中心轴温度：点位={Position}mm, 温度={Temperature}℃", position, temperature);
                        AppendSystemMessage($"已记录中心轴温度：{position}(mm) = {temperature:F1}℃");
                    }
                    else
                    {
                        MessageBox.Show("无法读取校温热电偶温度。",
                            "错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "记录中心轴温度失败");
                MessageBox.Show($"记录中心轴温度失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 重置中心轴温度按钮点击事件
         * 说明: 与原Web项目保持一致，只重置数据，不保存到数据库
         */
        private void btnResetCenter_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否有已记录的温度数据
                int recordedCount = _centerTempData.Count(kvp => kvp.Value > 0);
                
                if (recordedCount == 0)
                {
                    MessageBox.Show("没有需要重置的中心轴温度数据。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 询问是否重置
                DialogResult result = MessageBox.Show(
                    $"已记录 {recordedCount} 个点位的温度数据。\n\n是否重置中心轴温度数据？",
                    "重置确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 重置中心轴温度数据
                    InitializeCenterTempData();

                    // 更新显示
                    UpdateCenterTempDisplay();

                    // 清空图表
                    if (_centerTempSeries != null && _centerChartModel != null)
                    {
                        _centerTempSeries.Points.Clear();
                        _centerChartModel.InvalidatePlot(true);
                    }

                    Log.Information("已重置中心轴温度记录");
                    AppendSystemMessage("已重置中心轴温度记录。");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "重置中心轴温度失败");
                MessageBox.Show($"重置中心轴温度失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 更新中心轴温度记录显示
         */
        private void UpdateCenterTempDisplay()
        {
            try
            {
                // 在grpCenterResults中查找对应的TextBox并更新值
                foreach (Control control in grpCenterResults.Controls)
                {
                    if (control is TableLayoutPanel tableLayout)
                    {
                        foreach (Control cell in tableLayout.Controls)
                        {
                            if (cell is TextBox textBox && textBox.Name.StartsWith("txtCenter"))
                            {
                                // 从TextBox名称中提取位置（例如："txtCenter145" -> 145）
                                string positionStr = textBox.Name.Replace("txtCenter", "");
                                if (int.TryParse(positionStr, out int position))
                                {
                                    if (_centerTempData.ContainsKey(position))
                                    {
                                        textBox.Text = _centerTempData[position].ToString("F1");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新中心轴温度记录显示失败");
            }
        }

        /*
         * 功能: 更新中心轴温度图表
         */
        private void UpdateCenterChart()
        {
            try
            {
                if (_centerTempSeries != null && _centerChartModel != null)
                {
                    // 清空现有数据点
                    _centerTempSeries.Points.Clear();

                    // 添加已记录的温度数据点（按位置从小到大排序）
                    var sortedData = _centerTempData
                        .Where(kvp => kvp.Value > 0)  // 只显示已记录的数据
                        .OrderBy(kvp => kvp.Key)
                        .ToList();

                    foreach (var data in sortedData)
                    {
                        _centerTempSeries.Points.Add(new DataPoint(data.Key, data.Value));
                    }

                    // 刷新图表
                    _centerChartModel.InvalidatePlot(true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新中心轴温度图表失败");
            }
        }

        /*
         * 功能: 查询历史校验记录（异步方法）
         */
        private async Task<List<CalibrationRecord>> QueryCalibrationRecordsAsync(
            DateTime startDate,
            DateTime endDate,
            string calibrationType = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var dbContext = new ISO11820DbContext())
                    {
                        var query = dbContext.CalibrationRecords.AsQueryable();

                        // 按日期范围筛选
                        query = query.Where(c => c.CalibrationDate >= startDate && c.CalibrationDate <= endDate);

                        // 按校验类型筛选（如果提供）
                        if (!string.IsNullOrEmpty(calibrationType))
                        {
                            query = query.Where(c => c.CalibrationType == calibrationType);
                        }

                        // 按日期降序排序
                        query = query.OrderByDescending(c => c.CalibrationDate);

                        var records = query.ToList();
                        foreach (var record in records)
                        {
                            record.Memo = ReportPathMemoHelper.GetDisplayMemo(record.Memo);
                        }

                        return records;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "查询历史校验记录失败");
                    throw;
                }
            });
        }

        /*
         * 功能: 显示历史校验记录对话框
         */
        private async void ShowCalibrationHistoryDialog()
        {
            try
            {
                Log.Information("用户查看历史校验记录");

                // 创建一个简单的对话框显示历史记录
                Form historyForm = new Form
                {
                    Text = "历史校验记录",
                    Width = 1000,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MinimizeBox = false,
                    MaximizeBox = true
                };

                // 创建日期选择器
                var lblStartDate = new Label
                {
                    Text = "开始日期:",
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                historyForm.Controls.Add(lblStartDate);

                var dtpStartDate = new DateTimePicker
                {
                    Location = new Point(100, 17),
                    Width = 150,
                    Value = DateTime.Now.AddMonths(-3)  // 默认最近3个月
                };
                historyForm.Controls.Add(dtpStartDate);

                var lblEndDate = new Label
                {
                    Text = "结束日期:",
                    Location = new Point(270, 20),
                    AutoSize = true
                };
                historyForm.Controls.Add(lblEndDate);

                var dtpEndDate = new DateTimePicker
                {
                    Location = new Point(350, 17),
                    Width = 150,
                    Value = DateTime.Now
                };
                historyForm.Controls.Add(dtpEndDate);

                // 创建查询按钮
                var btnQuery = new Button
                {
                    Text = "查询",
                    Location = new Point(520, 15),
                    Width = 80,
                    Height = 30,
                    BackColor = Color.FromArgb(0, 123, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnQuery.FlatAppearance.BorderSize = 0;
                historyForm.Controls.Add(btnQuery);

                // 创建DataGridView显示记录
                var dgvHistory = new DataGridView
                {
                    Location = new Point(20, 60),
                    Width = 940,
                    Height = 450,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    RowHeadersVisible = false,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // 配置列
                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    HeaderText = "记录ID",
                    DataPropertyName = "Id",
                    Width = 80
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "CalibrationDate",
                    HeaderText = "校验日期",
                    DataPropertyName = "CalibrationDate",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" }
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Operator",
                    HeaderText = "操作员",
                    DataPropertyName = "Operator",
                    Width = 100
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "CalibrationType",
                    HeaderText = "校验类型",
                    DataPropertyName = "CalibrationType",
                    Width = 100
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TAvg",
                    HeaderText = "平均温度(℃)",
                    DataPropertyName = "TAvg",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TAvgDevAxis",
                    HeaderText = "轴向平均偏差(℃)",
                    DataPropertyName = "TAvgDevAxis",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TAvgDevLevel",
                    HeaderText = "平面平均偏差(℃)",
                    DataPropertyName = "TAvgDevLevel",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
                });

                dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Memo",
                    HeaderText = "备注",
                    DataPropertyName = "Memo",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                historyForm.Controls.Add(dgvHistory);

                // 创建关闭按钮
                var btnClose = new Button
                {
                    Text = "关闭",
                    Location = new Point(880, 520),
                    Width = 80,
                    Height = 30,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    BackColor = Color.FromArgb(108, 117, 125),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                btnClose.FlatAppearance.BorderSize = 0;
                historyForm.Controls.Add(btnClose);
                historyForm.CancelButton = btnClose;

                // 查询按钮点击事件
                btnQuery.Click += async (s, ev) =>
                {
                    try
                    {
                        btnQuery.Enabled = false;
                        historyForm.Cursor = Cursors.WaitCursor;

                        // 查询历史记录
                        var records = await QueryCalibrationRecordsAsync(
                            dtpStartDate.Value.Date,
                            dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1));

                        // 绑定数据
                        dgvHistory.DataSource = records;

                        Log.Information("查询到 {Count} 条历史校验记录", records.Count);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "查询历史校验记录失败");
                        MessageBox.Show($"查询历史校验记录失败: {ex.Message}",
                            "错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnQuery.Enabled = true;
                        historyForm.Cursor = Cursors.Default;
                    }
                };

                // 自动执行一次查询
                btnQuery.PerformClick();

                // 显示对话框
                historyForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "显示历史校验记录对话框失败");
                MessageBox.Show($"显示历史校验记录对话框失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 试验报告功能

        /*
         * 功能: 初始化试验报告视图
         */
        private void InitializeReportView()
        {
            try
            {
                // 初始化日期选择器（默认最近30天）
                dtpReportEndDate.Value = DateTime.Now;
                dtpReportStartDate.Value = DateTime.Now.AddDays(-30);

                // 初始化试验数据表格
                InitializeReportDataGrid();

                Log.Information("试验报告视图初始化完成");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "初始化试验报告视图失败");
                MessageBox.Show($"初始化试验报告视图失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 初始化试验数据表格
         */
        private void InitializeReportDataGrid()
        {
            dgvReportData.AutoGenerateColumns = false;
            dgvReportData.AllowUserToAddRows = false;
            dgvReportData.AllowUserToDeleteRows = false;
            dgvReportData.ReadOnly = true;
            dgvReportData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportData.RowHeadersVisible = false;
            dgvReportData.BackgroundColor = Color.White;
            dgvReportData.BorderStyle = BorderStyle.FixedSingle;
            dgvReportData.ColumnHeadersHeight = 35;
            dgvReportData.RowTemplate.Height = 30;

            // 清空现有列
            dgvReportData.Columns.Clear();

            // 添加列：试验日期
            var colTestDate = new DataGridViewTextBoxColumn
            {
                Name = "TestDate",
                HeaderText = "试验日期",
                DataPropertyName = "Testdate",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "yyyy-MM-dd"
                }
            };
            dgvReportData.Columns.Add(colTestDate);

            // 添加列：样品编号
            var colProductId = new DataGridViewTextBoxColumn
            {
                Name = "ProductId",
                HeaderText = "样品编号",
                DataPropertyName = "Productid",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvReportData.Columns.Add(colProductId);

            // 添加列：样品标识
            var colTestId = new DataGridViewTextBoxColumn
            {
                Name = "TestId",
                HeaderText = "样品标识",
                DataPropertyName = "Testid",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvReportData.Columns.Add(colTestId);

            // 添加列：样品名称
            var colProductName = new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                HeaderText = "样品名称",
                DataPropertyName = "Productname",
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvReportData.Columns.Add(colProductName);

            // 添加列：操作员
            var colOperator = new DataGridViewTextBoxColumn
            {
                Name = "Operator",
                HeaderText = "操作员",
                DataPropertyName = "Operatorid",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvReportData.Columns.Add(colOperator);

            // 添加列：试验结果
            var colResult = new DataGridViewTextBoxColumn
            {
                Name = "Result",
                HeaderText = "试验结果",
                DataPropertyName = "Phenocode",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvReportData.Columns.Add(colResult);

            // 添加列：火焰时间(s)
            var colFlameTime = new DataGridViewTextBoxColumn
            {
                Name = "FlameTime",
                HeaderText = "火焰时间(s)",
                DataPropertyName = "Flametime",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "F1"
                }
            };
            dgvReportData.Columns.Add(colFlameTime);

            // 添加列：持续时间(s)
            var colDuration = new DataGridViewTextBoxColumn
            {
                Name = "Duration",
                HeaderText = "持续时间(s)",
                DataPropertyName = "Flameduration",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "F1"
                }
            };
            dgvReportData.Columns.Add(colDuration);

            // 添加列：残余质量(g)
            var colPostWeight = new DataGridViewTextBoxColumn
            {
                Name = "PostWeight",
                HeaderText = "残余质量(g)",
                DataPropertyName = "Postweight",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "F2"
                }
            };
            dgvReportData.Columns.Add(colPostWeight);

            // 设置列标题样式
            dgvReportData.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReportData.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(65, 105, 225);
            dgvReportData.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReportData.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9F, FontStyle.Bold);
        }

        /*
         * 功能: 查询按钮点击事件
         */
        private async void btnReportQuery_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击查询按钮");

                // 禁用按钮防止重复点击
                btnReportQuery.Enabled = false;

                using (var progress = new ProgressIndicator(this, "正在查询试验数据..."))
                {
                    // 获取查询条件
                    DateTime startDate = dtpReportStartDate.Value.Date;
                    DateTime endDate = dtpReportEndDate.Value.Date.AddDays(1).AddSeconds(-1); // 包含结束日期的全天
                    string productId = txtReportProductId.Text.Trim();
                    string testId = txtReportTestId.Text.Trim();

                    // 调用数据库查询
                    var testData = await QueryTestDataAsync(startDate, endDate, productId, testId);

                    // 绑定数据到表格
                    dgvReportData.DataSource = testData;

                    Log.Information("查询完成，共找到 {Count} 条记录", testData.Count);
                    AppendSystemMessage($"查询完成，共找到 {testData.Count} 条试验记录。");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "查询试验数据失败");
                MessageBox.Show($"查询试验数据失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                btnReportQuery.Enabled = true;
            }
        }

        /*
         * 功能: 重置按钮点击事件
         */
        private void btnReportReset_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击重置按钮");

                // 重置查询条件
                dtpReportEndDate.Value = DateTime.Now;
                dtpReportStartDate.Value = DateTime.Now.AddDays(-30);
                txtReportProductId.Text = string.Empty;
                txtReportTestId.Text = string.Empty;

                // 清空表格数据
                dgvReportData.DataSource = null;

                AppendSystemMessage("查询条件已重置。");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "重置查询条件失败");
                MessageBox.Show($"重置查询条件失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 查询试验数据（异步方法）
         */
        private async Task<List<Testmaster>> QueryTestDataAsync(
            DateTime startDate, 
            DateTime endDate, 
            string productId, 
            string testId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var dbContext = new ISO11820DbContext())
                    {
                        var query = dbContext.Testmasters.AsQueryable();

                        // 按日期范围筛选
                        query = query.Where(t => t.Testdate >= startDate && t.Testdate <= endDate);

                        // 按样品编号筛选（如果提供）
                        if (!string.IsNullOrEmpty(productId))
                        {
                            query = query.Where(t => t.Productid.Contains(productId));
                        }

                        // 按样品标识筛选（如果提供）
                        if (!string.IsNullOrEmpty(testId))
                        {
                            query = query.Where(t => t.Testid.Contains(testId));
                        }

                        // 按日期降序排序
                        query = query.OrderByDescending(t => t.Testdate);

                        var records = query.ToList();
                        foreach (var record in records)
                        {
                            record.Memo = ReportPathMemoHelper.GetDisplayMemo(record.Memo);
                        }

                        return records;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "数据库查询失败");
                    throw;
                }
            });
        }

        /*
         * 功能: 导出Excel按钮点击事件
         */
        private async void btnReportExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击导出Excel按钮");

                // 检查是否有数据
                if (dgvReportData.Rows.Count == 0)
                {
                    MessageBox.Show("没有可导出的数据，请先查询试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 获取选中的试验记录
                if (dgvReportData.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要导出的试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var selectedRow = dgvReportData.SelectedRows[0];
                var testData = selectedRow.DataBoundItem as Testmaster;

                if (testData == null)
                {
                    MessageBox.Show("无法获取试验数据。",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // 显示保存文件对话框
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel文件 (*.xlsx)|*.xlsx";
                    saveFileDialog.FileName = $"试验报告_{testData.Testid}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    saveFileDialog.Title = "导出试验报告";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 禁用按钮防止重复点击
                        btnReportExportExcel.Enabled = false;

                        using (var progress = new ProgressIndicator(this, "正在导出Excel..."))
                        {
                            // 使用ExportService导出
                            var success = await _exportService.ExportTestReportToExcel(testData.Testid, saveFileDialog.FileName);

                            if (success)
                            {
                                Log.Information("导出Excel成功，文件路径: {FilePath}", saveFileDialog.FileName);
                                ExceptionHandler.ShowSuccess($"导出成功！\n文件保存在: {saveFileDialog.FileName}");
                                AppendSystemMessage($"试验报告已导出到Excel: {Path.GetFileName(saveFileDialog.FileName)}");
                            }
                            else
                            {
                                ExceptionHandler.ShowWarning("导出Excel失败，请检查数据是否完整。");
                            }
                        }

                        // 恢复按钮状态
                        btnReportExportExcel.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "导出Excel");
                
                // 恢复按钮状态
                btnReportExportExcel.Enabled = true;
            }
        }

        /*
         * 功能: 导出PDF按钮点击事件（可选功能）
         */
        private void btnReportExportPdf_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击导出PDF按钮");

                MessageBox.Show("PDF导出功能尚未实现，请使用Excel导出功能。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出PDF失败");
                MessageBox.Show($"导出PDF失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 导出数据到Excel文件
         */
        private void ExportToExcel(string filePath)
        {
            try
            {
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    // 创建工作表
                    var worksheet = package.Workbook.Worksheets.Add("试验报告");

                    // 设置标题行
                    for (int i = 0; i < dgvReportData.Columns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = dgvReportData.Columns[i].HeaderText;
                    }

                    // 设置标题行样式
                    using (var range = worksheet.Cells[1, 1, 1, dgvReportData.Columns.Count])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(65, 105, 225));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    // 填充数据行
                    for (int i = 0; i < dgvReportData.Rows.Count; i++)
                    {
                        for (int j = 0; j < dgvReportData.Columns.Count; j++)
                        {
                            var cellValue = dgvReportData.Rows[i].Cells[j].Value;
                            worksheet.Cells[i + 2, j + 1].Value = cellValue;
                        }
                    }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    // 保存文件
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
                    package.SaveAs(fileInfo);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出Excel文件失败");
                throw;
            }
        }

        #endregion

        #region 记录查询功能

        /*
         * 功能: 初始化记录查询视图
         */
        private void InitializeQueryView()
        {
            try
            {
                // 初始化日期选择器（默认最近30天）
                dtpQueryEndDate.Value = DateTime.Now;
                dtpQueryStartDate.Value = DateTime.Now.AddDays(-30);

                // 初始化查询数据表格
                InitializeQueryDataGrid();

                Log.Information("记录查询视图初始化完成");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "初始化记录查询视图失败");
                MessageBox.Show($"初始化记录查询视图失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 初始化查询数据表格
         */
        private void InitializeQueryDataGrid()
        {
            dgvQueryData.AutoGenerateColumns = false;
            dgvQueryData.AllowUserToAddRows = false;
            dgvQueryData.AllowUserToDeleteRows = false;
            dgvQueryData.ReadOnly = true;
            dgvQueryData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvQueryData.RowHeadersVisible = false;
            dgvQueryData.BackgroundColor = Color.White;
            dgvQueryData.BorderStyle = BorderStyle.FixedSingle;
            dgvQueryData.ColumnHeadersHeight = 35;
            dgvQueryData.RowTemplate.Height = 30;

            // 清空现有列
            dgvQueryData.Columns.Clear();

            // 添加列：试验日期
            var colTestDate = new DataGridViewTextBoxColumn
            {
                Name = "TestDate",
                HeaderText = "试验日期",
                DataPropertyName = "Testdate",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "yyyy-MM-dd"
                }
            };
            dgvQueryData.Columns.Add(colTestDate);

            // 添加列：样品编号
            var colProductId = new DataGridViewTextBoxColumn
            {
                Name = "ProductId",
                HeaderText = "样品编号",
                DataPropertyName = "Productid",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colProductId);

            // 添加列：样品标识
            var colTestId = new DataGridViewTextBoxColumn
            {
                Name = "TestId",
                HeaderText = "样品标识",
                DataPropertyName = "Testid",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colTestId);

            // 添加列：样品名称
            var colProductName = new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                HeaderText = "样品名称",
                DataPropertyName = "Productname",
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colProductName);

            // 添加列：操作员
            var colOperator = new DataGridViewTextBoxColumn
            {
                Name = "Operator",
                HeaderText = "操作员",
                DataPropertyName = "Operator",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colOperator);

            // 添加列：试验结果
            var colResult = new DataGridViewTextBoxColumn
            {
                Name = "Result",
                HeaderText = "试验结果",
                DataPropertyName = "Phenocode",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colResult);

            // 添加列：初始质量(g)
            var colInitWeight = new DataGridViewTextBoxColumn
            {
                Name = "InitWeight",
                HeaderText = "初始质量(g)",
                DataPropertyName = "Preweight",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "F2"
                }
            };
            dgvQueryData.Columns.Add(colInitWeight);

            // 添加列：残余质量(g)
            var colPostWeight = new DataGridViewTextBoxColumn
            {
                Name = "PostWeight",
                HeaderText = "残余质量(g)",
                DataPropertyName = "Postweight",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9F),
                    Format = "F2"
                }
            };
            dgvQueryData.Columns.Add(colPostWeight);

            // 添加列：试验备注
            var colRemark = new DataGridViewTextBoxColumn
            {
                Name = "Remark",
                HeaderText = "试验备注",
                DataPropertyName = "Memo",
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Font = new Font("Arial", 9F)
                }
            };
            dgvQueryData.Columns.Add(colRemark);

            // 设置列标题样式
            dgvQueryData.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvQueryData.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(65, 105, 225);
            dgvQueryData.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvQueryData.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9F, FontStyle.Bold);
        }

        /*
         * 功能: 查询按钮点击事件
         */
        private async void btnQuerySearch_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击记录查询按钮");

                // 禁用按钮防止重复点击
                btnQuerySearch.Enabled = false;

                using (var progress = new ProgressIndicator(this, "正在查询试验记录..."))
                {
                    // 获取查询条件
                    DateTime startDate = dtpQueryStartDate.Value.Date;
                    DateTime endDate = dtpQueryEndDate.Value.Date.AddDays(1).AddSeconds(-1); // 包含结束日期的全天
                    string productId = txtQueryProductId.Text.Trim();
                    string testId = txtQueryTestId.Text.Trim();
                    string operatorId = txtQueryOperator.Text.Trim();

                    // 调用数据库查询
                    var testData = await QueryRecordsAsync(startDate, endDate, productId, testId, operatorId);

                    // 绑定数据到表格
                    dgvQueryData.DataSource = testData;

                    Log.Information("查询完成，共找到 {Count} 条记录", testData.Count);
                    
                    // Requirements 5.5: 如果没有记录匹配查询条件，显示未找到结果的消息
                    if (testData.Count == 0)
                    {
                        AppendSystemMessage("未找到符合条件的试验记录。请尝试调整查询条件。");
                        MessageBox.Show("未找到符合条件的试验记录。\n\n请尝试：\n• 扩大日期范围\n• 检查样品编号是否正确\n• 清空部分查询条件",
                            "查询结果",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendSystemMessage($"查询完成，共找到 {testData.Count} 条试验记录。");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "查询试验记录失败");
                MessageBox.Show($"查询试验记录失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                btnQuerySearch.Enabled = true;
            }
        }

        /*
         * 功能: 重置按钮点击事件
         */
        private void btnQueryReset_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击重置按钮");

                // 重置查询条件
                dtpQueryEndDate.Value = DateTime.Now;
                dtpQueryStartDate.Value = DateTime.Now.AddDays(-30);
                txtQueryProductId.Text = string.Empty;
                txtQueryTestId.Text = string.Empty;
                txtQueryOperator.Text = string.Empty;

                // 清空表格数据
                dgvQueryData.DataSource = null;

                AppendSystemMessage("查询条件已重置。");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "重置查询条件失败");
                MessageBox.Show($"重置查询条件失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 查询试验记录（异步方法）
         */
        private async Task<List<Testmaster>> QueryRecordsAsync(
            DateTime startDate, 
            DateTime endDate, 
            string productId, 
            string testId,
            string operatorId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var dbContext = new ISO11820DbContext())
                    {
                        var query = dbContext.Testmasters
                            .Include(t => t.Product)  // 包含关联的Product数据
                            .AsQueryable();

                        // 按日期范围筛选
                        query = query.Where(t => t.Testdate >= startDate && t.Testdate <= endDate);

                        // 按样品编号筛选（如果提供）
                        if (!string.IsNullOrEmpty(productId))
                        {
                            query = query.Where(t => t.Productid.Contains(productId));
                        }

                        // 按样品标识筛选（如果提供）
                        if (!string.IsNullOrEmpty(testId))
                        {
                            query = query.Where(t => t.Testid.Contains(testId));
                        }

                        // 按操作员筛选（如果提供）
                        if (!string.IsNullOrEmpty(operatorId))
                        {
                            query = query.Where(t => t.Operator.Contains(operatorId));
                        }

                        // 按日期降序排序
                        query = query.OrderByDescending(t => t.Testdate);

                        return query.ToList();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "数据库查询失败");
                    throw;
                }
            });
        }

        /*
         * 功能: 查看详情按钮点击事件
         */
        private void btnQueryViewDetails_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击查看详情按钮");

                // 检查是否选中了记录
                if (dgvQueryData.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择一条试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 获取选中的记录
                var selectedRow = dgvQueryData.SelectedRows[0];
                var testData = selectedRow.DataBoundItem as Testmaster;

                if (testData != null)
                {
                    // 构建详情信息
                    var details = new System.Text.StringBuilder();
                    details.AppendLine("=== 试验记录详情 ===");
                    details.AppendLine();
                    details.AppendLine($"试验日期: {testData.Testdate:yyyy-MM-dd}");
                    details.AppendLine($"样品编号: {testData.Productid}");
                    details.AppendLine($"样品标识: {testData.Testid}");
                    
                    // 从关联的Product对象获取产品信息
                    if (testData.Product != null)
                    {
                        details.AppendLine($"样品名称: {testData.Product.Productname}");
                        details.AppendLine($"规格型号: {testData.Product.Specific}");
                        details.AppendLine();
                        details.AppendLine($"样品高度: {testData.Product.Height} mm");
                        details.AppendLine($"样品直径: {testData.Product.Diameter} mm");
                    }
                    
                    details.AppendLine($"初始质量: {testData.Preweight} g");
                    details.AppendLine($"残余质量: {testData.Postweight} g");
                    details.AppendLine();
                    details.AppendLine($"环境温度: {testData.Ambtemp} ℃");
                    details.AppendLine($"环境湿度: {testData.Ambhumi} %");
                    details.AppendLine();
                    details.AppendLine($"试验结果: {testData.Phenocode}");
                    details.AppendLine($"火焰时间: {testData.Flametime} s");
                    details.AppendLine($"持续时间: {testData.Flameduration} s");
                    details.AppendLine();
                    details.AppendLine($"操作员: {testData.Operator}");
                    details.AppendLine($"检验依据: {testData.According}");
                    details.AppendLine($"试验备注: {ReportPathMemoHelper.GetDisplayMemo(testData.Memo)}");

                    // 显示详情对话框
                    MessageBox.Show(details.ToString(),
                        "试验记录详情",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    Log.Information("查看试验记录详情：样品编号={ProductId}, 样品标识={TestId}", 
                        testData.Productid, testData.Testid);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "查看详情失败");
                MessageBox.Show($"查看详情失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * 功能: 导出CSV按钮点击事件
         */
        private async void btnQueryExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击导出查询结果Excel按钮");

                if (dgvQueryData.Rows.Count == 0)
                {
                    MessageBox.Show("没有可导出的数据，请先查询试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var records = dgvQueryData.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow)
                    .Select(row => row.DataBoundItem as Testmaster)
                    .Where(record => record != null)
                    .Cast<Testmaster>()
                    .ToList();

                if (records.Count == 0)
                {
                    MessageBox.Show("无法获取查询结果数据。",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel文件 (*.xlsx)|*.xlsx";
                    saveFileDialog.FileName = $"试验记录查询结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    saveFileDialog.Title = "导出查询结果";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        btnQueryExportExcel.Enabled = false;

                        using (var progress = new ProgressIndicator(this, "正在导出Excel..."))
                        {
                            var success = await _exportService.ExportQueryResultsToExcel(
                                records,
                                saveFileDialog.FileName,
                                dtpQueryStartDate.Value.Date,
                                dtpQueryEndDate.Value.Date,
                                txtQueryProductId.Text.Trim(),
                                txtQueryTestId.Text.Trim(),
                                txtQueryOperator.Text.Trim());

                            if (success)
                            {
                                Log.Information("导出查询结果Excel成功，文件路径: {FilePath}", saveFileDialog.FileName);
                                ExceptionHandler.ShowSuccess($"导出成功！\n文件保存在: {saveFileDialog.FileName}");
                                AppendSystemMessage($"查询结果已导出到Excel: {Path.GetFileName(saveFileDialog.FileName)}");
                            }
                            else
                            {
                                ExceptionHandler.ShowWarning("导出查询结果Excel失败，请检查数据是否完整。");
                            }
                        }

                        btnQueryExportExcel.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "导出查询结果Excel");
                btnQueryExportExcel.Enabled = true;
            }
        }

        /*
         * 功能: 导出CSV按钮点击事件
         */
        private async void btnQueryExportCsv_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击导出CSV按钮");

                // 检查是否有数据
                if (dgvQueryData.Rows.Count == 0)
                {
                    MessageBox.Show("没有可导出的数据，请先查询试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 获取选中的试验记录
                if (dgvQueryData.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要导出的试验记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var selectedRow = dgvQueryData.SelectedRows[0];
                var testData = selectedRow.DataBoundItem as Testmaster;

                if (testData == null)
                {
                    MessageBox.Show("无法获取试验数据。",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // 显示保存文件对话框
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV文件 (*.csv)|*.csv";
                    saveFileDialog.FileName = $"温度数据_{testData.Testid}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    saveFileDialog.Title = "导出温度数据";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 禁用按钮防止重复点击
                        btnQueryExportCsv.Enabled = false;

                        using (var progress = new ProgressIndicator(this, "正在导出CSV..."))
                        {
                            // 使用ExportService导出
                            var success = await _exportService.ExportTemperatureDataToCsv(testData.Testid, saveFileDialog.FileName);

                            if (success)
                            {
                                Log.Information("导出CSV成功，文件路径: {FilePath}", saveFileDialog.FileName);
                                ExceptionHandler.ShowSuccess($"导出成功！\n文件保存在: {saveFileDialog.FileName}");
                                AppendSystemMessage($"温度数据已导出到CSV: {Path.GetFileName(saveFileDialog.FileName)}");
                            }
                            else
                            {
                                ExceptionHandler.ShowWarning("导出CSV失败，请检查数据是否完整。");
                            }
                        }

                        // 恢复按钮状态
                        btnQueryExportCsv.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "导出CSV");
                
                // 恢复按钮状态
                btnQueryExportCsv.Enabled = true;
            }
        }

        /*
         * 功能: 导出数据到CSV文件
         */
        private void ExportToCsv(string filePath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    // 写入标题行
                    for (int i = 0; i < dgvQueryData.Columns.Count; i++)
                    {
                        csv.WriteField(dgvQueryData.Columns[i].HeaderText);
                    }
                    csv.NextRecord();

                    // 写入数据行
                    for (int i = 0; i < dgvQueryData.Rows.Count; i++)
                    {
                        for (int j = 0; j < dgvQueryData.Columns.Count; j++)
                        {
                        var cellValue = dgvQueryData.Rows[i].Cells[j].Value;
                            csv.WriteField(cellValue?.ToString() ?? string.Empty);
                        }
                        csv.NextRecord();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出CSV文件失败");
                throw;
            }
        }

        #endregion

        #region 数据导出功能

        /*
         * 功能: 导出当前温度曲线图表到图片
         */
        public void ExportChartToImage()
        {
            try
            {
                Log.Information("用户请求导出温度曲线图表");

                // 检查图表是否存在
                if (_chartModel == null)
                {
                    MessageBox.Show("没有可导出的图表数据。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                bool hasChartData = _chartModel.Series
                    .OfType<LineSeries>()
                    .Any(series => series.Points.Count > 0);
                if (!hasChartData)
                {
                    MessageBox.Show("当前温度曲线没有可导出的数据。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 显示保存文件对话框
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel文件 (*.xlsx)|*.xlsx|PNG图片 (*.png)|*.png|JPEG图片 (*.jpg)|*.jpg|BMP图片 (*.bmp)|*.bmp";
                    saveFileDialog.FileName = $"温度曲线_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    saveFileDialog.Title = "导出温度曲线图表";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var progress = new ProgressIndicator(this, "正在导出图表..."))
                        {
                            var extension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();
                            if (extension == ".xlsx")
                            {
                                _exportService.ExportChartToExcel(_chartModel, saveFileDialog.FileName);
                            }
                            else
                            {
                                _exportService.ExportChartToImage(_chartModel, saveFileDialog.FileName, 1200, 600);
                            }

                            var exportedFilePath = Path.GetFullPath(saveFileDialog.FileName);
                            Log.Information("导出图表成功，文件路径: {FilePath}", exportedFilePath);
                            AppendSystemMessage($"温度曲线图表已导出: {exportedFilePath}");

                            var openResult = MessageBox.Show(
                                $"温度曲线导出成功。\n\n保存位置：\n{exportedFilePath}\n\n是否立即打开文件？",
                                "导出成功",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (openResult == DialogResult.Yes)
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = exportedFilePath,
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "导出图表失败", "ExportChartToImage");
            }
        }

        /*
         * 功能: 批量导出试验数据
         */
        public async void BatchExportTestData()
        {
            try
            {
                Log.Information("用户请求批量导出试验数据");

                // 检查是否有选中的记录
                if (dgvQueryData.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要导出的试验记录。\n\n提示：可以按住Ctrl或Shift键选择多条记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 获取选中的试验ID列表
                var testIds = new List<string>();
                foreach (DataGridViewRow row in dgvQueryData.SelectedRows)
                {
                    var testData = row.DataBoundItem as Testmaster;
                    if (testData != null)
                    {
                        testIds.Add(testData.Testid);
                    }
                }

                if (testIds.Count == 0)
                {
                    MessageBox.Show("没有有效的试验记录可导出。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 显示导出选项对话框
                using (var exportDialog = new Form())
                {
                    exportDialog.Text = "批量导出选项";
                    exportDialog.Size = new Size(400, 250);
                    exportDialog.StartPosition = FormStartPosition.CenterParent;
                    exportDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    exportDialog.MaximizeBox = false;
                    exportDialog.MinimizeBox = false;

                    var lblInfo = new Label
                    {
                        Text = $"已选择 {testIds.Count} 条试验记录，请选择导出格式：",
                        Location = new Point(20, 20),
                        Size = new Size(350, 30),
                        Font = new Font("Microsoft YaHei UI", 9F)
                    };
                    exportDialog.Controls.Add(lblInfo);

                    var chkCsv = new CheckBox
                    {
                        Text = "导出温度数据 (CSV)",
                        Location = new Point(40, 60),
                        Size = new Size(300, 25),
                        Checked = true,
                        Font = new Font("Microsoft YaHei UI", 9F)
                    };
                    exportDialog.Controls.Add(chkCsv);

                    var chkExcel = new CheckBox
                    {
                        Text = "导出试验报告 (Excel)",
                        Location = new Point(40, 90),
                        Size = new Size(300, 25),
                        Checked = true,
                        Font = new Font("Microsoft YaHei UI", 9F)
                    };
                    exportDialog.Controls.Add(chkExcel);

                    var btnOk = new Button
                    {
                        Text = "开始导出",
                        Location = new Point(120, 140),
                        Size = new Size(100, 35),
                        DialogResult = DialogResult.OK,
                        BackColor = Color.FromArgb(40, 167, 69),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Microsoft YaHei UI", 9F)
                    };
                    btnOk.FlatAppearance.BorderSize = 0;
                    exportDialog.Controls.Add(btnOk);

                    var btnCancel = new Button
                    {
                        Text = "取消",
                        Location = new Point(230, 140),
                        Size = new Size(100, 35),
                        DialogResult = DialogResult.Cancel,
                        BackColor = Color.FromArgb(108, 117, 125),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Microsoft YaHei UI", 9F)
                    };
                    btnCancel.FlatAppearance.BorderSize = 0;
                    exportDialog.Controls.Add(btnCancel);

                    if (exportDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 选择导出文件夹
                        using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                        {
                            folderDialog.Description = "选择导出文件夹";
                            folderDialog.ShowNewFolderButton = true;

                            if (folderDialog.ShowDialog() == DialogResult.OK)
                            {
                                // 创建进度窗口
                                using (var progressForm = new Form())
                                {
                                    progressForm.Text = "批量导出进度";
                                    progressForm.Size = new Size(450, 150);
                                    progressForm.StartPosition = FormStartPosition.CenterParent;
                                    progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                                    progressForm.MaximizeBox = false;
                                    progressForm.MinimizeBox = false;
                                    progressForm.ControlBox = false;

                                    var lblProgress = new Label
                                    {
                                        Text = "正在导出，请稍候...",
                                        Location = new Point(20, 20),
                                        Size = new Size(400, 25),
                                        Font = new Font("Microsoft YaHei UI", 9F)
                                    };
                                    progressForm.Controls.Add(lblProgress);

                                    var progressBar = new ProgressBar
                                    {
                                        Location = new Point(20, 55),
                                        Size = new Size(400, 25),
                                        Style = ProgressBarStyle.Continuous
                                    };
                                    progressForm.Controls.Add(progressBar);

                                    // 显示进度窗口
                                    progressForm.Show();
                                    Application.DoEvents();

                                    // 创建进度报告器
                                    var progress = new Progress<int>(percent =>
                                    {
                                        progressBar.Value = percent;
                                        lblProgress.Text = $"正在导出... {percent}%";
                                        Application.DoEvents();
                                    });

                                    // 执行批量导出
                                    var successCount = await _exportService.BatchExportTestData(
                                        testIds,
                                        folderDialog.SelectedPath,
                                        chkCsv.Checked,
                                        chkExcel.Checked,
                                        progress);

                                    // 关闭进度窗口
                                    progressForm.Close();

                                    // 显示结果
                                    Log.Information("批量导出完成，成功 {SuccessCount}/{TotalCount}", successCount, testIds.Count);
                                    ExceptionHandler.ShowSuccess($"批量导出完成！\n\n成功导出: {successCount} 个文件\n导出位置: {folderDialog.SelectedPath}");
                                    AppendSystemMessage($"批量导出完成，成功 {successCount} 个文件。");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "批量导出");
            }
        }

        #endregion

        #region 汇总报告功能

        /*
         * 功能: 汇总报告按钮点击事件
         */
        private async void btnQuerySummaryReport_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击汇总报告按钮");

                // 检查是否有选中的记录
                if (dgvQueryData.SelectedRows.Count < 2)
                {
                    MessageBox.Show("请至少选择2条试验记录来生成汇总报告。\n\n提示：按住Ctrl或Shift键可以选择多条记录。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 获取选中的试验记录
                var selectedTests = new List<(string ProductId, string TestId)>();
                foreach (DataGridViewRow row in dgvQueryData.SelectedRows)
                {
                    var testData = row.DataBoundItem as Testmaster;
                    if (testData != null)
                    {
                        selectedTests.Add((testData.Productid, testData.Testid));
                    }
                }

                if (selectedTests.Count < 2)
                {
                    MessageBox.Show("没有足够的有效试验记录来生成汇总报告。",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 确认生成汇总报告
                var confirmResult = MessageBox.Show(
                    $"已选择 {selectedTests.Count} 条试验记录。\n\n是否生成汇总报告？\n\n注意：不完整的试验记录将被自动排除。",
                    "确认生成汇总报告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult != DialogResult.Yes)
                {
                    return;
                }

                // 禁用按钮防止重复点击
                btnQuerySummaryReport.Enabled = false;

                using (var progress = new ProgressIndicator(this, "正在生成汇总报告..."))
                {
                    // 创建报告服务
                    var reportConfig = ConfigurationHelper.GetReportConfiguration();

                    using (var dbContext = new ISO11820DbContext())
                    {
                        var reportService = new ReportService(reportConfig, dbContext, Log.Logger);

                        // 生成汇总报告
                        var result = await reportService.GenerateSummaryReportAsync(
                            selectedTests,
                            ReportFormat.ExcelAndPdf);

                        if (result.Success)
                        {
                            // 检查是否有被排除的记录
                            var message = $"汇总报告生成成功！\n\n";
                            
                            if (result.ExcludedTestIds.Count > 0)
                            {
                                message += $"注意：{result.ExcludedTestIds.Count} 条不完整记录已被排除。\n\n";
                            }

                            message += $"Excel报告: {result.ExcelFilePath}\n";
                            if (!string.IsNullOrEmpty(result.PdfFilePath))
                            {
                                message += $"PDF报告: {result.PdfFilePath}\n";
                            }
                            message += $"\n耗时: {result.ElapsedMilliseconds}ms";

                            Log.Information("汇总报告生成成功: {ExcelPath}", result.ExcelFilePath);
                            
                            // 询问是否打开报告
                            var openResult = MessageBox.Show(
                                message + "\n\n是否打开报告文件？",
                                "汇总报告生成成功",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (openResult == DialogResult.Yes && !string.IsNullOrEmpty(result.ExcelFilePath))
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = result.ExcelFilePath,
                                    UseShellExecute = true
                                });
                            }

                            AppendSystemMessage($"汇总报告已生成，包含 {selectedTests.Count - result.ExcludedTestIds.Count} 条试验记录。");
                        }
                        else
                        {
                            Log.Error("汇总报告生成失败: {ErrorMessage}", result.ErrorMessage);
                            MessageBox.Show($"汇总报告生成失败：\n\n{result.ErrorMessage}",
                                "错误",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "生成汇总报告失败");
                MessageBox.Show($"生成汇总报告失败: {ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                btnQuerySummaryReport.Enabled = true;
            }
        }

        #endregion
    }
}
