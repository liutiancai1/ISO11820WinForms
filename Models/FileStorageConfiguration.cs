namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 文件存储配置
    /// </summary>
    public class FileStorageConfiguration
    {
        /// <summary>
        /// 基础存储目录
        /// </summary>
        public string BaseDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 校验记录存储目录
        /// </summary>
        public string CalibrationDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 审计日志存储目录
        /// </summary>
        public string AuditLogDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 试验数据存储目录
        /// </summary>
        public string TestDataDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用文件存储
        /// </summary>
        public bool EnableFileStorage { get; set; } = true;

        /// <summary>
        /// 索引文件名称
        /// </summary>
        public string IndexFileName { get; set; } = "index.json";
    }
}
