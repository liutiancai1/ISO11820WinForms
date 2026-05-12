using ISO11820WinForms.Services;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class DaqWorkerModbusTests
{
    [Fact]
    public void ApplyModbusRegisters_MapsChannelsFromSingleModule()
    {
        var sensors = CreateDigitalSensors();
        var worker = new TestableDaqWorker(sensors, calibrationChannelIndex: 4);

        worker.ExposeApplyModbusRegisters(new ushort[] { 100, 200, 300, 400, 500 });

        Assert.Equal(100, sensors.Sensors[0].Outputvalue);
        Assert.Equal(200, sensors.Sensors[1].Outputvalue);
        Assert.Equal(300, sensors.Sensors[2].Outputvalue);
        Assert.Equal(400, sensors.Sensors[3].Outputvalue);
        Assert.Equal(500, sensors.Sensors[16].Outputvalue);
    }

    [Fact]
    public void ApplyModbusRegisters_WhenSensorHasRangeConfiguration_ConvertsRawValue()
    {
        var sensors = CreateAnalogSensors();
        var worker = new TestableDaqWorker(sensors, calibrationChannelIndex: 4);

        worker.ExposeApplyModbusRegisters(new ushort[] { 0, 32768, 65535, 16384, 49152 });

        Assert.Equal(0, sensors.Sensors[0].Outputvalue, 3);
        Assert.InRange(sensors.Sensors[1].Outputvalue, 499.9, 500.1);
        Assert.InRange(sensors.Sensors[2].Outputvalue, 999.9, 1000.1);
        Assert.InRange(sensors.Sensors[3].Outputvalue, 249.9, 250.1);
        Assert.InRange(sensors.Sensors[16].Outputvalue, 749.9, 750.1);
    }

    private static SensorDictionary CreateDigitalSensors()
    {
        return new SensorDictionary
        {
            Sensors = new Dictionary<int, Sensor>
            {
                [0] = CreateSensor(0, SignalType.Digital, configureRange: false),
                [1] = CreateSensor(1, SignalType.Digital, configureRange: false),
                [2] = CreateSensor(2, SignalType.Digital, configureRange: false),
                [3] = CreateSensor(3, SignalType.Digital, configureRange: false),
                [16] = CreateSensor(16, SignalType.Digital, configureRange: false)
            }
        };
    }

    private static SensorDictionary CreateAnalogSensors()
    {
        return new SensorDictionary
        {
            Sensors = new Dictionary<int, Sensor>
            {
                [0] = CreateSensor(0, SignalType.mV, configureRange: true),
                [1] = CreateSensor(1, SignalType.mV, configureRange: true),
                [2] = CreateSensor(2, SignalType.mV, configureRange: true),
                [3] = CreateSensor(3, SignalType.mV, configureRange: true),
                [16] = CreateSensor(16, SignalType.mV, configureRange: true)
            }
        };
    }

    private static Sensor CreateSensor(int sensorId, SignalType signalType, bool configureRange)
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
            Signaltype = (byte)signalType,
            Signalzero = configureRange ? 0 : 0,
            Signalspan = configureRange ? 65535 : 0,
            Outputzero = 0,
            Outputspan = configureRange ? 1000 : 0
        };
    }

    private sealed class TestableDaqWorker : DaqWorker
    {
        private readonly int _calibrationChannelIndex;

        public TestableDaqWorker(SensorDictionary sensors, int calibrationChannelIndex = 4)
            : base(sensors)
        {
            _calibrationChannelIndex = calibrationChannelIndex;
        }

        public void ExposeApplyModbusRegisters(IReadOnlyList<ushort> registers)
        {
            ApplyModbusRegisters(registers);
        }

        protected override int GetCalibrationChannelIndex()
        {
            return _calibrationChannelIndex;
        }

        protected override Dictionary<int, Sensor> LoadSensors()
        {
            return new Dictionary<int, Sensor>();
        }
    }
}
