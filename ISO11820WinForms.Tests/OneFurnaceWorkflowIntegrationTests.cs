using System.Reflection;
using ISO11820WinForms.Core;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Microsoft.Extensions.Configuration;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class OneFurnaceWorkflowIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IConfiguration? _originalConfiguration;

    public OneFurnaceWorkflowIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        _originalConfiguration = GetConfigurationField().GetValue(null) as IConfiguration;

        Directory.CreateDirectory(_tempDirectory);
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:SqlitePath"] = Path.Combine(_tempDirectory, "workflow.db"),
            ["Simulation:EnableSimulation"] = "false",
            ["Simulation:SimulateSensors"] = "false",
            ["Simulation:SimulatePidController"] = "false",
            ["FileStorage:BaseDirectory"] = Path.Combine(_tempDirectory, "Storage"),
            ["FileStorage:TestDataDirectory"] = Path.Combine(_tempDirectory, "Storage", "TestData"),
            ["Report:TemplateFilePath"] = Path.Combine(_tempDirectory, "missing-template.xlsx"),
            ["Report:OutputDirectory"] = Path.Combine(_tempDirectory, "Reports")
        });
    }

    [Fact]
    public async Task SubmitPostTestAsync_WhenTestCompleted_WritesCsvAndClearsActiveTest()
    {
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());
        _ = ConfigurationService.Instance;

        var productId = $"P-{Guid.NewGuid():N}";
        var testId = $"T-{Guid.NewGuid():N}";
        var product = CreateProduct(productId);
        var test = CreateTest(productId, testId);
        test.Ambtemp = 25;
        test.Preweight = 100;
        test.Postweight = 80;
        test.Totaltesttime = 1800;

        using (var context = new ISO11820DbContext())
        {
            context.Productmasters.Add(product);
            context.Testmasters.Add(test);
            await context.SaveChangesAsync();
        }

        var master = new TestablePostTestMaster
        {
            Status = MasterStatus.Complete
        };
        master.SetProductData(product);
        master.SetTestData(test);
        master.AddSample(new SensorDataCatch { Timer = 0, Temp1 = 100, Temp2 = 100, TempSuf = 30, TempCen = 28 });
        master.AddSample(new SensorDataCatch { Timer = 60, Temp1 = 750, Temp2 = 749, TempSuf = 310, TempCen = 240 });

        var service = new SampleTestSessionService(master);

        var result = await service.SubmitPostTestAsync("0000", 0, 0, 80);

        Assert.True(result.Success);
        Assert.True(File.Exists(TestDataPathHelper.GetSensorDataFilePath(productId, testId)));
        Assert.Null(master.GetActiveTestOrNull());
    }

    public void Dispose()
    {
        SetConfiguration(_originalConfiguration);
        ResetConfigurationService();
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
            Operator = "tester",
            Apparatusid = "F01",
            Apparatusname = "1号炉",
            Apparatuschkdate = DateTime.Today,
            Rptno = productId,
            Phenocode = "0000",
            Flag = "00000000"
        };
    }

    private static FieldInfo GetConfigurationField()
    {
        return typeof(ConfigurationHelper).GetField("_configuration", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ConfigurationHelper._configuration.");
    }

    private static void ResetConfigurationService()
    {
        var field = typeof(ConfigurationService).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ConfigurationService._instance.");
        field.SetValue(null, null);
    }

    private static void SetConfiguration(IConfiguration? configuration)
    {
        GetConfigurationField().SetValue(null, configuration);
        ResetConfigurationService();
    }

    private static void SetConfiguration(IReadOnlyDictionary<string, string?>? values)
    {
        IConfiguration? configuration = null;
        if (values != null)
        {
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        SetConfiguration(configuration);
    }

    private sealed class TestablePostTestMaster : TestMaster
    {
        public TestablePostTestMaster()
            : base(new SensorDictionary(), new FakeManipulator())
        {
        }

        public void AddSample(SensorDataCatch sample)
        {
            _bufSensorData.Add(sample);
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
