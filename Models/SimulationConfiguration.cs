namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 仿真模式配置
    /// </summary>
    public class SimulationConfiguration
    {
        /// <summary>
        /// 是否启用仿真模式（总开关）
        /// </summary>
        public bool EnableSimulation { get; set; } = false;

        /// <summary>
        /// 是否模拟传感器数据
        /// </summary>
        public bool SimulateSensors { get; set; } = true;

        /// <summary>
        /// 是否模拟PID控制器
        /// </summary>
        public bool SimulatePidController { get; set; } = true;

        /// <summary>
        /// 初始炉温（°C）
        /// </summary>
        public double InitialFurnaceTemp { get; set; } = 25.0;

        /// <summary>
        /// 目标炉温（°C）
        /// </summary>
        public double TargetFurnaceTemp { get; set; } = 750.0;

        /// <summary>
        /// 升温速率（°C/秒）
        /// </summary>
        public double HeatingRatePerSecond { get; set; } = 5.0;

        /// <summary>
        /// 温度波动范围（°C）
        /// </summary>
        public double TempFluctuation { get; set; } = 0.5;

        /// <summary>
        /// 稳定判定阈值（°C）
        /// </summary>
        public double StableThreshold { get; set; } = 5.0;

        /// <summary>
        /// 是否模拟火焰事件
        /// </summary>
        public bool SimulateFlame { get; set; } = false;

        /// <summary>
        /// 火焰开始时间（试验开始后秒数）
        /// </summary>
        public int FlameStartTime { get; set; } = 120;

        /// <summary>
        /// 火焰持续时间（秒）
        /// </summary>
        public int FlameDuration { get; set; } = 8;
    }
}
