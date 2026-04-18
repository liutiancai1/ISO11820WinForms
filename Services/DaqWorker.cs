using System.IO.Ports;
using System.Text;
using ISO11820WinForms.Core;
using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using ISO11820WinForms.Global;
using Serilog;

namespace ISO11820WinForms.Services
{
    // 注意：SensorDataEventArgs 类定义在 ISO11820WinForms.Core.EventArgs.cs 中
    // 包含 Timer, Temp1, Temp2, TempSurface, TempCenter, TempDrift, TempCalibration 字段

    /*
     * 类型名称: DaqWorker
     * 功能: WinForms版本的数据采集服务
     *      1. 初始化试验设备传感器连接
     *      2. 以小于1s的时间间隔刷新数据采集传感器最新采集数据
     *      3. 支持仿真模式，无需真实硬件即可测试
     */
    public class DaqWorker
    {
        //传感器集合对象
        private SensorDictionary _sensors;
        //用于定时查询传感器实时值的计时器
        private System.Threading.Timer? _timer;
        //串口读取异常计数器
        private int _counter;
        //串口对象
        SerialPort? _serialPort;
        //是否正在运行
        private bool _isRunning = false;
        //计时器（秒）
        private double _elapsedSeconds = 0;

        // 传感器数据更新事件
        public event EventHandler<SensorDataEventArgs>? SensorDataReceived;

        // 性能优化：缓存清理计数器（每5分钟清理一次过期缓存）
        private int _cacheCleanupCounter = 0;
        private const int CACHE_CLEANUP_INTERVAL = 375; // 5分钟 = 300秒 / 0.8秒 = 375次

        // 仿真模式支持
        private readonly SimulationConfiguration? _simulationConfig;
        private SensorSimulator? _simulator;
        private bool _isSimulationMode = false;

        //构造函数
        public DaqWorker(SensorDictionary sensors)
        {
            _sensors = sensors;
            //初始化串口异常计数器
            _counter = 0;
            
            // 加载仿真配置
            _simulationConfig = ConfigurationHelper.GetSection<SimulationConfiguration>("Simulation");
            _isSimulationMode = _simulationConfig?.EnableSimulation == true && _simulationConfig?.SimulateSensors == true;
            
            if (_isSimulationMode && _simulationConfig != null)
            {
                _simulator = new SensorSimulator(_simulationConfig);
                Log.Information("数据采集服务已启用仿真模式");
            }
        }

        /// <summary>
        /// 获取传感器模拟器（仅在仿真模式下可用）
        /// </summary>
        public SensorSimulator? Simulator => _simulator;

        /*
         * 功能: 启动数据采集服务
         */
        public void Start()
        {
            if (_isRunning)
            {
                Log.Warning("数据采集服务已在运行中，忽略重复启动请求");
                return;
            }

            Log.Information("启动数据采集服务");

            //从数据库加载传感器参数至内存
            using (var ctx = new ISO11820DbContext())
            {
                _sensors.Sensors = ctx.Sensors.ToDictionary(x => x.Sensorid);
                Log.Information("从数据库加载了 {Count} 个传感器配置", _sensors.Sensors.Count);
            }

            // 从配置文件读取串口设置
            var hardwareConfig = ConfigurationHelper.GetSection<HardwareConfiguration>("Hardware");
            string sensorPort = hardwareConfig?.SensorPort ?? "COM3";

            //初始化串口对象
            Log.Information("初始化串口对象: {Port}, 9600, 8N1", sensorPort);
            _serialPort = new SerialPort(sensorPort, 9600, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 200;  //设置读超时
            _serialPort.WriteTimeout = 200; //设置写超时
            _serialPort.Handshake = Handshake.None;
            _serialPort.NewLine = "\r"; //将默认NewLine字符('\n')改为Adam模块指令的结尾字符
            _serialPort.Encoding = Encoding.UTF8; //使用UTF8编码

            //创建定时任务
            _timer = new System.Threading.Timer(DoWork, null, Timeout.Infinite, 800);
            Log.Information("数据采集定时器已创建，采集间隔: 800ms");

            // 仿真模式直接启动定时器，真实硬件模式需要先打开串口
            if (_isSimulationMode)
            {
                _timer.Change(0, 800);
                Log.Information("仿真模式：数据采集定时器已启动");
            }
            else
            {
                // 真实硬件模式：打开串口后启动定时器
                try
                {
                    _serialPort.Open();
                    if (_serialPort.IsOpen)
                    {
                        _timer.Change(0, 800);
                        Log.Information("串口已打开，数据采集服务开始工作");
                    }
                    else
                    {
                        Log.Error("串口打开失败，无法启动数据采集");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "串口打开失败: {Message}", ex.Message);
                    throw new InvalidOperationException($"无法打开串口 {_serialPort.PortName}，请检查硬件连接", ex);
                }
            }

            _isRunning = true;
        }

        /*
         * 功能: 停止数据采集服务
         */
        public void Stop()
        {
            if (!_isRunning)
                return;

            //停止定时任务
            _timer?.Change(Timeout.Infinite, 0);
            
            //关闭串口
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                Console.WriteLine("DAQ service serial port closed...");
            }

            _isRunning = false;
        }

        /*
         * 功能: 定时从通信接口获取传感器数据并更新内存缓存
         * 说明: 
         *   - 一号炉传感器ID: 0(炉内温度1), 1(炉内温度2), 2(表面温度), 3(中心温度)
         *   - 校准热电偶传感器ID: 16
         *   - 支持仿真模式，无需真实硬件
         */
        private void DoWork(object? state)
        {
            // 累加计时器（每800ms执行一次，即0.8秒）
            _elapsedSeconds += 0.8;

            // 仿真模式：使用模拟器生成数据
            if (_isSimulationMode && _simulator != null)
            {
                DoSimulationWork();
                return;
            }

            // 真实硬件模式：从串口读取数据
            DoHardwareWork();
        }

        /// <summary>
        /// 仿真模式数据采集
        /// 修改：从 Modbus Slave 读取温度数据用于显示
        /// </summary>
        private void DoSimulationWork()
        {
            if (_simulator == null) return;

            // 更新模拟器状态（保留用于其他功能）
            _simulator.Update(_elapsedSeconds);

            // 从全局上下文获取 ApparatusManipulator 来读取 Modbus 温度
            double modbusTemp = 25.0; // 默认值
            try
            {
                var testMaster = SystemContext.Current.Master1;
                if (testMaster != null)
                {
                    // 读取 Modbus 地址 0x0102 (258) 的当前温度
                    var tempRaw = testMaster.Manipulator.GetCurrentTemp();
                    modbusTemp = tempRaw / 10.0; // 温度值除以10得到实际温度
                }
            }
            catch (Exception ex)
            {
                Log.Warning("从 Modbus 读取温度失败，使用默认值: {Message}", ex.Message);
            }

            // 触发传感器数据事件，使用 Modbus 温度
            OnSensorDataReceived(new SensorDataEventArgs
            {
                Timer = (int)_elapsedSeconds,
                Temp1 = modbusTemp,
                Temp2 = modbusTemp - 0.1,
                TempSurface = _simulator.SurfaceTemp,
                TempCenter = _simulator.CenterTemp,
                TempDrift = 0, // 温度漂移由 TestMaster 计算
                TempCalibration = _simulator.CalibrationTemp
            });

            // 性能优化：定期清理过期缓存
            CleanupCacheIfNeeded();
        }

        /// <summary>
        /// 真实硬件模式数据采集
        /// </summary>
        private void DoHardwareWork()
        {
            string cmd;
            string data = string.Empty;
            
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // 获取第一个4018+模块的通道值（一号炉温度传感器）
                    cmd = "#01";
                    _serialPort.WriteLine(cmd);
                    data = _serialPort.ReadLine();
                    if (data.Length == 57)
                    {
                        // 设置一号试验炉温度值
                        // 通道0: 炉内温度1, 通道1: 炉内温度2, 通道2: 表面温度, 通道3: 中心温度
                        if (_sensors.Sensors.ContainsKey(0))
                            _sensors.Sensors[0].SetInputValue(double.Parse(data.Substring(1 + 0 * 7, 7)));
                        if (_sensors.Sensors.ContainsKey(1))
                            _sensors.Sensors[1].SetInputValue(double.Parse(data.Substring(1 + 1 * 7, 7)));
                        if (_sensors.Sensors.ContainsKey(2))
                            _sensors.Sensors[2].SetInputValue(double.Parse(data.Substring(1 + 2 * 7, 7)));
                        if (_sensors.Sensors.ContainsKey(3))
                            _sensors.Sensors[3].SetInputValue(double.Parse(data.Substring(1 + 3 * 7, 7)));
                    }
                    
                    // 获取第二个4018+模块的通道值（校准热电偶等）
                    cmd = "#02";
                    data = string.Empty;
                    _serialPort.WriteLine(cmd);
                    data = _serialPort.ReadLine();
                    if (data.Length == 57)
                    {
                        // 通道0: 校准热电偶温度（传感器ID 16）
                        if (_sensors.Sensors.ContainsKey(16))
                            _sensors.Sensors[16].SetInputValue(double.Parse(data.Substring(1 + 0 * 7, 7)));
                    }
                }
                catch(TimeoutException e)
                {
                    // 处理串口操作超时异常
                    if(++_counter == 3)
                    {
                        //连续超时3次,系统认为串口出现意外中断情况
                        Log.Warning("串口连续超时3次，可能存在硬件故障");
                    }
                    Log.Debug("串口读取超时: {Message}", e.Message);
                }
                catch (Exception e)
                {
                    Log.Error(e, "串口读取异常");
                }
            }

            // 触发传感器数据事件
            // 传感器ID映射（一号炉）：
            // 0 - 炉内温度1, 1 - 炉内温度2, 2 - 表面温度, 3 - 中心温度
            // 16 - 校准热电偶温度
            OnSensorDataReceived(new SensorDataEventArgs
            {
                Timer = (int)_elapsedSeconds,
                Temp1 = _sensors.Sensors.ContainsKey(0) ? _sensors.Sensors[0].Outputvalue : 0,
                Temp2 = _sensors.Sensors.ContainsKey(1) ? _sensors.Sensors[1].Outputvalue : 0,
                TempSurface = _sensors.Sensors.ContainsKey(2) ? _sensors.Sensors[2].Outputvalue : 0,
                TempCenter = _sensors.Sensors.ContainsKey(3) ? _sensors.Sensors[3].Outputvalue : 0,
                TempDrift = 0, // 温度漂移由 TestMaster 计算
                TempCalibration = _sensors.Sensors.ContainsKey(16) ? _sensors.Sensors[16].Outputvalue : 0 // 校准热电偶温度（传感器ID为16）
            });

            // 性能优化：定期清理过期缓存
            CleanupCacheIfNeeded();
        }

        /// <summary>
        /// 定期清理过期缓存
        /// </summary>
        private void CleanupCacheIfNeeded()
        {

            _cacheCleanupCounter++;
            if (_cacheCleanupCounter >= CACHE_CLEANUP_INTERVAL)
            {
                _cacheCleanupCounter = 0;
                try
                {
                    CacheService.Instance.ClearExpiredCache();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "清理过期缓存失败");
                }
            }
        }

        /*
         * 功能: 触发传感器数据事件
         */
        protected virtual void OnSensorDataReceived(SensorDataEventArgs e)
        {
            SensorDataReceived?.Invoke(this, e);
        }

        /*
         * 功能: 重置计时器
         * 注意: 不重置模拟器状态，保持加热状态
         */
        public void ResetTimer()
        {
            _elapsedSeconds = 0;
            // 不调用 _simulator?.Reset()，避免中断加热状态
        }

        /// <summary>
        /// 是否处于仿真模式
        /// </summary>
        public bool IsSimulationMode => _isSimulationMode;

        /*
         * 功能: 释放资源
         */
        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            _serialPort?.Dispose();
        }
    }
}
