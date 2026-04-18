namespace ISO11820WinForms.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            lblSystemName = new ToolStripMenuItem();
            导出ToolStripMenuItem = new ToolStripMenuItem();
            导出温度曲线ToolStripMenuItem = new ToolStripMenuItem();
            批量导出ToolStripMenuItem = new ToolStripMenuItem();
            日志ToolStripMenuItem = new ToolStripMenuItem();
            关于ToolStripMenuItem = new ToolStripMenuItem();
            退出ToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            menuStrip3 = new MenuStrip();
            样品试验ToolStripMenuItem = new ToolStripMenuItem();
            系统校验ToolStripMenuItem = new ToolStripMenuItem();
            试验报告ToolStripMenuItem = new ToolStripMenuItem();
            记录查询ToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPage样品试验 = new TabPage();
            panelSample = new Panel();
            panelMessageBottom = new Panel();
            btnRealTimeData = new Button();
            btnSystemMessage = new Button();
            messagePanel = new Panel();
            dgvSystemMessage = new DataGridView();
            dgvRealTimeData = new DataGridView();
            chartPanel = new Panel();
            panelDataDisplay = new Panel();
            dataTempRise = new Label();
            lblTempRise = new Label();
            dataCenterTemp = new Label();
            lblCenterTemp = new Label();
            dataSurfaceTemp = new Label();
            lblSurfaceTemp = new Label();
            dataTemp2 = new Label();
            lblTemp2 = new Label();
            dataTemp1 = new Label();
            lblTemp1 = new Label();
            dataTime = new Label();
            lblTime = new Label();
            panelOperations = new Panel();
            btnStopHeating = new Button();
            btnStartHeating = new Button();
            btnParamSettings = new Button();
            btnRecordLogs = new Button();
            btnStopRecord = new Button();
            btnOpenRecord = new Button();
            btnNewTest = new Button();
            tabPage系统校验 = new TabPage();
            tabPage试验报告 = new TabPage();
            tabPage记录查询 = new TabPage();
            panelCalibration = new Panel();
            panelReport = new Panel();
            panelQuery = new Panel();
            grpQueryConditions = new GroupBox();
            lblQueryStartDate = new Label();
            dtpQueryStartDate = new DateTimePicker();
            lblQueryEndDate = new Label();
            dtpQueryEndDate = new DateTimePicker();
            lblQueryOperator = new Label();
            txtQueryOperator = new TextBox();
            lblQueryProductId = new Label();
            txtQueryProductId = new TextBox();
            lblQueryTestId = new Label();
            txtQueryTestId = new TextBox();
            btnQuerySearch = new Button();
            btnQueryReset = new Button();
            dgvQueryData = new DataGridView();
            btnQueryViewDetails = new Button();
            btnQueryExportCsv = new Button();
            btnQuerySummaryReport = new Button();
            grpReportQuery = new GroupBox();
            lblReportStartDate = new Label();
            dtpReportStartDate = new DateTimePicker();
            lblReportEndDate = new Label();
            dtpReportEndDate = new DateTimePicker();
            lblReportProductId = new Label();
            txtReportProductId = new TextBox();
            lblReportTestId = new Label();
            txtReportTestId = new TextBox();
            btnReportQuery = new Button();
            btnReportReset = new Button();
            dgvReportData = new DataGridView();
            btnReportExportExcel = new Button();
            btnReportExportPdf = new Button();
            grpSurfaceCalibration = new GroupBox();
            grpCenterCalibration = new GroupBox();
            lblCaliTemp = new Label();
            dataCaliTemp = new Label();
            lblSurfacePosition = new Label();
            cmbSurfacePosition = new ComboBox();
            btnCalculate = new Button();
            btnRecordSurface = new Button();
            dgvSurfaceTemp = new DataGridView();
            grpSurfaceResults = new GroupBox();
            lblCenterPosition = new Label();
            cmbCenterPosition = new ComboBox();
            btnResetCenter = new Button();
            btnRecordCenter = new Button();
            panelCenterChart = new Panel();
            grpCenterResults = new GroupBox();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            menuStrip3.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage样品试验.SuspendLayout();
            panelSample.SuspendLayout();
            panelMessageBottom.SuspendLayout();
            messagePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSystemMessage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvRealTimeData).BeginInit();
            panelDataDisplay.SuspendLayout();
            panelOperations.SuspendLayout();
            tabPage系统校验.SuspendLayout();
            panelCalibration.SuspendLayout();
            grpSurfaceCalibration.SuspendLayout();
            grpCenterCalibration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSurfaceTemp).BeginInit();
            grpSurfaceResults.SuspendLayout();
            grpCenterResults.SuspendLayout();
            tabPage试验报告.SuspendLayout();
            panelReport.SuspendLayout();
            grpReportQuery.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReportData).BeginInit();
            tabPage记录查询.SuspendLayout();
            panelQuery.SuspendLayout();
            grpQueryConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueryData).BeginInit();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { lblSystemName, 导出ToolStripMenuItem, 日志ToolStripMenuItem, 关于ToolStripMenuItem, 退出ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(8, 2, 0, 2);
            menuStrip1.Size = new Size(1317, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // lblSystemName
            // 
            lblSystemName.Enabled = false;
            lblSystemName.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            lblSystemName.Name = "lblSystemName";
            lblSystemName.Size = new Size(297, 29);
            lblSystemName.Text = "建筑材料不燃性试验系统 版本 3.0";
            // 
            // 导出ToolStripMenuItem
            // 
            导出ToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            导出ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 导出温度曲线ToolStripMenuItem, 批量导出ToolStripMenuItem });
            导出ToolStripMenuItem.Name = "导出ToolStripMenuItem";
            导出ToolStripMenuItem.Size = new Size(53, 29);
            导出ToolStripMenuItem.Text = "导出";
            // 
            // 导出温度曲线ToolStripMenuItem
            // 
            导出温度曲线ToolStripMenuItem.Name = "导出温度曲线ToolStripMenuItem";
            导出温度曲线ToolStripMenuItem.Size = new Size(180, 26);
            导出温度曲线ToolStripMenuItem.Text = "导出温度曲线";
            导出温度曲线ToolStripMenuItem.Click += 导出温度曲线ToolStripMenuItem_Click;
            // 
            // 批量导出ToolStripMenuItem
            // 
            批量导出ToolStripMenuItem.Name = "批量导出ToolStripMenuItem";
            批量导出ToolStripMenuItem.Size = new Size(180, 26);
            批量导出ToolStripMenuItem.Text = "批量导出";
            批量导出ToolStripMenuItem.Click += 批量导出ToolStripMenuItem_Click;
            // 
            // 日志ToolStripMenuItem
            // 
            日志ToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            日志ToolStripMenuItem.Name = "日志ToolStripMenuItem";
            日志ToolStripMenuItem.Size = new Size(53, 29);
            日志ToolStripMenuItem.Text = "日志";
            日志ToolStripMenuItem.Click += 日志ToolStripMenuItem_Click;
            // 
            // 关于ToolStripMenuItem
            // 
            关于ToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            关于ToolStripMenuItem.Size = new Size(53, 29);
            关于ToolStripMenuItem.Text = "关于";
            关于ToolStripMenuItem.Click += 关于ToolStripMenuItem_Click;
            // 
            // 退出ToolStripMenuItem
            // 
            退出ToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            退出ToolStripMenuItem.Size = new Size(53, 29);
            退出ToolStripMenuItem.Text = "退出";
            退出ToolStripMenuItem.Click += 退出ToolStripMenuItem_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(menuStrip3);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 33);
            panel1.Name = "panel1";
            panel1.Size = new Size(1317, 28);
            panel1.TabIndex = 1;
            // 
            // menuStrip3
            // 
            menuStrip3.ImageScalingSize = new Size(20, 20);
            menuStrip3.Items.AddRange(new ToolStripItem[] { 样品试验ToolStripMenuItem, 系统校验ToolStripMenuItem, 试验报告ToolStripMenuItem, 记录查询ToolStripMenuItem });
            menuStrip3.Location = new Point(0, 0);
            menuStrip3.Name = "menuStrip3";
            menuStrip3.Size = new Size(1317, 28);
            menuStrip3.TabIndex = 1;
            menuStrip3.Text = "menuStrip3";
            // 
            // 样品试验ToolStripMenuItem
            // 
            样品试验ToolStripMenuItem.Name = "样品试验ToolStripMenuItem";
            样品试验ToolStripMenuItem.Size = new Size(83, 24);
            样品试验ToolStripMenuItem.Text = "样品试验";
            样品试验ToolStripMenuItem.Click += MenuItem_Click;
            // 
            // 系统校验ToolStripMenuItem
            // 
            系统校验ToolStripMenuItem.Name = "系统校验ToolStripMenuItem";
            系统校验ToolStripMenuItem.Size = new Size(83, 24);
            系统校验ToolStripMenuItem.Text = "系统校验";
            系统校验ToolStripMenuItem.Click += MenuItem_Click;
            // 
            // 试验报告ToolStripMenuItem
            // 
            试验报告ToolStripMenuItem.Name = "试验报告ToolStripMenuItem";
            试验报告ToolStripMenuItem.Size = new Size(83, 24);
            试验报告ToolStripMenuItem.Text = "试验报告";
            试验报告ToolStripMenuItem.Click += MenuItem_Click;
            // 
            // 记录查询ToolStripMenuItem
            // 
            记录查询ToolStripMenuItem.Name = "记录查询ToolStripMenuItem";
            记录查询ToolStripMenuItem.Size = new Size(83, 24);
            记录查询ToolStripMenuItem.Text = "记录查询";
            记录查询ToolStripMenuItem.Click += MenuItem_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage样品试验);
            tabControl1.Controls.Add(tabPage系统校验);
            tabControl1.Controls.Add(tabPage试验报告);
            tabControl1.Controls.Add(tabPage记录查询);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 61);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1317, 645);
            tabControl1.TabIndex = 2;
            tabControl1.TabStop = false;
            // 
            // tabPage样品试验
            // 
            tabPage样品试验.Controls.Add(panelSample);
            tabPage样品试验.Location = new Point(4, 29);
            tabPage样品试验.Name = "tabPage样品试验";
            tabPage样品试验.Size = new Size(1309, 612);
            tabPage样品试验.TabIndex = 0;
            tabPage样品试验.Text = "样品试验";
            tabPage样品试验.UseVisualStyleBackColor = true;
            // 
            // panelSample
            // 
            panelSample.Controls.Add(panelMessageBottom);
            panelSample.Controls.Add(messagePanel);
            panelSample.Controls.Add(chartPanel);
            panelSample.Controls.Add(panelDataDisplay);
            panelSample.Controls.Add(panelOperations);
            panelSample.Dock = DockStyle.Fill;
            panelSample.Location = new Point(0, 0);
            panelSample.Name = "panelSample";
            panelSample.Size = new Size(1309, 612);
            panelSample.TabIndex = 0;
            // 
            // panelMessageBottom
            // 
            panelMessageBottom.BackColor = Color.FromArgb(240, 240, 240);
            panelMessageBottom.Controls.Add(btnRealTimeData);
            panelMessageBottom.Controls.Add(btnSystemMessage);
            panelMessageBottom.Dock = DockStyle.Bottom;
            panelMessageBottom.Location = new Point(320, 430);
            panelMessageBottom.Name = "panelMessageBottom";
            panelMessageBottom.Size = new Size(1004, 45);
            panelMessageBottom.TabIndex = 4;
            // 
            // btnRealTimeData
            // 
            btnRealTimeData.BackColor = Color.FromArgb(100, 150, 255);
            btnRealTimeData.FlatStyle = FlatStyle.Flat;
            btnRealTimeData.FlatAppearance.BorderSize = 0;
            btnRealTimeData.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnRealTimeData.ForeColor = Color.White;
            btnRealTimeData.Location = new Point(180, 7);
            btnRealTimeData.Name = "btnRealTimeData";
            btnRealTimeData.Size = new Size(160, 32);
            btnRealTimeData.TabIndex = 1;
            btnRealTimeData.Text = "实时数据";
            btnRealTimeData.UseVisualStyleBackColor = false;
            btnRealTimeData.Cursor = Cursors.Hand;
            btnRealTimeData.Click += btnRealTimeData_Click;
            // 
            // btnSystemMessage
            // 
            btnSystemMessage.BackColor = Color.FromArgb(65, 105, 225);
            btnSystemMessage.FlatStyle = FlatStyle.Flat;
            btnSystemMessage.FlatAppearance.BorderSize = 0;
            btnSystemMessage.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnSystemMessage.ForeColor = Color.White;
            btnSystemMessage.Location = new Point(10, 7);
            btnSystemMessage.Name = "btnSystemMessage";
            btnSystemMessage.Size = new Size(160, 32);
            btnSystemMessage.TabIndex = 0;
            btnSystemMessage.Text = "系统消息";
            btnSystemMessage.UseVisualStyleBackColor = false;
            btnSystemMessage.Cursor = Cursors.Hand;
            btnSystemMessage.Click += btnSystemMessage_Click;
            // 
            // messagePanel
            // 
            messagePanel.BackColor = Color.White;
            messagePanel.BorderStyle = BorderStyle.FixedSingle;
            messagePanel.Controls.Add(dgvSystemMessage);
            messagePanel.Controls.Add(dgvRealTimeData);
            messagePanel.Dock = DockStyle.Bottom;
            messagePanel.Location = new Point(320, 475);
            messagePanel.Name = "messagePanel";
            messagePanel.Size = new Size(1004, 150);
            messagePanel.TabIndex = 3;
            // 
            // dgvSystemMessage
            // 
            dgvSystemMessage.AllowUserToAddRows = false;
            dgvSystemMessage.AllowUserToDeleteRows = false;
            dgvSystemMessage.BackgroundColor = Color.White;
            dgvSystemMessage.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSystemMessage.Dock = DockStyle.Fill;
            dgvSystemMessage.Location = new Point(0, 0);
            dgvSystemMessage.Name = "dgvSystemMessage";
            dgvSystemMessage.ReadOnly = true;
            dgvSystemMessage.RowHeadersWidth = 51;
            dgvSystemMessage.RowTemplate.Height = 29;
            dgvSystemMessage.Size = new Size(1002, 138);
            dgvSystemMessage.TabIndex = 0;
            dgvSystemMessage.Visible = true;
            // 
            // dgvRealTimeData
            // 
            dgvRealTimeData.AllowUserToAddRows = false;
            dgvRealTimeData.AllowUserToDeleteRows = false;
            dgvRealTimeData.BackgroundColor = Color.White;
            dgvRealTimeData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRealTimeData.Dock = DockStyle.Fill;
            dgvRealTimeData.Location = new Point(0, 0);
            dgvRealTimeData.Name = "dgvRealTimeData";
            dgvRealTimeData.ReadOnly = true;
            dgvRealTimeData.RowHeadersWidth = 51;
            dgvRealTimeData.RowTemplate.Height = 29;
            dgvRealTimeData.Size = new Size(1002, 138);
            dgvRealTimeData.TabIndex = 1;
            dgvRealTimeData.Visible = false;
            // 
            // chartPanel
            // 
            chartPanel.BackColor = Color.White;
            chartPanel.BorderStyle = BorderStyle.FixedSingle;
            chartPanel.Dock = DockStyle.Fill;
            chartPanel.Location = new Point(320, 50);
            chartPanel.Name = "chartPanel";
            chartPanel.Size = new Size(1004, 562);
            chartPanel.TabIndex = 2;
            // 
            // panelDataDisplay
            // 
            panelDataDisplay.BackColor = Color.White;
            panelDataDisplay.BorderStyle = BorderStyle.FixedSingle;
            panelDataDisplay.Controls.Add(dataTempRise);
            panelDataDisplay.Controls.Add(lblTempRise);
            panelDataDisplay.Controls.Add(dataCenterTemp);
            panelDataDisplay.Controls.Add(lblCenterTemp);
            panelDataDisplay.Controls.Add(dataSurfaceTemp);
            panelDataDisplay.Controls.Add(lblSurfaceTemp);
            panelDataDisplay.Controls.Add(dataTemp2);
            panelDataDisplay.Controls.Add(lblTemp2);
            panelDataDisplay.Controls.Add(dataTemp1);
            panelDataDisplay.Controls.Add(lblTemp1);
            panelDataDisplay.Controls.Add(dataTime);
            panelDataDisplay.Controls.Add(lblTime);
            panelDataDisplay.Dock = DockStyle.Left;
            panelDataDisplay.Location = new Point(0, 50);
            panelDataDisplay.Name = "panelDataDisplay";
            panelDataDisplay.Size = new Size(320, 562);
            panelDataDisplay.TabIndex = 1;
            panelDataDisplay.Paint += PanelDataDisplay_Paint;
            // 
            // dataTempRise
            // 
            dataTempRise.BackColor = Color.FromArgb(33, 37, 41);
            dataTempRise.BorderStyle = BorderStyle.FixedSingle;
            dataTempRise.Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataTempRise.ForeColor = Color.FromArgb(255, 215, 0);
            dataTempRise.Location = new Point(170, 268);
            dataTempRise.Name = "dataTempRise";
            dataTempRise.Size = new Size(135, 36);
            dataTempRise.TabIndex = 11;
            dataTempRise.Text = "888.8";
            dataTempRise.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTempRise
            // 
            lblTempRise.BackColor = Color.FromArgb(52, 58, 64);
            lblTempRise.BorderStyle = BorderStyle.FixedSingle;
            lblTempRise.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblTempRise.ForeColor = Color.White;
            lblTempRise.Location = new Point(10, 268);
            lblTempRise.Name = "lblTempRise";
            lblTempRise.Size = new Size(160, 36);
            lblTempRise.TabIndex = 10;
            lblTempRise.Text = "温度涨移 (℃)";
            lblTempRise.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataCenterTemp
            // 
            dataCenterTemp.BackColor = Color.FromArgb(33, 37, 41);
            dataCenterTemp.BorderStyle = BorderStyle.FixedSingle;
            dataCenterTemp.Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataCenterTemp.ForeColor = Color.FromArgb(255, 215, 0);
            dataCenterTemp.Location = new Point(170, 226);
            dataCenterTemp.Name = "dataCenterTemp";
            dataCenterTemp.Size = new Size(135, 36);
            dataCenterTemp.TabIndex = 9;
            dataCenterTemp.Text = "888.8";
            dataCenterTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCenterTemp
            // 
            lblCenterTemp.BackColor = Color.FromArgb(52, 58, 64);
            lblCenterTemp.BorderStyle = BorderStyle.FixedSingle;
            lblCenterTemp.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblCenterTemp.ForeColor = Color.White;
            lblCenterTemp.Location = new Point(10, 226);
            lblCenterTemp.Name = "lblCenterTemp";
            lblCenterTemp.Size = new Size(160, 36);
            lblCenterTemp.TabIndex = 8;
            lblCenterTemp.Text = "中心温度 (℃)";
            lblCenterTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataSurfaceTemp
            // 
            dataSurfaceTemp.BackColor = Color.FromArgb(33, 37, 41);
            dataSurfaceTemp.BorderStyle = BorderStyle.FixedSingle;
            dataSurfaceTemp.Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataSurfaceTemp.ForeColor = Color.FromArgb(255, 215, 0);
            dataSurfaceTemp.Location = new Point(170, 184);
            dataSurfaceTemp.Name = "dataSurfaceTemp";
            dataSurfaceTemp.Size = new Size(135, 36);
            dataSurfaceTemp.TabIndex = 7;
            dataSurfaceTemp.Text = "888.8";
            dataSurfaceTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSurfaceTemp
            // 
            lblSurfaceTemp.BackColor = Color.FromArgb(52, 58, 64);
            lblSurfaceTemp.BorderStyle = BorderStyle.FixedSingle;
            lblSurfaceTemp.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblSurfaceTemp.ForeColor = Color.White;
            lblSurfaceTemp.Location = new Point(10, 184);
            lblSurfaceTemp.Name = "lblSurfaceTemp";
            lblSurfaceTemp.Size = new Size(160, 36);
            lblSurfaceTemp.TabIndex = 6;
            lblSurfaceTemp.Text = "表面温度 (℃)";
            lblSurfaceTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataTemp2
            // 
            dataTemp2.BackColor = Color.FromArgb(33, 37, 41);
            dataTemp2.BorderStyle = BorderStyle.FixedSingle;
            dataTemp2.Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataTemp2.ForeColor = Color.FromArgb(255, 215, 0);
            dataTemp2.Location = new Point(170, 142);
            dataTemp2.Name = "dataTemp2";
            dataTemp2.Size = new Size(135, 36);
            dataTemp2.TabIndex = 5;
            dataTemp2.Text = "888.8";
            dataTemp2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTemp2
            // 
            lblTemp2.BackColor = Color.FromArgb(52, 58, 64);
            lblTemp2.BorderStyle = BorderStyle.FixedSingle;
            lblTemp2.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblTemp2.ForeColor = Color.White;
            lblTemp2.Location = new Point(10, 142);
            lblTemp2.Name = "lblTemp2";
            lblTemp2.Size = new Size(160, 36);
            lblTemp2.TabIndex = 4;
            lblTemp2.Text = "炉内温度2 (℃)";
            lblTemp2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataTemp1
            // 
            dataTemp1.BackColor = Color.FromArgb(33, 37, 41);
            dataTemp1.BorderStyle = BorderStyle.FixedSingle;
            dataTemp1.Font = new Font("Consolas", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataTemp1.ForeColor = Color.FromArgb(255, 215, 0);
            dataTemp1.Location = new Point(170, 100);
            dataTemp1.Name = "dataTemp1";
            dataTemp1.Size = new Size(135, 36);
            dataTemp1.TabIndex = 3;
            dataTemp1.Text = "888.8";
            dataTemp1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTemp1
            // 
            lblTemp1.BackColor = Color.FromArgb(52, 58, 64);
            lblTemp1.BorderStyle = BorderStyle.FixedSingle;
            lblTemp1.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblTemp1.ForeColor = Color.White;
            lblTemp1.Location = new Point(10, 100);
            lblTemp1.Name = "lblTemp1";
            lblTemp1.Size = new Size(160, 36);
            lblTemp1.TabIndex = 2;
            lblTemp1.Text = "炉内温度1 (℃)";
            lblTemp1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataTime
            // 
            dataTime.BackColor = Color.FromArgb(33, 37, 41);
            dataTime.BorderStyle = BorderStyle.FixedSingle;
            dataTime.Font = new Font("Consolas", 16F, FontStyle.Bold, GraphicsUnit.Point);
            dataTime.ForeColor = Color.FromArgb(0, 255, 127);
            dataTime.Location = new Point(170, 50);
            dataTime.Name = "dataTime";
            dataTime.Size = new Size(135, 40);
            dataTime.TabIndex = 1;
            dataTime.Text = "888.8";
            dataTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTime
            // 
            lblTime.BackColor = Color.FromArgb(52, 58, 64);
            lblTime.BorderStyle = BorderStyle.FixedSingle;
            lblTime.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblTime.ForeColor = Color.White;
            lblTime.Location = new Point(10, 50);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(160, 40);
            lblTime.TabIndex = 0;
            lblTime.Text = "计时 (s)";
            lblTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelOperations
            // 
            panelOperations.BackColor = Color.FromArgb(240, 240, 240);
            panelOperations.Controls.Add(btnStopHeating);
            panelOperations.Controls.Add(btnStartHeating);
            panelOperations.Controls.Add(btnParamSettings);
            panelOperations.Controls.Add(btnRecordLogs);
            panelOperations.Controls.Add(btnStopRecord);
            panelOperations.Controls.Add(btnOpenRecord);
            panelOperations.Controls.Add(btnNewTest);
            panelOperations.Dock = DockStyle.Top;
            panelOperations.Location = new Point(0, 0);
            panelOperations.Name = "panelOperations";
            panelOperations.Size = new Size(1309, 50);
            panelOperations.TabIndex = 0;
            // 
            // btnStopHeating
            // 
            btnStopHeating.BackColor = Color.FromArgb(220, 53, 69);
            btnStopHeating.FlatStyle = FlatStyle.Flat;
            btnStopHeating.FlatAppearance.BorderSize = 0;
            btnStopHeating.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnStopHeating.ForeColor = Color.White;
            btnStopHeating.Location = new Point(670, 10);
            btnStopHeating.Name = "btnStopHeating";
            btnStopHeating.Size = new Size(100, 32);
            btnStopHeating.TabIndex = 6;
            btnStopHeating.Text = "停止升温";
            btnStopHeating.UseVisualStyleBackColor = false;
            btnStopHeating.Cursor = Cursors.Hand;
            btnStopHeating.Click += btnStopHeating_Click;
            // 
            // btnStartHeating
            // 
            btnStartHeating.BackColor = Color.FromArgb(255, 140, 0);
            btnStartHeating.FlatStyle = FlatStyle.Flat;
            btnStartHeating.FlatAppearance.BorderSize = 0;
            btnStartHeating.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnStartHeating.ForeColor = Color.White;
            btnStartHeating.Location = new Point(560, 10);
            btnStartHeating.Name = "btnStartHeating";
            btnStartHeating.Size = new Size(100, 32);
            btnStartHeating.TabIndex = 5;
            btnStartHeating.Text = "开始升温";
            btnStartHeating.UseVisualStyleBackColor = false;
            btnStartHeating.Cursor = Cursors.Hand;
            btnStartHeating.Click += btnStartHeating_Click;
            // 
            // btnParamSettings
            // 
            btnParamSettings.BackColor = Color.FromArgb(108, 117, 125);
            btnParamSettings.FlatStyle = FlatStyle.Flat;
            btnParamSettings.FlatAppearance.BorderSize = 0;
            btnParamSettings.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnParamSettings.ForeColor = Color.White;
            btnParamSettings.Location = new Point(450, 10);
            btnParamSettings.Name = "btnParamSettings";
            btnParamSettings.Size = new Size(100, 32);
            btnParamSettings.TabIndex = 4;
            btnParamSettings.Text = "参数设置";
            btnParamSettings.UseVisualStyleBackColor = false;
            btnParamSettings.Cursor = Cursors.Hand;
            btnParamSettings.Click += new System.EventHandler(this.btnSetParam_Click);
            // 
            // btnRecordLogs
            // 
            btnRecordLogs.BackColor = Color.FromArgb(111, 66, 193);
            btnRecordLogs.FlatStyle = FlatStyle.Flat;
            btnRecordLogs.FlatAppearance.BorderSize = 0;
            btnRecordLogs.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnRecordLogs.ForeColor = Color.White;
            btnRecordLogs.Location = new Point(340, 10);
            btnRecordLogs.Name = "btnRecordLogs";
            btnRecordLogs.Size = new Size(100, 32);
            btnRecordLogs.TabIndex = 3;
            btnRecordLogs.Text = "试验记录";
            btnRecordLogs.UseVisualStyleBackColor = false;
            btnRecordLogs.Cursor = Cursors.Hand;
            btnRecordLogs.Click += new System.EventHandler(this.btnRecordLogs_Click);
            // 
            // btnStopRecord
            // 
            btnStopRecord.BackColor = Color.FromArgb(220, 53, 69);
            btnStopRecord.FlatStyle = FlatStyle.Flat;
            btnStopRecord.FlatAppearance.BorderSize = 0;
            btnStopRecord.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnStopRecord.ForeColor = Color.White;
            btnStopRecord.Location = new Point(230, 10);
            btnStopRecord.Name = "btnStopRecord";
            btnStopRecord.Size = new Size(100, 32);
            btnStopRecord.TabIndex = 2;
            btnStopRecord.Text = "停止记录";
            btnStopRecord.UseVisualStyleBackColor = false;
            btnStopRecord.Cursor = Cursors.Hand;
            btnStopRecord.Click += btnStopRecord_Click;
            // 
            // btnOpenRecord
            // 
            btnOpenRecord.BackColor = Color.FromArgb(0, 123, 255);
            btnOpenRecord.FlatStyle = FlatStyle.Flat;
            btnOpenRecord.FlatAppearance.BorderSize = 0;
            btnOpenRecord.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnOpenRecord.ForeColor = Color.White;
            btnOpenRecord.Location = new Point(116, 10);
            btnOpenRecord.Name = "btnOpenRecord";
            btnOpenRecord.Size = new Size(100, 32);
            btnOpenRecord.TabIndex = 1;
            btnOpenRecord.Text = "开始记录";
            btnOpenRecord.UseVisualStyleBackColor = false;
            btnOpenRecord.Cursor = Cursors.Hand;
            btnOpenRecord.Click += btnOpenRecord_Click;
            // 
            // btnNewTest
            // 
            btnNewTest.BackColor = Color.FromArgb(255, 193, 7);
            btnNewTest.FlatStyle = FlatStyle.Flat;
            btnNewTest.FlatAppearance.BorderSize = 0;
            btnNewTest.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnNewTest.ForeColor = Color.White;
            btnNewTest.Location = new Point(10, 10);
            btnNewTest.Name = "btnNewTest";
            btnNewTest.Size = new Size(100, 32);
            btnNewTest.TabIndex = 0;
            btnNewTest.Text = "新建试验";
            btnNewTest.UseVisualStyleBackColor = false;
            btnNewTest.Cursor = Cursors.Hand;
            btnNewTest.Click += btnNewTest_Click;
            // 
            // tabPage系统校验
            // 
            tabPage系统校验.Controls.Add(panelCalibration);
            tabPage系统校验.Location = new Point(4, 29);
            tabPage系统校验.Name = "tabPage系统校验";
            tabPage系统校验.Padding = new Padding(3);
            tabPage系统校验.Size = new Size(1309, 612);
            tabPage系统校验.TabIndex = 1;
            tabPage系统校验.Text = "系统校验";
            tabPage系统校验.UseVisualStyleBackColor = true;
            // 
            // panelCalibration
            // 
            panelCalibration.BackColor = Color.White;
            panelCalibration.Dock = DockStyle.Fill;
            panelCalibration.Location = new Point(3, 3);
            panelCalibration.Name = "panelCalibration";
            panelCalibration.Padding = new Padding(10);
            panelCalibration.Size = new Size(1303, 606);
            panelCalibration.TabIndex = 0;
            // 
            // grpSurfaceCalibration
            // 
            grpSurfaceCalibration.Controls.Add(grpSurfaceResults);
            grpSurfaceCalibration.Controls.Add(dgvSurfaceTemp);
            grpSurfaceCalibration.Controls.Add(btnRecordSurface);
            grpSurfaceCalibration.Controls.Add(btnCalculate);
            grpSurfaceCalibration.Controls.Add(cmbSurfacePosition);
            grpSurfaceCalibration.Controls.Add(lblSurfacePosition);
            grpSurfaceCalibration.Controls.Add(dataCaliTemp);
            grpSurfaceCalibration.Controls.Add(lblCaliTemp);
            grpSurfaceCalibration.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            grpSurfaceCalibration.Name = "grpSurfaceCalibration";
            grpSurfaceCalibration.Padding = new Padding(5);
            grpSurfaceCalibration.TabIndex = 0;
            grpSurfaceCalibration.TabStop = false;
            grpSurfaceCalibration.Text = "炉壁温度均匀性校验";
            // 
            // lblCaliTemp
            // 
            lblCaliTemp.BackColor = Color.FromArgb(65, 105, 225);
            lblCaliTemp.Font = new Font("Arial", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblCaliTemp.ForeColor = Color.White;
            lblCaliTemp.Location = new Point(15, 28);
            lblCaliTemp.Name = "lblCaliTemp";
            lblCaliTemp.Size = new Size(160, 28);
            lblCaliTemp.TabIndex = 0;
            lblCaliTemp.Text = "校温热电偶温度(℃)";
            lblCaliTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataCaliTemp
            // 
            dataCaliTemp.BackColor = Color.Black;
            dataCaliTemp.Font = new Font("Arial", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dataCaliTemp.ForeColor = Color.FromArgb(255, 255, 0);
            dataCaliTemp.Location = new Point(180, 28);
            dataCaliTemp.Name = "dataCaliTemp";
            dataCaliTemp.Size = new Size(100, 28);
            dataCaliTemp.TabIndex = 1;
            dataCaliTemp.Text = "888.8";
            dataCaliTemp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSurfacePosition
            // 
            lblSurfacePosition.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            lblSurfacePosition.Location = new Point(15, 65);
            lblSurfacePosition.Name = "lblSurfacePosition";
            lblSurfacePosition.Size = new Size(80, 26);
            lblSurfacePosition.TabIndex = 2;
            lblSurfacePosition.Text = "炉壁点位:";
            lblSurfacePosition.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbSurfacePosition
            // 
            cmbSurfacePosition.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSurfacePosition.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            cmbSurfacePosition.FormattingEnabled = true;
            cmbSurfacePosition.Location = new Point(100, 65);
            cmbSurfacePosition.Name = "cmbSurfacePosition";
            cmbSurfacePosition.Size = new Size(130, 26);
            cmbSurfacePosition.TabIndex = 3;
            // 
            // btnCalculate
            // 
            btnCalculate.BackColor = Color.FromArgb(100, 150, 255);
            btnCalculate.FlatStyle = FlatStyle.Flat;
            btnCalculate.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnCalculate.ForeColor = Color.White;
            btnCalculate.Location = new Point(240, 64);
            btnCalculate.Name = "btnCalculate";
            btnCalculate.Size = new Size(80, 28);
            btnCalculate.TabIndex = 4;
            btnCalculate.Text = "计算";
            btnCalculate.UseVisualStyleBackColor = false;
            btnCalculate.Click += btnCalculate_Click;
            // 
            // btnRecordSurface
            // 
            btnRecordSurface.BackColor = Color.FromArgb(150, 100, 200);
            btnRecordSurface.FlatStyle = FlatStyle.Flat;
            btnRecordSurface.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnRecordSurface.ForeColor = Color.White;
            btnRecordSurface.Location = new Point(330, 64);
            btnRecordSurface.Name = "btnRecordSurface";
            btnRecordSurface.Size = new Size(80, 28);
            btnRecordSurface.TabIndex = 5;
            btnRecordSurface.Text = "记录";
            btnRecordSurface.UseVisualStyleBackColor = false;
            btnRecordSurface.Click += btnRecordSurface_Click;
            // 
            // dgvSurfaceTemp
            // 
            dgvSurfaceTemp.AllowUserToAddRows = false;
            dgvSurfaceTemp.AllowUserToDeleteRows = false;
            dgvSurfaceTemp.BackgroundColor = Color.White;
            dgvSurfaceTemp.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSurfaceTemp.Location = new Point(15, 100);
            dgvSurfaceTemp.Name = "dgvSurfaceTemp";
            dgvSurfaceTemp.ReadOnly = true;
            dgvSurfaceTemp.RowHeadersWidth = 51;
            dgvSurfaceTemp.RowTemplate.Height = 29;
            dgvSurfaceTemp.Size = new Size(550, 175);
            dgvSurfaceTemp.TabIndex = 6;
            dgvSurfaceTemp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            // 
            // grpSurfaceResults
            // 
            grpSurfaceResults.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            grpSurfaceResults.Location = new Point(580, 25);
            grpSurfaceResults.Name = "grpSurfaceResults";
            grpSurfaceResults.Padding = new Padding(5);
            grpSurfaceResults.Size = new Size(690, 250);
            grpSurfaceResults.TabIndex = 7;
            grpSurfaceResults.TabStop = false;
            grpSurfaceResults.Text = "温度均匀性计算结果";
            grpSurfaceResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // grpCenterCalibration
            // 
            grpCenterCalibration.Controls.Add(grpCenterResults);
            grpCenterCalibration.Controls.Add(panelCenterChart);
            grpCenterCalibration.Controls.Add(btnRecordCenter);
            grpCenterCalibration.Controls.Add(btnResetCenter);
            grpCenterCalibration.Controls.Add(cmbCenterPosition);
            grpCenterCalibration.Controls.Add(lblCenterPosition);
            grpCenterCalibration.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            grpCenterCalibration.Name = "grpCenterCalibration";
            grpCenterCalibration.Padding = new Padding(5);
            grpCenterCalibration.TabIndex = 1;
            grpCenterCalibration.TabStop = false;
            grpCenterCalibration.Text = "中心轴温度均匀性校验";
            // 
            // lblCenterPosition
            // 
            lblCenterPosition.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            lblCenterPosition.Location = new Point(15, 28);
            lblCenterPosition.Name = "lblCenterPosition";
            lblCenterPosition.Size = new Size(80, 26);
            lblCenterPosition.TabIndex = 0;
            lblCenterPosition.Text = "中心点位:";
            lblCenterPosition.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbCenterPosition
            // 
            cmbCenterPosition.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCenterPosition.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            cmbCenterPosition.FormattingEnabled = true;
            cmbCenterPosition.Location = new Point(100, 28);
            cmbCenterPosition.Name = "cmbCenterPosition";
            cmbCenterPosition.Size = new Size(130, 26);
            cmbCenterPosition.TabIndex = 1;
            // 
            // btnResetCenter
            // 
            btnResetCenter.BackColor = Color.FromArgb(40, 167, 69);
            btnResetCenter.FlatStyle = FlatStyle.Flat;
            btnResetCenter.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnResetCenter.ForeColor = Color.White;
            btnResetCenter.Location = new Point(240, 27);
            btnResetCenter.Name = "btnResetCenter";
            btnResetCenter.Size = new Size(80, 28);
            btnResetCenter.TabIndex = 2;
            btnResetCenter.Text = "保存";
            btnResetCenter.UseVisualStyleBackColor = false;
            btnResetCenter.Click += btnResetCenter_Click;
            // 
            // btnRecordCenter
            // 
            btnRecordCenter.BackColor = Color.FromArgb(150, 100, 200);
            btnRecordCenter.FlatStyle = FlatStyle.Flat;
            btnRecordCenter.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnRecordCenter.ForeColor = Color.White;
            btnRecordCenter.Location = new Point(330, 27);
            btnRecordCenter.Name = "btnRecordCenter";
            btnRecordCenter.Size = new Size(80, 28);
            btnRecordCenter.TabIndex = 3;
            btnRecordCenter.Text = "记录";
            btnRecordCenter.UseVisualStyleBackColor = false;
            btnRecordCenter.Click += btnRecordCenter_Click;
            // 
            // panelCenterChart
            // 
            panelCenterChart.BackColor = Color.White;
            panelCenterChart.BorderStyle = BorderStyle.FixedSingle;
            panelCenterChart.Location = new Point(15, 62);
            panelCenterChart.Name = "panelCenterChart";
            panelCenterChart.Size = new Size(550, 220);
            panelCenterChart.TabIndex = 4;
            panelCenterChart.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            // 
            // grpCenterResults
            // 
            grpCenterResults.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            grpCenterResults.Location = new Point(580, 25);
            grpCenterResults.Name = "grpCenterResults";
            grpCenterResults.Padding = new Padding(5);
            grpCenterResults.Size = new Size(690, 257);
            grpCenterResults.TabIndex = 5;
            grpCenterResults.TabStop = false;
            grpCenterResults.Text = "中心轴温度记录";
            grpCenterResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // panelReport
            // 
            panelReport.BackColor = Color.White;
            panelReport.Controls.Add(btnReportExportPdf);
            panelReport.Controls.Add(btnReportExportExcel);
            panelReport.Controls.Add(dgvReportData);
            panelReport.Controls.Add(grpReportQuery);
            panelReport.Dock = DockStyle.Fill;
            panelReport.Location = new Point(3, 3);
            panelReport.Name = "panelReport";
            panelReport.Padding = new Padding(10);
            panelReport.Size = new Size(1303, 606);
            panelReport.TabIndex = 0;
            // 
            // grpReportQuery
            // 
            grpReportQuery.Controls.Add(btnReportReset);
            grpReportQuery.Controls.Add(btnReportQuery);
            grpReportQuery.Controls.Add(txtReportTestId);
            grpReportQuery.Controls.Add(lblReportTestId);
            grpReportQuery.Controls.Add(txtReportProductId);
            grpReportQuery.Controls.Add(lblReportProductId);
            grpReportQuery.Controls.Add(dtpReportEndDate);
            grpReportQuery.Controls.Add(lblReportEndDate);
            grpReportQuery.Controls.Add(dtpReportStartDate);
            grpReportQuery.Controls.Add(lblReportStartDate);
            grpReportQuery.Dock = DockStyle.Top;
            grpReportQuery.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            grpReportQuery.Location = new Point(10, 10);
            grpReportQuery.Name = "grpReportQuery";
            grpReportQuery.Padding = new Padding(10);
            grpReportQuery.Size = new Size(1283, 100);
            grpReportQuery.TabIndex = 0;
            grpReportQuery.TabStop = false;
            grpReportQuery.Text = "查询条件";
            // 
            // lblReportStartDate
            // 
            lblReportStartDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblReportStartDate.Location = new Point(20, 35);
            lblReportStartDate.Name = "lblReportStartDate";
            lblReportStartDate.Size = new Size(100, 25);
            lblReportStartDate.TabIndex = 0;
            lblReportStartDate.Text = "开始日期:";
            lblReportStartDate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpReportStartDate
            // 
            dtpReportStartDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dtpReportStartDate.Format = DateTimePickerFormat.Short;
            dtpReportStartDate.Location = new Point(125, 35);
            dtpReportStartDate.Name = "dtpReportStartDate";
            dtpReportStartDate.Size = new Size(150, 27);
            dtpReportStartDate.TabIndex = 1;
            // 
            // lblReportEndDate
            // 
            lblReportEndDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblReportEndDate.Location = new Point(295, 35);
            lblReportEndDate.Name = "lblReportEndDate";
            lblReportEndDate.Size = new Size(100, 25);
            lblReportEndDate.TabIndex = 2;
            lblReportEndDate.Text = "结束日期:";
            lblReportEndDate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpReportEndDate
            // 
            dtpReportEndDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dtpReportEndDate.Format = DateTimePickerFormat.Short;
            dtpReportEndDate.Location = new Point(400, 35);
            dtpReportEndDate.Name = "dtpReportEndDate";
            dtpReportEndDate.Size = new Size(150, 27);
            dtpReportEndDate.TabIndex = 3;
            // 
            // lblReportProductId
            // 
            lblReportProductId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblReportProductId.Location = new Point(570, 35);
            lblReportProductId.Name = "lblReportProductId";
            lblReportProductId.Size = new Size(100, 25);
            lblReportProductId.TabIndex = 4;
            lblReportProductId.Text = "样品编号:";
            lblReportProductId.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtReportProductId
            // 
            txtReportProductId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtReportProductId.Location = new Point(675, 35);
            txtReportProductId.Name = "txtReportProductId";
            txtReportProductId.Size = new Size(150, 27);
            txtReportProductId.TabIndex = 5;
            // 
            // lblReportTestId
            // 
            lblReportTestId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblReportTestId.Location = new Point(845, 35);
            lblReportTestId.Name = "lblReportTestId";
            lblReportTestId.Size = new Size(100, 25);
            lblReportTestId.TabIndex = 6;
            lblReportTestId.Text = "样品标识:";
            lblReportTestId.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtReportTestId
            // 
            txtReportTestId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtReportTestId.Location = new Point(950, 35);
            txtReportTestId.Name = "txtReportTestId";
            txtReportTestId.Size = new Size(150, 27);
            txtReportTestId.TabIndex = 7;
            // 
            // btnReportQuery
            // 
            btnReportQuery.BackColor = Color.FromArgb(100, 150, 255);
            btnReportQuery.FlatStyle = FlatStyle.Flat;
            btnReportQuery.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnReportQuery.ForeColor = Color.White;
            btnReportQuery.Location = new Point(1120, 33);
            btnReportQuery.Name = "btnReportQuery";
            btnReportQuery.Size = new Size(80, 30);
            btnReportQuery.TabIndex = 8;
            btnReportQuery.Text = "查询";
            btnReportQuery.UseVisualStyleBackColor = false;
            btnReportQuery.Click += btnReportQuery_Click;
            // 
            // btnReportReset
            // 
            btnReportReset.BackColor = Color.FromArgb(150, 150, 150);
            btnReportReset.FlatStyle = FlatStyle.Flat;
            btnReportReset.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnReportReset.ForeColor = Color.White;
            btnReportReset.Location = new Point(1210, 33);
            btnReportReset.Name = "btnReportReset";
            btnReportReset.Size = new Size(60, 30);
            btnReportReset.TabIndex = 9;
            btnReportReset.Text = "重置";
            btnReportReset.UseVisualStyleBackColor = false;
            btnReportReset.Click += btnReportReset_Click;
            // 
            // dgvReportData
            // 
            dgvReportData.AllowUserToAddRows = false;
            dgvReportData.AllowUserToDeleteRows = false;
            dgvReportData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvReportData.BackgroundColor = Color.White;
            dgvReportData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReportData.Location = new Point(10, 120);
            dgvReportData.Name = "dgvReportData";
            dgvReportData.ReadOnly = true;
            dgvReportData.RowHeadersWidth = 51;
            dgvReportData.RowTemplate.Height = 29;
            dgvReportData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportData.Size = new Size(1283, 430);
            dgvReportData.TabIndex = 1;
            // 
            // btnReportExportExcel
            // 
            btnReportExportExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnReportExportExcel.BackColor = Color.FromArgb(40, 167, 69);
            btnReportExportExcel.FlatStyle = FlatStyle.Flat;
            btnReportExportExcel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnReportExportExcel.ForeColor = Color.White;
            btnReportExportExcel.Location = new Point(1073, 560);
            btnReportExportExcel.Name = "btnReportExportExcel";
            btnReportExportExcel.Size = new Size(100, 35);
            btnReportExportExcel.TabIndex = 2;
            btnReportExportExcel.Text = "导出Excel";
            btnReportExportExcel.UseVisualStyleBackColor = false;
            btnReportExportExcel.Click += btnReportExportExcel_Click;
            // 
            // btnReportExportPdf
            // 
            btnReportExportPdf.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnReportExportPdf.BackColor = Color.FromArgb(220, 53, 69);
            btnReportExportPdf.FlatStyle = FlatStyle.Flat;
            btnReportExportPdf.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnReportExportPdf.ForeColor = Color.White;
            btnReportExportPdf.Location = new Point(1183, 560);
            btnReportExportPdf.Name = "btnReportExportPdf";
            btnReportExportPdf.Size = new Size(100, 35);
            btnReportExportPdf.TabIndex = 3;
            btnReportExportPdf.Text = "导出PDF";
            btnReportExportPdf.UseVisualStyleBackColor = false;
            btnReportExportPdf.Click += btnReportExportPdf_Click;
            // 
            // tabPage试验报告
            // 
            tabPage试验报告.Controls.Add(panelReport);
            tabPage试验报告.Location = new Point(4, 29);
            tabPage试验报告.Name = "tabPage试验报告";
            tabPage试验报告.Padding = new Padding(3);
            tabPage试验报告.Size = new Size(1309, 612);
            tabPage试验报告.TabIndex = 2;
            tabPage试验报告.Text = "试验报告";
            tabPage试验报告.UseVisualStyleBackColor = true;
            // 
            // tabPage记录查询
            // 
            tabPage记录查询.Controls.Add(panelQuery);
            tabPage记录查询.Location = new Point(4, 29);
            tabPage记录查询.Name = "tabPage记录查询";
            tabPage记录查询.Padding = new Padding(3);
            tabPage记录查询.Size = new Size(1309, 612);
            tabPage记录查询.TabIndex = 3;
            tabPage记录查询.Text = "记录查询";
            tabPage记录查询.UseVisualStyleBackColor = true;
            // 
            // panelQuery
            // 
            panelQuery.BackColor = Color.White;
            panelQuery.Controls.Add(btnQuerySummaryReport);
            panelQuery.Controls.Add(btnQueryExportCsv);
            panelQuery.Controls.Add(btnQueryViewDetails);
            panelQuery.Controls.Add(dgvQueryData);
            panelQuery.Controls.Add(grpQueryConditions);
            panelQuery.Dock = DockStyle.Fill;
            panelQuery.Location = new Point(3, 3);
            panelQuery.Name = "panelQuery";
            panelQuery.Padding = new Padding(10);
            panelQuery.Size = new Size(1303, 606);
            panelQuery.TabIndex = 0;
            // 
            // grpQueryConditions
            // 
            grpQueryConditions.Controls.Add(btnQueryReset);
            grpQueryConditions.Controls.Add(btnQuerySearch);
            grpQueryConditions.Controls.Add(txtQueryTestId);
            grpQueryConditions.Controls.Add(lblQueryTestId);
            grpQueryConditions.Controls.Add(txtQueryProductId);
            grpQueryConditions.Controls.Add(lblQueryProductId);
            grpQueryConditions.Controls.Add(txtQueryOperator);
            grpQueryConditions.Controls.Add(lblQueryOperator);
            grpQueryConditions.Controls.Add(dtpQueryEndDate);
            grpQueryConditions.Controls.Add(lblQueryEndDate);
            grpQueryConditions.Controls.Add(dtpQueryStartDate);
            grpQueryConditions.Controls.Add(lblQueryStartDate);
            grpQueryConditions.Dock = DockStyle.Top;
            grpQueryConditions.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            grpQueryConditions.Location = new Point(10, 10);
            grpQueryConditions.Name = "grpQueryConditions";
            grpQueryConditions.Padding = new Padding(10);
            grpQueryConditions.Size = new Size(1283, 120);
            grpQueryConditions.TabIndex = 0;
            grpQueryConditions.TabStop = false;
            grpQueryConditions.Text = "查询条件";
            // 
            // lblQueryStartDate
            // 
            lblQueryStartDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblQueryStartDate.Location = new Point(20, 35);
            lblQueryStartDate.Name = "lblQueryStartDate";
            lblQueryStartDate.Size = new Size(100, 25);
            lblQueryStartDate.TabIndex = 0;
            lblQueryStartDate.Text = "开始日期:";
            lblQueryStartDate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpQueryStartDate
            // 
            dtpQueryStartDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dtpQueryStartDate.Format = DateTimePickerFormat.Short;
            dtpQueryStartDate.Location = new Point(125, 35);
            dtpQueryStartDate.Name = "dtpQueryStartDate";
            dtpQueryStartDate.Size = new Size(150, 27);
            dtpQueryStartDate.TabIndex = 1;
            // 
            // lblQueryEndDate
            // 
            lblQueryEndDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblQueryEndDate.Location = new Point(295, 35);
            lblQueryEndDate.Name = "lblQueryEndDate";
            lblQueryEndDate.Size = new Size(100, 25);
            lblQueryEndDate.TabIndex = 2;
            lblQueryEndDate.Text = "结束日期:";
            lblQueryEndDate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpQueryEndDate
            // 
            dtpQueryEndDate.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dtpQueryEndDate.Format = DateTimePickerFormat.Short;
            dtpQueryEndDate.Location = new Point(400, 35);
            dtpQueryEndDate.Name = "dtpQueryEndDate";
            dtpQueryEndDate.Size = new Size(150, 27);
            dtpQueryEndDate.TabIndex = 3;
            // 
            // lblQueryOperator
            // 
            lblQueryOperator.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblQueryOperator.Location = new Point(570, 35);
            lblQueryOperator.Name = "lblQueryOperator";
            lblQueryOperator.Size = new Size(100, 25);
            lblQueryOperator.TabIndex = 4;
            lblQueryOperator.Text = "操作员:";
            lblQueryOperator.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtQueryOperator
            // 
            txtQueryOperator.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtQueryOperator.Location = new Point(675, 35);
            txtQueryOperator.Name = "txtQueryOperator";
            txtQueryOperator.Size = new Size(150, 27);
            txtQueryOperator.TabIndex = 5;
            // 
            // lblQueryProductId
            // 
            lblQueryProductId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblQueryProductId.Location = new Point(20, 75);
            lblQueryProductId.Name = "lblQueryProductId";
            lblQueryProductId.Size = new Size(100, 25);
            lblQueryProductId.TabIndex = 6;
            lblQueryProductId.Text = "样品编号:";
            lblQueryProductId.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtQueryProductId
            // 
            txtQueryProductId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtQueryProductId.Location = new Point(125, 75);
            txtQueryProductId.Name = "txtQueryProductId";
            txtQueryProductId.Size = new Size(150, 27);
            txtQueryProductId.TabIndex = 7;
            // 
            // lblQueryTestId
            // 
            lblQueryTestId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblQueryTestId.Location = new Point(295, 75);
            lblQueryTestId.Name = "lblQueryTestId";
            lblQueryTestId.Size = new Size(100, 25);
            lblQueryTestId.TabIndex = 8;
            lblQueryTestId.Text = "样品标识:";
            lblQueryTestId.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtQueryTestId
            // 
            txtQueryTestId.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtQueryTestId.Location = new Point(400, 75);
            txtQueryTestId.Name = "txtQueryTestId";
            txtQueryTestId.Size = new Size(150, 27);
            txtQueryTestId.TabIndex = 9;
            // 
            // btnQuerySearch
            // 
            btnQuerySearch.BackColor = Color.FromArgb(100, 150, 255);
            btnQuerySearch.FlatStyle = FlatStyle.Flat;
            btnQuerySearch.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnQuerySearch.ForeColor = Color.White;
            btnQuerySearch.Location = new Point(675, 73);
            btnQuerySearch.Name = "btnQuerySearch";
            btnQuerySearch.Size = new Size(80, 30);
            btnQuerySearch.TabIndex = 10;
            btnQuerySearch.Text = "查询";
            btnQuerySearch.UseVisualStyleBackColor = false;
            btnQuerySearch.Click += btnQuerySearch_Click;
            // 
            // btnQueryReset
            // 
            btnQueryReset.BackColor = Color.FromArgb(150, 150, 150);
            btnQueryReset.FlatStyle = FlatStyle.Flat;
            btnQueryReset.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnQueryReset.ForeColor = Color.White;
            btnQueryReset.Location = new Point(765, 73);
            btnQueryReset.Name = "btnQueryReset";
            btnQueryReset.Size = new Size(60, 30);
            btnQueryReset.TabIndex = 11;
            btnQueryReset.Text = "重置";
            btnQueryReset.UseVisualStyleBackColor = false;
            btnQueryReset.Click += btnQueryReset_Click;
            // 
            // dgvQueryData
            // 
            dgvQueryData.AllowUserToAddRows = false;
            dgvQueryData.AllowUserToDeleteRows = false;
            dgvQueryData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvQueryData.BackgroundColor = Color.White;
            dgvQueryData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvQueryData.Location = new Point(10, 140);
            dgvQueryData.Name = "dgvQueryData";
            dgvQueryData.ReadOnly = true;
            dgvQueryData.RowHeadersWidth = 51;
            dgvQueryData.RowTemplate.Height = 29;
            dgvQueryData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvQueryData.Size = new Size(1283, 410);
            dgvQueryData.TabIndex = 1;
            // 
            // btnQueryViewDetails
            // 
            btnQueryViewDetails.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnQueryViewDetails.BackColor = Color.FromArgb(100, 150, 255);
            btnQueryViewDetails.FlatStyle = FlatStyle.Flat;
            btnQueryViewDetails.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnQueryViewDetails.ForeColor = Color.White;
            btnQueryViewDetails.Location = new Point(1073, 560);
            btnQueryViewDetails.Name = "btnQueryViewDetails";
            btnQueryViewDetails.Size = new Size(100, 35);
            btnQueryViewDetails.TabIndex = 2;
            btnQueryViewDetails.Text = "查看详情";
            btnQueryViewDetails.UseVisualStyleBackColor = false;
            btnQueryViewDetails.Click += btnQueryViewDetails_Click;
            // 
            // btnQueryExportCsv
            // 
            btnQueryExportCsv.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnQueryExportCsv.BackColor = Color.FromArgb(40, 167, 69);
            btnQueryExportCsv.FlatStyle = FlatStyle.Flat;
            btnQueryExportCsv.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnQueryExportCsv.ForeColor = Color.White;
            btnQueryExportCsv.Location = new Point(1183, 560);
            btnQueryExportCsv.Name = "btnQueryExportCsv";
            btnQueryExportCsv.Size = new Size(100, 35);
            btnQueryExportCsv.TabIndex = 3;
            btnQueryExportCsv.Text = "导出CSV";
            btnQueryExportCsv.UseVisualStyleBackColor = false;
            btnQueryExportCsv.Click += btnQueryExportCsv_Click;
            // 
            // btnQuerySummaryReport
            // 
            btnQuerySummaryReport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnQuerySummaryReport.BackColor = Color.FromArgb(255, 140, 0);
            btnQuerySummaryReport.FlatStyle = FlatStyle.Flat;
            btnQuerySummaryReport.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnQuerySummaryReport.ForeColor = Color.White;
            btnQuerySummaryReport.Location = new Point(963, 560);
            btnQuerySummaryReport.Name = "btnQuerySummaryReport";
            btnQuerySummaryReport.Size = new Size(100, 35);
            btnQuerySummaryReport.TabIndex = 4;
            btnQuerySummaryReport.Text = "汇总报告";
            btnQuerySummaryReport.UseVisualStyleBackColor = false;
            btnQuerySummaryReport.Click += btnQuerySummaryReport_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 800);
            Controls.Add(tabControl1);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            MinimumSize = new Size(1024, 768);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "建筑材料不燃性试验系统";
            WindowState = FormWindowState.Maximized;
            FormClosing += MainForm_FormClosing;
            // 设置应用程序图标（如果存在）
            try
            {
                var iconPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { /* 忽略图标加载错误 */ }
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            menuStrip3.ResumeLayout(false);
            menuStrip3.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage样品试验.ResumeLayout(false);
            panelSample.ResumeLayout(false);
            panelMessageBottom.ResumeLayout(false);
            messagePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSystemMessage).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvRealTimeData).EndInit();
            panelDataDisplay.ResumeLayout(false);
            panelOperations.ResumeLayout(false);
            tabPage系统校验.ResumeLayout(false);
            panelCalibration.ResumeLayout(false);
            grpSurfaceCalibration.ResumeLayout(false);
            grpCenterCalibration.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSurfaceTemp).EndInit();
            grpSurfaceResults.ResumeLayout(false);
            grpCenterResults.ResumeLayout(false);
            tabPage试验报告.ResumeLayout(false);
            panelReport.ResumeLayout(false);
            grpReportQuery.ResumeLayout(false);
            grpReportQuery.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReportData).EndInit();
            tabPage记录查询.ResumeLayout(false);
            panelQuery.ResumeLayout(false);
            grpQueryConditions.ResumeLayout(false);
            grpQueryConditions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueryData).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem lblSystemName;
        private System.Windows.Forms.ToolStripMenuItem 导出ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 导出温度曲线ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 批量导出ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 日志ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 关于ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.MenuStrip menuStrip3;
        private System.Windows.Forms.ToolStripMenuItem 样品试验ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 系统校验ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 试验报告ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 记录查询ToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage样品试验;
        private System.Windows.Forms.TabPage tabPage系统校验;
        private System.Windows.Forms.TabPage tabPage试验报告;
        private System.Windows.Forms.TabPage tabPage记录查询;
        private System.Windows.Forms.Panel panelSample;
        private System.Windows.Forms.Panel messagePanel;
        private System.Windows.Forms.Panel chartPanel;
        private System.Windows.Forms.Panel panelDataDisplay;
        private System.Windows.Forms.Label dataTempRise;
        private System.Windows.Forms.Label lblTempRise;
        private System.Windows.Forms.Label dataCenterTemp;
        private System.Windows.Forms.Label lblCenterTemp;
        private System.Windows.Forms.Label dataSurfaceTemp;
        private System.Windows.Forms.Label lblSurfaceTemp;
        private System.Windows.Forms.Label dataTemp2;
        private System.Windows.Forms.Label lblTemp2;
        private System.Windows.Forms.Label dataTemp1;
        private System.Windows.Forms.Label lblTemp1;
        private System.Windows.Forms.Label dataTime;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Panel panelOperations;
        private System.Windows.Forms.Button btnStopHeating;
        private System.Windows.Forms.Button btnStartHeating;
        private System.Windows.Forms.Button btnParamSettings;
        private System.Windows.Forms.Button btnRecordLogs;
        private System.Windows.Forms.Button btnStopRecord;
        private System.Windows.Forms.Button btnOpenRecord;
        private System.Windows.Forms.Button btnNewTest;
        private System.Windows.Forms.Panel panelMessageBottom;
        private System.Windows.Forms.Button btnRealTimeData;
        private System.Windows.Forms.Button btnSystemMessage;
        private System.Windows.Forms.DataGridView dgvSystemMessage;
        private System.Windows.Forms.DataGridView dgvRealTimeData;
        
        // 系统校验相关控件
        private System.Windows.Forms.Panel panelCalibration;
        private System.Windows.Forms.GroupBox grpSurfaceCalibration;
        private System.Windows.Forms.GroupBox grpCenterCalibration;
        private System.Windows.Forms.Label lblCaliTemp;
        private System.Windows.Forms.Label dataCaliTemp;
        private System.Windows.Forms.Label lblSurfacePosition;
        private System.Windows.Forms.ComboBox cmbSurfacePosition;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.Button btnRecordSurface;
        private System.Windows.Forms.DataGridView dgvSurfaceTemp;
        private System.Windows.Forms.GroupBox grpSurfaceResults;
        private System.Windows.Forms.Label lblCenterPosition;
        private System.Windows.Forms.ComboBox cmbCenterPosition;
        private System.Windows.Forms.Button btnResetCenter;
        private System.Windows.Forms.Button btnRecordCenter;
        private System.Windows.Forms.Panel panelCenterChart;
        private System.Windows.Forms.GroupBox grpCenterResults;
        
        // 试验报告相关控件
        private System.Windows.Forms.Panel panelReport;
        private System.Windows.Forms.GroupBox grpReportQuery;
        private System.Windows.Forms.Label lblReportStartDate;
        private System.Windows.Forms.DateTimePicker dtpReportStartDate;
        private System.Windows.Forms.Label lblReportEndDate;
        private System.Windows.Forms.DateTimePicker dtpReportEndDate;
        private System.Windows.Forms.Label lblReportProductId;
        private System.Windows.Forms.TextBox txtReportProductId;
        private System.Windows.Forms.Label lblReportTestId;
        private System.Windows.Forms.TextBox txtReportTestId;
        private System.Windows.Forms.Button btnReportQuery;
        private System.Windows.Forms.Button btnReportReset;
        private System.Windows.Forms.DataGridView dgvReportData;
        private System.Windows.Forms.Button btnReportExportExcel;
        private System.Windows.Forms.Button btnReportExportPdf;
        
        // 记录查询相关控件
        private System.Windows.Forms.Panel panelQuery;
        private System.Windows.Forms.GroupBox grpQueryConditions;
        private System.Windows.Forms.Label lblQueryStartDate;
        private System.Windows.Forms.DateTimePicker dtpQueryStartDate;
        private System.Windows.Forms.Label lblQueryEndDate;
        private System.Windows.Forms.DateTimePicker dtpQueryEndDate;
        private System.Windows.Forms.Label lblQueryOperator;
        private System.Windows.Forms.TextBox txtQueryOperator;
        private System.Windows.Forms.Label lblQueryProductId;
        private System.Windows.Forms.TextBox txtQueryProductId;
        private System.Windows.Forms.Label lblQueryTestId;
        private System.Windows.Forms.TextBox txtQueryTestId;
        private System.Windows.Forms.Button btnQuerySearch;
        private System.Windows.Forms.Button btnQueryReset;
        private System.Windows.Forms.DataGridView dgvQueryData;
        private System.Windows.Forms.Button btnQueryViewDetails;
        private System.Windows.Forms.Button btnQueryExportCsv;
        private System.Windows.Forms.Button btnQuerySummaryReport;
    }
}