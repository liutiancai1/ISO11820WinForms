using ISO11820WinForms.Global;
using ISO11820WinForms.Utilities;
using Serilog;
using Serilog.Events;

namespace ISO11820WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 初始化 Serilog 日志系统
            InitializeLogger();

            try
            {
                Log.Information("应用程序启动");

                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();

                // 确保 Resources 文件夹存在
                ResourceHelper.EnsureResourcesFolderExists();

                // 检查数据库连接
                Log.Information("检查数据库连接");
                if (!DatabaseHelper.CheckConnectionWithPrompt(false))
                {
                    Log.Error("数据库连接失败，应用程序无法启动");
                    return;
                }

                // 初始化系统上下文
                try
                {
                    SystemContext.Current.Init();
                    Log.Information("系统上下文初始化成功");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "系统初始化失败");
                    ExceptionHandler.Handle(ex, "系统初始化失败，应用程序无法启动", "Program.Main");
                    return;
                }

                // 启动登录窗体
                Log.Information("启动登录窗体");
                Application.Run(new LoginForm());

                // 应用程序退出时清理资源
                Log.Information("应用程序正在关闭");
                SystemContext.Current.Cleanup();
                Log.Information("应用程序已关闭");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序发生未处理的异常");
                MessageBox.Show($"应用程序发生严重错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 确保日志被刷新到磁盘
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// 初始化 Serilog 日志系统
        /// </summary>
        private static void InitializeLogger()
        {
            // 获取日志文件路径
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "iso11820-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30,  // 保留最近30天的日志
                    fileSizeLimitBytes: 10 * 1024 * 1024,  // 单个文件最大10MB
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            Log.Information("日志系统初始化完成，日志目录: {LogDirectory}", logDirectory);
        }
    }
}