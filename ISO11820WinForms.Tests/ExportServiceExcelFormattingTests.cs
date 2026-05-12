using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using ISO11820WinForms.Services;
using OxyPlot;
using OxyPlot.Series;
using TestServer.Models;
using Xunit;

namespace ISO11820WinForms.Tests;

public class ExportServiceExcelFormattingTests
{
    [Fact]
    public void CreateTestInfoSheet_UsesFormalReportLayout()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("试验信息");
        var service = new ExportService();

        InvokeCreateTestInfoSheet(service, sheet, CreateTestRecord());

        Assert.Equal("建筑材料不燃性试验报告", sheet.Cells["A1"].Text);
        Assert.Contains("A1:F1", sheet.MergedCells);
        Assert.Equal("试验概览", sheet.Cells["A3"].Text);
        Assert.Equal("样品信息", sheet.Cells["A7"].Text);
        Assert.Equal("试验环境", sheet.Cells["A12"].Text);
        Assert.Equal("试验结果", sheet.Cells["A16"].Text);
        Assert.True(sheet.Cells["A1"].Style.Font.Bold);
        Assert.True(sheet.View.ShowGridLines == false);
    }

    [Fact]
    public async Task ExportQueryResultsToExcel_CreatesStandaloneQueryWorkbook()
    {
        var service = new ExportService();
        var method = typeof(ExportService).GetMethod(
            "ExportQueryResultsToExcel",
            new[]
            {
                typeof(IReadOnlyList<Testmaster>),
                typeof(string),
                typeof(DateTime),
                typeof(DateTime),
                typeof(string),
                typeof(string),
                typeof(string)
            });

        Assert.NotNull(method);

        var filePath = Path.Combine(Path.GetTempPath(), $"query-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            var task = (Task<bool>)method!.Invoke(
                service,
                new object[]
                {
                    new List<Testmaster> { CreateTestRecord() },
                    filePath,
                    new DateTime(2026, 4, 1),
                    new DateTime(2026, 4, 30),
                    "P001",
                    "T001",
                    "admin"
                })!;

            Assert.True(await task);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var sheet = package.Workbook.Worksheets["查询结果"];

            Assert.NotNull(sheet);
            Assert.Equal("试验记录查询结果", sheet!.Cells["A1"].Text);
            Assert.Equal("查询条件", sheet.Cells["A3"].Text);
            Assert.Equal("试验日期", sheet.Cells["A6"].Text);
            Assert.Equal("样品编号", sheet.Cells["B6"].Text);
            Assert.Equal("试验备注", sheet.Cells["I6"].Text);
            Assert.True(sheet.Cells["A6:I6"].AutoFilter);
            Assert.Equal("已完成", sheet.Cells["F7"].Text);
            Assert.Equal("admin", sheet.Cells["E7"].Text);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ExportChartToImage_CreatesPngFile()
    {
        var service = new ExportService();
        var filePath = Path.Combine(Path.GetTempPath(), $"chart-export-{Guid.NewGuid():N}.png");

        try
        {
            var plotModel = new PlotModel { Title = "温度曲线" };
            var lineSeries = new LineSeries();
            lineSeries.Points.Add(new DataPoint(0, 20));
            lineSeries.Points.Add(new DataPoint(1, 35));
            lineSeries.Points.Add(new DataPoint(2, 48));
            plotModel.Series.Add(lineSeries);

            service.ExportChartToImage(plotModel, filePath, 640, 360);

            Assert.True(File.Exists(filePath));
            Assert.True(new FileInfo(filePath).Length > 0);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ExportChartToExcel_CreatesWorkbookWithChartAndData()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var service = new ExportService();
        var filePath = Path.Combine(Path.GetTempPath(), $"chart-export-{Guid.NewGuid():N}.xlsx");

        try
        {
            var plotModel = new PlotModel { Title = "温度曲线" };

            var surfaceSeries = new LineSeries { Title = "表面温度" };
            surfaceSeries.Color = OxyColor.FromRgb(70, 178, 137);
            surfaceSeries.Points.Add(new DataPoint(0, 20));
            surfaceSeries.Points.Add(new DataPoint(1, 35));
            surfaceSeries.Points.Add(new DataPoint(2, 48));
            plotModel.Series.Add(surfaceSeries);

            var centerSeries = new LineSeries { Title = "中心温度" };
            centerSeries.Points.Add(new DataPoint(0, 18));
            centerSeries.Points.Add(new DataPoint(1, 30));
            centerSeries.Points.Add(new DataPoint(2, 40));
            plotModel.Series.Add(centerSeries);

            service.ExportChartToExcel(plotModel, filePath);

            Assert.True(File.Exists(filePath));

            using var package = new ExcelPackage(new FileInfo(filePath));
            var sheet = package.Workbook.Worksheets["温度曲线"];

            Assert.NotNull(sheet);
            Assert.Equal("温度曲线", sheet!.Cells["A1"].Text);
            Assert.Equal("时间(s)", sheet.Cells["A21"].Text);
            Assert.Equal("表面温度", sheet.Cells["B21"].Text);
            Assert.Equal("中心温度", sheet.Cells["C21"].Text);
            var chart = Assert.IsAssignableFrom<ExcelChart>(sheet.Drawings["temperatureCurveChart"]);
            Assert.Equal(eChartType.XYScatterLinesNoMarkers, chart.ChartType);
            Assert.True(chart.From.Column >= 4);
            Assert.Equal(System.Drawing.Color.FromArgb(70, 178, 137), chart.Series[0].Border.Fill.Color);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static void InvokeCreateTestInfoSheet(ExportService service, ExcelWorksheet sheet, Testmaster testData)
    {
        var method = typeof(ExportService).GetMethod("CreateTestInfoSheet", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Unable to find CreateTestInfoSheet method.");

        method.Invoke(service, new object[] { sheet, testData });
    }

    private static Testmaster CreateTestRecord()
    {
        return new Testmaster
        {
            Productid = "P001",
            Testid = "T001",
            Testdate = new DateTime(2026, 4, 20, 14, 30, 0),
            Operator = "admin",
            Ambtemp = 23.5f,
            Ambhumi = 55.2f,
            Preweight = 100.25f,
            Postweight = 92.10f,
            Lostweight = 8.15f,
            LostweightPer = 8.13f,
            Phenocode = "已完成",
            Flametime = 12,
            Flameduration = 46,
            Deltatf = 18.9f,
            Apparatusid = "COM9",
            Apparatusname = "不燃性试验炉",
            Memo = "查询备注",
            Product = new Productmaster
            {
                Productid = "P001",
                Productname = "岩棉板",
                Specific = "300x300",
                Height = 50,
                Diameter = 45
            }
        };
    }
}
