namespace TestServer.Models
{
    /// <summary>
    /// 报告生成配置
    /// </summary>
    public class ReportConfiguration
    {
        /// <summary>
        /// 报告模板文件路径
        /// </summary>
        public string TemplateFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 报告输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 生成后是否自动打开报告
        /// </summary>
        public bool AutoOpenAfterGeneration { get; set; } = false;

        /// <summary>
        /// 是否同时生成 PDF 格式
        /// </summary>
        public bool GeneratePdf { get; set; } = true;

        /// <summary>
        /// 临时文件目录
        /// </summary>
        public string TempDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用 PDF 导出
        /// </summary>
        public bool EnablePdfExport { get; set; } = true;

        /// <summary>
        /// 汇总报告模板文件路径
        /// </summary>
        public string SummaryTemplateFilePath { get; set; } = string.Empty;
    }
}
