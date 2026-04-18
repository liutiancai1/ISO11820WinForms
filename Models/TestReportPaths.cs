namespace TestServer.Models
{
    /// <summary>
    /// 试验报告文件路径（用于序列化到 Testmaster.Memo 字段）
    /// </summary>
    public class TestReportPaths
    {
        /// <summary>
        /// Excel 报告路径
        /// </summary>
        public string? ExcelReportPath { get; set; }

        /// <summary>
        /// PDF 报告路径
        /// </summary>
        public string? PdfReportPath { get; set; }

        /// <summary>
        /// 火焰视频路径
        /// </summary>
        public string? FlameVideoPath { get; set; }
    }
}
