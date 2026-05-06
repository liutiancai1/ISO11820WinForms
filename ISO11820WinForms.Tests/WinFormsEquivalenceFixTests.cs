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

        master.InvokeRecordingTick();

        Assert.Equal(MasterStatus.Complete, master.Status);
        Assert.Equal(120, test.Totaltesttime);
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

        public void InvokeRecordingTick()
        {
            var method = typeof(TestMaster1).GetMethod("DoRecording", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(this, new object?[] { new List<MasterMessage>() });
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

        public void AddSensorData(SensorDataCatch data)
        {
            _bufSensorData.Add(data);
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
