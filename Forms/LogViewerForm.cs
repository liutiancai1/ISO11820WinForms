using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Serilog;

namespace ISO11820WinForms.Forms
{
    /// <summary>
    /// 日志查看窗体
    /// </summary>
    public partial class LogViewerForm : Form
    {
        private TextBox txtLogContent;
        private ComboBox cmbLogFiles;
        private Button btnRefresh;
        private Button btnOpenFolder;
        private Label lblLogFile;
        private string _logDirectory;

        public LogViewerForm()
        {
            InitializeComponent();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            LoadLogFiles();
        }

        private void InitializeComponent()
        {
            this.Text = "日志查看器";
            this.Size = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = true;

            // 标签
            lblLogFile = new Label
            {
                Text = "选择日志文件:",
                Location = new System.Drawing.Point(10, 15),
                Size = new System.Drawing.Size(100, 20)
            };

            // 日志文件下拉框
            cmbLogFiles = new ComboBox
            {
                Location = new System.Drawing.Point(110, 12),
                Size = new System.Drawing.Size(400, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbLogFiles.SelectedIndexChanged += CmbLogFiles_SelectedIndexChanged;

            // 刷新按钮
            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new System.Drawing.Point(520, 10),
                Size = new System.Drawing.Size(80, 28)
            };
            btnRefresh.Click += BtnRefresh_Click;

            // 打开文件夹按钮
            btnOpenFolder = new Button
            {
                Text = "打开日志文件夹",
                Location = new System.Drawing.Point(610, 10),
                Size = new System.Drawing.Size(120, 28)
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;

            // 日志内容文本框
            txtLogContent = new TextBox
            {
                Location = new System.Drawing.Point(10, 45),
                Size = new System.Drawing.Size(860, 500),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9),
                WordWrap = false
            };

            // 添加控件到窗体
            this.Controls.Add(lblLogFile);
            this.Controls.Add(cmbLogFiles);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnOpenFolder);
            this.Controls.Add(txtLogContent);
        }

        /// <summary>
        /// 加载日志文件列表
        /// </summary>
        private void LoadLogFiles()
        {
            try
            {
                cmbLogFiles.Items.Clear();

                if (!Directory.Exists(_logDirectory))
                {
                    txtLogContent.Text = "日志目录不存在。";
                    return;
                }

                var logFiles = Directory.GetFiles(_logDirectory, "iso11820-*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (logFiles.Count == 0)
                {
                    txtLogContent.Text = "没有找到日志文件。";
                    return;
                }

                foreach (var file in logFiles)
                {
                    cmbLogFiles.Items.Add(Path.GetFileName(file));
                }

                // 默认选择最新的日志文件
                if (cmbLogFiles.Items.Count > 0)
                {
                    cmbLogFiles.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载日志文件列表失败");
                MessageBox.Show($"加载日志文件列表失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 日志文件选择改变事件
        /// </summary>
        private void CmbLogFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbLogFiles.SelectedItem == null)
                return;

            LoadLogContent(cmbLogFiles.SelectedItem.ToString()!);
        }

        /// <summary>
        /// 加载日志内容
        /// </summary>
        private void LoadLogContent(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_logDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    txtLogContent.Text = "日志文件不存在。";
                    return;
                }

                // 读取日志文件内容
                // 使用 FileShare.ReadWrite 允许在日志文件正在被写入时读取
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    txtLogContent.Text = reader.ReadToEnd();
                }

                // 滚动到底部显示最新日志
                txtLogContent.SelectionStart = txtLogContent.Text.Length;
                txtLogContent.ScrollToCaret();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载日志内容失败: {FileName}", fileName);
                MessageBox.Show($"加载日志内容失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadLogFiles();
        }

        /// <summary>
        /// 打开日志文件夹按钮点击事件
        /// </summary>
        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(_logDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _logDirectory);
                }
                else
                {
                    MessageBox.Show("日志目录不存在。", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开日志文件夹失败");
                MessageBox.Show($"打开日志文件夹失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
