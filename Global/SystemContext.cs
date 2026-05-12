using ISO11820WinForms.Core;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Serilog;

namespace ISO11820WinForms.Global
{
    public class SystemContext
    {
        private static SystemContext? _instance;
        private static readonly object LockObject = new object();

        public static SystemContext Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        _instance ??= new SystemContext();
                    }
                }

                return _instance;
            }
        }

        public SensorDictionary Sensors { get; }
        public TestMasters Masters { get; }
        public DaqWorker Daq { get; private set; }
        public AppGlobalWinForms Global { get; }
        public IModbusRtuGateway? SharedModbusGateway { get; private set; }
        public TestMaster1? Master1 { get; private set; }
        public SampleTestSessionService? Session { get; private set; }

        private SystemContext()
        {
            Sensors = new SensorDictionary();
            Masters = new TestMasters();
            Global = new AppGlobalWinForms();
            Daq = new DaqWorker(Sensors);
        }

        public void Init()
        {
            Log.Information("开始初始化系统上下文");

            var configService = Services.ConfigurationService.Instance;
            if (!configService.ValidateConfiguration())
            {
                Log.Warning("配置校验失败，但系统将继续初始化");
            }

            if (!configService.InitializeDirectories())
            {
                Log.Warning("目录初始化失败，部分功能可能不可用");
            }

            Log.Information(configService.GetConfigurationSummary());

            Log.Information("数据采集服务已启动");

            var pidPort = ConfigurationHelper.GetPidPort();
            var powerPort = ConfigurationHelper.GetPowerPort();
            var sensorPort = ConfigurationHelper.GetSensorPort();
            var sensorProtocol = ConfigurationHelper.GetSensorProtocol();
            var constPower = ConfigurationHelper.GetConstPower();
            var pidTemperature = ConfigurationHelper.GetPidTemperature();
            var sensorStationNumber = ConfigurationHelper.GetSensorStationNumber();
            var pidStationNumber = ConfigurationHelper.GetPidStationNumber();
            var sensorRegisterStartAddress = ConfigurationHelper.NormalizeSensorRegisterStartAddress(
                sensorProtocol,
                ConfigurationHelper.GetSensorRegisterStartAddress());
            var sensorRegisterCount = ConfigurationHelper.GetSensorRegisterCount();
            var sensorReadTimeoutMs = ConfigurationHelper.GetSensorReadTimeoutMs();
            var calibrationChannelIndex = ConfigurationHelper.GetCalibrationChannelIndex();
            var simulationConfig = ConfigurationHelper.GetSection<SimulationConfiguration>("Simulation");
            var useSimulationMode = simulationConfig?.EnableSimulation == true;

            Log.Information(
                "硬件配置: PID端口={PidPort}, 功率端口={PowerPort}, 采集端口={SensorPort}, 恒功率={ConstPower}, PID温度={PidTemperature}",
                pidPort,
                powerPort,
                sensorPort,
                constPower,
                pidTemperature);

            Log.Information(
                "硬件站号配置：PID站号={PidStationNumber}，ADAM站号={SensorStationNumber}，校准通道={CalibrationChannelIndex}",
                pidStationNumber,
                sensorStationNumber,
                calibrationChannelIndex);

            var apparatus = Global.DictApparatus.Values.FirstOrDefault();
            var useSharedSerialPort = ConfigurationHelper.IsSharedHardwarePortConfigured();
            if (useSharedSerialPort)
            {
                pidPort = sensorPort;
                powerPort = sensorPort;
                Log.Information("检测到单串口共享模式，统一使用端口 {SharedPort}", sensorPort);
            }
            else if (apparatus != null)
            {
                pidPort = apparatus.Pidport ?? pidPort;
                powerPort = apparatus.Powerport ?? powerPort;
                constPower = (int)(apparatus.Constpower ?? constPower);

                Log.Information(
                    "使用数据库设备配置: PID端口={PidPort}, 功率端口={PowerPort}, 恒功率={ConstPower}",
                    pidPort,
                    powerPort,
                    constPower);
            }
            else
            {
                Log.Warning("未找到数据库设备配置，使用 appsettings.json 中的端口配置");
            }

            ModbusSerialDetectionResult? detectionResult = null;
            if (!useSimulationMode && useSharedSerialPort && string.Equals(sensorProtocol, "ModbusRtu", StringComparison.OrdinalIgnoreCase))
            {
                var detector = new AdamModbusSerialSettingsDetector();
                detectionResult = detector.Detect(
                    sensorPort,
                    (byte)sensorStationNumber,
                    (ushort)sensorRegisterStartAddress,
                    sensorRegisterCount,
                    sensorReadTimeoutMs);

                SharedModbusGateway ??= new SharedModbusRtuGateway(sensorPort, detectionResult.Settings);
                Log.Information(
                    "已启用共享 Modbus 网关: 端口={Port}, 串口参数={SerialSettings}, 自动探测={Detected}",
                    sensorPort,
                    detectionResult.Settings.ToDisplayString(),
                    detectionResult.IsDetected);
            }
            else if (useSimulationMode)
            {
                Log.Information("离线仿真模式已启用，跳过共享串口自动探测和硬件连接");
            }

            Daq = new DaqWorker(Sensors, SharedModbusGateway, detectionResult);
            Daq.Start();

            var manipulator = new ApparatusManipulator(
                pidPort,
                powerPort,
                (short)constPower,
                (short)pidTemperature,
                (byte)pidStationNumber,
                SharedModbusGateway);

            Log.Information("当前运行模式：{Mode}", useSimulationMode ? "离线仿真模式" : "纯硬件模式");

            Master1 = new TestMaster1(this, Sensors, manipulator);
            if (Daq.IsSimulationMode && Daq.Simulator != null)
            {
                Master1.SetSimulator(Daq.Simulator);
                Log.Information("已将采集模拟器绑定到试验控制器和 PID 控制器");
            }

            Session = new SampleTestSessionService(Master1);
            Masters.addMaster(Master1);

            Log.Information("已创建一号试验炉控制器 ID: {MasterId}", Master1.MasterId);

            var controllerInitialized = Master1.OnInitialized();
            if (controllerInitialized)
            {
                Log.Information("试验控制器已初始化，状态机定时器已启动");
            }
            else
            {
                Log.Warning("试验控制器初始化未完成，系统将以未连接状态继续启动");
            }

            Log.Information("系统上下文初始化完成");
        }

        public void Cleanup()
        {
            Log.Information("开始清理系统资源");

            Daq.Stop();
            Daq.Dispose();
            SharedModbusGateway?.Dispose();
            SharedModbusGateway = null;
            Session = null;
            Master1 = null;
            Masters.DictTestMaster.Clear();

            CacheService.Instance.ClearAllCache();
            Log.Information("已清理所有缓存");
            Log.Information("系统资源清理完成");
        }
    }
}
