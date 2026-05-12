using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using OfficeOpenXml;
using Serilog;
using TestServer.Models;
using Xunit;

namespace ISO11820WinForms.Tests;

public class ReportTemplateEngineOutputTests : IDisposable
{
    private readonly string _tempRoot;

    public ReportTemplateEngineOutputTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ISO11820ReportEngineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WritesRawTemperatureDataToSeparateSheet()
    {
        var engine = CreateEngine();

        var excelPath = await engine.GenerateExcelReportAsync(CreateReportData());

        using var package = new ExcelPackage(new FileInfo(excelPath));
        var reportSheet = package.Workbook.Worksheets[0];
        var dataSheet = package.Workbook.Worksheets["温度数据"];

        Assert.NotNull(dataSheet);
        Assert.True(string.IsNullOrWhiteSpace(reportSheet.Cells[20, 1].Text));
        Assert.Equal("时间(s)", dataSheet!.Cells[4, 1].Text);
        Assert.Equal("炉内温度1", dataSheet.Cells[4, 2].Text);
        Assert.Equal("炉内温度2", dataSheet.Cells[4, 3].Text);
        Assert.Equal("表面温度", dataSheet.Cells[4, 4].Text);
        Assert.Equal("中心温度", dataSheet.Cells[4, 5].Text);
        Assert.Equal(0.0, Convert.ToDouble(dataSheet.Cells[5, 1].Value));
        Assert.Equal(750.0, dataSheet.Cells[5, 2].Value);
    }

    [Fact]
    public async Task ExportToPdfAsync_WithReportData_GeneratesPdfFromSummaryAndCurve()
    {
        var engine = CreateEngine();
        var reportData = CreateReportData();
        var excelPath = await engine.GenerateExcelReportAsync(reportData);

        var pdfPath = await engine.ExportToPdfAsync(excelPath, reportData);

        Assert.True(File.Exists(pdfPath));
        var bytes = await File.ReadAllBytesAsync(pdfPath);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(bytes.Length > 1_000);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WhenTemplateMissing_CreatesBasicTemplateAndReport()
    {
        var templatePath = Path.Combine(_tempRoot, "Templates", "ReportTemplate.xlsx");
        var engine = new ReportTemplateEngine(
            new ReportConfiguration
            {
                TemplateFilePath = templatePath,
                OutputDirectory = Path.Combine(_tempRoot, "reports"),
                TempDirectory = Path.Combine(_tempRoot, "temp")
            },
            Log.Logger);

        var excelPath = await engine.GenerateExcelReportAsync(CreateReportData());

        Assert.True(File.Exists(templatePath));
        Assert.True(File.Exists(excelPath));
        using var package = new ExcelPackage(new FileInfo(excelPath));
        Assert.NotNull(package.Workbook.Worksheets["报告"]);
        Assert.NotNull(package.Workbook.Worksheets["温度数据"]);
        Assert.NotNull(package.Workbook.Worksheets["温度曲线"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private ReportTemplateEngine CreateEngine()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var templatePath = Path.Combine(_tempRoot, "template.xlsx");
        using (var package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("报告");
            package.SaveAs(new FileInfo(templatePath));
        }

        return new ReportTemplateEngine(
            new ReportConfiguration
            {
                TemplateFilePath = templatePath,
                OutputDirectory = Path.Combine(_tempRoot, "reports"),
                TempDirectory = Path.Combine(_tempRoot, "temp")
            },
            Log.Logger);
    }

    private static TestReportData CreateReportData()
    {
        return new TestReportData
        {
            ProductInfo = new Productmaster
            {
                Productid = "P001",
                Productname = "sample",
                Specific = "spec",
                Diameter = 45,
                Height = 50
            },
            TestInfo = new Testmaster
            {
                Productid = "P001",
                Testid = "T001",
                Testdate = new DateTime(2026, 5, 7),
                According = "ISO 11820",
                Operator = "operator",
                Apparatusid = "FURNACE-01",
                Apparatusname = "furnace",
                Apparatuschkdate = new DateTime(2026, 1, 1),
                Constpower = 2048,
                Rptno = "R001",
                Ambtemp = 23.5,
                Ambhumi = 55,
                Totaltesttime = 120,
                Preweight = 100,
                Postweight = 88,
                Lostweight = 12,
                LostweightPer = 12,
                Phenocode = "0001",
                Flametime = 12,
                Flameduration = 8,
                Maxtf1 = 751,
                Maxtf2 = 752,
                Maxts = 310,
                Maxtc = 280,
                Finaltf1 = 750,
                Finaltf2 = 751,
                Finalts = 300,
                Finaltc = 270,
                Deltatf1 = 726.5,
                Deltatf2 = 727.5,
                Deltatf = 276.5,
                Deltatc = 246.5,
                Flag = "10000000"
            },
            ApparatusInfo = new Apparatus
            {
                Apparatusid = 1,
                Innernumber = "FURNACE-01",
                Apparatusname = "furnace",
                Checkdatef = new DateTime(2026, 1, 1),
                Checkdatet = new DateTime(2027, 1, 1),
                Pidport = "COM9",
                Powerport = "COM9",
                Constpower = 2048
            },
            SensorData = new List<SensorDataPoint>
            {
                new() { TimeStamp = 0, Tf1 = 750.0, Tf2 = 749.8, Ts = 120.5, Tc = 110.2 },
                new() { TimeStamp = 1, Tf1 = 750.2, Tf2 = 750.0, Ts = 121.1, Tc = 110.8 },
                new() { TimeStamp = 2, Tf1 = 750.1, Tf2 = 750.1, Ts = 121.8, Tc = 111.4 }
            }
        };
    }
}
