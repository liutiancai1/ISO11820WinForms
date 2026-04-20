using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using TestServer.Models;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Global;
using Serilog;
using ISO11820WinForms.Utilities;
using ISO11820WinForms.UI;

namespace ISO11820WinForms.Forms
{
    public partial class NewTestForm : Form
    {
        private readonly TestmasterService _testmasterService;

        public NewTestForm()
        {
            InitializeComponent();

            _testmasterService = new TestmasterService();

            // 设置按钮事件
            button1.Click += btnOK_Click;
            button2.Click += btnCancel_Click;

            // 初始化默认值
            InitializeDefaultValues();
            ApplyDialogTheme();
        }

        private void ApplyDialogTheme()
        {
            UiTheme.ApplyFormTheme(this, dialog: true);
            UiTheme.ApplyToControlTree(this);

            Text = "新建试验";
            BackColor = UiTheme.AppBackground;
            button1.Text = "创建试验";
            button2.Text = "取消";
            AcceptButton = button1;
            CancelButton = button2;

            UiTheme.StyleButton(button1, ButtonTone.Primary);
            UiTheme.StyleButton(button2, ButtonTone.Neutral);

            textBox12.Enabled = true;
            textBox12.ReadOnly = true;
            textBox12.BackColor = Color.FromArgb(240, 236, 229);
        }

        /*
         * 功能: 初始化表单默认值
         */
        private void InitializeDefaultValues()
        {
            ExceptionHandler.TryExecute(() =>
            {
                // 设置试验日期为当前日期
                dateTimePicker1.Value = DateTime.Now;
                
                // 从全局上下文获取设备信息
                var apparatus = SystemContext.Current.Global.DictApparatus.Values.FirstOrDefault();
                if (apparatus != null)
                {
                    textBox14.Text = apparatus.Apparatusid.ToString();  // 设备编号
                    textBox15.Text = apparatus.Apparatusname ?? "不燃性试验装置";     // 设备名称
                    textBox16.Text = apparatus.Checkdatet.ToString("yyyy-MM-dd"); // 检定日期
                    textBox17.Text = (apparatus.Constpower ?? 6).ToString();    // 恒功率值
                }
            }, "初始化表单失败", "InitializeDefaultValues");
        }

        /*
         * 功能: 确定按钮点击事件 - Form层只负责UI交互
         * 性能优化：使用异步方法避免阻塞UI线程
         */
        private async void btnOK_Click(object? sender, EventArgs e)
        {
            try
            {
                Log.Information("用户提交新建试验表单");
                
                // 1. 调用服务层验证数据（业务逻辑在Service层）
                var validationResult = _testmasterService.ValidateTestData(
                    textBox1.Text, textBox2.Text,
                    textBox3.Text, textBox4.Text, textBox5.Text,
                    textBox7.Text, textBox8.Text, textBox9.Text,
                    textBox13.Text);

                if (!validationResult.isValid)
                {
                    ExceptionHandler.HandleValidationError(validationResult.errorMessage);
                    return;
                }

                // 2. 构建产品信息对象（Form层只负责数据收集）
                var productmaster = new Productmaster
                {
                    Productid = textBox3.Text.Trim(),
                    Productname = textBox5.Text.Trim(),
                    Specific = textBox6.Text.Trim(),
                    Height = float.Parse(textBox7.Text),
                    Diameter = float.Parse(textBox8.Text),
                    Flag = "0"
                };

                // 3. 构建试验信息对象（只初始化用户输入的必要字段，参考Web版做法）
                var testmaster = new Testmaster
                {
                    Productid = textBox3.Text.Trim(),
                    Testid = textBox4.Text.Trim(),
                    Ambtemp = float.Parse(textBox1.Text),
                    Ambhumi = float.Parse(textBox2.Text),
                    Testdate = dateTimePicker1.Value,
                    According = textBox12.Text.Trim(),
                    Operator = textBox13.Text.Trim(),
                    Apparatusid = textBox14.Text.Trim(),
                    Apparatusname = textBox15.Text.Trim(),
                    Apparatuschkdate = DateTime.Parse(textBox16.Text),
                    Constpower = int.Parse(textBox17.Text),
                    Rptno = textBox3.Text.Trim(),  // 报告编号自动设置为样品编号
                    Preweight = double.Parse(textBox9.Text),  // 试样初始质量
                    Phenocode = "0000",  // 默认现象编码
                    Memo = textBox10.Text.Trim(),  // 试验备注
                    Flag = "00000000"  // 标志位：第1位-试验是否完成, 第2位-是否出结论
                };
                // 说明：其他计算字段（Totaltesttime、Postweight、Lostweight等）
                // 不在此初始化，由SQL Server使用DEFAULT值处理

                Log.Information("创建新试验，样品编号: {ProductId}, 样品标识: {TestId}, 操作员: {Operator}", 
                    testmaster.Productid, testmaster.Testid, testmaster.Operator);

                // 4. 调用服务层执行业务逻辑（标准分层架构）
                // 性能优化：使用异步方法避免阻塞UI线程
                var result = await _testmasterService.CreateNewTestAsync(productmaster, testmaster);

                if (!result.success)
                {
                    Log.Error("创建试验失败: {ErrorMessage}", result.message);
                    ExceptionHandler.ShowWarning(result.message, "创建试验失败");
                    return;
                }

                Log.Information("创建试验成功: {Message}", result.message);
                
                // 5. 显示成功消息（UI层职责）
                ExceptionHandler.ShowSuccess(result.message);

                // 6. 返回成功结果（图表重置在MainForm的btnNewTest_Click中完成）
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleDatabaseException(ex, "创建试验");
            }
        }

        /*
         * 功能: 取消按钮点击事件
         */
        private void btnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void NewTestForm_Load(object sender, EventArgs e)
        {
            // 空实现，保留以便未来扩展
        }
    }
}
