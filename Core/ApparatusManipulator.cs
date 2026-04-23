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

    public enum OperationType
    {
        Read,
        Write
    }

    public class ApparatusManipulator
    {
        private readonly string _pidPort;
        private readonly byte _unitIdentifier;
        private readonly ModbusRtuClient? _pidClient;
        private readonly IModbusRtuGateway? _sharedModbusGateway;
        private readonly bool _isSimulationMode;

        private ushort _currentOutput;
        private ushort _pidOutput;
        private ushort _currentTemp;
        private bool _isConnected;
        private SensorSimulator? _simulator;

        public ApparatusStatus apparatusStatus { get; set; }
        public short Temperature { get; set; }
        public short ConstPower { get; set; }

        public ApparatusManipulator(
            string pidport,
            string powerport,
            short constPower,
            short temperature = 750,
            byte unitIdentifier = 0x01,
            IModbusRtuGateway? sharedModbusGateway = null)
        {
            _pidPort = pidport;
            _unitIdentifier = unitIdentifier;
            _sharedModbusGateway = sharedModbusGateway;
            apparatusStatus = ApparatusStatus.None;
            _currentOutput = 0xFFFF;
            _pidOutput = 0x6400;
            Temperature = temperature;
            ConstPower = constPower;

            var simConfig = ConfigurationHelper.GetSection<SimulationConfiguration>("Simulation");
            _isSimulationMode = simConfig?.EnableSimulation == true && simConfig.SimulatePidController;
            if (_isSimulationMode)
            {
                Log.Information("已启用 PID 离线仿真模式，控制命令不会访问串口");
            }

            if (!_isSimulationMode && _sharedModbusGateway == null)
            {
                _pidClient = new ModbusRtuClient
                {
                    BaudRate = 9600,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
            }
        }

        public void SetSimulator(SensorSimulator? simulator)
        {
            _simulator = simulator;
        }

        public bool IsSimulationMode => _isSimulationMode;
        public bool IsConnected => _isSimulationMode || _isConnected;
        public string PidPortName => _pidPort;
        public int UnitIdentifier => _unitIdentifier;
        public string? LastConnectionError { get; private set; }

        public virtual bool EstablishConnection()
        {
            if (_isSimulationMode)
            {
                Log.Information("[仿真] PID 控制器连接成功");
                apparatusStatus = ApparatusStatus.Manual;
                _currentOutput = 0;
                _isConnected = true;
                LastConnectionError = null;
                return true;
            }

            Log.Information("正在连接 PID 控制器，端口: {Port}", _pidPort);

            return SerialPortCoordinator.RunExclusive(_pidPort, () =>
            {
                try
                {
                    OpenPidClient();
                    Log.Information("PID 控制器连接成功，开始初始化...");

                    var targetTemperature = Convert.ToUInt16(Temperature * 10);
                    Log.Debug("写入目标温度: 0x{Value:X4} ({Temp}°C)", targetTemperature, Temperature);

                    var initialized =
                        WriteSingleRegister(0x0000, targetTemperature) &&
                        WriteSingleRegister(0x0002, 0) &&
                        WriteSingleRegister(0x0038, 3);

                    if (!initialized)
                    {
                        Log.Warning("PID 控制器初始化命令执行失败");
                        _isConnected = false;
                        return false;
                    }

                    apparatusStatus = ApparatusStatus.Manual;
                    _currentOutput = 0;
                    _isConnected = true;
                    LastConnectionError = null;
                    Log.Information("PID 控制器初始化完成");
                    return true;
                }
                catch (TimeoutException ex)
                {
                    return HandleConnectionFailure(ex, "PID 控制器初始化超时");
                }
                catch (ModbusException ex)
                {
                    return HandleConnectionFailure(ex, "PID 控制器初始化失败");
                }
                catch (Exception ex)
                {
                    return HandleConnectionFailure(ex, "PID 控制器连接异常");
                }
                finally
                {
                    ClosePidClient();
                }
            });
        }

        public (bool, ushort) SendPidModuleCmd(OperationType type, ushort address, ushort value)
        {
            if (_isSimulationMode)
            {
                return SimulatePidCommand(type, address, value);
            }

            return SerialPortCoordinator.RunExclusive(_pidPort, () =>
            {
                try
                {
                    OpenPidClient();

                    if (type == OperationType.Write)
                    {
                        var success = WriteSingleRegister(address, value);
                        UpdateConnectionState(success, success ? null : "写入失败");
                        return (success, (ushort)0);
                    }

                    var data = ReadSingleRegister(address);
                    UpdateConnectionState(true, null);
                    return (true, data);
                }
                catch (TimeoutException ex)
                {
                    Log.Warning("Modbus 通信超时: {Message}", ex.Message);
                    UpdateConnectionState(false, ex.Message);
                    return (false, (ushort)0);
                }
                catch (ModbusException ex)
                {
                    Log.Error(ex, "Modbus 通信错误");
                    UpdateConnectionState(false, ex.Message);
                    return (false, (ushort)0);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "PID 串口通信异常");
                    UpdateConnectionState(false, ex.Message);
                    return (false, (ushort)0);
                }
                finally
                {
                    ClosePidClient();
                }
            });
        }

        private (bool, ushort) SimulatePidCommand(OperationType type, ushort address, ushort value)
        {
            ushort data = 0;

            if (type == OperationType.Write)
            {
                Log.Debug("[仿真] PID 写入: 地址=0x{Address:X4}, 值=0x{Value:X4}", address, value);

                switch (address)
                {
                    case 0x0002:
                        _currentOutput = value;
                        if (_simulator != null)
                        {
                            _simulator.ManualOutput = value;
                        }
                        break;
                    case 0x0038:
                        if (_simulator != null)
                        {
                            _simulator.IsPidMode = value == 4;
                        }
                        break;
                }

                return (true, 0);
            }

            switch (address)
            {
                case 0x0101:
                    data = _simulator?.PidOutput ?? _pidOutput;
                    break;
                case 0x0102:
                    data = _simulator?.CurrentTemp ?? _currentTemp;
                    break;
            }

            Log.Debug("[仿真] PID 读取: 地址=0x{Address:X4}, 返回值=0x{Data:X4}", address, data);
            return (true, data);
        }

        public bool StartHeating()
        {
            if (_isSimulationMode)
            {
                _simulator?.StartHeating();
                Log.Information("[仿真] 开始加热");
                return true;
            }

            var currentTemp = GetCurrentTemp();
            if (currentTemp < 3000)
            {
                return SetOutputPower(7680) && SwitchToManual();
            }

            if (currentTemp < 5000)
            {
                return SetOutputPower(12800) && SwitchToManual();
            }

            if (currentTemp < 6000)
            {
                return SetOutputPower(17920) && SwitchToManual();
            }

            if (currentTemp < 7000)
            {
                return SetOutputPower(23040) && SwitchToManual();
            }

            return SwitchToPID();
        }

        public bool StopHeating()
        {
            if (_isSimulationMode)
            {
                _simulator?.StopHeating();
                Log.Information("[仿真] 停止加热");
                return true;
            }

            var success = SetOutputPower(0) && SwitchToManual();
            if (success)
            {
                _currentOutput = 0xFFFF;
            }

            return success;
        }

        public bool SwitchToPID()
        {
            if (apparatusStatus == ApparatusStatus.Pid)
            {
                return true;
            }

            var (success, _) = SendPidModuleCmd(OperationType.Write, 0x0038, 4);
            if (success)
            {
                apparatusStatus = ApparatusStatus.Pid;
            }

            return success;
        }

        public bool SwitchToManual()
        {
            if (apparatusStatus == ApparatusStatus.Manual)
            {
                return true;
            }

            var (success, _) = SendPidModuleCmd(OperationType.Write, 0x0038, 3);
            if (success)
            {
                apparatusStatus = ApparatusStatus.Manual;
            }

            return success;
        }

        public bool SetOutputPower(ushort value)
        {
            if (_currentOutput == value)
            {
                return true;
            }

            var (success, _) = SendPidModuleCmd(OperationType.Write, 0x0002, value);
            if (success)
            {
                _currentOutput = value;
            }

            return success;
        }

        public ushort GetCurrentOutput()
        {
            return _currentOutput;
        }

        public ushort GetPidOutput()
        {
            var (success, data) = SendPidModuleCmd(OperationType.Read, 0x0101, 0);
            if (success)
            {
                _pidOutput = data;
            }

            return _pidOutput;
        }

        public ushort GetCurrentTemp()
        {
            var (success, data) = SendPidModuleCmd(OperationType.Read, 0x0102, 0);
            if (success)
            {
                _currentTemp = data;
            }

            return _currentTemp;
        }

        private void OpenPidClient()
        {
            if (_sharedModbusGateway != null || _pidClient == null)
            {
                return;
            }

            if (_pidClient.IsConnected)
            {
                _pidClient.Close();
            }

            _pidClient.Connect(_pidPort, ModbusEndianness.BigEndian);
        }

        private void ClosePidClient()
        {
            if (_sharedModbusGateway != null || _pidClient == null)
            {
                return;
            }

            try
            {
                if (_pidClient.IsConnected)
                {
                    _pidClient.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("关闭 PID 串口时忽略异常: {Message}", ex.Message);
            }
        }

        private bool WriteSingleRegister(ushort address, ushort value)
        {
            Log.Debug("Modbus写入: 地址=0x{Address:X4}, 值=0x{Value:X4}", address, value);
            if (_sharedModbusGateway != null)
            {
                _sharedModbusGateway.WriteSingleRegister(_unitIdentifier, address, value, 1000);
                return true;
            }

            _pidClient!.WriteSingleRegister(_unitIdentifier, address, value);
            return true;
        }

        private ushort ReadSingleRegister(ushort address)
        {
            Log.Debug("Modbus读取: 地址=0x{Address:X4}", address);
            if (_sharedModbusGateway != null)
            {
                return _sharedModbusGateway.ReadHoldingRegisters(_unitIdentifier, address, 1, 1000)[0];
            }

            return _pidClient!.ReadHoldingRegisters<ushort>(_unitIdentifier, address, 1)[0];
        }

        private bool HandleConnectionFailure(Exception ex, string message)
        {
            Log.Warning(ex, "{Message}: {Detail}", message, ex.Message);
            UpdateConnectionState(false, ex.Message);
            return false;
        }

        private void UpdateConnectionState(bool isConnected, string? errorMessage)
        {
            _isConnected = isConnected;
            LastConnectionError = errorMessage;
        }
    }
}
