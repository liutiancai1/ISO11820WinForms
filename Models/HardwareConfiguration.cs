namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 硬件配置
    /// </summary>
    public class HardwareConfiguration
    {
        /// <summary>
        /// PID控制器串口
        /// </summary>
        public string PidPort { get; set; } = "COM1";

        /// <summary>
        /// 电力调整器串口
        /// </summary>
        public string PowerPort { get; set; } = "COM2";

        /// <summary>
        /// 传感器数据采集串口
        /// </summary>
        public string SensorPort { get; set; } = "COM3";

        /// <summary>
        /// 恒功率输出值
        /// </summary>
        public short ConstPower { get; set; } = 2048;

        /// <summary>
        /// PID控制目标温度（°C）
        /// </summary>
        public short PidTemperature { get; set; } = 750;

        public string SensorProtocol { get; set; } = "ModbusRtu";

        public int SensorStationNumber { get; set; } = 1;

        public int PidStationNumber { get; set; } = 2;

        public int SensorRegisterStartAddress { get; set; } = 1;

        public int SensorRegisterCount { get; set; } = 8;

        public int SensorReadTimeoutMs { get; set; } = 1000;

        public int CalibrationChannelIndex { get; set; } = 4;
    }
}
