using ISO11820WinForms.Services;
using System.IO.Ports;
using Xunit;
using Sensor = TestServer.Models.Sensor;
using SensorDictionary = ISO11820WinForms.Core.SensorDictionary;

namespace ISO11820WinForms.Tests;

public class DaqWorkerStartupTests
{
    [Fact]
    public void Start_WhenSensorPortUnavailable_DoesNotThrow()
    {
        var worker = new TestableDaqWorker(new SensorDictionary());

        try
        {
            var exception = Record.Exception(() => worker.Start());

            Assert.Null(exception);
            Assert.False(worker.IsSensorConnected);
        }
        finally
        {
            worker.Dispose();
        }
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

        protected override SerialPort CreateSerialPort(string sensorPort)
        {
            return new SerialPort(sensorPort, 9600, Parity.None, 8, StopBits.One);
        }
    }
}
