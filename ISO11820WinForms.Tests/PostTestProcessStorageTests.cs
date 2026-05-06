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

public class PostTestProcessStorageTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IConfiguration? _originalConfiguration;

    public PostTestProcessStorageTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        _originalConfiguration = GetConfigurationField().GetValue(null) as IConfiguration;

        Directory.CreateDirectory(_tempDirectory);
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
    }

    [Fact]
    public async Task PostTestProcess_WritesCsvToConfiguredStorage_AndDoesNotCreateLegacyProductDirectory()
    {
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());
        _ = ConfigurationService.Instance;

        var productId = $"P-{Guid.NewGuid():N}";
        var testId = $"T-{Guid.NewGuid():N}";
        var legacyProductDirectory = $@"D:\ISO11820\{productId}";

        Assert.False(Directory.Exists(legacyProductDirectory));

        var product = CreateProduct(productId);
        var test = CreateTest(productId, testId);
        test.Ambtemp = 25;
        test.Preweight = 100;
        test.Postweight = 90;

        using (var context = new ISO11820DbContext())
        {
            context.Productmasters.Add(product);
            context.Testmasters.Add(test);
            await context.SaveChangesAsync();
        }

        var master = new TestablePostTestMaster();
        master.SetProductData(product);
        master.SetTestData(test);
        master.AddSample(new SensorDataCatch { Timer = 0, Temp1 = 100, Temp2 = 101, TempSuf = 30, TempCen = 35 });
        master.AddSample(new SensorDataCatch { Timer = 10, Temp1 = 150, Temp2 = 140, TempSuf = 60, TempCen = 55 });

        await master.PostTestProcess();

        var csvPath = TestDataPathHelper.GetSensorDataFilePath(productId, testId);

        Assert.True(File.Exists(csvPath));
        Assert.False(Directory.Exists(legacyProductDirectory));

        using var verificationContext = new ISO11820DbContext();
        var savedTest = await verificationContext.Testmasters.FindAsync(productId, testId);
        Assert.NotNull(savedTest);
        Assert.Equal("10000000", savedTest!.Flag);
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
            Operator = "operator",
            Apparatusid = "FURNACE-01",
            Apparatusname = "一号试验炉",
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

        public void AddSample(SensorDataCatch data)
        {
            _bufSensorData.Add(data);
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
