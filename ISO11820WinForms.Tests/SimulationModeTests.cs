using System.Reflection;
using ISO11820WinForms.Core;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Microsoft.Extensions.Configuration;
using Xunit;
using Sensor = TestServer.Models.Sensor;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class SimulationModeTests : IDisposable
{
    public SimulationModeTests()
    {
        SetConfiguration(new Dictionary<string, string?>
        {
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
            ["Simulation:StableThreshold"] = "5"
        });
    }

    [Fact]
    public void DaqWorker_Start_WhenSimulationEnabled_UsesSimulatorInsteadOfHardware()
    {
        var worker = new TestableDaqWorker(new SensorDictionary());

        try
        {
            worker.Start();

            Assert.True(worker.IsSimulationMode);
            Assert.NotNull(worker.Simulator);
            Assert.True(worker.IsSensorConnected);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Fact]
    public void ApparatusManipulator_WhenSimulationEnabled_DoesNotRequireSerialPort()
    {
        var manipulator = new ApparatusManipulator("COM998", "COM998", 2048, 750, 2);
        var simulator = new SensorSimulator(ConfigurationHelper.GetSection<ISO11820WinForms.Models.SimulationConfiguration>("Simulation")!);

        manipulator.SetSimulator(simulator);

        Assert.True(manipulator.EstablishConnection());
        Assert.True(manipulator.IsSimulationMode);
        Assert.True(manipulator.IsConnected);
        Assert.True(manipulator.StartHeating());
        Assert.True(simulator.IsHeating);
    }

    [Fact]
    public void TestMaster1_StartAndStopRecording_UsesOverrideBehaviorThroughBaseReference()
    {
        var sensors = new SensorDictionary();
        var simulator = new SensorSimulator(ConfigurationHelper.GetSection<ISO11820WinForms.Models.SimulationConfiguration>("Simulation")!);
        var manipulator = new ApparatusManipulator("COM998", "COM998", 2048, 750, 2);
        manipulator.SetSimulator(simulator);

        var master = new TestMaster1(null!, sensors, manipulator);
        master.SetSimulator(simulator);
        TestMaster baseReference = master;

        Assert.True(baseReference.StartRecording());
        Assert.True(simulator.IsRecording);

        Assert.True(baseReference.StopRecording());
        Assert.False(simulator.IsRecording);
    }

    public void Dispose()
    {
        SetConfiguration(null);
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

    private sealed class TestableDaqWorker : DaqWorker
    {
        public TestableDaqWorker(SensorDictionary sensors)
            : base(sensors)
        {
        }

        protected override Dictionary<int, Sensor> LoadSensors()
        {
            return new Dictionary<int, Sensor>();
        }
    }
}
