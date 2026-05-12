using ISO11820WinForms.Services;
using Xunit;

namespace ISO11820WinForms.Tests;

public class TestPackageServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public TestPackageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ISO11820PackageTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void CreatePackage_WhenCalledTwiceWithSameTestAndTime_UsesDifferentDirectories()
    {
        var sourceRoot = Path.Combine(_tempRoot, "source");
        Directory.CreateDirectory(sourceRoot);
        var excelPath = Path.Combine(sourceRoot, "report.xlsx");
        var pdfPath = Path.Combine(sourceRoot, "report.pdf");
        var csvPath = Path.Combine(sourceRoot, "sensordata.csv");
        var curvePath = Path.Combine(sourceRoot, "temperature_curve.png");
        File.WriteAllText(excelPath, "excel");
        File.WriteAllText(pdfPath, "pdf");
        File.WriteAllText(csvPath, "timer,temp");
        File.WriteAllText(curvePath, "png");

        var service = new TestPackageService(Path.Combine(_tempRoot, "packages"));
        var timestamp = new DateTime(2026, 5, 7, 9, 30, 0, 123);

        var firstPackage = service.CreatePackage("P001", "T001", excelPath, pdfPath, csvPath, timestamp, temperatureCurveImagePath: curvePath);
        var secondPackage = service.CreatePackage("P001", "T001", excelPath, pdfPath, csvPath, timestamp, temperatureCurveImagePath: curvePath);

        Assert.NotEqual(firstPackage, secondPackage);
        Assert.True(Directory.Exists(firstPackage));
        Assert.True(Directory.Exists(secondPackage));
        Assert.True(File.Exists(Path.Combine(firstPackage, "P001_T001_report.xlsx")));
        Assert.True(File.Exists(Path.Combine(firstPackage, "P001_T001_report.pdf")));
        Assert.True(File.Exists(Path.Combine(firstPackage, "P001_T001_sensordata.csv")));
        Assert.True(File.Exists(Path.Combine(firstPackage, "P001_T001_temperature_curve.png")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
