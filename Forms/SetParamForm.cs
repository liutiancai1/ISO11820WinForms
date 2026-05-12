using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820WinForms.UI;

namespace ISO11820WinForms.Forms
{
    /// <summary>
    /// 参数设置对话框
    /// 用于配置设备参数（设备编号、名称、检定日期、恒功率值）
    /// </summary>
    public partial class SetParamForm : Form
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public string ApparatusId { get; private set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string ApparatusName { get; private set; } = string.Empty;

        /// <summary>
        /// 检定日期（起始）
        /// </summary>
        public DateTime CheckDateFrom { get; private set; } = DateTime.Now;

        /// <summary>
        /// 检定日期（终止）
        /// </summary>
        public DateTime CheckDateTo { get; private set; } = DateTime.Now.AddYears(1);

        /// <summary>
        /// PID 端口
        /// </summary>
        public string PidPort { get; private set; } = string.Empty;

        /// <summary>
        /// 恒功率值
        /// </summary>
        public int ConstPower { get; private set; } = 0;

        public SetParamForm()
        {
            InitializeComponent();
            ApplyDialogTheme();
        }

        private void ApplyDialogTheme()
        {
            UiTheme.ApplyFormTheme(this, dialog: true);
            UiTheme.ApplyToControlTree(this);

            Text = "参数设置";
            BackColor = UiTheme.AppBackground;
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            UiTheme.StyleButton(btnOK, ButtonTone.Primary);
            UiTheme.StyleButton(btnCancel, ButtonTone.Neutral);

            grpApparatus.BackColor = UiTheme.SurfaceRaised;
            grpCommunication.BackColor = UiTheme.SurfaceRaised;
        }

        /// <summary>
        /// 构造函数（带初始值）
        /// </summary>
        public SetParamForm(string apparatusId, string apparatusName, DateTime checkDateFrom, DateTime checkDateTo, string pidPort, int constPower) : this()
        {
            // 设置初始值
            txtApparatusId.Text = apparatusId;
            txtApparatusName.Text = apparatusName;
            dtpCheckDateFrom.Value = checkDateFrom;
            dtpCheckDateTo.Value = checkDateTo;
            txtPidPort.Text = pidPort;
            numConstPower.Value = constPower;
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void btnOK_Click(object sender, EventArgs e)
        {
            // 验证输入数据
            if (!ValidateInput())
            {
                return;
            }

            // 保存数据
            SaveData();

            // 设置对话框结果为OK
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// 验证输入数据
        /// </summary>
        private bool ValidateInput()
        {
            // 验证设备编号
            if (string.IsNullOrWhiteSpace(txtApparatusId.Text))
            {
                MessageBox.Show("请输入设备编号！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtApparatusId.Focus();
                return false;
            }

            // 验证设备名称
            if (string.IsNullOrWhiteSpace(txtApparatusName.Text))
            {
                MessageBox.Show("请输入设备名称！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtApparatusName.Focus();
                return false;
            }

            // 验证检定日期范围
            if (dtpCheckDateTo.Value < dtpCheckDateFrom.Value)
            {
                MessageBox.Show("检定日期终止时间不能早于起始时间！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpCheckDateTo.Focus();
                return false;
            }

            // 验证 PID 端口
            if (string.IsNullOrWhiteSpace(txtPidPort.Text))
            {
                MessageBox.Show("请输入 PID 端口！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPidPort.Focus();
                return false;
            }

            // 验证恒功率值
            if (numConstPower.Value < 0 || numConstPower.Value > 25600)
            {
                MessageBox.Show("恒功率值必须在 0-25600 范围内！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numConstPower.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 保存数据到属性
        /// </summary>
        private void SaveData()
        {
            ApparatusId = txtApparatusId.Text.Trim();
            ApparatusName = txtApparatusName.Text.Trim();
            CheckDateFrom = dtpCheckDateFrom.Value;
            CheckDateTo = dtpCheckDateTo.Value;
            PidPort = txtPidPort.Text.Trim();
            ConstPower = (int)numConstPower.Value;
        }
    }
}
