using System.Globalization;
using System.IO.Ports;
using System.Text;
using FluentModbus;
using ISO11820WinForms.Core;
using ISO11820WinForms.Global;
using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using Serilog;
using Sensor = TestServer.Models.Sensor;

namespace ISO11820WinForms.Services
{
    public class DaqWorker
    {
        private readonly SensorDictionary _sensors;
        private readonly IModbusRtuGateway? _sharedModbusGateway;
        private readonly ModbusSerialDetectionResult? _modbusDetectionResult;
        private readonly SimulationConfiguration? _simulationConfig;

        private System.Threading.Timer? _timer;
        private SensorSimulator? _simulator;
        private int _counter;
        private int _pollingFlag;
        private int _cacheCleanupCounter;
        private string _sensorPortName = "COM3";
        private string _sensorProtocol = "ModbusRtu";
        private int _sensorStationNumber = 1;
        private int _sensorRegisterStartAddress;
        private int _sensorRegisterCount = 8;
        private int _sensorReadTimeoutMs = 1000;
        private int _calibrationChannelIndex = 4;
        private bool _isRunning;
        private bool _isSensorConnected;
        private bool _isSimulationMode;
        private double _elapsedSeconds;
        private ModbusSerialSettings _sensorSerialSettings = ModbusSerialSettings.Default;
        private string? _preferredRegisterKind;

        private const int CacheCleanupInterval = 375;
        private const int PollIntervalMs = 800;

        public DaqWorker(
            SensorDictionary sensors,
            IModbusRtuGateway? sharedModbusGateway = null,
            ModbusSerialDetectionResult? modbusDetectionResult = null)
        {
            _sensors = sensors;
            _sharedModbusGateway = sharedModbusGateway;
            _modbusDetectionResult = modbusDetectionResult;
            _sensors.Sensors ??= new Dictionary<int, Sensor>();

            _simulationConfig = ConfigurationHelper.GetSection<SimulationConfiguration>("Simulation");
            _isSimulationMode = _simulationConfig?.EnableSimulation == true && _simulationConfig.SimulateSensors;
            if (_isSimulationMode)
            {
                _simulator = new SensorSimulator(_simulationConfig!);
                _isSensorConnected = true;
                Log.Information("已启用传感器离线仿真模式，采集数据将由模拟器生成");
            }
            else
            {
                _simulator = null;
            }
        }

        public event EventHandler<SensorDataEventArgs>? SensorDataReceived;

        public SensorSimulator? Simulator => _simulator;
        public bool IsSensorConnected => _isSensorConnected;
        public string SensorPortName => _sensorPortName;
        public string? LastConnectionError { get; private set; }
        public bool IsSimulationMode => _isSimulationMode;

        public void Start()
        {
            if (_isRunning)
            {
                Log.Warning("数据采集服务已在运行，忽略重复启动");
                return;
            }

            Log.Information("启动数据采集服务");

            _sensors.Sensors = LoadSensors();
            Log.Information("从数据库加载了 {Count} 个传感器配置", _sensors.Sensors.Count);

            _sensorPortName = ConfigurationHelper.GetSensorPort();
            _sensorProtocol = ConfigurationHelper.GetSensorProtocol();
            var configuredStationNumber = ConfigurationHelper.GetSensorStationNumber();
            _sensorStationNumber = configuredStationNumber;
            var configuredRegisterStartAddress = ConfigurationHelper.GetSensorRegisterStartAddress();
            _sensorRegisterStartAddress = ConfigurationHelper.NormalizeSensorRegisterStartAddress(
                _sensorProtocol,
                configuredRegisterStartAddress);
            _sensorRegisterCount = ConfigurationHelper.GetSensorRegisterCount();
            _sensorReadTimeoutMs = ConfigurationHelper.GetSensorReadTimeoutMs();
            _calibrationChannelIndex = ConfigurationHelper.GetCalibrationChannelIndex();
            _sensorSerialSettings = _sharedModbusGateway?.Settings ?? ModbusSerialSettings.Default;
            _preferredRegisterKind = _modbusDetectionResult?.RegisterKind;
            if (_modbusDetectionResult?.IsDetected == true)
            {
                _sensorStationNumber = _modbusDetectionResult.StationNumber;
                _sensorRegisterStartAddress = _modbusDetectionResult.StartAddress;
            }
            _isSensorConnected = false;
            LastConnectionError = null;
            Log.Information("ADAM 当前 Modbus 串口参数: {SerialSettings}", _sensorSerialSettings.ToDisplayString());

            if (_sensorRegisterStartAddress != configuredRegisterStartAddress)
            {
                Log.Information(
                    "检测到 ADAM Modbus 起始寄存器配置为 {ConfiguredStartAddress}，已自动修正为 {EffectiveStartAddress}",
                    configuredRegisterStartAddress,
                    _sensorRegisterStartAddress);
            }

            if (_sensorStationNumber != configuredStationNumber)
            {
                Log.Information(
                    "Detected ADAM station override: configured={ConfiguredStationNumber}, effective={EffectiveStationNumber}",
                    configuredStationNumber,
                    _sensorStationNumber);
            }

            Log.Information(
                "采集链路初始化: 端口={Port}, 协议={Protocol}, 站号={StationNumber}, 起始寄存器={RegisterStart}, 寄存器数量={RegisterCount}, 超时={Timeout}ms, 校准通道={CalibrationChannel}",
                _sensorPortName,
                _sensorProtocol,
                _sensorStationNumber,
                _sensorRegisterStartAddress,
                _sensorRegisterCount,
                _sensorReadTimeoutMs,
                _calibrationChannelIndex);

            Log.Information(
                "初始化串口对象: {Port}, 9600, 8N1, ADAM站号={StationNumber}, 校准通道={CalibrationChannel}",
                _sensorPortName,
                _sensorStationNumber,
                _calibrationChannelIndex);

            _timer?.Dispose();
            _timer = new System.Threading.Timer(DoWork, null, Timeout.Infinite, PollIntervalMs);
            Log.Information("数据采集定时器已创建，采集间隔: {Interval}ms", PollIntervalMs);

            if (_isSimulationMode)
            {
                _isSensorConnected = true;
                _timer.Change(0, PollIntervalMs);
                _isRunning = true;
                Log.Information("仿真模式下已启动数据采集定时器");
                return;
            }

            ProbeHardwareConnection();
            _timer.Change(PollIntervalMs, PollIntervalMs);
            _isRunning = true;
        }

        protected virtual Dictionary<int, Sensor> LoadSensors()
        {
            using var ctx = new ISO11820DbContext();
            return ctx.Sensors.ToDictionary(x => x.Sensorid);
        }

        protected virtual SerialPort CreateSerialPort(string sensorPort)
        {
            return new SerialPort(sensorPort, 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = _sensorReadTimeoutMs,
                WriteTimeout = _sensorReadTimeoutMs,
                Handshake = Handshake.None,
                NewLine = "\r",
                Encoding = Encoding.ASCII
            };
        }

        protected virtual bool UseModbusSensorProtocol()
        {
            return string.Equals(_sensorProtocol, "ModbusRtu", StringComparison.OrdinalIgnoreCase);
        }

        protected virtual string BuildSensorReadCommand(int stationNumber)
        {
            return $"#{stationNumber:00}";
        }

        protected virtual int GetCalibrationChannelIndex()
        {
            return _calibrationChannelIndex;
        }

        protected virtual void ApplyAdamFrame(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            if (data.Length < 1 + 4 * 7)
            {
                Log.Debug("ADAM 返回帧长度不足，无法解析: {Length}", data.Length);
                return;
            }

            UpdateSensorValue(SensorChannelHelper.FurnaceTemp1SensorId, ParseChannelValue(data, 0));
            UpdateSensorValue(SensorChannelHelper.FurnaceTemp2SensorId, ParseChannelValue(data, 1));
            UpdateSensorValue(SensorChannelHelper.SurfaceTempSensorId, ParseChannelValue(data, 2));
            UpdateSensorValue(SensorChannelHelper.CenterTempSensorId, ParseChannelValue(data, 3));

            var calibrationChannelIndex = GetCalibrationChannelIndex();
            var channelCount = (data.Length - 1) / 7;
            if (calibrationChannelIndex >= 0 && calibrationChannelIndex < channelCount)
            {
                UpdateSensorValue(
                    SensorChannelHelper.CalibrationTempSensorId,
                    ParseChannelValue(data, calibrationChannelIndex));
            }
        }

        protected virtual void ApplyModbusRegisters(IReadOnlyList<ushort> registers)
        {
            if (registers == null || registers.Count < 4)
            {
                Log.Debug("Modbus 采集返回寄存器数量不足，无法解析: {Count}", registers?.Count ?? 0);
                return;
            }

            UpdateSensorRawValue(SensorChannelHelper.FurnaceTemp1SensorId, registers[0]);
            UpdateSensorRawValue(SensorChannelHelper.FurnaceTemp2SensorId, registers[1]);
            UpdateSensorRawValue(SensorChannelHelper.SurfaceTempSensorId, registers[2]);
            UpdateSensorRawValue(SensorChannelHelper.CenterTempSensorId, registers[3]);

            var calibrationChannelIndex = GetCalibrationChannelIndex();
            if (calibrationChannelIndex >= 0 && calibrationChannelIndex < registers.Count)
            {
                UpdateSensorRawValue(
                    SensorChannelHelper.CalibrationTempSensorId,
                    registers[calibrationChannelIndex]);
            }
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _isRunning = false;
            _isSensorConnected = false;
        }

        private void DoWork(object? state)
        {
            _elapsedSeconds += 0.8;

            if (_isSimulationMode && _simulator != null)
            {
                DoSimulationWork();
                return;
            }

            DoHardwareWork();
        }

        private void DoSimulationWork()
        {
            if (_simulator == null)
            {
                return;
            }

            _simulator.Update(_elapsedSeconds);

            var modbusTemp = _simulator.FurnaceTemp1;

            PublishCurrentSensorData(
                (int)_elapsedSeconds,
                modbusTemp,
                modbusTemp - 0.1,
                _simulator.SurfaceTemp,
                _simulator.CenterTemp,
                _simulator.CalibrationTemp);

            CleanupCacheIfNeeded();
        }

        private void DoHardwareWork()
        {
            if (Interlocked.Exchange(ref _pollingFlag, 1) == 1)
            {
                return;
            }

            try
            {
                TryReadHardwareData(raiseEvent: true);
                CleanupCacheIfNeeded();
            }
            finally
            {
                Interlocked.Exchange(ref _pollingFlag, 0);
            }
        }

        private void ProbeHardwareConnection()
        {
            if (Interlocked.Exchange(ref _pollingFlag, 1) == 1)
            {
                return;
            }

            try
            {
                TryReadHardwareData(raiseEvent: false);
            }
            finally
            {
                Interlocked.Exchange(ref _pollingFlag, 0);
            }
        }

        private void TryReadHardwareData(bool raiseEvent)
        {
            SerialPortCoordinator.RunExclusive(_sensorPortName, () =>
            {
                try
                {
                    if (UseModbusSensorProtocol())
                    {
                        var registers = ReadModbusRegisters(
                            _sensorPortName,
                            _sensorStationNumber,
                            _sensorRegisterStartAddress,
                            _sensorRegisterCount);
                        ApplyModbusRegisters(registers);
                    }
                    else
                    {
                        using var serialPort = CreateSerialPort(_sensorPortName);
                        serialPort.Open();
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();

                        var frame = ReadFrame(serialPort, BuildSensorReadCommand(_sensorStationNumber));
                        ApplyAdamFrame(frame);
                    }

                    _counter = 0;
                    _isSensorConnected = true;
                    LastConnectionError = null;
                }
                catch (TimeoutException ex)
                {
                    _isSensorConnected = false;
                    LastConnectionError = ex.Message;

                    if (++_counter >= 3)
                    {
                        _counter = 0;
                        Log.Warning("传感器串口连续超时 3 次，当前端口: {Port}", _sensorPortName);
                    }

                    Log.Debug("传感器串口读取超时: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    _isSensorConnected = false;
                    LastConnectionError = ex.Message;
                    Log.Warning(ex, "传感器串口读取失败，系统将继续运行: {Message}", ex.Message);
                }
            });

            if (raiseEvent)
            {
                PublishCurrentSensorData(
                    (int)_elapsedSeconds,
                    SensorChannelHelper.GetOutputValue(_sensors.Sensors, SensorChannelHelper.FurnaceTemp1SensorId),
                    SensorChannelHelper.GetOutputValue(_sensors.Sensors, SensorChannelHelper.FurnaceTemp2SensorId),
                    SensorChannelHelper.GetOutputValue(_sensors.Sensors, SensorChannelHelper.SurfaceTempSensorId),
                    SensorChannelHelper.GetOutputValue(_sensors.Sensors, SensorChannelHelper.CenterTempSensorId),
                    SensorChannelHelper.GetOutputValue(_sensors.Sensors, SensorChannelHelper.CalibrationTempSensorId));
            }
        }

        private static string ReadFrame(SerialPort serialPort, string command)
        {
            serialPort.WriteLine(command);
            return serialPort.ReadLine();
        }

        private IReadOnlyList<ushort> ReadModbusRegisters(
            string sensorPort,
            int stationNumber,
            int startAddress,
            int registerCount)
        {
            if (string.Equals(_preferredRegisterKind, "InputRegisters", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return ReadModbusInputRegisters(sensorPort, stationNumber, startAddress, registerCount);
                }
                catch (TimeoutException ex)
                {
                    Log.Warning(
                        "ADAM Input Registers read timed out, fallback to Holding Registers: port={Port}, station={StationNumber}, start={StartAddress}, count={Count}, reason={Message}",
                        sensorPort,
                        stationNumber,
                        startAddress,
                        registerCount,
                        ex.Message);
                    return ReadModbusHoldingRegisters(sensorPort, stationNumber, startAddress, registerCount);
                }
                catch (ModbusException ex)
                {
                    Log.Warning(
                        "ADAM Input Registers read failed, fallback to Holding Registers: port={Port}, station={StationNumber}, start={StartAddress}, count={Count}, reason={Message}",
                        sensorPort,
                        stationNumber,
                        startAddress,
                        registerCount,
                        ex.Message);
                    return ReadModbusHoldingRegisters(sensorPort, stationNumber, startAddress, registerCount);
                }
            }

            try
            {
                return ReadModbusHoldingRegisters(sensorPort, stationNumber, startAddress, registerCount);
            }
            catch (TimeoutException ex)
            {
                Log.Warning(
                    "ADAM Holding Registers 读取超时，准备回退到 Input Registers: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}, 原因={Message}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount,
                    ex.Message);
                return ReadModbusInputRegisters(sensorPort, stationNumber, startAddress, registerCount);
            }
            catch (ModbusException ex)
            {
                Log.Warning(
                    "ADAM Holding Registers 读取失败，准备回退到 Input Registers: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}, 原因={Message}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount,
                    ex.Message);
                return ReadModbusInputRegisters(sensorPort, stationNumber, startAddress, registerCount);
            }
        }

        private IReadOnlyList<ushort> ReadModbusHoldingRegisters(
            string sensorPort,
            int stationNumber,
            int startAddress,
            int registerCount)
        {
            if (_sharedModbusGateway != null)
            {
                Log.Debug(
                    "通过共享 Modbus 网关读取 ADAM Holding Registers: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount);
                return _sharedModbusGateway.ReadHoldingRegisters(
                    (byte)stationNumber,
                    (ushort)startAddress,
                    registerCount,
                    _sensorReadTimeoutMs);
            }

            var client = new ModbusRtuClient
            {
                BaudRate = _sensorSerialSettings.BaudRate,
                Parity = _sensorSerialSettings.Parity,
                StopBits = _sensorSerialSettings.StopBits,
                ReadTimeout = _sensorReadTimeoutMs,
                WriteTimeout = _sensorReadTimeoutMs
            };

            try
            {
                client.Connect(sensorPort, ModbusEndianness.BigEndian);
                Log.Debug(
                    "ADAM Holding Registers 读取: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount);
                return client.ReadHoldingRegisters<ushort>((byte)stationNumber, (ushort)startAddress, registerCount).ToArray();
            }
            finally
            {
                if (client.IsConnected)
                {
                    client.Close();
                }
            }
        }

        private IReadOnlyList<ushort> ReadModbusInputRegisters(
            string sensorPort,
            int stationNumber,
            int startAddress,
            int registerCount)
        {
            if (_sharedModbusGateway != null)
            {
                Log.Debug(
                    "通过共享 Modbus 网关读取 ADAM Input Registers: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount);
                return _sharedModbusGateway.ReadInputRegisters(
                    (byte)stationNumber,
                    (ushort)startAddress,
                    registerCount,
                    _sensorReadTimeoutMs);
            }

            var client = new ModbusRtuClient
            {
                BaudRate = _sensorSerialSettings.BaudRate,
                Parity = _sensorSerialSettings.Parity,
                StopBits = _sensorSerialSettings.StopBits,
                ReadTimeout = _sensorReadTimeoutMs,
                WriteTimeout = _sensorReadTimeoutMs
            };

            try
            {
                client.Connect(sensorPort, ModbusEndianness.BigEndian);
                Log.Debug(
                    "ADAM Input Registers 读取: 端口={Port}, 站号={StationNumber}, 起始地址={StartAddress}, 数量={Count}",
                    sensorPort,
                    stationNumber,
                    startAddress,
                    registerCount);
                return client.ReadInputRegisters<ushort>((byte)stationNumber, (ushort)startAddress, registerCount).ToArray();
            }
            finally
            {
                if (client.IsConnected)
                {
                    client.Close();
                }
            }
        }

        private static double ParseChannelValue(string data, int channelIndex)
        {
            return double.Parse(
                data.Substring(1 + channelIndex * 7, 7),
                CultureInfo.InvariantCulture);
        }

        private void UpdateSensorValue(int sensorId, double value)
        {
            if (_sensors.Sensors.TryGetValue(sensorId, out var sensor))
            {
                sensor.SetInputValue(value);
            }
        }

        private void UpdateSensorRawValue(int sensorId, ushort rawValue)
        {
            if (!_sensors.Sensors.TryGetValue(sensorId, out var sensor))
            {
                return;
            }

            sensor.Inputvalue = rawValue;
            if (Math.Abs(sensor.Signalspan - sensor.Signalzero) < double.Epsilon)
            {
                sensor.Outputvalue = rawValue;
                return;
            }

            sensor.Outputvalue =
                sensor.Outputzero +
                (sensor.Outputspan - sensor.Outputzero) *
                ((rawValue - sensor.Signalzero) / (sensor.Signalspan - sensor.Signalzero));
        }

        private void PublishCurrentSensorData(
            int timer,
            double temp1,
            double temp2,
            double tempSurface,
            double tempCenter,
            double tempCalibration)
        {
            OnSensorDataReceived(new SensorDataEventArgs
            {
                Timer = timer,
                Temp1 = temp1,
                Temp2 = temp2,
                TempSurface = tempSurface,
                TempCenter = tempCenter,
                TempDrift = 0,
                TempCalibration = tempCalibration
            });
        }

        private void CleanupCacheIfNeeded()
        {
            _cacheCleanupCounter++;
            if (_cacheCleanupCounter < CacheCleanupInterval)
            {
                return;
            }

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

        protected virtual void OnSensorDataReceived(SensorDataEventArgs e)
        {
            SensorDataReceived?.Invoke(this, e);
        }

        public void ResetTimer()
        {
            _elapsedSeconds = 0;
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
        }
    }
}
