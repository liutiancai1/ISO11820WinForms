using ISO11820WinForms.Services;
using OfficeOpenXml;
using TestServer.Models;
using Xunit;

namespace ISO11820WinForms.Tests;

public class TemperatureCurveExportServiceTests
{
    [Fact]
    public void AddTemperatureCurveWorksheet_WhenSensorDataExists_AddsLineChartAndDataTable()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("报告");

        TemperatureCurveExportService.AddTemperatureCurveWorksheet(package, CreateSensorData());

        var sheet = package.Workbook.Worksheets["温度曲线"];
        Assert.NotNull(sheet);
        Assert.Equal("时间(s)", sheet!.Cells[4, 1].Text);
        Assert.Equal("炉内温度1", sheet.Cells[4, 2].Text);
        Assert.Equal("炉内温度2", sheet.Cells[4, 3].Text);
        Assert.Equal("表面温度", sheet.Cells[4, 4].Text);
        Assert.Equal("中心温度", sheet.Cells[4, 5].Text);
        Assert.Equal(0, sheet.Cells[5, 1].Value);
        Assert.Equal(750.0, sheet.Cells[5, 2].Value);
        Assert.Contains(sheet.Drawings, drawing => drawing.Name == "temperatureCurveChart");
    }

    private static List<SensorDataPoint> CreateSensorData()
    {
        return new List<SensorDataPoint>
        {
            new() { TimeStamp = 0, Tf1 = 750.0, Tf2 = 749.8, Ts = 120.5, Tc = 110.2 },
            new() { TimeStamp = 1, Tf1 = 750.2, Tf2 = 750.0, Ts = 121.1, Tc = 110.8 },
            new() { TimeStamp = 2, Tf1 = 750.1, Tf2 = 750.1, Ts = 121.8, Tc = 111.4 }
        };
    }
}
