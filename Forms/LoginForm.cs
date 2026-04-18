using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using System.Windows.Forms;
using System;
using ISO11820WinForms.Forms;
using Serilog;
using ISO11820WinForms.Utilities;

namespace ISO11820WinForms
{
    public partial class LoginForm : Form
    {
        private OperatorService operatorService;
        public LoginForm()
        {
            InitializeComponent();
            operatorService = new OperatorService();

            // 设置密码框回车键事件
            this.password.KeyPress += Password_KeyPress;
        }

        private void Password_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 如果按下回车键，触发登录
            if (e.KeyChar == (char)Keys.Enter)
            {
                login_Click(sender, e);
                e.Handled = true; // 阻止"叮"的提示音
            }
        }

        private void login_Click(object sender, EventArgs e)
        {
            string pwd = this.password.Text;

            // 简单验证密码是否为空
            if (string.IsNullOrWhiteSpace(pwd))
            {
                ExceptionHandler.HandleValidationError("请输入密码。");
                return;
            }

            try
            {
                // 根据选中的用户类型确定用户名
                string username = admin.Checked ? "admin" : "experimenter";
                Log.Information("用户尝试登录，用户名: {Username}", username);

                // 使用服务层方法验证用户
                var user = operatorService.AuthenticateUser(username, pwd);

                if (user != null)
                {
                    Log.Information("用户登录成功，用户名: {Username}, 用户ID: {UserId}", username, user.Userid);
                    
                    // 打开主窗体
                    MainForm mainForm = new MainForm(user);
                    mainForm.FormClosed += MainForm_FormClosed;
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    Log.Warning("用户登录失败：密码错误，用户名: {Username}", username);
                    ExceptionHandler.ShowWarning("密码错误，请重新输入。", "登录失败");
                    password.Clear();
                    password.Focus();
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "用户登录");
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 当主窗体关闭时，退出整个应用程序
            Application.Exit();
        }
    }
}