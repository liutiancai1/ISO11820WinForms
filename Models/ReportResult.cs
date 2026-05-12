using System.Collections.Generic;

namespace TestServer.Models
{
    /// <summary>
    /// 报告生成结果
    /// </summary>
    public class ReportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Excel 报告文件路径
        /// </summary>
        public string? ExcelFilePath { get; set; }

        /// <summary>
        /// PDF 报告文件路径
        /// </summary>
        public string? PdfFilePath { get; set; }

        public string? TestPackagePath { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 生成耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 被排除的不完整试验ID列表（仅汇总报告使用）
        /// </summary>
        public List<string> ExcludedTestIds { get; set; } = new();
    }
}
