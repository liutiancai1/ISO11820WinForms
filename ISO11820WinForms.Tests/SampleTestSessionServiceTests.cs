using ISO11820WinForms.Core;
using ISO11820WinForms.Services;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class SampleTestSessionServiceTests
{
    [Fact]
    public void StartRecording_WhenNoActiveTest_ReturnsFailure()
    {
        var master = new StubMaster();
        var service = new SampleTestSessionService(master);

        var result = service.StartRecording();

        Assert.False(result.Success);
        Assert.Equal("请先新建本次试验", result.Message);
        Assert.False(master.StartRecordingCalled);
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenActiveTestExists_AppliesPostDataAndRunsPostProcess()
    {
        var master = new StubMaster();
        var test = CreateTest();
        test.Totaltesttime = 120;
        master.SetTestData(test);
        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0001", 12, 8, 88.5);

        Assert.True(result.Success);
        Assert.Equal("试验数据已保存。", result.Message);
        Assert.True(master.PostTestProcessCalled);
        Assert.Equal("0001", test.Phenocode);
        Assert.Equal(12, test.Flametime);
        Assert.Equal(8, test.Flameduration);
        Assert.Equal(88.5, test.Postweight);
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenReportGenerated_ReturnsExcelReportPath()
    {
        var master = new StubMaster();
        var test = CreateTest();
        test.Totaltesttime = 120;
        master.SetTestData(test);
        master.ReportResultToPublish = new ReportResult
        {
            Success = true,
            ExcelFilePath = @"D:\Reports\TestReport_P001_T001.xlsx",
            TestPackagePath = @"D:\Reports\TestPackages\20260507\P001_T001_20260507_093000_123"
        };
        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0001", 12, 8, 88.5);

        Assert.True(result.Success);
        Assert.True(result.ReportGenerated);
        Assert.Equal(@"D:\Reports\TestReport_P001_T001.xlsx", result.ExcelReportPath);
        Assert.Equal(@"D:\Reports\TestPackages\20260507\P001_T001_20260507_093000_123", result.TestPackagePath);
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenReportGenerationFails_StillReturnsSavedResult()
    {
        var master = new StubMaster();
        var test = CreateTest();
        test.Totaltesttime = 120;
        master.SetTestData(test);
        master.ReportResultToPublish = new ReportResult
        {
            Success = false,
            ErrorMessage = "模板文件不存在"
        };
        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0001", 12, 8, 88.5);

        Assert.True(result.Success);
        Assert.False(result.ReportGenerated);
        Assert.Contains("模板文件不存在", result.Message);
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenNoActiveTest_ReturnsFailure()
    {
        var master = new StubMaster();
        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0001", 12, 8, 88.5);

        Assert.False(result.Success);
        Assert.Equal("请先新建本次试验", result.Message);
        Assert.False(master.PostTestProcessCalled);
        Assert.False(master.HasActiveTest);
        Assert.Null(master.GetActiveTestOrNull());
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenTestNotCompleted_ReturnsFailure()
    {
        var master = new StubMaster();
        var test = CreateTest();
        test.Phenocode = "0000";
        test.Flametime = 0;
        test.Flameduration = 0;
        test.Postweight = 0;
        test.Totaltesttime = 0;
        master.SetTestData(test);
        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0001", 12, 8, 88.5);

        Assert.False(result.Success);
        Assert.Equal("试验尚未完成", result.Message);
        Assert.False(master.PostTestProcessCalled);
        Assert.Equal("0000", test.Phenocode);
        Assert.Equal(0, test.Flametime);
        Assert.Equal(0, test.Flameduration);
        Assert.Equal(0, test.Postweight);
    }

    private static Testmaster CreateTest()
    {
        return new Testmaster
        {
            Productid = "P001",
            Testid = "T001",
            Testdate = DateTime.Today,
            According = "ISO 11820",
            Operator = "tester",
            Apparatusid = "FURNACE-01",
            Apparatusname = "1号炉",
            Apparatuschkdate = DateTime.Today,
            Rptno = "R001",
            Phenocode = "0000",
            Flag = "00000000"
        };
    }

    private sealed class StubMaster : TestMaster
    {
        public StubMaster()
            : base(new SensorDictionary(), new FakeManipulator())
        {
        }

        public bool StartRecordingCalled { get; private set; }

        public bool PostTestProcessCalled { get; private set; }

        public ReportResult? ReportResultToPublish { get; set; }

        public override bool StartRecording()
        {
            StartRecordingCalled = true;
            return true;
        }

        public override Task PostTestProcess()
        {
            PostTestProcessCalled = true;
            PublishPostTestReportResult(ReportResultToPublish);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeManipulator : ApparatusManipulator
    {
        public FakeManipulator()
            : base("COM998", "COM998", 2048, 750)
        {
        }
    }
}
