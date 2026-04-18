using System;
using System.Windows.Forms;
using Serilog;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 统一的异常处理类
    /// 提供标准化的错误处理、日志记录和用户友好的错误消息显示
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// 处理一般异常并显示用户友好的错误消息
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="userMessage">用户友好的错误消息</param>
        /// <param name="logContext">日志上下文信息</param>
        public static void Handle(Exception ex, string userMessage, string logContext = "")
        {
            // 记录详细错误到日志
            if (!string.IsNullOrEmpty(logContext))
            {
                Log.Error(ex, "{Context}: {Message}", logContext, ex.Message);
            }
            else
            {
                Log.Error(ex, "发生异常: {Message}", ex.Message);
            }

            // 显示用户友好的错误消息
            MessageBox.Show(
                $"{userMessage}\n\n详细信息: {ex.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// 处理数据库相关异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="operation">操作描述（如"保存试验数据"）</param>
        public static void HandleDatabaseException(Exception ex, string operation)
        {
            Log.Error(ex, "数据库操作失败: {Operation}", operation);

            string userMessage = $"{operation}失败。\n\n可能的原因：\n" +
                                "• 数据库连接中断\n" +
                                "• 数据格式不正确\n" +
                                "• 权限不足\n\n" +
                                "请检查数据库连接或联系系统管理员。";

            MessageBox.Show(
                userMessage,
                "数据库错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// 处理硬件通信异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="deviceName">设备名称（如"PID控制器"、"功率控制器"）</param>
        public static void HandleHardwareException(Exception ex, string deviceName)
        {
            Log.Error(ex, "硬件通信失败: {DeviceName}", deviceName);

            string userMessage = $"与{deviceName}通信失败。\n\n可能的原因：\n" +
                                "• 设备未连接或断开\n" +
                                "• 串口被其他程序占用\n" +
                                "• 设备故障\n\n" +
                                "请检查设备连接和电源状态。";

            MessageBox.Show(
                userMessage,
                "硬件通信错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// 处理数据验证异常
        /// </summary>
        /// <param name="validationMessage">验证失败的具体消息</param>
        public static void HandleValidationError(string validationMessage)
        {
            Log.Warning("数据验证失败: {Message}", validationMessage);

            MessageBox.Show(
                validationMessage,
                "数据验证",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 处理文件操作异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="operation">操作描述（如"导出Excel文件"）</param>
        /// <param name="filePath">文件路径</param>
        public static void HandleFileException(Exception ex, string operation, string filePath = "")
        {
            Log.Error(ex, "文件操作失败: {Operation}, 文件路径: {FilePath}", operation, filePath);

            string userMessage = $"{operation}失败。\n\n可能的原因：\n" +
                                "• 文件正在被其他程序使用\n" +
                                "• 磁盘空间不足\n" +
                                "• 没有写入权限\n\n" +
                                "请关闭相关文件或检查磁盘空间。";

            if (!string.IsNullOrEmpty(filePath))
            {
                userMessage += $"\n\n文件路径: {filePath}";
            }

            MessageBox.Show(
                userMessage,
                "文件操作错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// 处理配置文件异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="configKey">配置项名称</param>
        public static void HandleConfigurationException(Exception ex, string configKey)
        {
            Log.Error(ex, "配置读取失败: {ConfigKey}", configKey);

            string userMessage = $"读取配置项 '{configKey}' 失败。\n\n" +
                                "请检查 appsettings.json 文件是否存在且格式正确。";

            MessageBox.Show(
                userMessage,
                "配置错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        /// <summary>
        /// 显示信息提示
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">标题（默认为"提示"）</param>
        public static void ShowInfo(string message, string title = "提示")
        {
            Log.Information("显示信息提示: {Message}", message);

            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示警告提示
        /// </summary>
        /// <param name="message">警告消息</param>
        /// <param name="title">标题（默认为"警告"）</param>
        public static void ShowWarning(string message, string title = "警告")
        {
            Log.Warning("显示警告提示: {Message}", message);

            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 显示成功提示
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <param name="title">标题（默认为"成功"）</param>
        public static void ShowSuccess(string message, string title = "成功")
        {
            Log.Information("显示成功提示: {Message}", message);

            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题（默认为"确认"）</param>
        /// <returns>用户是否点击了"是"</returns>
        public static bool Confirm(string message, string title = "确认")
        {
            Log.Information("显示确认对话框: {Message}", message);

            DialogResult result = MessageBox.Show(
                message,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// 显示带有"是/否/取消"选项的确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题（默认为"确认"）</param>
        /// <returns>用户的选择结果</returns>
        public static DialogResult ConfirmWithCancel(string message, string title = "确认")
        {
            Log.Information("显示确认对话框（带取消）: {Message}", message);

            return MessageBox.Show(
                message,
                title,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        /// <summary>
        /// 尝试执行操作，如果失败则显示错误消息
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="errorMessage">失败时显示的错误消息</param>
        /// <param name="logContext">日志上下文</param>
        /// <returns>操作是否成功</returns>
        public static bool TryExecute(Action action, string errorMessage, string logContext = "")
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Handle(ex, errorMessage, logContext);
                return false;
            }
        }

        /// <summary>
        /// 尝试执行带返回值的操作，如果失败则显示错误消息并返回默认值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <param name="defaultValue">失败时返回的默认值</param>
        /// <param name="errorMessage">失败时显示的错误消息</param>
        /// <param name="logContext">日志上下文</param>
        /// <returns>操作结果或默认值</returns>
        public static T TryExecute<T>(Func<T> func, T defaultValue, string errorMessage, string logContext = "")
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Handle(ex, errorMessage, logContext);
                return defaultValue;
            }
        }
    }
}
