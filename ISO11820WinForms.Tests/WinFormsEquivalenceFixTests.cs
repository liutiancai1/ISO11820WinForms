using System.Reflection;
using ISO11820WinForms.Core;
using ISO11820WinForms.Forms;
using ISO11820WinForms.Global;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using ISO11820_2020.Models;
using Microsoft.Extensions.Configuration;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class WinFormsEquivalenceFixTests : IDisposable
{
    private readonly string _tempDirectory;

    public WinFormsEquivalenceFixTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:SqlitePath"] = Path.Combine(_tempDirectory, "ISO11820.Tests.db"),
            ["Simulation:EnableSimulation"] = "false",
            ["Simulation:SimulateSensors"] = "false",
            ["Simulation:SimulatePidController"] = "false",
            ["FileStorage:BaseDirectory"] = Path.Combine(_tempDirectory, "ISO11820"),
            ["FileStorage:TestDataDirectory"] = Path.Combine(_tempDirectory, "ISO11820", "TestData"),
            ["Report:TemplateFilePath"] = Path.Combine(_tempDirectory, "missing-template.xlsx"),
            ["Report:OutputDirectory"] = Path.Combine(_tempDirectory, "Reports")
        });
        ResetSystemContext();
    }

    [Fact]
    public void CheckStartCriteria_WhenNotInSimulation_RequiresFullBaseCriteria()
    {
        var master = new TestableTestMaster1();
        master.SetCurrentTemperatures(750, 750);

        var results = Enumerable.Range(0, 10)
            .Select(_ => master.CheckStartCriteria())
            .ToList();

        Assert.All(results, Assert.False);
    }

    [Fact]
    public void CheckStartCriteria_WhenDriftWindowIsFullButNotCalculated_DoesNotUseDefaultZeroDrift()
    {
        var master = new TestableTestMaster();
        master.SetCurrentTemperatures(750, 750);
        master.LoadTenMinuteTemperatureWindow(_ => 750, _ => 750);

        Assert.False(master.CheckStartCriteria());
    }

    [Fact]
    public void CalculateDrift10Min_ReportsDegreesPerTenMinutes()
    {
        var master = new TestableTestMaster();
        master.LoadTenMinuteTemperatureWindow(
            second => 750 + second / 599.0,
            second => 750 + second / 599.0);

        master.CalculateTenMinuteDrift();

        Assert.Equal(1.0, master.CurrentCalculation.Temp1Drift10Min, 3);
        Assert.Equal(1.0, master.CurrentCalculation.Temp2Drift10Min, 3);
        Assert.Equal(1.0, master.CurrentCalculation.TempDriftMean, 3);
    }

    [Fact]
    public void TemperatureDriftUnitText_UsesDegreesPerTenMinutes()
    {
        Assert.Equal("℃/10min", TestMaster.TemperatureDriftUnitText);
    }

    [Fact]
    public async Task CreateNewTestAsync_WhenTestIdAlreadyExists_DoesNotReplaceCurrentMasterCache()
    {
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());
        ResetSystemContext();

        var master = new TestableTestMaster();
        SystemContext.Current.Masters.addMaster(master);
        using (var context = new ISO11820DbContext())
        {
            context.Productmasters.Add(CreateProduct("P001"));
            context.Testmasters.Add(CreateTest("P001", "T001"));
            await context.SaveChangesAsync();
        }

        var service = new TestmasterService();
        var result = await service.CreateNewTestAsync(CreateProduct("P001"), CreateTest("P001", "T001"));

        Assert.False(result.success);
        Assert.Null(master.CurrentProduct);
        Assert.Null(master.CurrentTest);
    }

    [Fact]
    public void ApplyDerivedTestResults_PopulatesTemperatureMassAndConclusionFields()
    {
        var master = new TestableTestMaster();
        var test = CreateTest("P002", "T002");
        test.Ambtemp = 25;
        test.Preweight = 100;
        test.Postweight = 75;
        test.Totaltesttime = 20;

        master.SetTestData(test);
        master.AddSensorData(new SensorDataCatch { Timer = 0, Temp1 = 100, Temp2 = 101, TempSuf = 30, TempCen = 35 });
        master.AddSensorData(new SensorDataCatch { Timer = 10, Temp1 = 150, Temp2 = 140, TempSuf = 60, TempCen = 55 });
        master.AddSensorData(new SensorDataCatch { Timer = 20, Temp1 = 125, Temp2 = 145, TempSuf = 50, TempCen = 70 });

        master.ApplyDerivedResults();

        Assert.Equal(150, test.Maxtf1);
        Assert.Equal(10, test.Maxtf1Time);
        Assert.Equal(145, test.Maxtf2);
        Assert.Equal(20, test.Maxtf2Time);
        Assert.Equal(60, test.Maxts);
        Assert.Equal(10, test.MaxtsTime);
        Assert.Equal(70, test.Maxtc);
        Assert.Equal(20, test.MaxtcTime);
        Assert.Equal(125, test.Finaltf1);
        Assert.Equal(145, test.Finaltf2);
        Assert.Equal(50, test.Finalts);
        Assert.Equal(70, test.Finaltc);
        Assert.Equal(100, test.Deltatf1);
        Assert.Equal(120, test.Deltatf2);
        Assert.Equal(25, test.Deltats);
        Assert.Equal(25, test.Deltatf);
        Assert.Equal(45, test.Deltatc);
        Assert.Equal(25, test.Lostweight);
        Assert.Equal(25, test.LostweightPer);
        Assert.Equal("10000000", test.Flag);
    }

    [Fact]
    public void MainForm_OnlyAllowsPostTestRecordAfterComplete()
    {
        var method = typeof(MainForm).GetMethod("CanPostTestRecord", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.False((bool)method!.Invoke(null, new object?[] { MasterStatus.Idle, null })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Preparing, CreateTest("P003", "T003") })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Ready, CreateTest("P003", "T003") })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Recording, CreateTest("P003", "T003") })!);
        Assert.True((bool)method.Invoke(null, new object?[] { MasterStatus.Complete, CreateTest("P003", "T003") })!);

        var completedTest = CreateTest("P003", "T004");
        completedTest.Totaltesttime = 1800;
        Assert.True((bool)method.Invoke(null, new object?[] { MasterStatus.Preparing, completedTest })!);
    }

    [Fact]
    public void MainForm_DisablesStartRecordWhenCompletedTestHasNotBeenSaved()
    {
        var method = typeof(MainForm).GetMethod("CanStartRecord", BindingFlags.Static | BindingFlags.NonPublic);
        var activeTest = CreateTest("P003", "T005");
        var completedButUnsaved = CreateTest("P003", "T006");
        completedButUnsaved.Totaltesttime = 60;
        completedButUnsaved.Flag = "00000000";

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(null, new object?[] { MasterStatus.Ready, activeTest })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Ready, completedButUnsaved })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Complete, completedButUnsaved })!);
    }

    [Fact]
    public void MainForm_StatusMessageForCompletedUnsavedTestPromptsUserToSaveRecord()
    {
        var method = typeof(MainForm).GetMethod("GetStatusMessage", BindingFlags.Static | BindingFlags.NonPublic);
        var completedButUnsaved = CreateTest("P003", "T007");
        completedButUnsaved.Totaltesttime = 60;
        completedButUnsaved.Flag = "00000000";

        Assert.NotNull(method);
        var message = (string?)method!.Invoke(null, new object?[] { MasterStatus.Recording, MasterStatus.Complete, completedButUnsaved });

        Assert.Equal("试验已完成，请点击“试验记录”保存并生成报告。", message);
    }

    [Fact]
    public void MainForm_StatusMessageForCompletedUnsavedReadyTransitionDoesNotRepeatPrompt()
    {
        var method = typeof(MainForm).GetMethod("GetStatusMessage", BindingFlags.Static | BindingFlags.NonPublic);
        var completedButUnsaved = CreateTest("P003", "T008");
        completedButUnsaved.Totaltesttime = 60;
        completedButUnsaved.Flag = "00000000";

        Assert.NotNull(method);
        var preparingMessage = (string?)method!.Invoke(null, new object?[] { MasterStatus.Complete, MasterStatus.Preparing, completedButUnsaved });
        var readyMessage = (string?)method.Invoke(null, new object?[] { MasterStatus.Preparing, MasterStatus.Ready, completedButUnsaved });

        Assert.Null(preparingMessage);
        Assert.Null(readyMessage);
    }

    [Fact]
    public void MainForm_BlocksNewTestWhenCompletedTestHasNotBeenSaved()
    {
        var method = typeof(MainForm).GetMethod("CanCreateNewTest", BindingFlags.Static | BindingFlags.NonPublic);
        var completedButUnsaved = CreateTest("P004", "T005");
        completedButUnsaved.Totaltesttime = 1800;
        completedButUnsaved.Flag = "00000000";

        Assert.NotNull(method);
        Assert.False((bool)method!.Invoke(null, new object?[] { MasterStatus.Complete, completedButUnsaved })!);
        Assert.False((bool)method.Invoke(null, new object?[] { MasterStatus.Preparing, completedButUnsaved })!);
    }

    [Fact]
    public void MainForm_AllowsNewTestAfterCompletedTestHasBeenSaved()
    {
        var method = typeof(MainForm).GetMethod("CanCreateNewTest", BindingFlags.Static | BindingFlags.NonPublic);
        var savedTest = CreateTest("P004", "T006");
        savedTest.Totaltesttime = 1800;
        savedTest.Flag = "10000000";

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(null, new object?[] { MasterStatus.Preparing, savedTest })!);
        Assert.True((bool)method.Invoke(null, new object?[] { MasterStatus.Idle, null })!);
    }

    [Fact]
    public void MainForm_TryApplyMainPageTestMode_AppliesFixedDurationToCurrentTest()
    {
        var test = CreateTest("P005", "T001");

        var result = InvokeTryApplyMainPageTestMode(test, "固定时长", "45", out var errorMessage);

        Assert.True(result);
        Assert.Equal(string.Empty, errorMessage);
        Assert.True(test.UseFixedDuration);
        Assert.Equal(2700, test.TargetDurationSeconds);
    }

    [Fact]
    public void MainForm_TryApplyMainPageTestMode_AppliesStandardModeToCurrentTest()
    {
        var test = CreateTest("P005", "T002");
        SetFixedDuration(test, 1800);

        var result = InvokeTryApplyMainPageTestMode(test, "标准模式", "45", out var errorMessage);

        Assert.True(result);
        Assert.Equal(string.Empty, errorMessage);
        Assert.False(test.UseFixedDuration);
        Assert.Equal(3600, test.TargetDurationSeconds);
    }

    [Fact]
    public void DoRecording_WhenFixedDurationModeBeforeTarget_KeepsRecordingEvenIfTerminateCriteriaMet()
    {
        var master = new TestableTestMaster1();
        var test = CreateTest("P004", "T001");
        SetFixedDuration(test, 3600);
        master.SetTestData(test);
        master.Status = MasterStatus.Recording;
        master.Timer = 1800;

        master.InvokeRecordingTick();

        Assert.Equal(MasterStatus.Recording, master.Status);
        Assert.Equal(0, test.Totaltesttime);
        Assert.Equal(1801, master.Timer);
    }

    [Fact]
    public void DoRecording_WhenFixedDurationModeReachesTarget_CompletesAtTargetDuration()
    {
        var master = new TestableTestMaster1();
        var test = CreateTest("P004", "T002");
        SetFixedDuration(test, 120);
        master.SetTestData(test);
        master.Status = MasterStatus.Recording;
        master.Timer = 120;

        var messages = master.InvokeRecordingTick();

        Assert.Equal(MasterStatus.Complete, master.Status);
        Assert.Equal(120, test.Totaltesttime);
        Assert.Empty(messages);
    }

    public void Dispose()
    {
        ResetSystemContext();
        SetConfiguration(null);
    }

    private static Productmaster CreateProduct(string productId)
    {
        return new Productmaster
        {
            Productid = productId,
            Productname = "sample",
            Specific = "spec",
            Height = 50,
            Diameter = 45,
            Flag = "0"
        };
    }

    private static Testmaster CreateTest(string productId, string testId)
    {
        return new Testmaster
        {
            Productid = productId,
            Testid = testId,
            Testdate = DateTime.Today,
            According = "ISO 11820",
            Operator = "operator",
            Apparatusid = "FURNACE-01",
            Apparatusname = "一号试验炉",
            Apparatuschkdate = DateTime.Today,
            Rptno = productId,
            Phenocode = "0000",
            Flag = "00000000"
        };
    }

    private static void SetFixedDuration(Testmaster test, int seconds)
    {
        var useFixedDuration = typeof(Testmaster).GetProperty("UseFixedDuration");
        var targetDurationSeconds = typeof(Testmaster).GetProperty("TargetDurationSeconds");

        Assert.NotNull(useFixedDuration);
        Assert.NotNull(targetDurationSeconds);

        useFixedDuration!.SetValue(test, true);
        targetDurationSeconds!.SetValue(test, seconds);
    }

    private static bool InvokeTryApplyMainPageTestMode(
        Testmaster? test,
        string modeText,
        string durationMinutesText,
        out string errorMessage)
    {
        var method = typeof(MainForm).GetMethod("TryApplyMainPageTestMode", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object?[] args = { test, modeText, durationMinutesText, string.Empty };
        var result = (bool)method!.Invoke(null, args)!;
        errorMessage = (string)args[3]!;
        return result;
    }

    private static void SetConfiguration(IReadOnlyDictionary<string, string?>? values)
    {
        var field = typeof(ConfigurationHelper).GetField("_configuration", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ConfigurationHelper._configuration.");

        IConfiguration? configuration = null;
        if (values != null)
        {
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        field.SetValue(null, configuration);
    }

    private static void ResetSystemContext()
    {
        var field = typeof(SystemContext).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find SystemContext._instance.");
        field.SetValue(null, null);
    }

    private sealed class TestableTestMaster1 : TestMaster1
    {
        public TestableTestMaster1()
            : base(null!, new SensorDictionary(), new FakeManipulator())
        {
        }

        public void SetCurrentTemperatures(double temp1, double temp2)
        {
            _sensorDataCatch.Temp1 = temp1;
            _sensorDataCatch.Temp2 = temp2;
        }

        public List<MasterMessage> InvokeRecordingTick()
        {
            var method = typeof(TestMaster1).GetMethod("DoRecording", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var messages = new List<MasterMessage>();
            method!.Invoke(this, new object?[] { messages });
            return messages;
        }
    }

    private sealed class TestableTestMaster : TestMaster
    {
        public TestableTestMaster()
            : base(new SensorDictionary(), new FakeManipulator())
        {
        }

        public Productmaster? CurrentProduct => _productMaster;
        public Testmaster? CurrentTest => _testmaster;
        public CaculateDataCatch CurrentCalculation => _caculateDataCatch;

        public void AddSensorData(SensorDataCatch data)
        {
            _bufSensorData.Add(data);
        }

        public void SetCurrentTemperatures(double temp1, double temp2)
        {
            _sensorDataCatch.Temp1 = temp1;
            _sensorDataCatch.Temp2 = temp2;
        }

        public void LoadTenMinuteTemperatureWindow(Func<int, double> temp1Factory, Func<int, double> temp2Factory)
        {
            y1Data10Min.Clear();
            y2Data10Min.Clear();

            for (var second = 0; second < 600; second++)
            {
                y1Data10Min.Enqueue(temp1Factory(second));
                y2Data10Min.Enqueue(temp2Factory(second));
            }
        }

        public void CalculateTenMinuteDrift()
        {
            CaculateDrift10Min();
        }

        public void ApplyDerivedResults()
        {
            var method = typeof(TestMaster).GetMethod("ApplyDerivedTestResults", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(this, null);
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
