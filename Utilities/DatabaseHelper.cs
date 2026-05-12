using ISO11820WinForms.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TestServer.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 数据库连接与重试辅助。
    /// </summary>
    public static class DatabaseHelper
    {
        private const int DefaultMaxRetries = 3;
        private const int DefaultRetryDelayMs = 1000;

        public static bool TestConnection()
        {
            try
            {
                using var context = new ISO11820DbContext();
                return context.Database.CanConnect();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库连接测试失败");
                return false;
            }
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                await using var context = new ISO11820DbContext();
                return await context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库连接测试失败");
                return false;
            }
        }

        public static (bool isConnected, string message) GetConnectionStatus()
        {
            try
            {
                using var context = new ISO11820DbContext();
                if (!context.Database.CanConnect())
                {
                    return (false, "无法连接到 SQLite 数据库。");
                }

                var databasePath = ConfigurationHelper.GetSqliteDatabasePath();
                return (true, $"已连接到 SQLite 数据库: {databasePath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取数据库连接状态失败");
                return (false, $"数据库连接错误: {ex.Message}");
            }
        }

        public static T ExecuteWithRetry<T>(Func<T> operation, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    Log.Warning(ex, "数据库操作失败，准备重试 ({Attempt}/{MaxRetries})", attempt + 1, maxRetries);
                    Thread.Sleep(retryDelayMs);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Log.Error(ex, "数据库操作失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("数据库操作失败。");
        }

        public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    Log.Warning(ex, "数据库异步操作失败，准备重试 ({Attempt}/{MaxRetries})", attempt + 1, maxRetries);
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Log.Error(ex, "数据库异步操作失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("数据库操作失败。");
        }

        public static void ExecuteWithRetry(Action operation, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
        {
            ExecuteWithRetry(() =>
            {
                operation();
                return true;
            }, maxRetries, retryDelayMs);
        }

        public static async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, maxRetries, retryDelayMs);
        }

        public static bool CheckConnectionWithPrompt(bool showMessageOnSuccess = false)
        {
            var (isConnected, message) = GetConnectionStatus();
            if (isConnected)
            {
                Log.Information("数据库连接检查成功: {Message}", message);
                if (showMessageOnSuccess)
                {
                    ExceptionHandler.ShowSuccess(message, "数据库连接");
                }

                return true;
            }

            Log.Error("数据库连接检查失败: {Message}", message);
            ExceptionHandler.ShowWarning(
                $"{message}\n\n请检查：\n" +
                "• SQLite 数据库文件路径是否有效\n" +
                "• 程序是否有目录写入权限\n" +
                "• 数据库文件是否被其他程序占用",
                "数据库连接失败");
            return false;
        }

        public static async Task<bool> CheckConnectionWithPromptAsync(bool showMessageOnSuccess = false)
        {
            var isConnected = await TestConnectionAsync();
            if (isConnected)
            {
                var (_, message) = GetConnectionStatus();
                Log.Information("数据库连接检查成功: {Message}", message);
                if (showMessageOnSuccess)
                {
                    ExceptionHandler.ShowSuccess(message, "数据库连接");
                }

                return true;
            }

            Log.Error("数据库连接检查失败");
            ExceptionHandler.ShowWarning(
                "无法连接到 SQLite 数据库。\n\n请检查：\n" +
                "• SQLite 数据库文件路径是否有效\n" +
                "• 程序是否有目录写入权限\n" +
                "• 数据库文件是否被其他程序占用",
                "数据库连接失败");
            return false;
        }

        public static bool EnsureDatabaseCreated()
        {
            try
            {
                var databasePath = ConfigurationHelper.GetSqliteDatabasePath();
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var context = new ISO11820DbContext();
                var created = context.Database.EnsureCreated();
                SeedDefaultData(context);

                if (created)
                {
                    Log.Information("SQLite 数据库已创建: {DatabasePath}", databasePath);
                }
                else
                {
                    Log.Information("SQLite 数据库已存在: {DatabasePath}", databasePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "确保 SQLite 数据库创建失败");
                return false;
            }
        }

        public static async Task<bool> EnsureDatabaseCreatedAsync()
        {
            try
            {
                var databasePath = ConfigurationHelper.GetSqliteDatabasePath();
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using var context = new ISO11820DbContext();
                var created = await context.Database.EnsureCreatedAsync();
                SeedDefaultData(context);

                if (created)
                {
                    Log.Information("SQLite 数据库已创建: {DatabasePath}", databasePath);
                }
                else
                {
                    Log.Information("SQLite 数据库已存在: {DatabasePath}", databasePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "确保 SQLite 数据库创建失败");
                return false;
            }
        }

        private static void SeedDefaultData(ISO11820DbContext context)
        {
            EnsureOperatorTable(context);
            SeedDefaultOperators(context);
            SeedDefaultApparatus(context);
            SeedDefaultSensors(context);
            context.SaveChanges();
        }

        private static void EnsureOperatorTable(ISO11820DbContext context)
        {
            context.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS operators (" +
                "userid TEXT NOT NULL, " +
                "username TEXT NOT NULL, " +
                "pwd TEXT NOT NULL, " +
                "usertype TEXT NOT NULL);");
        }

        private static void SeedDefaultOperators(ISO11820DbContext context)
        {
            context.Database.ExecuteSqlRaw(
                "INSERT INTO operators (userid, username, pwd, usertype) " +
                "SELECT '1', 'admin', '123456', 'admin' " +
                "WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');");

            context.Database.ExecuteSqlRaw(
                "INSERT INTO operators (userid, username, pwd, usertype) " +
                "SELECT '2', 'experimenter', '123456', 'operator' " +
                "WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');");
        }

        private static void SeedDefaultApparatus(ISO11820DbContext context)
        {
            if (context.Apparatuses.Any(a => a.Apparatusid == 0))
            {
                return;
            }

            context.Apparatuses.Add(new Apparatus
            {
                Apparatusid = 0,
                Innernumber = "FURNACE-01",
                Apparatusname = "一号试验炉",
                Checkdatef = DateTime.Today,
                Checkdatet = DateTime.Today.AddYears(1),
                Pidport = ConfigurationHelper.GetPidPort(),
                Powerport = ConfigurationHelper.GetPowerPort(),
                Constpower = ConfigurationHelper.GetConstPower()
            });
        }

        private static void SeedDefaultSensors(ISO11820DbContext context)
        {
            for (int sensorId = 0; sensorId <= 16; sensorId++)
            {
                if (context.Sensors.Any(s => s.Sensorid == sensorId))
                {
                    continue;
                }

                context.Sensors.Add(CreateDefaultSensor(sensorId));
            }
        }

        private static Sensor CreateDefaultSensor(int sensorId)
        {
            var displayName = sensorId switch
            {
                0 => "炉温1",
                1 => "炉温2",
                2 => "表面温度",
                3 => "中心温度",
                16 => "校准温度",
                _ => $"备用通道{sensorId + 1}"
            };

            return new Sensor
            {
                Sensorid = sensorId,
                Sensorname = $"Sensor{sensorId}",
                Dispname = displayName,
                Sensorgroup = sensorId == 16 ? "校准" : "采集",
                Unit = "℃",
                Discription = displayName,
                Flag = "启用",
                Signalzero = 0,
                Signalspan = 0,
                Outputzero = 0,
                Outputspan = 1000,
                Outputvalue = 0,
                Inputvalue = 0,
                Signaltype = (byte)SignalType.Digital
            };
        }
    }
}
