using System;
using System.Windows.Forms;

namespace ISO11820WinForms.Forms
{
    /// <summary>
    /// 试验记录对话框
    /// 用于记录试验现象数据（现象编码、火焰时间、持续时间、残余质量）
    /// </summary>
    public partial class TestPhenoForm : Form
    {
        /// <summary>
        /// 现象编码（默认"0000"表示无现象）
        /// </summary>
        public string PhenoCode { get; private set; } = "0000";

        /// <summary>
        /// 火焰发生时间（秒）
        /// </summary>
        public int FlameTime { get; private set; } = 0;

        /// <summary>
        /// 火焰持续时间（秒）
        /// </summary>
        public int FlameDuration { get; private set; } = 0;

        /// <summary>
        /// 残余质量（克）
        /// </summary>
        public double PostWeight { get; private set; } = 0.0;

        public TestPhenoForm()
        {
            InitializeComponent();
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
            // 验证残余质量
            if (string.IsNullOrWhiteSpace(txtPostWeight.Text))
            {
                MessageBox.Show("请输入残余质量！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPostWeight.Focus();
                return false;
            }

            if (!double.TryParse(txtPostWeight.Text, out double postWeight) || postWeight < 0)
            {
                MessageBox.Show("残余质量必须是非负数！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPostWeight.Focus();
                return false;
            }

            // 如果勾选了持续火焰，验证火焰时间和持续时间
            if (chkFlame.Checked)
            {
                if (!int.TryParse(numFlameTime.Value.ToString(), out int flameTime) || flameTime < 0)
                {
                    MessageBox.Show("火焰发生时间必须是非负整数！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numFlameTime.Focus();
                    return false;
                }

                if (!int.TryParse(numFlameDuration.Value.ToString(), out int flameDuration) || flameDuration < 0)
                {
                    MessageBox.Show("火焰持续时间必须是非负整数！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numFlameDuration.Focus();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 保存数据到属性
        /// </summary>
        private void SaveData()
        {
            // 保存现象编码（如果勾选了持续火焰，设置为"0001"，否则为"0000"）
            PhenoCode = chkFlame.Checked ? "0001" : "0000";

            // 保存火焰时间和持续时间
            if (chkFlame.Checked)
            {
                FlameTime = (int)numFlameTime.Value;
                FlameDuration = (int)numFlameDuration.Value;
            }
            else
            {
                FlameTime = 0;
                FlameDuration = 0;
            }

            // 保存残余质量
            PostWeight = double.Parse(txtPostWeight.Text);
        }

        /// <summary>
        /// 持续火焰复选框状态改变事件
        /// </summary>
        private void chkFlame_CheckedChanged(object sender, EventArgs e)
        {
            // 根据复选框状态启用或禁用火焰时间和持续时间输入
            numFlameTime.Enabled = chkFlame.Checked;
            numFlameDuration.Enabled = chkFlame.Checked;
        }
    }
}
