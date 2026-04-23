using ISO11820WinForms.Global;
using ISO11820WinForms.Utilities;
using Serilog;
using Serilog.Events;

namespace ISO11820WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            InitializeLogger();

            try
            {
                Log.Information("应用程序启动");

                ApplicationConfiguration.Initialize();
                ResourceHelper.EnsureResourcesFolderExists();

                Log.Information("确保数据库已创建");
                if (!DatabaseHelper.EnsureDatabaseCreated())
                {
                    Log.Error("数据库初始化失败，应用程序无法启动");
                    return;
                }

                Log.Information("检查数据库连接");
                if (!DatabaseHelper.CheckConnectionWithPrompt(false))
                {
                    Log.Error("数据库连接失败，应用程序无法启动");
                    return;
                }

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

                Log.Information("启动登录窗体");
                Application.Run(new LoginForm());

                Log.Information("应用程序正在关闭");
                SystemContext.Current.Cleanup();
                Log.Information("应用程序已关闭");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序发生未处理异常");
                MessageBox.Show($"应用程序发生严重错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void InitializeLogger()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

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
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            Log.Information("日志系统初始化完成，日志目录: {LogDirectory}", logDirectory);
        }
    }
}
