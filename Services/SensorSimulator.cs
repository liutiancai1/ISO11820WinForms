using ISO11820WinForms.Models;
using Serilog;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 传感器数据模拟器
    /// 模拟ISO 11820试验的完整温度曲线：升温→稳定→记录→完成
    /// </summary>
    public class SensorSimulator
    {
        private readonly SimulationConfiguration _config;
        private readonly Random _random = new Random();
        
        // 模拟状态
        private double _currentFurnaceTemp1;
        private double _currentFurnaceTemp2;
        private double _currentSurfaceTemp;
        private double _currentCenterTemp;
        private double _currentCalibrationTemp;
        
        // 加热状态
        private bool _isHeating = false;
        private bool _isStable = false;
        private bool _isRecording = false;
        private int _recordingStartTime = 0;
        private int _stableCounter = 0;
        
        // PID模拟状态
        private ushort _pidOutput = 0;
        private ushort _manualOutput = 0;
        private bool _isPidMode = false;

        public SensorSimulator(SimulationConfiguration config)
        {
            _config = config;
            Reset();
            Log.Information("传感器模拟器已初始化，初始温度: {Temp}°C", _config.InitialFurnaceTemp);
        }

        /// <summary>
        /// 重置模拟器状态
        /// </summary>
        public void Reset()
        {
            _currentFurnaceTemp1 = _config.InitialFurnaceTemp;
            _currentFurnaceTemp2 = _config.InitialFurnaceTemp + GetFluctuation();
            _currentSurfaceTemp = _config.InitialFurnaceTemp * 0.3;
            _currentCenterTemp = _config.InitialFurnaceTemp * 0.3;
            _currentCalibrationTemp = _config.InitialFurnaceTemp;
            _isHeating = false;
            _isStable = false;
            _isRecording = false;
            _recordingStartTime = 0;
            _stableCounter = 0;
            _pidOutput = 0;
            _manualOutput = 0;
            _isPidMode = false;
        }

        /// <summary>
        /// 开始加热
        /// </summary>
        public void StartHeating()
        {
            _isHeating = true;
            _isStable = false;
            _stableCounter = 0;
            Log.Information("[仿真] 开始加热，当前温度: {Temp1}°C", _currentFurnaceTemp1);
        }

        /// <summary>
        /// 停止加热
        /// </summary>
        public void StopHeating()
        {
            _isHeating = false;
            Log.Information("[仿真] 停止加热");
        }

        /// <summary>
        /// 开始记录
        /// </summary>
        public void StartRecording(int currentTime)
        {
            _isRecording = true;
            _recordingStartTime = currentTime;
            Log.Information("[仿真] 开始记录，时间: {Time}秒", currentTime);
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
            Log.Information("[仿真] 停止记录");
        }

        /// <summary>
        /// 更新模拟数据（每个采集周期调用）
        /// </summary>
        /// <param name="elapsedSeconds">已运行秒数</param>
        public void Update(double elapsedSeconds)
        {
            // 每10秒输出一次状态日志
            if ((int)elapsedSeconds % 10 == 0 && elapsedSeconds > 0)
            {
                Log.Debug("[仿真] Update: IsHeating={IsHeating}, IsStable={IsStable}, Temp={Temp}°C, StableCounter={Counter}", 
                    _isHeating, _isStable, _currentFurnaceTemp1, _stableCounter);
            }

            if (_isHeating)
            {
                UpdateHeatingPhase();
            }
            else
            {
                // 不加热时缓慢降温
                UpdateCoolingPhase();
            }

            // 更新表面和中心温度（样品温度）
            UpdateSampleTemperatures(elapsedSeconds);
            
            // 更新校准热电偶温度
            _currentCalibrationTemp = _currentFurnaceTemp1 + GetFluctuation() * 2;
        }

        private void UpdateHeatingPhase()
        {
            double targetTemp = _config.TargetFurnaceTemp;
            
            if (_currentFurnaceTemp1 < targetTemp - _config.StableThreshold)
            {
                // 升温阶段
                double heatingRate = _config.HeatingRatePerSecond * 0.8; // 0.8秒周期
                _currentFurnaceTemp1 += heatingRate + GetFluctuation();
                _currentFurnaceTemp2 += heatingRate + GetFluctuation();
                _isStable = false;
                _stableCounter = 0;
                
                // 模拟PID输出（升温时高输出）
                _pidOutput = (ushort)Math.Min(25600, 20000 + _random.Next(2000));
            }
            else
            {
                // 接近目标温度，进入稳定阶段
                _currentFurnaceTemp1 = targetTemp + GetFluctuation();
                _currentFurnaceTemp2 = targetTemp + GetFluctuation();
                _stableCounter++;
                
                // 稳定5秒后标记为稳定（降低阈值以便更快进入Ready状态）
                // 原来是12次（约10秒），现在改为6次（约5秒）
                if (_stableCounter > 3)
                {
                    if (!_isStable)
                    {
                        Log.Information("[仿真] 温度已稳定，StableCounter={Counter}", _stableCounter);
                    }
                    _isStable = true;
                }
                else
                {
                    Log.Debug("[仿真] 等待稳定中，StableCounter={Counter}/6", _stableCounter);
                }
                
                // 稳定时PID输出较低
                _pidOutput = (ushort)(6400 + _random.Next(1000)); // 约25%
            }
        }

        private void UpdateCoolingPhase()
        {
            // 缓慢降温
            if (_currentFurnaceTemp1 > _config.InitialFurnaceTemp)
            {
                _currentFurnaceTemp1 -= 0.5 + GetFluctuation() * 0.1;
                _currentFurnaceTemp2 -= 0.5 + GetFluctuation() * 0.1;
            }
            _pidOutput = 0;
        }

        private void UpdateSampleTemperatures(double elapsedSeconds)
        {
            if (_isRecording)
            {
                int recordingTime = (int)(elapsedSeconds - _recordingStartTime);
                
                // 模拟样品温度上升曲线
                // 表面温度快速上升
                double surfaceTarget = Math.Min(_currentFurnaceTemp1 * 0.95, 800);
                _currentSurfaceTemp += (surfaceTarget - _currentSurfaceTemp) * 0.02 + GetFluctuation();
                
                // 中心温度缓慢上升
                double centerTarget = Math.Min(_currentFurnaceTemp1 * 0.85, 750);
                _currentCenterTemp += (centerTarget - _currentCenterTemp) * 0.01 + GetFluctuation();
            }
            else
            {
                // 非记录状态，样品温度跟随炉温但较低
                _currentSurfaceTemp = _currentFurnaceTemp1 * 0.3 + GetFluctuation();
                _currentCenterTemp = _currentFurnaceTemp1 * 0.25 + GetFluctuation();
            }
        }

        private double GetFluctuation()
        {
            return (_random.NextDouble() - 0.5) * 2 * _config.TempFluctuation;
        }

        /// <summary>
        /// 是否应该触发火焰事件
        /// </summary>
        public bool ShouldTriggerFlame(int recordingTime)
        {
            if (!_config.SimulateFlame || !_isRecording)
                return false;
                
            return recordingTime >= _config.FlameStartTime && 
                   recordingTime < _config.FlameStartTime + _config.FlameDuration;
        }

        // 属性访问器
        public double FurnaceTemp1 => Math.Round(_currentFurnaceTemp1, 1);
        public double FurnaceTemp2 => Math.Round(_currentFurnaceTemp2, 1);
        public double SurfaceTemp => Math.Round(_currentSurfaceTemp, 1);
        public double CenterTemp => Math.Round(_currentCenterTemp, 1);
        public double CalibrationTemp => Math.Round(_currentCalibrationTemp, 1);
        public bool IsStable => _isStable;
        public bool IsHeating => _isHeating;
        public bool IsRecording => _isRecording;
        
        // PID模拟
        public ushort PidOutput => _pidOutput;
        public ushort ManualOutput { get => _manualOutput; set => _manualOutput = value; }
        public bool IsPidMode { get => _isPidMode; set => _isPidMode = value; }
        public ushort CurrentTemp => (ushort)(_currentFurnaceTemp1 * 10); // 0.1°C单位
    }
}
