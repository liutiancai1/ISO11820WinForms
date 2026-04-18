using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using TestServer.Models;
using Serilog;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 配置管理服务
    /// 负责读取配置和管理目录
    /// </summary>
    public class ConfigurationService
    {
        private readonly ILogger _logger;
        private static ConfigurationService? _instance;
        private static readonly object _lock = new object();

        // 配置对象
        public ReportConfiguration ReportConfig { get; private set; }
        public FlameDetectionConfiguration FlameDetectionConfig { get; private set; }
        public FileStorageConfiguration FileStorageConfig { get; private set; }

        private ConfigurationService()
        {
            _logger = Log.ForContext<ConfigurationService>();
            
            // 加载配置
            ReportConfig = ConfigurationHelper.GetReportConfiguration();
            FlameDetectionConfig = ConfigurationHelper.GetFlameDetectionConfiguration();
            FileStorageConfig = ConfigurationHelper.GetFileStorageConfiguration();
        }

        /// <summary>
        /// 获取配置服务单例
        /// </summary>
        public static ConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化所有必需的目录
        /// </summary>
        public bool InitializeDirectories()
        {
            try
            {
                _logger.Information("开始初始化目录结构");

                // 初始化报告相关目录
                if (!EnsureDirectoryExists(ReportConfig.OutputDirectory, "报告输出目录"))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(ReportConfig.TempDirectory))
                {
                    EnsureDirectoryExists(ReportConfig.TempDirectory, "临时文件目录");
                }

                // 初始化火焰检测相关目录
                if (FlameDetectionConfig.EnableFlameDetection)
                {
                    if (!EnsureDirectoryExists(FlameDetectionConfig.VideoOutputDirectory, "火焰视频输出目录"))
                    {
                        _logger.Warning("火焰视频输出目录创建失败，火焰检测功能可能受影响");
                    }
                }

                // 初始化文件存储相关目录
                if (FileStorageConfig.EnableFileStorage)
                {
                    if (!EnsureDirectoryExists(FileStorageConfig.BaseDirectory, "基础存储目录"))
                    {
                        return false;
                    }

                    EnsureDirectoryExists(FileStorageConfig.CalibrationDirectory, "校验记录目录");
                    EnsureDirectoryExists(FileStorageConfig.AuditLogDirectory, "审计日志目录");
                    EnsureDirectoryExists(FileStorageConfig.TestDataDirectory, "试验数据目录");
                }

                _logger.Information("目录结构初始化完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化目录结构失败");
                return false;
            }
        }

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        private bool EnsureDirectoryExists(string directory, string description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    _logger.Warning("{Description} 路径为空", description);
                    return false;
                }

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.Information("已创建{Description}: {Directory}", description, directory);
                }
                else
                {
                    _logger.Debug("{Description}已存在: {Directory}", description, directory);
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error(ex, "创建{Description}失败：权限不足 - {Directory}", description, directory);
                
                // 尝试使用默认路径作为回退
                var fallbackPath = GetFallbackDirectory(directory, description);
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    _logger.Information("尝试使用回退路径: {FallbackPath}", fallbackPath);
                    return EnsureDirectoryExists(fallbackPath, $"{description}(回退)");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "创建{Description}失败 - {Directory}", description, directory);
                
                // 尝试使用默认路径作为回退
                var fallbackPath = GetFallbackDirectory(directory, description);
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    _logger.Information("尝试使用回退路径: {FallbackPath}", fallbackPath);
                    return EnsureDirectoryExists(fallbackPath, $"{description}(回退)");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 获取回退目录路径
        /// </summary>
        private string GetFallbackDirectory(string originalPath, string description)
        {
            try
            {
                // 使用应用程序目录下的子目录作为回退
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var folderName = Path.GetFileName(originalPath);
                
                if (string.IsNullOrEmpty(folderName))
                {
                    // 如果无法获取文件夹名称，使用描述生成一个
                    folderName = description.Replace(" ", "").Replace("目录", "");
                }

                var fallbackPath = Path.Combine(appDirectory, "Data", folderName);
                _logger.Information("生成回退路径: {FallbackPath}", fallbackPath);
                
                return fallbackPath;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "生成回退路径失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        public bool ValidateConfiguration()
        {
            try
            {
                _logger.Information("开始验证配置");

                bool isValid = true;

                // 验证报告配置
                if (string.IsNullOrWhiteSpace(ReportConfig.TemplateFilePath))
                {
                    _logger.Warning("报告模板文件路径未配置");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(ReportConfig.OutputDirectory))
                {
                    _logger.Warning("报告输出目录未配置");
                    isValid = false;
                }

                // 验证火焰检测配置
                if (FlameDetectionConfig.EnableFlameDetection)
                {
                    if (FlameDetectionConfig.CameraIndex < 0)
                    {
                        _logger.Warning("摄像头索引无效: {CameraIndex}", FlameDetectionConfig.CameraIndex);
                        isValid = false;
                    }

                    if (FlameDetectionConfig.MaxBufferSize <= 0)
                    {
                        _logger.Warning("最大缓存大小无效: {MaxBufferSize}", FlameDetectionConfig.MaxBufferSize);
                        isValid = false;
                    }
                }

                // 验证文件存储配置
                if (FileStorageConfig.EnableFileStorage)
                {
                    if (string.IsNullOrWhiteSpace(FileStorageConfig.BaseDirectory))
                    {
                        _logger.Warning("基础存储目录未配置");
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    _logger.Information("配置验证通过");
                }
                else
                {
                    _logger.Warning("配置验证失败，存在无效配置项");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证配置时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfiguration()
        {
            try
            {
                _logger.Information("重新加载配置");
                
                ReportConfig = ConfigurationHelper.GetReportConfiguration();
                FlameDetectionConfig = ConfigurationHelper.GetFlameDetectionConfiguration();
                FileStorageConfig = ConfigurationHelper.GetFileStorageConfiguration();
                
                _logger.Information("配置重新加载完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "重新加载配置失败");
            }
        }

        /// <summary>
        /// 获取配置摘要信息
        /// </summary>
        public string GetConfigurationSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 配置摘要 ===");
            summary.AppendLine($"报告输出目录: {ReportConfig.OutputDirectory}");
            summary.AppendLine($"报告模板路径: {ReportConfig.TemplateFilePath}");
            summary.AppendLine($"启用PDF导出: {ReportConfig.EnablePdfExport}");
            summary.AppendLine($"启用火焰检测: {FlameDetectionConfig.EnableFlameDetection}");
            summary.AppendLine($"摄像头索引: {FlameDetectionConfig.CameraIndex}");
            summary.AppendLine($"启用文件存储: {FileStorageConfig.EnableFileStorage}");
            summary.AppendLine($"基础存储目录: {FileStorageConfig.BaseDirectory}");
            return summary.ToString();
        }
    }
}
