using System.IO.Ports;
using FluentModbus;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Serilog;

namespace ISO11820WinForms.Core
{
    public enum ApparatusStatus
    {
        None,
        Pid,
        Manual
    }

    // PID模块操作类型
    public enum OperationType
    {
        Read,
        Write
    }
    /* 
     * 类型定义: 该类型定义操作试验设备动作部件的基本功能
     *          支持仿真模式，无需真实硬件即可测试
     */
    public class ApparatusManipulator
    {
        private string _pidPort = string.Empty;
        private int _unitIdentifier = 0x01;
        private ushort _currentOutput; // 当前Pid控制模块的输出功率值(0-25600)(手动方式)
        private ushort _pidOutput;     // 当前Pid控制模块的输出功率值(0-25600)(Pid方式)
        private ushort _currentTemp;   // 当前控温热电偶的温度(单位:0.1)

        //设备当前工作模式(0:PID | 1:手动)
        public ApparatusStatus apparatusStatus { get; set; }
        //PID控制器通信端口
        private ModbusRtuClient _pidClient;
        //PID控制温度(默认为750℃)
        public Int16 Temperature { get; set; }
        //恒功率输出值(值域: 900 - 4096)
        public Int16 ConstPower { get; set; }

        // 仿真模式支持
        private readonly bool _isSimulationMode = false;
        private SensorSimulator? _simulator;

        public ApparatusManipulator(string pidport,string powerport, Int16 constPower, Int16 temperature = 750)
        {
            _pidPort = pidport;
            apparatusStatus = ApparatusStatus.None;
            _currentOutput = 0xFFFF;
            _pidOutput = 0x6400; // 对应100%
            
            // 纯硬件模式：忽略 PID 仿真开关，始终初始化真实控制器连接。
            var simConfig = ConfigurationHelper.GetSection<SimulationConfiguration>("Simulation");
            if (simConfig?.EnableSimulation == true || simConfig?.SimulatePidController == true)
            {
                Log.Warning("已忽略PID仿真配置，当前固定为纯硬件模式");
            }

            _isSimulationMode = false;
            _pidClient = new();
            _pidClient.BaudRate = 9600;
            _pidClient.Parity = Parity.None;
            _pidClient.StopBits = StopBits.One;
            _pidClient.ReadTimeout = 1000;
            _pidClient.WriteTimeout = 1000;
            
            //初始化PID控制器与电力调整器输出控制参数
            Temperature = temperature;
            ConstPower = constPower;
        }

        /// <summary>
        /// 设置传感器模拟器引用（仿真模式下使用）
        /// </summary>
        public void SetSimulator(SensorSimulator? simulator)
        {
            _simulator = simulator;
        }

        /// <summary>
        /// 是否处于仿真模式
        /// </summary>
        public bool IsSimulationMode => _isSimulationMode;

        /*
         * 功能: 建立与试验设备的实际连接
         *       1.设置PID控温器的目标控制温度为750℃                              
         *       2.设置PID控制器初始控制方式为手动控制
         *       3.设置PID控制器手动控制输出比例为0
         * 返回:
         *       true  - 成功连接所有接口
         *       false - 至少有一个接口连接失败
         */
        public bool EstablishConnection()
        {
            // 仿真模式：直接返回成功
            if (_isSimulationMode)
            {
                Log.Information("[仿真] PID控制器连接成功");
                apparatusStatus = ApparatusStatus.Manual;
                _currentOutput = 0;
                return true;
            }

            Log.Information("正在连接PID控制器，端口: {Port}", _pidPort);
            try
            {
                _pidClient.Connect(_pidPort, ModbusEndianness.BigEndian);
                if (_pidClient.IsConnected)
                {
                    Log.Information("PID控制器连接成功，开始初始化...");
                    Log.Debug("写入目标温度: 0x{Value:X4} ({Temp}°C)", Convert.ToUInt16(Temperature * 10), Temperature);
                    
                    if (SendPidModuleCmd(OperationType.Write, 0x0000, Convert.ToUInt16(Temperature * 10)).Item1
                        && SetOutputPower(0)
                        && SwitchToManual())
                    {
                        Log.Information("PID控制器初始化完成");
                        return true;
                    }
                    else
                    {
                        Log.Warning("PID控制器初始化命令执行失败");
                    }
                }
                else
                {
                    Log.Error("PID控制器连接失败，端口: {Port}", _pidPort);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PID控制器连接异常，端口: {Port}", _pidPort);
            }
            return false;
        }

        /* =========定义设备的执行动作函数,这些函数使用设备的通信端口发送控制指令 ========== */

        /*
         * 功能: 同步所有写指令
         * 参数:
         *       type    - 操作类型
         *       address - 寄存器地址
         *       value   - 要写入指定地址的值
         * 返回:
         *       (bool,ushort) - 操作成功则返回true以及获取的结果值(仅限读操作)
         */
        public (bool,ushort) SendPidModuleCmd(OperationType type, ushort address, ushort value)
        {
            // 仿真模式：模拟PID控制器响应
            if (_isSimulationMode)
            {
                return SimulatePidCommand(type, address, value);
            }

            bool ret = false;
            ushort data = 0;
            lock (this)
            {
                try {
                    if (_pidClient.IsConnected) {
                        if(type == OperationType.Write) {
                            Log.Debug("Modbus写入: 地址=0x{Address:X4}, 值=0x{Value:X4}", address, value);
                            _pidClient.WriteSingleRegister(_unitIdentifier, address, value);
                            Log.Debug("Modbus写入成功");
                            ret = true;
                        } else if (type == OperationType.Read) {
                            Log.Debug("Modbus读取: 地址=0x{Address:X4}", address);
                            data = _pidClient.ReadHoldingRegisters<ushort>(_unitIdentifier, address, 1)[0];
                            Log.Debug("Modbus读取成功: 值=0x{Data:X4} ({DataDec})", data, data);
                            ret = true;
                        }                        
                    }
                    else
                    {
                        Log.Warning("Modbus客户端未连接");
                    }
                } catch (TimeoutException ex) {
                    Log.Warning("Modbus通信超时: {Message}", ex.Message);
                    ret = false;
                    data = 0;
                } catch(ModbusException ex){
                    Log.Error(ex, "Modbus通信错误");
                    ret = false;
                    data = 0;
                }
                return (ret, data);
            }
        }

        /// <summary>
        /// 仿真模式下模拟PID控制器命令响应
        /// </summary>
        private (bool, ushort) SimulatePidCommand(OperationType type, ushort address, ushort value)
        {
            ushort data = 0;
            
            if (type == OperationType.Write)
            {
                Log.Debug("[仿真] PID写入: 地址=0x{Address:X4}, 值=0x{Value:X4}", address, value);
                
                switch (address)
                {
                    case 0x0000: // 目标温度
                        // 模拟设置目标温度
                        break;
                    case 0x0002: // 手动输出
                        _currentOutput = value;
                        if (_simulator != null)
                            _simulator.ManualOutput = value;
                        break;
                    case 0x0038: // 控制模式
                        if (_simulator != null)
                            _simulator.IsPidMode = (value == 4);
                        break;
                }
                return (true, 0);
            }
            else // Read
            {
                switch (address)
                {
                    case 0x0101: // PID输出
                        data = _simulator?.PidOutput ?? _pidOutput;
                        break;
                    case 0x0102: // 当前温度
                        data = _simulator?.CurrentTemp ?? _currentTemp;
                        break;
                    default:
                        data = 0;
                        break;
                }
                Log.Debug("[仿真] PID读取: 地址=0x{Address:X4}, 返回值=0x{Data:X4}", address, data);
                return (true, data);
            }
        }

        /*
         * 功能: 开始加热,按温度阶段调整输出功率
         * 返回:
         *       true  - 设置成功,开始加热
         *       false - 设置失败,未开始加热
         */
        public bool StartHeating()
        {
            // 仿真模式：直接返回成功（即使没有模拟器实例）
            if (_isSimulationMode)
            {
                _simulator?.StartHeating();
                Log.Information("[仿真] 开始加热");
                return true;
            }

            bool ret = false;
            //根据当前温度阶段设置起始加热功率(软起动),当温度达到745℃时切换至PID控温
            ushort curTemp = GetCurrentTemp();
            if (curTemp < 3000) {
                ret = SetOutputPower(7680) && SwitchToManual();  // 30%输出功率
            } else if (curTemp < 5000) {
                ret = SetOutputPower(12800) && SwitchToManual(); // 50%输出功率
            } else if(curTemp < 6000)  {
                ret = SetOutputPower(17920) && SwitchToManual(); // 70%输出功率
            } else if (curTemp < 7000) {
                ret = SetOutputPower(23040) && SwitchToManual();  // 90%输出功率
            } else {
                ret = SwitchToPID(); // 以目标控制温度执行PID控温
            }
            return ret;
        }

        /*
         * 功能: 停止加热(切断炉体供电)
         */
        public bool StopHeating()
        {
            // 仿真模式：直接返回成功
            if (_isSimulationMode)
            {
                _simulator?.StopHeating();
                Log.Information("[仿真] 停止加热");
                return true;
            }

            bool ret = false;
            /* 停止PID温控器输出 */
            //设置手动控制时输出比例为0并切换PID控制器为手动模式
            ret = SetOutputPower(0) && SwitchToManual();
            if (ret)
            {
                _currentOutput = 0xFFFF;
            }
            return ret;
        }

        /*
         * 功能: 将当前加热方式切换至PID方式
         */
        public bool SwitchToPID()
        {            
            if(apparatusStatus == ApparatusStatus.Pid) 
                return true;
            //设置PID温控器工作方式为Auto,目标温度为750℃            
            var (ret, data) = SendPidModuleCmd(OperationType.Write, 0x0038, 4);
            if (ret)
            {
                apparatusStatus = ApparatusStatus.Pid;
            }
            return ret;
        }

        /*
         * 功能: 将PID控制器切换至手动控制模式
         */
        public bool SwitchToManual()
        {
            if (apparatusStatus == ApparatusStatus.Manual)
                return true;         
            var (ret, data) = SendPidModuleCmd(OperationType.Write, 0x0038, 3);
            if(ret)
            {
                apparatusStatus = ApparatusStatus.Manual;
            }
            return ret;
        }        

        /*
         * 功能: 设置当前控制输出比例(手动方式)(0-25600 对应 0%-100%)
         * 参数:
         *       value - 要设定为的输出比例
         */
        public bool SetOutputPower(ushort value)
        {
            if (_currentOutput == value)
                return true;
            var (ret, data) = SendPidModuleCmd(OperationType.Write, 0x0002, value);
            if(ret)
            {
                _currentOutput = value;
            }
            return ret;
        }

        /*
         * 功能: 获取当前控制输出比例(手动方式)
         */
        public ushort GetCurrentOutput()
        {
            return _currentOutput;
        }

        /*
         * 功能: 获取当前控制输出比例(PID方式)
         */
        public ushort GetPidOutput()
        {
            var (ret, data) = SendPidModuleCmd(OperationType.Read, 0x0101, 0);
            if (ret)
            {
                _pidOutput = data;
            }
            return _pidOutput;
        }

        /*
         * 功能: 获取控温热电偶当前温度值
         */
        public ushort GetCurrentTemp()
        {
            var (ret, data) = SendPidModuleCmd(OperationType.Read, 0x0102, 0);
            if(ret)
            {
                _currentTemp = data;
            }
            return _currentTemp;
        }
    }
}
