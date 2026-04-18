using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Serilog;
using System.Drawing;

namespace ISO11820WinForms.Core
{
    /// <summary>
    /// 火焰分析器
    /// 使用Emgu.CV进行视频流火焰检测
    /// Requirements: 2.1, 2.2, 2.3, 2.4
    /// </summary>
    public class FlameAnalyzer : IDisposable
    {
        #region 字段和属性

        /// <summary>
        /// 分析器ID（同试验控制器ID）
        /// </summary>
        public int AnalyzerId { get; set; }

        /// <summary>
        /// 视频流URL
        /// </summary>
        private readonly string _videoUrl;

        /// <summary>
        /// 视频流操作对象
        /// </summary>
        private VideoCapture? _videoCapture;

        /// <summary>
        /// 视频操作锁定对象
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 分析帧的目标尺寸（用于提高分析效率）
        /// </summary>
        private readonly Size _newSize = new Size(240, 160);

        /// <summary>
        /// ROI区域边界坐标数组
        /// </summary>
        private readonly List<Point> _roiPts = new List<Point>();

        /// <summary>
        /// 背景移除对象（用于运动检测）
        /// </summary>
        private BackgroundSubtractorMOG2? _substractor;

        /// <summary>
        /// ROI区域掩膜
        /// </summary>
        private Mat? _mask;

        /// <summary>
        /// 当前帧原始图
        /// </summary>
        private Mat _frame = new Mat();

        /// <summary>
        /// 当前帧Mask图
        /// </summary>
        private Mat _maskFrame = new Mat();

        /// <summary>
        /// 调整尺寸后的图片
        /// </summary>
        private Mat _sizedFrame = new Mat();

        /// <summary>
        /// 用于输出视频文件的火焰帧缓存
        /// Requirement 2.4: 实现火焰帧缓存
        /// </summary>
        private readonly List<Mat> _flameFrames = new List<Mat>();

        /// <summary>
        /// 颜色检测轮廓
        /// </summary>
        private VectorOfVectorOfPoint _contForColor = new VectorOfVectorOfPoint();

        /// <summary>
        /// 运动检测轮廓
        /// </summary>
        private VectorOfVectorOfPoint _contForMotion = new VectorOfVectorOfPoint();

        /// <summary>
        /// 指示当前帧是否检测出火焰
        /// </summary>
        private bool _detected;

        /// <summary>
        /// 当前试验过程的第一火焰帧发生时间
        /// </summary>
        private DateTime _dtFirstFlameTime;

        /// <summary>
        /// 火焰检测过程中前一火焰帧的发生时间
        /// </summary>
        private DateTime _dtPreFlameTime;

        /// <summary>
        /// 火焰检测过程中当前火焰帧的发生时间
        /// </summary>
        private DateTime _dtCurFlameTime;

        /// <summary>
        /// 指示分析器是否正在运行
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// 指示是否已释放资源
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger _logger;

        #endregion

        #region 事件

        /// <summary>
        /// 火焰检测事件
        /// Requirement 2.3: 检测到持续火焰事件时触发
        /// </summary>
        public event EventHandler<FlameEventArgs>? FlameDetected;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">分析器ID（同试验控制器ID）</param>
        /// <param name="url">摄像头RTSP协议字符串</param>
        public FlameAnalyzer(int id, string url)
        {
            AnalyzerId = id;
            _videoUrl = url;
            _logger = Log.ForContext<FlameAnalyzer>();

            // 初始化火焰分析相关变量
            _detected = false;
            _dtFirstFlameTime = DateTime.Now;
            _dtPreFlameTime = DateTime.Now;
            _dtCurFlameTime = DateTime.Now;
            _isRunning = false;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载ROI边界坐标
        /// </summary>
        /// <param name="filepath">包含分析区域ROI边界坐标的csv文件</param>
        public void LoadROI(string filepath)
        {
            try
            {
                _roiPts.Clear();
                using (var reader = new StreamReader(filepath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var values = line.Split(',');
                            if (values.Length >= 2)
                            {
                                _roiPts.Add(new Point(
                                    Convert.ToInt32(values[0]),
                                    Convert.ToInt32(values[1])));
                            }
                        }
                    }
                }
                _logger.Information("加载ROI坐标成功，共 {Count} 个点", _roiPts.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载ROI坐标失败: {FilePath}", filepath);
            }
        }

        /// <summary>
        /// 制作ROI区域Mask
        /// </summary>
        public void SetMask()
        {
            if (_roiPts.Count == 0)
            {
                _logger.Warning("ROI坐标为空，无法创建Mask");
                return;
            }

            try
            {
                // 初始化Mask
                _mask = Mat.Zeros(_newSize.Height, _newSize.Width, DepthType.Cv8U, 1);
                
                // 将System.Drawing.Point[]转换为CV.IInputArray类型
                using var vp = new VectorOfPoint(_roiPts.ToArray());
                
                // 制作ROI区域Mask
                CvInvoke.FillConvexPoly(_mask, vp, new MCvScalar(255));
                
                _logger.Information("ROI Mask创建成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "创建ROI Mask失败");
            }
        }

        /// <summary>
        /// 开始执行火焰检测程序
        /// Requirement 2.1: 接收视频帧并分析
        /// </summary>
        public void StartAnalyzing()
        {
            if (_isRunning)
            {
                _logger.Warning("火焰分析器已在运行中");
                return;
            }

            try
            {
                // 初始化背景移除对象
                _substractor = new BackgroundSubtractorMOG2(shadowDetection: false);

                // 如果没有设置Mask，创建默认的全区域Mask
                if (_mask == null)
                {
                    _mask = Mat.Zeros(_newSize.Height, _newSize.Width, DepthType.Cv8U, 1);
                    _mask.SetTo(new MCvScalar(255));
                }

                // 初始化视频捕获对象
                _videoCapture = new VideoCapture(_videoUrl);
                
                if (!_videoCapture.IsOpened)
                {
                    // Requirement 2.5: 视频捕获失败时记录错误日志并继续试验操作
                    _logger.Error("无法打开视频流: {Url}", _videoUrl);
                    return;
                }

                _videoCapture.ImageGrabbed += Capture_ImageGrabbed;
                _videoCapture.Start();
                _isRunning = true;

                // 重置火焰检测状态
                _detected = false;
                _flameFrames.Clear();

                _logger.Information("火焰分析器启动成功");
            }
            catch (Exception ex)
            {
                // Requirement 2.5: 视频捕获失败时记录错误日志并继续试验操作
                _logger.Error(ex, "启动火焰分析器失败");
                _isRunning = false;
            }
        }

        /// <summary>
        /// 停止执行火焰检测程序
        /// </summary>
        public void StopAnalyzing()
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                _isRunning = false;

                if (_videoCapture != null)
                {
                    _videoCapture.ImageGrabbed -= Capture_ImageGrabbed;
                    _videoCapture.Stop();
                    _videoCapture.Dispose();
                    _videoCapture = null;
                }

                _logger.Information("火焰分析器已停止");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "停止火焰分析器时发生错误");
            }
        }

        /// <summary>
        /// 保存火焰帧至视频文件
        /// Requirement 2.4: 试验记录停止时将火焰视频帧输出到文件
        /// </summary>
        /// <param name="filename">视频文件的输出地址</param>
        /// <returns>是否成功输出</returns>
        public async Task<bool> OutputFlameFramesAsync(string filename)
        {
            return await Task.Run(() =>
            {
                if (_flameFrames.Count == 0)
                {
                    _logger.Information("没有火焰帧需要输出");
                    return true;
                }

                try
                {
                    // 确保输出目录存在
                    var directory = Path.GetDirectoryName(filename);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 初始化视频输出对象
                    using var writer = new VideoWriter(
                        filename,
                        VideoWriter.Fourcc('X', 'V', 'I', 'D'),
                        25, // 帧率
                        new Size(640, 480),
                        true);

                    if (!writer.IsOpened)
                    {
                        _logger.Error("无法创建视频输出文件: {Filename}", filename);
                        return false;
                    }

                    // 逐帧输出视频文件
                    foreach (var frame in _flameFrames)
                    {
                        if (!frame.IsEmpty)
                        {
                            // 调整帧大小到输出尺寸
                            using var resizedFrame = new Mat();
                            CvInvoke.Resize(frame, resizedFrame, new Size(640, 480));
                            writer.Write(resizedFrame);
                        }
                    }

                    _logger.Information("火焰视频输出成功: {Filename}, 共 {Count} 帧", 
                        filename, _flameFrames.Count);

                    // 清空火焰帧缓存
                    ClearFlameFrames();

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "输出火焰视频失败: {Filename}", filename);
                    return false;
                }
            });
        }

        /// <summary>
        /// 获取当前缓存的火焰帧数量
        /// </summary>
        public int GetFlameFrameCount()
        {
            return _flameFrames.Count;
        }

        /// <summary>
        /// 清空火焰帧缓存
        /// </summary>
        public void ClearFlameFrames()
        {
            foreach (var frame in _flameFrames)
            {
                frame?.Dispose();
            }
            _flameFrames.Clear();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 视频帧捕获事件处理
        /// Requirement 2.1: 接收视频帧并分析
        /// </summary>
        private void Capture_ImageGrabbed(object? sender, EventArgs e)
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                lock (_lockObj)
                {
                    if (_videoCapture == null || !_videoCapture.IsOpened)
                    {
                        return;
                    }

                    if (!_videoCapture.Retrieve(_frame))
                    {
                        return;
                    }

                    if (_frame.IsEmpty)
                    {
                        return;
                    }

                    // 记录当前帧的获取时间
                    _dtCurFlameTime = DateTime.Now;

                    // 提取炉芯ROI区域
                    if (_mask != null && !_mask.IsEmpty)
                    {
                        _frame.CopyTo(_maskFrame, _mask);
                    }
                    else
                    {
                        _frame.CopyTo(_maskFrame);
                    }

                    // 调整帧大小以提高分析效率
                    CvInvoke.Resize(_maskFrame, _sizedFrame, _newSize, 0, 0);

                    // 执行火焰检测
                    DoFlameAnalyze();
                }
            }
            catch (Exception ex)
            {
                // Requirement 2.5: 视频捕获失败时记录错误日志并继续试验操作
                _logger.Warning(ex, "处理视频帧时发生错误");
            }
        }

        /// <summary>
        /// 火焰分析函数
        /// Requirement 2.1: 使用颜色和运动检测分析每一帧是否存在火焰
        /// Requirement 2.2: 连续5秒或更长时间检测到火焰时记录
        /// Requirement 2.3: 检测到持续火焰事件时触发事件并停止后续检测
        /// </summary>
        private void DoFlameAnalyze()
        {
            if (_substractor == null)
            {
                return;
            }

            using var gray = new Mat();
            using var temp1 = new Mat();
            using var temp2 = new Mat();

            _contForColor.Clear();
            _contForMotion.Clear();

            // 转换为灰度图
            CvInvoke.CvtColor(_sizedFrame, gray, ColorConversion.Bgr2Gray);

            // 基于颜色的判定
            // 使用高斯模糊减少噪声
            CvInvoke.GaussianBlur(gray, temp1, new Size(9, 9), 0);
            // 二值化处理，阈值160用于检测高亮区域（火焰通常是高亮的）
            CvInvoke.Threshold(temp1, temp1, 160, 255, ThresholdType.Binary);
            // 查找轮廓
            CvInvoke.FindContours(temp1, _contForColor, null, RetrType.External, ChainApproxMethod.ChainApproxNone);

            // 基于动作追踪的判定
            // 使用背景减除检测运动
            _substractor.Apply(_sizedFrame, temp2, 0.4);
            CvInvoke.FindContours(temp2, _contForMotion, null, RetrType.External, ChainApproxMethod.ChainApproxNone);

            // 对运动轮廓进行排序，取面积最大的作为判定依据
            var lstConts = new List<VectorOfPoint>();
            for (var i = 0; i < _contForMotion.Size; i++)
            {
                lstConts.Add(_contForMotion[i]);
            }
            lstConts.Sort((cnt1, cnt2) =>
            {
                return CvInvoke.ContourArea(cnt2).CompareTo(CvInvoke.ContourArea(cnt1));
            });

            // 判定当前帧是否有火焰
            // 条件：颜色检测有轮廓 AND 运动检测有轮廓 AND 最大运动轮廓面积 > 0
            bool hasFlame = _contForColor.Size > 0 
                && _contForMotion.Size > 0 
                && lstConts.Count > 0 
                && CvInvoke.ContourArea(lstConts[0]) > 0;

            if (hasFlame)
            {
                if (!_detected)
                {
                    // 记录第一帧火焰产生的时间戳
                    _dtFirstFlameTime = _dtCurFlameTime;
                    // 设置火焰检测标志
                    _detected = true;
                }

                // 更新前一火焰帧的时间戳
                _dtPreFlameTime = _dtCurFlameTime;

                // 记录当前帧画面（克隆以避免被覆盖）
                _flameFrames.Add(_frame.Clone());
            }
            else if (_detected)
            {
                // 当前帧无火焰，但尚处于连续火焰过程中
                // 时间间隔小于1秒的情况，认为是连续火焰帧
                if ((_dtCurFlameTime - _dtPreFlameTime).TotalMilliseconds < 1000)
                {
                    // 记录当前帧画面
                    _flameFrames.Add(_frame.Clone());
                }
                else
                {
                    // 火焰中断超过1秒，判断本次连续火焰持续时间
                    _detected = false;

                    int flameDuration = (int)(_dtPreFlameTime - _dtFirstFlameTime).TotalSeconds;

                    // Requirement 2.2: 连续5秒或更长时间检测到火焰
                    if (flameDuration >= 5)
                    {
                        // Requirement 2.3: 停止后续检测
                        _videoCapture?.Stop();
                        _isRunning = false;

                        // 触发火焰检测事件
                        FireFlameDetected(new FlameEventArgs
                        {
                            Time = _dtFirstFlameTime,
                            Duration = flameDuration
                        });

                        _logger.Information("检测到持续火焰: 起始时间={Time}, 持续时间={Duration}秒",
                            _dtFirstFlameTime, flameDuration);
                    }
                    else
                    {
                        // 火焰持续时间不足5秒，清空缓存继续检测
                        ClearFlameFrames();
                    }
                }
            }
        }

        /// <summary>
        /// 触发火焰检测事件
        /// </summary>
        /// <param name="e">火焰事件参数</param>
        protected virtual void FireFlameDetected(FlameEventArgs e)
        {
            FlameDetected?.Invoke(this, e);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                StopAnalyzing();

                _mask?.Dispose();
                _frame?.Dispose();
                _maskFrame?.Dispose();
                _sizedFrame?.Dispose();
                _substractor?.Dispose();
                _contForColor?.Dispose();
                _contForMotion?.Dispose();

                ClearFlameFrames();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~FlameAnalyzer()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// 火焰事件参数
    /// </summary>
    public class FlameEventArgs : EventArgs
    {
        /// <summary>
        /// 起火时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 持续燃烧时间（秒）
        /// </summary>
        public int Duration { get; set; }
    }
}
