using System.Text.Json;
using ISO11820WinForms.Utilities;
using TestServer.Models;
using Xunit;

namespace ISO11820WinForms.Tests.Utilities;

public class ReportPathMemoHelperTests
{
    [Fact]
    public void GetDisplayMemo_ReturnsEmpty_WhenMemoContainsLegacyReportPathPayload()
    {
        var legacyMemo = JsonSerializer.Serialize(new TestReportPaths
        {
            ExcelReportPath = @"D:\ISO11820\Reports\test.xlsx"
        });

        var displayMemo = ReportPathMemoHelper.GetDisplayMemo(legacyMemo);

        Assert.Equal(string.Empty, displayMemo);
    }

    [Fact]
    public void GetDisplayMemo_ReturnsOriginalMemo_WhenMemoIsPlainText()
    {
        const string memo = "试验备注";

        var displayMemo = ReportPathMemoHelper.GetDisplayMemo(memo);

        Assert.Equal(memo, displayMemo);
    }
}
