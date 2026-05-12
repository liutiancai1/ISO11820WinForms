using ISO11820WinForms.Core;
using ISO11820WinForms.Services;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class ModbusGatewayUsageTests
{
    [Fact]
    public void DaqWorker_Start_WhenGatewayProvided_ReadsAdamRegistersThroughSharedGateway()
    {
        var sensors = CreateSensors();
        var gateway = new FakeModbusRtuGateway(new ushort[] { 100, 200, 300, 400, 500 });
        var worker = new TestableDaqWorker(sensors, gateway);

        try
        {
            worker.Start();

            Assert.Single(gateway.ReadRequests);
            Assert.Equal((byte)1, gateway.ReadRequests[0].StationNumber);
            Assert.Equal((ushort)1, gateway.ReadRequests[0].StartAddress);
            Assert.Equal(8, gateway.ReadRequests[0].RegisterCount);
            Assert.Equal(100, sensors.Sensors[0].Outputvalue);
            Assert.Equal(200, sensors.Sensors[1].Outputvalue);
            Assert.Equal(300, sensors.Sensors[2].Outputvalue);
            Assert.Equal(400, sensors.Sensors[3].Outputvalue);
            Assert.Equal(500, sensors.Sensors[16].Outputvalue);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Fact]
    public void ApparatusManipulator_EstablishConnection_WhenGatewayProvided_UsesPidStationNumber()
    {
        var gateway = new FakeModbusRtuGateway(Array.Empty<ushort>());
        var manipulator = new ApparatusManipulator("COM9", "COM9", 6, 750, 2, gateway);

        var success = manipulator.EstablishConnection();

        Assert.True(success);
        Assert.Equal(3, gateway.WriteRequests.Count);
        Assert.All(gateway.WriteRequests, request => Assert.Equal((byte)2, request.StationNumber));
        Assert.Contains(gateway.WriteRequests, request => request.Address == 0x0000 && request.Value == 7500);
        Assert.Contains(gateway.WriteRequests, request => request.Address == 0x0002 && request.Value == 0);
        Assert.Contains(gateway.WriteRequests, request => request.Address == 0x0038 && request.Value == 3);
    }

    [Fact]
    public void DaqWorker_Start_WhenHoldingRegistersTimeout_FallsBackToInputRegisters()
    {
        var sensors = CreateSensors();
        var gateway = new FakeModbusRtuGateway(new ushort[] { 111, 222, 333, 444, 555 })
        {
            ThrowOnHoldingRead = true
        };
        var worker = new TestableDaqWorker(sensors, gateway);

        try
        {
            worker.Start();

            Assert.Single(gateway.HoldingReadRequests);
            Assert.Single(gateway.InputReadRequests);
            Assert.Equal(111, sensors.Sensors[0].Outputvalue);
            Assert.Equal(222, sensors.Sensors[1].Outputvalue);
            Assert.Equal(333, sensors.Sensors[2].Outputvalue);
            Assert.Equal(444, sensors.Sensors[3].Outputvalue);
            Assert.Equal(555, sensors.Sensors[16].Outputvalue);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Fact]
    public void DaqWorker_Start_WhenDetectionPrefersZeroBasedInputRegisters_UsesDetectedAddressAndRegisterKind()
    {
        var sensors = CreateSensors();
        var gateway = new FakeModbusRtuGateway(new ushort[] { 211, 222, 233, 244, 255 });
        var detectionResult = new ModbusSerialDetectionResult(true, ModbusSerialSettings.Default, 1, 0, "InputRegisters");
        var worker = new TestableDaqWorker(sensors, gateway, detectionResult);

        try
        {
            worker.Start();

            Assert.Empty(gateway.HoldingReadRequests);
            Assert.Single(gateway.InputReadRequests);
            Assert.Equal((byte)1, gateway.InputReadRequests[0].StationNumber);
            Assert.Equal((ushort)0, gateway.InputReadRequests[0].StartAddress);
            Assert.Equal(211, sensors.Sensors[0].Outputvalue);
            Assert.Equal(222, sensors.Sensors[1].Outputvalue);
            Assert.Equal(233, sensors.Sensors[2].Outputvalue);
            Assert.Equal(244, sensors.Sensors[3].Outputvalue);
            Assert.Equal(255, sensors.Sensors[16].Outputvalue);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Fact]
    public void DaqWorker_Start_WhenDetectionFindsDifferentStation_UsesDetectedStation()
    {
        var sensors = CreateSensors();
        var gateway = new FakeModbusRtuGateway(new ushort[] { 311, 322, 333, 344, 355 });
        var detectionResult = new ModbusSerialDetectionResult(true, ModbusSerialSettings.Default, 23, 1, "HoldingRegisters");
        var worker = new TestableDaqWorker(sensors, gateway, detectionResult);

        try
        {
            worker.Start();

            Assert.Single(gateway.HoldingReadRequests);
            Assert.Equal((byte)23, gateway.HoldingReadRequests[0].StationNumber);
            Assert.Equal((ushort)1, gateway.HoldingReadRequests[0].StartAddress);
            Assert.Equal(311, sensors.Sensors[0].Outputvalue);
            Assert.Equal(322, sensors.Sensors[1].Outputvalue);
            Assert.Equal(333, sensors.Sensors[2].Outputvalue);
            Assert.Equal(344, sensors.Sensors[3].Outputvalue);
            Assert.Equal(355, sensors.Sensors[16].Outputvalue);
        }
        finally
        {
            worker.Dispose();
        }
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

    private sealed class TestableDaqWorker : DaqWorker
    {
        private readonly SensorDictionary _sensorDictionary;

        public TestableDaqWorker(SensorDictionary sensors, IModbusRtuGateway gateway)
            : base(sensors, gateway)
        {
            _sensorDictionary = sensors;
        }

        public TestableDaqWorker(
            SensorDictionary sensors,
            IModbusRtuGateway gateway,
            ModbusSerialDetectionResult detectionResult)
            : base(sensors, gateway, detectionResult)
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
        public bool ThrowOnHoldingRead { get; set; }
        public List<ReadRequest> ReadRequests { get; } = new();
        public List<ReadRequest> HoldingReadRequests { get; } = new();
        public List<ReadRequest> InputReadRequests { get; } = new();
        public List<WriteRequest> WriteRequests { get; } = new();

        public ushort[] ReadHoldingRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            var request = new ReadRequest(stationNumber, startAddress, registerCount, timeoutMs);
            ReadRequests.Add(request);
            HoldingReadRequests.Add(request);

            if (ThrowOnHoldingRead)
            {
                throw new TimeoutException("holding register timeout");
            }

            return _registers;
        }

        public ushort[] ReadInputRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            var request = new ReadRequest(stationNumber, startAddress, registerCount, timeoutMs);
            ReadRequests.Add(request);
            InputReadRequests.Add(request);
            return _registers;
        }

        public void WriteSingleRegister(byte stationNumber, ushort address, ushort value, int timeoutMs)
        {
            WriteRequests.Add(new WriteRequest(stationNumber, address, value, timeoutMs));
        }

        public void Dispose()
        {
        }
    }

    private sealed record ReadRequest(byte StationNumber, ushort StartAddress, int RegisterCount, int TimeoutMs);
    private sealed record WriteRequest(byte StationNumber, ushort Address, ushort Value, int TimeoutMs);
}
