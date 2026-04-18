namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 火焰检测配置
    /// </summary>
    public class FlameDetectionConfiguration
    {
        /// <summary>
        /// 摄像头设备索引
        /// </summary>
        public int CameraIndex { get; set; } = 0;

        /// <summary>
        /// 视频帧宽度
        /// </summary>
        public int FrameWidth { get; set; } = 640;

        /// <summary>
        /// 视频帧高度
        /// </summary>
        public int FrameHeight { get; set; } = 480;

        /// <summary>
        /// 视频帧率（FPS）
        /// </summary>
        public int FrameRate { get; set; } = 30;

        /// <summary>
        /// 最大帧缓存大小
        /// </summary>
        public int MaxBufferSize { get; set; } = 1000;

        /// <summary>
        /// 预览帧率（FPS）
        /// </summary>
        public int PreviewFrameRate { get; set; } = 10;

        /// <summary>
        /// 火焰视频输出目录
        /// </summary>
        public string VideoOutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 火焰前后保留时间（秒）
        /// </summary>
        public int FlameBufferSeconds { get; set; } = 5;

        /// <summary>
        /// 是否启用火焰检测
        /// </summary>
        public bool EnableFlameDetection { get; set; } = true;

        /// <summary>
        /// 颜色阈值 - 色调最小值（HSV）
        /// </summary>
        public int HueMin { get; set; } = 0;

        /// <summary>
        /// 颜色阈值 - 色调最大值（HSV）
        /// </summary>
        public int HueMax { get; set; } = 30;

        /// <summary>
        /// 颜色阈值 - 饱和度最小值（HSV）
        /// </summary>
        public int SaturationMin { get; set; } = 100;

        /// <summary>
        /// 颜色阈值 - 亮度最小值（HSV）
        /// </summary>
        public int ValueMin { get; set; } = 100;

        /// <summary>
        /// 运动检测阈值
        /// </summary>
        public double MotionThreshold { get; set; } = 25.0;
    }
}
