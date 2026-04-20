using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820WinForms.Forms;
using ISO11820WinForms.Services;
using ISO11820WinForms.UI;
using ISO11820WinForms.Utilities;
using Serilog;
using TestServer.Models;

namespace ISO11820WinForms
{
    public partial class LoginForm : Form
    {
        private readonly OperatorService operatorService;

        public LoginForm()
        {
            InitializeComponent();
            operatorService = new OperatorService();

            BuildIndustrialLoginView();
        }

        private void BuildIndustrialLoginView()
        {
            UiTheme.ApplyFormTheme(this);
            Text = "系统登录";
            ClientSize = new Size(1040, 620);
            MinimumSize = new Size(1040, 620);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            AcceptButton = login;

            label1.Text = "建筑材料不燃性试验系统";
            label1.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = UiTheme.Ink;
            label1.Location = new Point(48, 92);
            label1.Size = new Size(360, 96);

            var leftPanel = new Panel
            {
                Location = new Point(28, 28),
                Size = new Size(430, 564),
                BackColor = UiTheme.SurfaceStrongAlt,
                Tag = "theme-skip"
            };

            var leftEyebrow = new Label
            {
                AutoSize = false,
                Location = new Point(48, 48),
                Size = new Size(160, 24),
                ForeColor = UiTheme.Accent,
                Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "ISO 1182 / HARDWARE",
                BackColor = Color.Transparent
            };

            var leftSubtitle = new Label
            {
                AutoSize = false,
                Location = new Point(50, 210),
                Size = new Size(360, 120),
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "纯硬件模式 / 实时采集 / 试验控制\r\n以更清晰的层级显示关键操作与设备状态。",
                BackColor = Color.Transparent
            };

            var leftAccent = new Panel
            {
                Location = new Point(50, 352),
                Size = new Size(92, 6),
                BackColor = UiTheme.Accent,
                Tag = "theme-skip"
            };

            var leftFootnote = new Label
            {
                AutoSize = false,
                Location = new Point(50, 384),
                Size = new Size(312, 84),
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "请使用正确角色和访问口令进入系统。\r\n未连接硬件时显示的是占位初值，不代表真实采集结果。",
                BackColor = Color.Transparent
            };

            leftPanel.Controls.Add(leftEyebrow);
            leftPanel.Controls.Add(label1);
            leftPanel.Controls.Add(leftSubtitle);
            leftPanel.Controls.Add(leftAccent);
            leftPanel.Controls.Add(leftFootnote);

            var card = new Panel
            {
                Location = new Point(540, 88),
                Size = new Size(430, 440),
                BackColor = UiTheme.SurfaceRaised,
                Tag = "theme-skip"
            };
            leftPanel.BorderStyle = BorderStyle.FixedSingle;
            card.BorderStyle = BorderStyle.FixedSingle;

            label2.Text = "进入控制台";
            label2.Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Bold, GraphicsUnit.Point);
            label2.ForeColor = UiTheme.Ink;
            label2.Location = new Point(42, 42);
            label2.Size = new Size(220, 44);

            var helperLabel = new Label
            {
                AutoSize = false,
                Location = new Point(44, 90),
                Size = new Size(320, 44),
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "请选择登录角色，再输入访问口令。",
                BackColor = Color.Transparent
            };

            var roleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(44, 152),
                Size = new Size(100, 22),
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "角色",
                BackColor = Color.Transparent
            };

            experimenter.Location = new Point(44, 186);
            experimenter.Size = new Size(144, 40);
            admin.Location = new Point(198, 186);
            admin.Size = new Size(144, 40);
            experimenter.CheckedChanged += RoleOption_CheckedChanged;
            admin.CheckedChanged += RoleOption_CheckedChanged;

            lable1.Text = "访问口令";
            lable1.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lable1.ForeColor = UiTheme.InkMuted;
            lable1.Location = new Point(44, 258);
            lable1.Size = new Size(100, 22);

            password.Location = new Point(44, 290);
            password.Size = new Size(340, 40);
            password.Font = new Font("Consolas", 13F, FontStyle.Bold, GraphicsUnit.Point);
            password.BorderStyle = BorderStyle.FixedSingle;
            password.BackColor = UiTheme.Surface;
            password.ForeColor = UiTheme.Ink;
            password.PlaceholderText = "请输入密码";

            login.Location = new Point(44, 352);
            login.Size = new Size(340, 48);
            login.Text = "登录系统";
            UiTheme.StyleButton(login, ButtonTone.Primary);

            var tipLabel = new Label
            {
                AutoSize = false,
                Location = new Point(44, 410),
                Size = new Size(340, 48),
                ForeColor = UiTheme.InkMuted,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "管理员用于参数维护，试验员用于日常试验操作。",
                BackColor = Color.Transparent
            };

            card.Controls.Add(label2);
            card.Controls.Add(helperLabel);
            card.Controls.Add(roleLabel);
            card.Controls.Add(experimenter);
            card.Controls.Add(admin);
            card.Controls.Add(lable1);
            card.Controls.Add(password);
            card.Controls.Add(login);
            card.Controls.Add(tipLabel);

            Controls.Clear();
            Controls.Add(leftPanel);
            Controls.Add(card);

            UpdateRoleButtonStyles();
        }

        private void RoleOption_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateRoleButtonStyles();
        }

        private void UpdateRoleButtonStyles()
        {
            UiTheme.StyleChipRadioButton(experimenter, experimenter.Checked);
            UiTheme.StyleChipRadioButton(admin, admin.Checked);
        }

        private void login_Click(object sender, EventArgs e)
        {
            string pwd = password.Text;

            if (string.IsNullOrWhiteSpace(pwd))
            {
                ExceptionHandler.HandleValidationError("请输入密码。");
                return;
            }

            try
            {
                string username = admin.Checked ? "admin" : "experimenter";
                Log.Information("用户尝试登录，用户名: {Username}", username);

                Operator? user = operatorService.AuthenticateUser(username, pwd);
                if (user is not null)
                {
                    Log.Information("用户登录成功，用户名: {Username}, 用户ID: {UserId}", username, user.Userid);

                    MainForm mainForm = new(user);
                    mainForm.FormClosed += MainForm_FormClosed;
                    mainForm.Show();
                    Hide();
                }
                else
                {
                    Log.Warning("用户登录失败，密码错误，用户名: {Username}", username);
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

        private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
