using ISO11820WinForms.Utilities;
using TestServer.Models;
using Xunit;

namespace ISO11820WinForms.Tests.Utilities;

public class SensorChannelHelperTests
{
    [Fact]
    public void GetSurfaceAndCenterTemperatures_UsesSurfaceAndCenterSensorIds()
    {
        var sensors = new Dictionary<int, Sensor>
        {
            [0] = CreateSensor(100),
            [1] = CreateSensor(200),
            [2] = CreateSensor(300),
            [3] = CreateSensor(400)
        };

        var (surface, center) = SensorChannelHelper.GetSurfaceAndCenterTemperatures(sensors);

        Assert.Equal(300, surface);
        Assert.Equal(400, center);
    }

    private static Sensor CreateSensor(double outputValue)
    {
        return new Sensor
        {
            Sensorname = "sensor",
            Dispname = "sensor",
            Sensorgroup = "group",
            Unit = "C",
            Discription = "desc",
            Flag = "flag",
            Outputvalue = outputValue
        };
    }
}
