using ISO11820WinForms.Core;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Serilog;

namespace ISO11820WinForms.Global
{
    /*
     * 系统全局上下文单例
     * 管理系统级别的共享资源和服务
     */
    public class SystemContext
    {
        private static SystemContext? _instance;
        private static readonly object _lock = new object();

        // 单例实例
        public static SystemContext Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SystemContext();
                        }
                    }
                }
                return _instance;
            }
        }

        // 传感器字典
        public SensorDictionary Sensors { get; private set; }
        
        // 试验控制器集合
        public TestMasters Masters { get; private set; }
        
        // 数据采集服务
        public DaqWorker Daq { get; private set; }
        
        // 全局配置对象
        public AppGlobalWinForms Global { get; private set; }

        // 一号试验炉控制器（TestMaster1实例）
        // Requirement 7.1: 使用TestMaster1替代TestMaster
        public TestMaster1? Master1 { get; private set; }

        // 私有构造函数
        private SystemContext()
        {
            Sensors = new SensorDictionary();
            Masters = new TestMasters();
            Global = new AppGlobalWinForms();
            Daq = new DaqWorker(Sensors);
        }

        /*
         * 功能: 初始化系统上下文
         * 说明: 
         *   1. 加载全局配置
         *   2. 启动数据采集服务
         *   3. 创建默认试验控制器
         */
        public void Init()
        {
            Log.Information("开始初始化系统上下文");

            // 初始化配置服务和目录结构
            var configService = Services.ConfigurationService.Instance;
            if (!configService.ValidateConfiguration())
            {
                Log.Warning("配置验证失败，但系统将继续初始化");
            }

            if (!configService.InitializeDirectories())
            {
                Log.Warning("目录初始化失败，某些功能可能受影响");
            }

            Log.Information(configService.GetConfigurationSummary());

            // 启动数据采集服务
            Daq.Start();
            Log.Information("数据采集服务已启动");

            // 从配置文件读取硬件参数
            string pidPort = ConfigurationHelper.GetPidPort();
            string powerPort = ConfigurationHelper.GetPowerPort();
            int constPower = ConfigurationHelper.GetConstPower();
            int pidTemperature = ConfigurationHelper.GetPidTemperature();

            Log.Information("硬件配置: PID端口={PidPort}, 功率端口={PowerPort}, 恒功率={ConstPower}, PID温度={PidTemperature}", 
                pidPort, powerPort, constPower, pidTemperature);

            // 创建一个默认的TestMaster实例
            // 优先从Global中获取设备配置信息，如果没有则使用配置文件中的默认值
            var apparatus = Global.DictApparatus.Values.FirstOrDefault();
            if (apparatus != null)
            {
                // 使用数据库中的设备配置，如果为空则使用配置文件中的值
                pidPort = apparatus.Pidport ?? pidPort;
                powerPort = apparatus.Powerport ?? powerPort;
                constPower = (int)(apparatus.Constpower ?? constPower);
                
                Log.Information("使用数据库设备配置: PID端口={PidPort}, 功率端口={PowerPort}, 恒功率={ConstPower}", 
                    pidPort, powerPort, constPower);
            }
            else
            {
                Log.Warning("未找到数据库设备配置信息，使用配置文件中的默认值");
            }

            // 创建设备操作对象
            var manipulator = new ApparatusManipulator(
                pidPort,
                powerPort,
                (Int16)constPower,
                (Int16)pidTemperature
            );

            // 仿真模式：将DaqWorker的模拟器传递给ApparatusManipulator
            if (Daq.IsSimulationMode && Daq.Simulator != null)
            {
                manipulator.SetSimulator(Daq.Simulator);
                Log.Information("仿真模式：已关联传感器模拟器到设备操作器");
            }

            // 创建TestMaster1（一号试验炉控制器）
            // Requirement 7.1: 使用TestMaster1替代TestMaster，实现完整状态机
            Master1 = new TestMaster1(this, Sensors, manipulator);

            // 仿真模式：将模拟器也传递给TestMaster1
            if (Daq.IsSimulationMode && Daq.Simulator != null)
            {
                Master1.SetSimulator(Daq.Simulator);
                Log.Information("仿真模式：已关联传感器模拟器到试验控制器");
            }

            // 添加到控制器集合（保持向后兼容）
            Masters.addMaster(Master1);
            
            Log.Information("已创建一号试验炉控制器 ID: {MasterId}", Master1.MasterId);

            // 初始化试验控制器（启动状态机定时器）
            Master1.OnInitialized();
            Log.Information("试验控制器已初始化，状态机定时器已启动");
            Log.Information("系统上下文初始化完成");
        }

        /*
         * 功能: 清理系统资源
         * 性能优化：清理缓存
         */
        public void Cleanup()
        {
            Log.Information("开始清理系统资源");
            
            // 停止数据采集服务
            Daq?.Stop();
            Daq?.Dispose();
            
            // 性能优化：清理所有缓存
            CacheService.Instance.ClearAllCache();
            Log.Information("已清理所有缓存");
            
            Log.Information("系统资源清理完成");
        }
    }
}
