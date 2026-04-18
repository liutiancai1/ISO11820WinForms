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
    }
}
