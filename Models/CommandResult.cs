namespace ISO11820WinForms.Models
{
    public class CommandResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public bool ReportGenerated { get; init; }

        public string? ExcelReportPath { get; init; }

        public string? PdfReportPath { get; init; }

        public string? TestPackagePath { get; init; }

        public static CommandResult Ok(
            string message,
            bool reportGenerated = false,
            string? excelReportPath = null,
            string? pdfReportPath = null,
            string? testPackagePath = null)
        {
            return new CommandResult
            {
                Success = true,
                Message = message,
                ReportGenerated = reportGenerated,
                ExcelReportPath = excelReportPath,
                PdfReportPath = pdfReportPath,
                TestPackagePath = testPackagePath
            };
        }

        public static CommandResult Fail(string message)
        {
            return new CommandResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
