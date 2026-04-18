namespace TestServer.Models
{
    /// <summary>
    /// 试验报告相关文件路径
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
