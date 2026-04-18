namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 校验记录
    /// </summary>
    public class CalibrationRecord
    {
        /// <summary>
        /// 记录唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 校验日期时间
        /// </summary>
        public DateTime CalibrationDate { get; set; }

        /// <summary>
        /// 校验类型（Surface=炉壁温度校验, Center=中心轴温度校验）
        /// </summary>
        public string CalibrationType { get; set; } = string.Empty;

        /// <summary>
        /// 设备 ID
        /// </summary>
        public int ApparatusId { get; set; }

        /// <summary>
        /// 操作员
        /// </summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// 温度数据点列表
        /// </summary>
        public List<TemperaturePoint> TemperatureData { get; set; } = new List<TemperaturePoint>();

        /// <summary>
        /// 均匀性结果
        /// </summary>
        public double? UniformityResult { get; set; }

        /// <summary>
        /// 最大偏差
        /// </summary>
        public double? MaxDeviation { get; set; }

        /// <summary>
        /// 平均温度
        /// </summary>
        public double? AverageTemperature { get; set; }

        /// <summary>
        /// 是否通过校验标准
        /// </summary>
        public bool PassedCriteria { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ========== 炉壁温度校验专用字段 ==========
        
        /// <summary>
        /// A1 位置温度
        /// </summary>
        public double? TempA1 { get; set; }

        /// <summary>
        /// A2 位置温度
        /// </summary>
        public double? TempA2 { get; set; }

        /// <summary>
        /// A3 位置温度
        /// </summary>
        public double? TempA3 { get; set; }

        /// <summary>
        /// B1 位置温度
        /// </summary>
        public double? TempB1 { get; set; }

        /// <summary>
        /// B2 位置温度
        /// </summary>
        public double? TempB2 { get; set; }

        /// <summary>
        /// B3 位置温度
        /// </summary>
        public double? TempB3 { get; set; }

        /// <summary>
        /// C1 位置温度
        /// </summary>
        public double? TempC1 { get; set; }

        /// <summary>
        /// C2 位置温度
        /// </summary>
        public double? TempC2 { get; set; }

        /// <summary>
        /// C3 位置温度
        /// </summary>
        public double? TempC3 { get; set; }

        // ========== 计算结果字段 ==========

        /// <summary>
        /// 平均温度
        /// </summary>
        public double? TAvg { get; set; }

        /// <summary>
        /// 轴1平均温度
        /// </summary>
        public double? TAvgAxis1 { get; set; }

        /// <summary>
        /// 轴2平均温度
        /// </summary>
        public double? TAvgAxis2 { get; set; }

        /// <summary>
        /// 轴3平均温度
        /// </summary>
        public double? TAvgAxis3 { get; set; }

        /// <summary>
        /// 层A平均温度
        /// </summary>
        public double? TAvgLevela { get; set; }

        /// <summary>
        /// 层B平均温度
        /// </summary>
        public double? TAvgLevelb { get; set; }

        /// <summary>
        /// 层C平均温度
        /// </summary>
        public double? TAvgLevelc { get; set; }

        /// <summary>
        /// 轴1温度偏差
        /// </summary>
        public double? TDevAxis1 { get; set; }

        /// <summary>
        /// 轴2温度偏差
        /// </summary>
        public double? TDevAxis2 { get; set; }

        /// <summary>
        /// 轴3温度偏差
        /// </summary>
        public double? TDevAxis3 { get; set; }

        /// <summary>
        /// 层A温度偏差
        /// </summary>
        public double? TDevLevela { get; set; }

        /// <summary>
        /// 层B温度偏差
        /// </summary>
        public double? TDevLevelb { get; set; }

        /// <summary>
        /// 层C温度偏差
        /// </summary>
        public double? TDevLevelc { get; set; }

        /// <summary>
        /// 轴向平均偏差
        /// </summary>
        public double? TAvgDevAxis { get; set; }

        /// <summary>
        /// 层向平均偏差
        /// </summary>
        public double? TAvgDevLevel { get; set; }

        // ========== 中心轴温度校验专用字段 ==========

        /// <summary>
        /// 中心轴温度数据（JSON格式）
        /// </summary>
        public string? CenterTempData { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 温度数据点
    /// </summary>
    public class TemperaturePoint
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 温度值
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 传感器位置
        /// </summary>
        public string SensorPosition { get; set; } = string.Empty;
    }

    /// <summary>
    /// 校验记录摘要（用于索引）
    /// </summary>
    public class CalibrationRecordSummary
    {
        /// <summary>
        /// 记录 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 校验日期
        /// </summary>
        public DateTime CalibrationDate { get; set; }

        /// <summary>
        /// 校验类型
        /// </summary>
        public string CalibrationType { get; set; } = string.Empty;

        /// <summary>
        /// 操作员
        /// </summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// 是否通过
        /// </summary>
        public bool PassedCriteria { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 校验索引
    /// </summary>
    public class CalibrationIndex
    {
        /// <summary>
        /// 记录摘要列表
        /// </summary>
        public List<CalibrationRecordSummary> Records { get; set; } = new List<CalibrationRecordSummary>();
    }
}
