using System.Text.Json;
using TestServer.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 兼容历史 Memo 中报告路径 JSON 的显示辅助类
    /// </summary>
    public static class ReportPathMemoHelper
    {
        public static string GetDisplayMemo(string? memo)
        {
            return TryParseLegacyReportPaths(memo, out _) ? string.Empty : memo ?? string.Empty;
        }

        public static bool TryParseLegacyReportPaths(string? memo, out TestReportPaths? reportPaths)
        {
            reportPaths = null;

            if (string.IsNullOrWhiteSpace(memo))
            {
                return false;
            }

            var trimmedMemo = memo.TrimStart();
            if (!trimmedMemo.StartsWith("{", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<TestReportPaths>(memo);
                if (parsed == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(parsed.ExcelReportPath)
                    && string.IsNullOrWhiteSpace(parsed.PdfReportPath)
                    && string.IsNullOrWhiteSpace(parsed.FlameVideoPath))
                {
                    return false;
                }

                reportPaths = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
