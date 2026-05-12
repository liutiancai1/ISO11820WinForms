using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ISO11820WinForms.UI;
using Serilog;

namespace ISO11820WinForms.Forms
{
    public partial class LogViewerForm : Form
    {
        private TextBox txtLogContent = null!;
        private ComboBox cmbLogFiles = null!;
        private Button btnRefresh = null!;
        private Button btnOpenFolder = null!;
        private Label lblLogFile = null!;
        private readonly string _logDirectory;

        public LogViewerForm()
        {
            InitializeComponent();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            ApplyViewerTheme();
            LoadLogFiles();
        }

        private void InitializeComponent()
        {
            Text = "日志查看器";
            Size = new Size(980, 660);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;

            lblLogFile = new Label
            {
                Text = "日志文件",
                Location = new Point(18, 18),
                Size = new Size(90, 24)
            };

            cmbLogFiles = new ComboBox
            {
                Location = new Point(108, 15),
                Size = new Size(420, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cmbLogFiles.SelectedIndexChanged += CmbLogFiles_SelectedIndexChanged;

            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(548, 14),
                Size = new Size(88, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnOpenFolder = new Button
            {
                Text = "打开日志目录",
                Location = new Point(648, 14),
                Size = new Size(128, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;

            txtLogContent = new TextBox
            {
                Location = new Point(18, 58),
                Size = new Size(928, 546),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(lblLogFile);
            Controls.Add(cmbLogFiles);
            Controls.Add(btnRefresh);
            Controls.Add(btnOpenFolder);
            Controls.Add(txtLogContent);
        }

        private void ApplyViewerTheme()
        {
            UiTheme.ApplyFormTheme(this);
            UiTheme.ApplyToControlTree(this);

            BackColor = UiTheme.AppBackground;
            UiTheme.StyleButton(btnRefresh, ButtonTone.Primary, compact: true);
            UiTheme.StyleButton(btnOpenFolder, ButtonTone.Neutral, compact: true);
            txtLogContent.BackColor = UiTheme.Surface;
            txtLogContent.ForeColor = UiTheme.Ink;
            txtLogContent.BorderStyle = BorderStyle.FixedSingle;
        }

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
                    .OrderByDescending(File.GetLastWriteTime)
                    .ToList();

                if (logFiles.Count == 0)
                {
                    txtLogContent.Text = "没有找到日志文件。";
                    return;
                }

                foreach (string file in logFiles)
                {
                    cmbLogFiles.Items.Add(Path.GetFileName(file));
                }

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

        private void CmbLogFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbLogFiles.SelectedItem is string fileName)
            {
                LoadLogContent(fileName);
            }
        }

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

                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(fileStream);
                txtLogContent.Text = reader.ReadToEnd();
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

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadLogFiles();
        }

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
                Log.Error(ex, "打开日志目录失败");
                MessageBox.Show($"打开日志目录失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
