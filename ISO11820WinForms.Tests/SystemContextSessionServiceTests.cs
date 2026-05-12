using System.Reflection;
using ISO11820WinForms.Global;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ISO11820WinForms.Tests;

public class SystemContextSessionServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IConfiguration? _originalConfiguration;

    public SystemContextSessionServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        _originalConfiguration = GetConfigurationField().GetValue(null) as IConfiguration;

        SetConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:SqlitePath"] = Path.Combine(_tempDirectory, "ISO11820.Tests.db"),
            ["Hardware:PidPort"] = "COM998",
            ["Hardware:PowerPort"] = "COM998",
            ["Hardware:SensorPort"] = "COM998",
            ["Hardware:SensorProtocol"] = "Adam",
            ["Hardware:SensorStationNumber"] = "1",
            ["Hardware:SensorReadTimeoutMs"] = "50",
            ["Simulation:EnableSimulation"] = "true",
            ["Simulation:SimulateSensors"] = "true",
            ["Simulation:SimulatePidController"] = "true",
            ["Simulation:InitialFurnaceTemp"] = "25",
            ["Simulation:TargetFurnaceTemp"] = "750",
            ["Simulation:HeatingRatePerSecond"] = "5",
            ["Simulation:TempFluctuation"] = "0",
            ["Simulation:StableThreshold"] = "5",
            ["FileStorage:BaseDirectory"] = Path.Combine(_tempDirectory, "ISO11820"),
            ["FileStorage:TestDataDirectory"] = Path.Combine(_tempDirectory, "ISO11820", "TestData"),
            ["Report:TemplateFilePath"] = Path.Combine(_tempDirectory, "missing-template.xlsx"),
            ["Report:OutputDirectory"] = Path.Combine(_tempDirectory, "Reports")
        });

        Directory.CreateDirectory(_tempDirectory);
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());
    }

    [Fact]
    public void Init_WhenMaster1Created_ExposesSessionService()
    {
        var context = CreateIsolatedSystemContext();

        try
        {
            context.Init();

            Assert.NotNull(context.Master1);
            Assert.NotNull(context.Session);
            Assert.IsType<SampleTestSessionService>(context.Session);
            Assert.Same(context.Master1, GetSessionMaster(context.Session!));
        }
        finally
        {
            context.Cleanup();
        }
    }

    [Fact]
    public void Cleanup_WhenSessionCreated_ClearsSession()
    {
        var context = CreateIsolatedSystemContext();

        context.Init();
        Assert.NotNull(context.Session);
        Assert.NotNull(context.Master1);
        Assert.NotEmpty(context.Masters.DictTestMaster);

        context.Cleanup();

        Assert.Null(context.Session);
        Assert.Null(context.Master1);
        Assert.Empty(context.Masters.DictTestMaster);
    }

    public void Dispose()
    {
        SetConfiguration(_originalConfiguration);
    }

    private static SystemContext CreateIsolatedSystemContext()
    {
        var constructor = typeof(SystemContext).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException("Unable to find private SystemContext constructor.");

        return (SystemContext)constructor.Invoke(null);
    }

    private static FieldInfo GetConfigurationField()
    {
        return typeof(ConfigurationHelper).GetField("_configuration", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ConfigurationHelper._configuration.");
    }

    private static object? GetSessionMaster(SampleTestSessionService session)
    {
        var field = typeof(SampleTestSessionService).GetField("_master", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find SampleTestSessionService._master.");

        return field.GetValue(session);
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

        GetConfigurationField().SetValue(null, configuration);
    }

    private static void SetConfiguration(IConfiguration? configuration)
    {
        GetConfigurationField().SetValue(null, configuration);
    }
}
