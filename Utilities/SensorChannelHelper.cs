using TestServer.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 传感器通道映射辅助类
    /// </summary>
    public static class SensorChannelHelper
    {
        public const int FurnaceTemp1SensorId = 0;
        public const int FurnaceTemp2SensorId = 1;
        public const int SurfaceTempSensorId = 2;
        public const int CenterTempSensorId = 3;
        public const int CalibrationTempSensorId = 16;

        public static double GetOutputValue(IReadOnlyDictionary<int, Sensor> sensors, int sensorId)
        {
            if (sensors == null)
            {
                throw new ArgumentNullException(nameof(sensors));
            }

            return sensors.TryGetValue(sensorId, out var sensor) ? sensor.Outputvalue : 0;
        }

        public static (double SurfaceTemp, double CenterTemp) GetSurfaceAndCenterTemperatures(
            IReadOnlyDictionary<int, Sensor> sensors)
        {
            return (
                GetOutputValue(sensors, SurfaceTempSensorId),
                GetOutputValue(sensors, CenterTempSensorId));
        }
    }
}
