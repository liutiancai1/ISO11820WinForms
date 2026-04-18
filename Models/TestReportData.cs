namespace TestServer.Models
{
    /// <summary>
    /// 试验报告数据
    /// </summary>
    public class TestReportData
    {
        /// <summary>
        /// 试验主记录
        /// </summary>
        public Testmaster TestInfo { get; set; } = null!;

        /// <summary>
        /// 产品信息
        /// </summary>
        public Productmaster ProductInfo { get; set; } = null!;

        /// <summary>
        /// 传感器数据列表
        /// </summary>
        public List<SensorDataPoint> SensorData { get; set; } = new();

        /// <summary>
        /// 设备信息
        /// </summary>
        public Apparatus ApparatusInfo { get; set; } = null!;
    }

    /// <summary>
    /// 传感器数据点
    /// </summary>
    public class SensorDataPoint
    {
        /// <summary>
        /// 时间戳（秒）
        /// </summary>
        public int TimeStamp { get; set; }

        /// <summary>
        /// 炉壁温度1
        /// </summary>
        public double Tf1 { get; set; }

        /// <summary>
        /// 炉壁温度2
        /// </summary>
        public double Tf2 { get; set; }

        /// <summary>
        /// 样品温度
        /// </summary>
        public double Ts { get; set; }

        /// <summary>
        /// 中心温度
        /// </summary>
        public double Tc { get; set; }
    }
}
