namespace ISO11820_2020.Models
{
    // 定义用于标识试验控制器实时返回消息的对象类型
    public class MasterMessage
    {
        public string Time { get; set; } = string.Empty;
        public string Message { get; set; } =string.Empty;
        public int FlameTime { get; set; } = 0;
        public int FlameDuration { get; set; } = 0;
    }
}
