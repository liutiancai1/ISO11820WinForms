using ISO11820WinForms.Services;
using TestServer.Models;
using Xunit;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class DaqWorkerAdamTests
{
    [Fact]
    public void BuildSensorReadCommand_WhenStationIsOne_ReturnsAdamAsciiCommand()
    {
        var worker = new TestableDaqWorker(new SensorDictionary());

        var command = worker.ExposeBuildSensorReadCommand(1);

        Assert.Equal("#01", command);
    }

    [Fact]
    public void ApplyAdamFrame_MapsCalibrationFromSameModuleChannelFour()
    {
        var sensors = CreateSensors();
        var worker = new TestableDaqWorker(sensors, calibrationChannelIndex: 4);

        worker.ExposeApplyAdamFrame(">001.000002.000003.000004.000005.000006.000007.000008.000");

        Assert.Equal(1.0, sensors.Sensors[0].Outputvalue);
        Assert.Equal(2.0, sensors.Sensors[1].Outputvalue);
        Assert.Equal(3.0, sensors.Sensors[2].Outputvalue);
        Assert.Equal(4.0, sensors.Sensors[3].Outputvalue);
        Assert.Equal(5.0, sensors.Sensors[16].Outputvalue);
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
        private readonly int _calibrationChannelIndex;

        public TestableDaqWorker(SensorDictionary sensors, int calibrationChannelIndex = 4)
            : base(sensors)
        {
            _calibrationChannelIndex = calibrationChannelIndex;
        }

        public string ExposeBuildSensorReadCommand(int stationNumber)
        {
            return BuildSensorReadCommand(stationNumber);
        }

        public void ExposeApplyAdamFrame(string frame)
        {
            ApplyAdamFrame(frame);
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
