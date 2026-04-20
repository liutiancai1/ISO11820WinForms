using Microsoft.Extensions.Configuration;
using ISO11820WinForms.Models;
using TestServer.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 配置文件读取辅助类
    /// 提供对 appsettings.json 配置文件的访问
    /// </summary>
    public class ConfigurationHelper
    {
        private static IConfiguration? _configuration;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取配置实例（单例模式）
        /// </summary>
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

        /// <summary>
        /// 构建配置对象
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        public static string GetConnectionString(string name = "ISO11820")
        {
            var connectionString = Configuration.GetConnectionString(name);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"未找到名为 '{name}' 的数据库连接字符串");
            }
            return connectionString;
        }

        /// <summary>
        /// 获取硬件配置 - PID 端口
        /// </summary>
        public static string GetPidPort()
        {
            return Configuration["Hardware:PidPort"] ?? "COM1";
        }

        /// <summary>
        /// 获取硬件配置 - 功率端口
        /// </summary>
        public static string GetPowerPort()
        {
            return Configuration["Hardware:PowerPort"] ?? "COM2";
        }

        /// <summary>
        /// 获取硬件配置 - 传感器端口
        /// </summary>
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

        /// <summary>
        /// 获取硬件配置 - 恒功率值
        /// </summary>
        public static int GetConstPower()
        {
            var value = Configuration["Hardware:ConstPower"];
            return int.TryParse(value, out var result) ? result : 2048;
        }

        /// <summary>
        /// 获取硬件配置 - PID 温度设置
        /// </summary>
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

        /// <summary>
        /// 获取配置值（通用方法）
        /// </summary>
        public static string? GetValue(string key)
        {
            return Configuration[key];
        }

        /// <summary>
        /// 获取配置值并转换为指定类型
        /// </summary>
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

        /// <summary>
        /// 获取报告配置
        /// </summary>
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

        /// <summary>
        /// 获取火焰检测配置
        /// </summary>
        public static FlameDetectionConfiguration GetFlameDetectionConfiguration()
        {
            var config = new FlameDetectionConfiguration();
            Configuration.GetSection("FlameDetection").Bind(config);
            config.VideoOutputDirectory = ResolveApplicationPath(config.VideoOutputDirectory);
            return config;
        }

        /// <summary>
        /// 获取文件存储配置
        /// </summary>
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

        /// <summary>
        /// 获取指定配置节并绑定到类型 T
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>配置对象，如果配置节不存在则返回 null</returns>
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

        /// <summary>
        /// 将配置中的相对路径解析为应用程序根目录下的绝对路径
        /// </summary>
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
