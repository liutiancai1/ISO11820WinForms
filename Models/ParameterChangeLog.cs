namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 参数变更日志
    /// </summary>
    public class ParameterChangeLog
    {
        /// <summary>
        /// 日志 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangeTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作员
        /// </summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// 设备 ID
        /// </summary>
        public int ApparatusId { get; set; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 旧值
        /// </summary>
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// 新值
        /// </summary>
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// 变更原因
        /// </summary>
        public string ChangeReason { get; set; } = string.Empty;
    }
}
