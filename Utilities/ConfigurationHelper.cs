using ISO11820WinForms.Models;
using Microsoft.Extensions.Configuration;
using TestServer.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 统一读取 appsettings.json 配置。
    /// </summary>
    public class ConfigurationHelper
    {
        private static IConfiguration? _configuration;
        private static readonly object _lock = new object();

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    lock (_lock)
                    {
                        if (_configuration == null)
                        {
                            _configuration = BuildConfiguration();
                        }
                    }
                }

                return _configuration;
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string GetDatabaseProvider()
        {
            return Configuration["Database:Provider"] ?? "Sqlite";
        }

        public static string GetSqliteDatabasePath()
        {
            var sqlitePath = Configuration["Database:SqlitePath"];
            if (string.IsNullOrWhiteSpace(sqlitePath))
            {
                sqlitePath = "Data\\ISO11820.db";
            }

            return ResolveApplicationPath(sqlitePath);
        }

        public static string GetConnectionString(string name = "ISO11820")
        {
            if (string.Equals(GetDatabaseProvider(), "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return $"Data Source={GetSqliteDatabasePath()}";
            }

            var connectionString = Configuration.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"未找到名为 '{name}' 的数据库连接字符串。");
            }

            return connectionString;
        }

        public static string GetPidPort()
        {
            return Configuration["Hardware:PidPort"] ?? "COM1";
        }

        public static string GetPowerPort()
        {
            return Configuration["Hardware:PowerPort"] ?? "COM2";
        }

        public static string GetSensorPort()
        {
            return Configuration["Hardware:SensorPort"] ?? "COM3";
        }

        public static bool AreSameSerialPort(string? leftPort, string? rightPort)
        {
            return SerialPortCoordinator.IsSamePort(leftPort, rightPort);
        }

        public static bool IsSharedHardwarePortConfigured()
        {
            return AreSameSerialPort(GetPidPort(), GetSensorPort())
                || AreSameSerialPort(GetPowerPort(), GetSensorPort());
        }

        public static int GetConstPower()
        {
            var value = Configuration["Hardware:ConstPower"];
            return int.TryParse(value, out var result) ? result : 2048;
        }

        public static int GetPidTemperature()
        {
            var value = Configuration["Hardware:PidTemperature"];
            return int.TryParse(value, out var result) ? result : 750;
        }

        public static string GetSensorProtocol()
        {
            return Configuration["Hardware:SensorProtocol"] ?? "ModbusRtu";
        }

        public static int GetSensorStationNumber()
        {
            var value = Configuration["Hardware:SensorStationNumber"];
            return int.TryParse(value, out var result) ? result : 1;
        }

        public static int GetPidStationNumber()
        {
            var value = Configuration["Hardware:PidStationNumber"];
            return int.TryParse(value, out var result) ? result : 2;
        }

        public static int GetSensorRegisterStartAddress()
        {
            var value = Configuration["Hardware:SensorRegisterStartAddress"];
            return int.TryParse(value, out var result) ? result : 1;
        }

        public static int NormalizeSensorRegisterStartAddress(string? sensorProtocol, int startAddress)
        {
            if (string.Equals(sensorProtocol, "ModbusRtu", StringComparison.OrdinalIgnoreCase) && startAddress < 0)
            {
                return 0;
            }

            return startAddress;
        }

        public static int GetSensorRegisterCount()
        {
            var value = Configuration["Hardware:SensorRegisterCount"];
            return int.TryParse(value, out var result) ? result : 8;
        }

        public static int GetSensorReadTimeoutMs()
        {
            var value = Configuration["Hardware:SensorReadTimeoutMs"];
            return int.TryParse(value, out var result) ? result : 1000;
        }

        public static int GetCalibrationChannelIndex()
        {
            var value = Configuration["Hardware:CalibrationChannelIndex"];
            return int.TryParse(value, out var result) ? result : 4;
        }

        public static string? GetValue(string key)
        {
            return Configuration[key];
        }

        public static T? GetValue<T>(string key, T defaultValue = default)
        {
            var value = Configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static ReportConfiguration GetReportConfiguration()
        {
            var config = new ReportConfiguration();
            Configuration.GetSection("Report").Bind(config);
            config.TemplateFilePath = ResolveApplicationPath(config.TemplateFilePath);
            config.SummaryTemplateFilePath = ResolveApplicationPath(config.SummaryTemplateFilePath);
            config.OutputDirectory = ResolveApplicationPath(config.OutputDirectory);
            config.TempDirectory = ResolveApplicationPath(config.TempDirectory);
            return config;
        }

        public static FlameDetectionConfiguration GetFlameDetectionConfiguration()
        {
            var config = new FlameDetectionConfiguration();
            Configuration.GetSection("FlameDetection").Bind(config);
            config.VideoOutputDirectory = ResolveApplicationPath(config.VideoOutputDirectory);
            return config;
        }

        public static FileStorageConfiguration GetFileStorageConfiguration()
        {
            var config = new FileStorageConfiguration();
            Configuration.GetSection("FileStorage").Bind(config);
            config.BaseDirectory = ResolveApplicationPath(config.BaseDirectory);
            config.CalibrationDirectory = ResolveApplicationPath(config.CalibrationDirectory);
            config.AuditLogDirectory = ResolveApplicationPath(config.AuditLogDirectory);
            config.TestDataDirectory = ResolveApplicationPath(config.TestDataDirectory);
            return config;
        }

        public static T? GetSection<T>(string sectionName) where T : class, new()
        {
            var section = Configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                return null;
            }

            var config = new T();
            section.Bind(config);
            return config;
        }

        public static string ResolveApplicationPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
        }
    }
}
