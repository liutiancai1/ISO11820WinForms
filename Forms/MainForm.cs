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
using ISO11820WinForms.Services;
using ISO11820WinForms.Global;
using Serilog;
using Microsoft.EntityFrameworkCore;
using ISO11820WinForms.Utilities;

namespace ISO11820WinForms.Forms
{
    public partial class MainForm : Form
    {
        private Operator _currentUser;
        private ToolStripMenuItem? _selectedMenuItem;

        // 图表相关字段
        private PlotView? _chartView;
        private PlotModel? _chartModel;
        private LineSeries? _seriesTF1;  // 炉内温度1（蓝色）
        private LineSeries? _seriesTF2;  // 炉内温度2（红色）
        private LineSeries? _seriesTS;   // 表面温度（绿色）
        private LineSeries? _seriesTC;   // 中心温度（黄色）

        private int _dataPointCount = 0;  // 数据点计数器
        private const int MAX_DATA_POINTS = 750;  // 600秒 ÷ 0.8秒 ≈ 750个点（10分钟）
        private const double Y_AXIS_MAX = 800;     // Y轴最大值

        // 性能优化：批量更新和节流
        private int _chartUpdateCounter = 0;
        private const int CHART_UPDATE_INTERVAL = 2;  // 每2次数据更新才刷新一次图表（减少重绘）
        private bool _isChartUpdatePending = false;

        // DaqWorker实例（假设已经在全局上下文中）
        private DaqWorker? _daqWorker;

        // TestMaster1实例（一号试验炉控制器）
        // Requirement 1.1, 1.2, 1.4: 订阅TestMaster1事件以接收实时数据和状态更新
        private TestMaster1? _testMaster1;

        // 系统消息和实时数据相关字段
        private const int MAX_REALTIME_DATA_ROWS = 75;  // 最多保留75条（60秒 ÷ 0.8秒）

        // 导出服务实例
        private ExportService _exportService = new ExportService();

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

            // 初始化系统校验视图
            InitializeCalibrationView();

            // 初始化试验报告视图
            InitializeReportView();

            // 初始化记录查询视图
            InitializeQueryView();

            // 添加按钮悬停效果
            InitializeButtonHoverEffects();
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

        /*
         * 功能: 初始化按钮悬停效果
         */
        private void InitializeButtonHoverEffects()
        {
            // 为所有按钮添加悬停效果
            AddButtonHoverEffect(btnNewTest, Color.FromArgb(255, 193, 7), Color.FromArgb(255, 213, 79));
            AddButtonHoverEffect(btnOpenRecord, Color.FromArgb(0, 123, 255), Color.FromArgb(0, 143, 255));
            AddButtonHoverEffect(btnStopRecord, Color.FromArgb(220, 53, 69), Color.FromArgb(200, 35, 51));
            AddButtonHoverEffect(btnRecordLogs, Color.FromArgb(111, 66, 193), Color.FromArgb(131, 86, 213));
            AddButtonHoverEffect(btnParamSettings, Color.FromArgb(108, 117, 125), Color.FromArgb(128, 137, 145));
            AddButtonHoverEffect(btnStartHeating, Color.FromArgb(255, 140, 0), Color.FromArgb(255, 160, 50));
            AddButtonHoverEffect(btnStopHeating, Color.FromArgb(220, 53, 69), Color.FromArgb(200, 35, 51));
            AddButtonHoverEffect(btnSystemMessage, Color.FromArgb(65, 105, 225), Color.FromArgb(85, 125, 245));
            AddButtonHoverEffect(btnRealTimeData, Color.FromArgb(100, 150, 255), Color.FromArgb(120, 170, 255));
        }

        /*
         * 功能: 为按钮添加悬停效果
         */
        private void AddButtonHoverEffect(Button button, Color normalColor, Color hoverColor)
        {
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = hoverColor;
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = normalColor;
            };
        }

        /*
         * 功能: 绘制数据显示面板（包含炉子图片）
         */
        private void PanelDataDisplay_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                // 尝试加载炉子图片
                var furnaceImage = ResourceHelper.LoadFurnaceImage();
                
                if (furnaceImage != null)
                {
                    using (furnaceImage)
                    {
                        // 在面板底部绘制炉子图片
                        int imageWidth = 200;
                        int imageHeight = 200;
                        int x = (panelDataDisplay.Width - imageWidth) / 2;
                        int y = panelDataDisplay.Height - imageHeight - 20;
                        
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
                int width = 180;
                int height = 180;
                int x = (panelDataDisplay.Width - width) / 2;
                int y = panelDataDisplay.Height - height - 30;

                // 绘制炉子外框
                using (var pen = new Pen(Color.FromArgb(108, 117, 125), 3))
                {
                    g.DrawRectangle(pen, x, y, width, height);
                }

                // 绘制炉子内部
                using (var brush = new SolidBrush(Color.FromArgb(255, 140, 0)))
                {
                    g.FillRectangle(brush, x + 10, y + 10, width - 20, height - 20);
                }

                // 绘制文字
                using (var font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
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
            // 恢复上一个选中项的样式
            if (_selectedMenuItem != null)
            {
                _selectedMenuItem.BackColor = SystemColors.Control;
                _selectedMenuItem.ForeColor = SystemColors.ControlText;
            }

            // 设置TabControl显示对应的标签页
            this.tabControl1.SelectedIndex = tabIndex;

            // 设置选中菜单项的样式
            selectedItem.BackColor = Color.FromArgb(51, 122, 183);
            selectedItem.ForeColor = Color.White;

            _selectedMenuItem = selectedItem;
        }

        private void btnNewTest_Click(object sender, EventArgs e)
        {
            try
            {
                Log.Information("用户点击新建试验按钮");
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
            
            using (var progress = new ProgressIndicator(this, "正在启动加热..."))
            {
                try
                {
                    Log.Information("用户点击开始升温按钮");

                    // 从全局上下文获取TestMaster实例（标准单例访问模式）
                    var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                    // 调用TestMaster的异步升温方法
                    var result = await testMaster.StartHeatingAsync();

                    if (result == 0)
                    {
                        Log.Information("试验装置开始加热成功");
                        AppendSystemMessage("试验装置开始加热。");
                    }
                    else
                    {
                        Log.Warning("试验装置开始加热失败，通信异常，返回码: {Result}", result);
                        ExceptionHandler.ShowWarning("通信异常，炉温加热未能启动。\n请检查设备连接。");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleHardwareException(ex, "加热控制器");
                }
                finally
                {
                    // 恢复按钮状态
                    btnStartHeating.Enabled = true;
                }
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
                
                // 从全局上下文获取TestMaster实例
                var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                // 调用TestMaster的停止加热方法
                var result = testMaster.StopHeating();

                if (result == 0)
                {
                    Log.Information("试验装置停止加热成功");
                    AppendSystemMessage("试验装置已停止加热。");
                }
                else
                {
                    Log.Warning("试验装置停止加热失败，通信异常，返回码: {Result}", result);
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
                
                // 从全局上下文获取TestMaster实例
                var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                // 判断是否已新建试验（业务规则验证）
                if (testMaster.GetTestData() == null)
                {
                    ExceptionHandler.ShowWarning("试验控制器尚未接收试验样品信息，请先新建本次试验。", "无法开始记录");
                    return;
                }

                // 调用TestMaster的开始记录方法
                var result = testMaster.StartRecording();

                if (result)
                {
                    var testData = testMaster.GetTestData();
                    Log.Information("开始记录试验数据成功，样品编号: {ProductId}, 样品标识: {TestId}", 
                        testData.Productid, testData.Testid);
                    AppendSystemMessage($"开始记录试验数据。样品编号: [{testData.Productid}], 样品标识: [{testData.Testid}]");
                }
                else
                {
                    Log.Warning("开始记录失败：设备连接异常");
                    ExceptionHandler.ShowWarning("启动记录失败，请检查设备连接。");
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
                
                // 从全局上下文获取TestMaster实例
                var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                // 调用TestMaster的停止记录方法
                var result = testMaster.StopRecording();

                if (result)
                {
                    Log.Information("停止记录成功");
                    AppendSystemMessage("计时结束。");
                }
                else
                {
                    Log.Warning("停止记录失败：设备连接异常");
                    ExceptionHandler.ShowWarning("停止记录失败，请检查设备连接。");
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
                // 从全局上下文获取TestMaster实例
                var testMaster = SystemContext.Current.Masters.DictTestMaster[0];

                // 判断是否已新建试验（业务规则验证）
                if (testMaster.GetTestData() == null)
                {
                    ExceptionHandler.ShowWarning("试验控制器尚未接收试验样品信息，请先新建本次试验。", "无法记录试验数据");
                    return;
                }

                // 打开试验记录对话框
                using (TestPhenoForm testPhenoForm = new TestPhenoForm())
                {
                    if (testPhenoForm.ShowDialog(this) == DialogResult.OK)
                    {
                        // 1. 设置试验后数据（现象编码、火焰时间、残余质量）
                        testMaster.SetPostTestData(
                            testPhenoForm.PhenoCode,
                            testPhenoForm.FlameTime,
                            testPhenoForm.FlameDuration,
                            testPhenoForm.PostWeight);

                        AppendSystemMessage($"试验记录数据已设置。现象编码: [{testPhenoForm.PhenoCode}], 残余质量: [{testPhenoForm.PostWeight}g]");

                        // 2. 执行试验后期处理（保存数据到数据库、生成报告文件）
                        using (var progress = new ProgressIndicator(this, "正在保存试验数据并生成报告..."))
                        {
                            await testMaster.PostTestProcess();
                        }

                        // 3. 清空本次试验数据缓存
                        testMaster.ResetTestData();

                        Log.Information("试验完成，数据已保存，报告已生成");
                        AppendSystemMessage("试验已完成，数据已保存到数据库，报告已生成。");
                        ExceptionHandler.ShowSuccess("试验数据已保存，报告已生成。");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "完成试验流程失败");
                ExceptionHandler.HandleDatabaseException(ex, "保存试验记录");
            }
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
                            AppendSystemMessage($"设备参数已更新。设备编号: [{setParamForm.ApparatusId}], 设备名称: [{setParamForm.ApparatusName}]");
                            ExceptionHandler.ShowSuccess("设备参数已成功保存。");
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
            // 使用统一的确认对话框
            if (!ExceptionHandler.Confirm("确定要退出系统吗？\n\n退出后所有未保存的数据将丢失。", "退出确认"))
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
                    _testMaster1.FlameDetected -= OnTestMasterFlameDetected;
                }
                
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
                
                // Requirement 1.3: 订阅FlameDetected事件以接收火焰检测通知
                _testMaster1.FlameDetected += OnTestMasterFlameDetected;
            }
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

                    // 更新温度曲线图表（使用Modbus数据，而非DaqWorker的ADAM数据）
                    UpdateChartFromModbus(e.Timer, e.SensorData);
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
         * 功能: 使用Modbus数据更新温度曲线图表
         * 说明: 由于DaqWorker (COM3 ADAM协议) 在仿真模式下超时返回0，
         *       图表数据改为使用TestMaster1广播的Modbus数据
         * 注意: 使用 _dataPointCount 作为X轴时间值，因为TestMaster每秒广播一次
         */
        private void UpdateChartFromModbus(int timer, SensorDataCatch sensorData)
        {
            try
            {
                if (_seriesTF1 == null || _seriesTF2 == null || _seriesTS == null || _seriesTC == null || _chartModel == null)
                {
                    Log.Warning("图表更新失败: 图表组件未初始化");
                    return;
                }

                // 使用 _dataPointCount 作为X轴时间值（秒）
                double xValue = _dataPointCount;

                // 调试日志：输出当前数据点信息
                Log.Information("图表更新: 点数={Count}, X={X}, Temp1={T1:F1}, Temp2={T2:F1}, TempSuf={TS:F1}, TempCen={TC:F1}", 
                    _dataPointCount, xValue, sensorData.Temp1, sensorData.Temp2, sensorData.TempSuf, sensorData.TempCen);

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
                        xAxis.Maximum = firstPoint.X + 600;
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
                string statusMessage = e.NewStatus switch
                {
                    MasterStatus.Idle => "系统空闲",
                    MasterStatus.Preparing => "正在升温准备中...",
                    MasterStatus.Ready => "已达到试验条件，可以开始记录",
                    MasterStatus.Recording => "正在记录试验数据...",
                    MasterStatus.Complete => "试验已完成",
                    MasterStatus.Exception => "系统异常",
                    _ => $"状态: {e.NewStatus}"
                };

                // 添加系统消息
                if (e.OldStatus != e.NewStatus)
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

        /*
         * 功能: 根据TestMaster状态更新按钮启用状态
         */
        private void UpdateButtonStates(MasterStatus status)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新按钮状态失败");
            }
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
                    Title = "实时温度曲线",
                    Background = OxyColors.White,
                    TitleFontSize = 16
                };

                // 配置X轴（时间轴）
                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "时间(s)",
                    Minimum = 0,
                    Maximum = 600,  // 显示最近10分钟
                    MajorStep = 60,  // 每60秒一个主刻度
                    MinorStep = 10,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineColor = OxyColor.FromRgb(230, 230, 230)
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
                    MajorGridlineColor = OxyColor.FromRgb(230, 230, 230)
                };
                _chartModel.Axes.Add(yAxis);

                // 创建4个数据线系列
                _seriesTF1 = new LineSeries
                {
                    Title = "TF1(炉内温度1)",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _chartModel.Series.Add(_seriesTF1);

                _seriesTF2 = new LineSeries
                {
                    Title = "TF2(炉内温度2)",
                    Color = OxyColors.Red,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _chartModel.Series.Add(_seriesTF2);

                _seriesTS = new LineSeries
                {
                    Title = "TS(表面温度)",
                    Color = OxyColors.Green,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _chartModel.Series.Add(_seriesTS);

                _seriesTC = new LineSeries
                {
                    Title = "TC(中心温度)",
                    Color = OxyColors.Gold,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _chartModel.Series.Add(_seriesTC);

                // 创建PlotView控件
                _chartView = new PlotView
                {
                    Model = _chartModel,
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
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
                
                // 更新校准温度稳定状态视觉指示
                // 根据 Requirements 4.3: 当温度在 750±5°C (745-755°C) 范围内时显示稳定状态
                UpdateCalibrationStabilityIndicator(e.TempCalibration);

                // 添加实时数据到表格（标准WinForms数据绑定模式）
                AppendRealTimeData(e);

                // 注意：图表更新已移至 OnTestMasterDataBroadcast -> UpdateChartFromModbus
                // 因为 DaqWorker (COM3 ADAM协议) 在仿真模式下超时返回0
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理传感器数据失败: {ex.Message}");
            }
        }

        /*
         * 功能: 重置图表（开始新试验时调用）
         * 性能优化：重置计数器和标志
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

                    // 重置性能优化相关的计数器
                    _chartUpdateCounter = 0;
                    _isChartUpdatePending = false;

                    // 重置X轴范围
                    var xAxis = _chartModel.Axes[0] as LinearAxis;
                    if (xAxis != null)
                    {
                        xAxis.Minimum = 0;
                        xAxis.Maximum = 600;
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
                    HeaderText = "温度漂移(℃)",
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
            // 切换按钮样式（标准选中状态高亮模式）
            btnSystemMessage.BackColor = Color.FromArgb(65, 105, 225);
            btnRealTimeData.BackColor = Color.FromArgb(100, 150, 255);

            // 切换显示
            dgvSystemMessage.Visible = true;
            dgvRealTimeData.Visible = false;
        }

        /*
         * 功能: 切换到实时数据显示
         */
        private void btnRealTimeData_Click(object sender, EventArgs e)
        {
            // 切换按钮样式（标准选中状态高亮模式）
            btnRealTimeData.BackColor = Color.FromArgb(65, 105, 225);
            btnSystemMessage.BackColor = Color.FromArgb(100, 150, 255);

            // 切换显示
            dgvRealTimeData.Visible = true;
            dgvSystemMessage.Visible = false;
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

        /*
         * 功能: 初始化系统校验视图（采用三列布局，模仿原Web项目）
         */
        private void InitializeCalibrationView()
        {
            try
            {
                // 清空panelCalibration
                panelCalibration.Controls.Clear();
                panelCalibration.BackColor = Color.FromArgb(240, 240, 240);
                panelCalibration.Padding = new Padding(10);

                // 创建主 TableLayoutPanel（2行1列，上下两个区域）
                var mainTableLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(0),
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                
                // 设置行高（各占50%）
                mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                mainTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                // 创建炉壁温度校验区域（第一行）
                var surfacePanel = CreateSurfaceCalibrationPanel();
                mainTableLayout.Controls.Add(surfacePanel, 0, 0);

                // 创建中心轴温度校验区域（第二行）
                var centerPanel = CreateCenterCalibrationPanel();
                mainTableLayout.Controls.Add(centerPanel, 0, 1);

                // 将 TableLayoutPanel 添加到 panelCalibration
                panelCalibration.Controls.Add(mainTableLayout);

                // 初始化数据
                InitializeSurfaceTempData();
                InitializeCenterTempData();
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

            var dataCaliTemp2 = new Label
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
            panel.Controls.Add(dataCaliTemp2);

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
                    Background = OxyColors.White,
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
                    MajorGridlineColor = OxyColor.FromRgb(230, 230, 230)
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
                    MajorGridlineColor = OxyColor.FromRgb(230, 230, 230)
                };
                _centerChartModel.Axes.Add(yAxis);

                // 创建数据线系列
                _centerTempSeries = new LineSeries
                {
                    Title = "中心轴温度",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = OxyColors.Blue
                };
                _centerChartModel.Series.Add(_centerTempSeries);

                // 创建PlotView控件
                _centerChartView = new PlotView
                {
                    Model = _centerChartModel,
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
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

                        return query.ToList();
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
                    details.AppendLine($"试验备注: {testData.Memo}");

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

                // 显示保存文件对话框
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG图片 (*.png)|*.png|JPEG图片 (*.jpg)|*.jpg|BMP图片 (*.bmp)|*.bmp";
                    saveFileDialog.FileName = $"温度曲线_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    saveFileDialog.Title = "导出温度曲线图表";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var progress = new ProgressIndicator(this, "正在导出图表..."))
                        {
                            // 使用ExportService导出图表
                            var success = _exportService.ExportChartToImage(_chartModel, saveFileDialog.FileName, 1200, 600);

                            if (success)
                            {
                                Log.Information("导出图表成功，文件路径: {FilePath}", saveFileDialog.FileName);
                                ExceptionHandler.ShowSuccess($"导出成功！\n文件保存在: {saveFileDialog.FileName}");
                                AppendSystemMessage($"温度曲线图表已导出: {Path.GetFileName(saveFileDialog.FileName)}");
                            }
                            else
                            {
                                ExceptionHandler.ShowWarning("导出图表失败。");
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
                    var reportConfig = new ReportConfiguration
                    {
                        OutputDirectory = Path.Combine(Application.StartupPath, "Reports"),
                        TemplateFilePath = Path.Combine(Application.StartupPath, "Templates", "ReportTemplate.xlsx"),
                        SummaryTemplateFilePath = Path.Combine(Application.StartupPath, "Templates", "SummaryTemplate.xlsx")
                    };

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