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

public class HardwareDiagnosticsTests : IDisposable
{
    [Fact]
    public void DaqWorker_ProbeNow_WhenGatewayReadSucceeds_ReturnsTrueAndRefreshesConnectionState()
    {
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Hardware:SensorPort"] = "COM9",
            ["Hardware:SensorProtocol"] = "ModbusRtu",
            ["Hardware:SensorStationNumber"] = "1",
            ["Hardware:SensorRegisterStartAddress"] = "1",
            ["Hardware:SensorRegisterCount"] = "8",
            ["Hardware:SensorReadTimeoutMs"] = "50",
            ["Simulation:EnableSimulation"] = "false",
            ["Simulation:SimulateSensors"] = "false"
        });

        var sensors = CreateSensors();
        var gateway = new FakeModbusRtuGateway(new ushort[] { 100, 200, 300, 400, 500 });
        var worker = new TestableDaqWorker(sensors, gateway);

        try
        {
            worker.Start();
            var initialReadCount = gateway.ReadRequests.Count;

            var success = worker.ProbeNow();

            Assert.True(success);
            Assert.True(worker.IsSensorConnected);
            Assert.Null(worker.LastConnectionError);
            Assert.True(gateway.ReadRequests.Count > initialReadCount);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Fact]
    public void HardwareDiagnosticService_ProbePid_WhenSimulationEnabled_ReturnsSimulationSuccess()
    {
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Simulation:EnableSimulation"] = "true",
            ["Simulation:SimulatePidController"] = "true"
        });

        var manipulator = new ApparatusManipulator("COM998", "COM998", 2048, 750, 2);
        var service = new HardwareDiagnosticService();

        var result = service.ProbePid(manipulator);

        Assert.True(result.Success);
        Assert.Equal("PID 控制器", result.DeviceName);
        Assert.Contains("仿真", result.Detail);
    }

    [Fact]
    public void HardwareDiagnosticService_ProbeSensor_WhenSimulationEnabled_ReturnsSimulationSuccess()
    {
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Hardware:SensorPort"] = "COM998",
            ["Simulation:EnableSimulation"] = "true",
            ["Simulation:SimulateSensors"] = "true"
        });

        var worker = new TestableDaqWorker(CreateSensors(), new FakeModbusRtuGateway(Array.Empty<ushort>()));
        var service = new HardwareDiagnosticService();

        try
        {
            worker.Start();

            var result = service.ProbeSensor(worker);

            Assert.True(result.Success);
            Assert.Equal("ADAM 采集", result.DeviceName);
            Assert.Contains("仿真", result.Detail);
        }
        finally
        {
            worker.Dispose();
        }
    }

    public void Dispose()
    {
        SetConfiguration(null);
    }

    private static SensorDictionary CreateSensors()
    {
        return new SensorDictionary
        {
            Sensors = new Dictionary<int, Sensor>
            {
                [0] = CreateSensor(0),
                [1] = CreateSensor(1),
                [2] = CreateSensor(2),
                [3] = CreateSensor(3),
                [16] = CreateSensor(16)
            }
        };
    }

    private static Sensor CreateSensor(int sensorId)
    {
        return new Sensor
        {
            Sensorid = sensorId,
            Sensorname = $"S{sensorId}",
            Dispname = $"S{sensorId}",
            Sensorgroup = "test",
            Unit = "C",
            Discription = "test",
            Flag = string.Empty,
            Signaltype = (byte)SignalType.Digital
        };
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
        private readonly SensorDictionary _sensorDictionary;

        public TestableDaqWorker(SensorDictionary sensors, IModbusRtuGateway gateway)
            : base(sensors, gateway)
        {
            _sensorDictionary = sensors;
        }

        protected override Dictionary<int, Sensor> LoadSensors()
        {
            return _sensorDictionary.Sensors;
        }

        protected override bool UseModbusSensorProtocol()
        {
            return true;
        }
    }

    private sealed class FakeModbusRtuGateway : IModbusRtuGateway
    {
        private readonly ushort[] _registers;

        public FakeModbusRtuGateway(ushort[] registers)
        {
            _registers = registers;
        }

        public ModbusSerialSettings Settings { get; } = ModbusSerialSettings.Default;
        public List<ReadRequest> ReadRequests { get; } = new();

        public ushort[] ReadHoldingRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            ReadRequests.Add(new ReadRequest(stationNumber, startAddress, registerCount, timeoutMs));
            return _registers;
        }

        public ushort[] ReadInputRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            ReadRequests.Add(new ReadRequest(stationNumber, startAddress, registerCount, timeoutMs));
            return _registers;
        }

        public void WriteSingleRegister(byte stationNumber, ushort address, ushort value, int timeoutMs)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed record ReadRequest(byte StationNumber, ushort StartAddress, int RegisterCount, int TimeoutMs);
}
