using System;
using System.Windows.Forms;
using System.Drawing;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 进度指示器辅助类
    /// 提供简单的进度显示和等待光标管理
    /// </summary>
    public class ProgressIndicator : IDisposable
    {
        private Form? _parentForm;
        private Cursor? _originalCursor;
        private string? _statusMessage;
        private bool _disposed = false;

        /// <summary>
        /// 创建进度指示器
        /// </summary>
        /// <param name="parentForm">父窗体</param>
        /// <param name="statusMessage">状态消息（可选）</param>
        public ProgressIndicator(Form parentForm, string statusMessage = "")
        {
            _parentForm = parentForm;
            _statusMessage = statusMessage;
            
            // 保存原始光标并设置等待光标
            _originalCursor = _parentForm.Cursor;
            _parentForm.Cursor = Cursors.WaitCursor;
            
            // 禁用父窗体以防止用户操作
            _parentForm.Enabled = false;
            
            // 强制刷新UI
            Application.DoEvents();
        }

        /// <summary>
        /// 更新状态消息
        /// </summary>
        /// <param name="message">新的状态消息</param>
        public void UpdateStatus(string message)
        {
            _statusMessage = message;
            Application.DoEvents();
        }

        /// <summary>
        /// 释放资源并恢复窗体状态
        /// </summary>
        public void Dispose()
        {
            if (!_disposed && _parentForm != null)
            {
                // 恢复光标
                if (_originalCursor != null)
                {
                    _parentForm.Cursor = _originalCursor;
                }
                
                // 重新启用父窗体
                _parentForm.Enabled = true;
                
                // 强制刷新UI
                Application.DoEvents();
                
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 进度对话框
    /// 用于显示长时间操作的进度
    /// </summary>
    public class ProgressDialog : Form
    {
        private ProgressBar progressBar;
        private Label lblMessage;
        private Label lblStatus;
        private Button btnCancel;
        private bool _cancellationRequested = false;

        /// <summary>
        /// 获取是否请求取消操作
        /// </summary>
        public bool CancellationRequested => _cancellationRequested;

        /// <summary>
        /// 创建进度对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">主要消息</param>
        /// <param name="allowCancel">是否允许取消</param>
        public ProgressDialog(string title, string message, bool allowCancel = false)
        {
            InitializeComponents(title, message, allowCancel);
        }

        private void InitializeComponents(string title, string message, bool allowCancel)
        {
            // 设置窗体属性
            this.Text = title;
            this.Size = new Size(450, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;

            // 创建主要消息标签
            lblMessage = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                Size = new Size(410, 30),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMessage);

            // 创建进度条
            progressBar = new ProgressBar
            {
                Location = new Point(20, 60),
                Size = new Size(410, 25),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            this.Controls.Add(progressBar);

            // 创建状态标签
            lblStatus = new Label
            {
                Text = "正在处理...",
                Location = new Point(20, 95),
                Size = new Size(410, 20),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblStatus);

            // 创建取消按钮（如果允许）
            if (allowCancel)
            {
                btnCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(350, 115),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.Click += (s, e) =>
                {
                    _cancellationRequested = true;
                    btnCancel.Enabled = false;
                    btnCancel.Text = "取消中...";
                };
                this.Controls.Add(btnCancel);
            }
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="percentage">进度百分比（0-100）</param>
        /// <param name="status">状态消息</param>
        public void UpdateProgress(int percentage, string status = "")
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() =>
                {
                    progressBar.Value = Math.Max(0, Math.Min(100, percentage));
                }));
            }
            else
            {
                progressBar.Value = Math.Max(0, Math.Min(100, percentage));
            }

            if (!string.IsNullOrEmpty(status))
            {
                UpdateStatus(status);
            }

            Application.DoEvents();
        }

        /// <summary>
        /// 更新状态消息
        /// </summary>
        /// <param name="status">状态消息</param>
        public void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() =>
                {
                    lblStatus.Text = status;
                }));
            }
            else
            {
                lblStatus.Text = status;
            }

            Application.DoEvents();
        }

        /// <summary>
        /// 设置为不确定模式（滚动条样式）
        /// </summary>
        public void SetIndeterminate()
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() =>
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                }));
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
        }

        /// <summary>
        /// 设置为确定模式（百分比样式）
        /// </summary>
        public void SetDeterminate()
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() =>
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                }));
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        /// <summary>
        /// 完成进度并关闭对话框
        /// </summary>
        public void Complete()
        {
            UpdateProgress(100, "完成");
            System.Threading.Thread.Sleep(500); // 短暂延迟以显示完成状态
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    /// <summary>
    /// 进度指示器扩展方法
    /// </summary>
    public static class ProgressExtensions
    {
        /// <summary>
        /// 显示简单的等待光标
        /// </summary>
        /// <param name="form">窗体</param>
        /// <param name="action">要执行的操作</param>
        public static void ShowWaitCursor(this Form form, Action action)
        {
            using (var progress = new ProgressIndicator(form))
            {
                try
                {
                    action();
                }
                finally
                {
                    // ProgressIndicator 会在 Dispose 时自动恢复
                }
            }
        }

        /// <summary>
        /// 显示进度对话框并执行操作
        /// </summary>
        /// <param name="form">父窗体</param>
        /// <param name="title">对话框标题</param>
        /// <param name="message">消息</param>
        /// <param name="action">要执行的操作（接收进度对话框作为参数）</param>
        /// <param name="allowCancel">是否允许取消</param>
        public static void ShowProgressDialog(this Form form, string title, string message, 
            Action<ProgressDialog> action, bool allowCancel = false)
        {
            using (var dialog = new ProgressDialog(title, message, allowCancel))
            {
                dialog.Show(form);
                
                try
                {
                    action(dialog);
                    
                    if (!dialog.CancellationRequested)
                    {
                        dialog.Complete();
                    }
                    else
                    {
                        dialog.DialogResult = DialogResult.Cancel;
                        dialog.Close();
                    }
                }
                catch (Exception)
                {
                    dialog.Close();
                    throw;
                }
            }
        }
    }
}
