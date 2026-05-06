using ISO11820_2020.Models;
using ISO11820WinForms.Global;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using TestServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ISO11820WinForms.Core
{
    /// <summary>
    /// 一号试验炉控制器
    /// 继承自TestMaster基类，实现完整的状态机逻辑
    /// </summary>
    public class TestMaster1 : TestMaster
    {
        // 应用程序全局上下文
        private readonly SystemContext _context;
        
        // 仿真模式支持
        private SensorSimulator? _simulator;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">系统上下文</param>
        /// <param name="sensors">传感器字典</param>
        /// <param name="apparatusManipulator">设备操作器</param>
        public TestMaster1(SystemContext context, SensorDictionary sensors, ApparatusManipulator apparatusManipulator)
            : base(sensors, apparatusManipulator)
        {
            _context = context;
            // 设置试验控制器ID - 对应一号试验炉
            MasterId = 0;
        }

        /// <summary>
        /// 设置传感器模拟器（仿真模式下使用）
        /// </summary>
        public void SetSimulator(SensorSimulator simulator)
        {
            _simulator = simulator;
            if (_apparatusManipulator != null)
            {
                _apparatusManipulator.SetSimulator(simulator);
            }
        }

        /// <summary>
        /// 获取设备操作器（用于访问 Modbus 温度等）
        /// </summary>
        public ApparatusManipulator Manipulator => _apparatusManipulator;
        public bool IsFlameDetectionEnabled => false;
        /// <summary>
        /// 初始化火焰分析器
        /// Requirement 2.3, 2.5: 初始化FlameAnalyzer并订阅FlameDetected事件
        /// </summary>
        /// <param name="videoUrl">视频流URL</param>
        /// <param name="roiFilePath">ROI坐标文件路径（可选）</param>
        public void InitializeFlameAnalyzer(string videoUrl, string? roiFilePath = null)
        {
            // 释放旧的火焰分析器
            if (_flameAnalyzer != null)
            {
                _flameAnalyzer.FlameDetected -= OnFlameAnalyzerFlameDetected;
                _flameAnalyzer.Dispose();
                _flameAnalyzer = null;
            }

            // 创建新的火焰分析器
            Serilog.Log.Information(
                "当前版本未启用火焰自动检测，忽略 InitializeFlameAnalyzer 调用: MasterId={MasterId}, VideoUrl={VideoUrl}",
                MasterId,
                videoUrl);

            // 加载ROI坐标（如果提供）

            // 订阅火焰检测事件
            // Requirement 2.3: 订阅FlameDetected事件

        }

        /// <summary>
        /// 处理FlameAnalyzer的火焰检测事件
        /// Requirement 2.3: 检测到持续火焰事件时触发FlameDetected事件
        /// </summary>
        private void OnFlameAnalyzerFlameDetected(object? sender, FlameEventArgs e)
        {
            // 调用基类的HandleFlameDetected方法
            HandleFlameDetected(sender, e);
        }

        /// <summary>
        /// 输出火焰视频文件
        /// Requirement 2.4: 试验记录停止时将火焰视频帧输出到文件
        /// </summary>
        /// <param name="outputPath">输出文件路径</param>
        /// <returns>是否成功输出</returns>
        public Task<bool> OutputFlameVideoAsync(string outputPath)
        {
            Serilog.Log.Information(
                "当前版本未启用火焰自动检测，忽略火焰视频导出: MasterId={MasterId}, OutputPath={OutputPath}",
                MasterId,
                outputPath);
            return Task.FromResult(false);
        }

        /// <summary>
        /// 重载传感器数据获取函数
        /// 从PID控制器和传感器字典获取实时数据
        /// 支持仿真模式
        /// </summary>
        protected override void FetchSensorData()
        {
            // 炉内温度始终从PID控制器（Modbus）读取
            // 即使在仿真模式下，也使用真实的Modbus温度
            _sensorDataCatch.Temp1 = _apparatusManipulator.GetCurrentTemp() / 10.0;
            _sensorDataCatch.Temp2 = _sensorDataCatch.Temp1; // 一号炉只有一个控温热电偶

            // 仿真模式：表面温度和中心温度从模拟器获取
            if (_simulator != null)
            {
                _sensorDataCatch.TempSuf = _simulator.SurfaceTemp;
                _sensorDataCatch.TempCen = _simulator.CenterTemp;
                return;
            }

            // 真实硬件模式：从统一的传感器通道映射获取表面温度和中心温度
            var (surfaceTemp, centerTemp) = SensorChannelHelper.GetSurfaceAndCenterTemperatures(_sensors.Sensors);
            _sensorDataCatch.TempSuf = surfaceTemp;
            _sensorDataCatch.TempCen = centerTemp;
        }


        /// <summary>
        /// 试验控制器工作函数（状态机）
        /// 每秒执行一次，根据当前状态驱动试验逻辑
        /// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6
        /// </summary>
        /// <param name="state">定时器状态对象</param>
        protected override void DoWork(object? state)
        {
            base.DoWork(state);

            // 刷新传感器最新采集数据
            FetchSensorData();

            // 调整10分钟缓存数据（用于漂移计算）
            y1Data10Min.Enqueue(_sensorDataCatch.Temp1);
            y2Data10Min.Enqueue(_sensorDataCatch.Temp2);
            if (y1Data10Min.Count == 601)
            {
                y1Data10Min.Dequeue();
                y2Data10Min.Dequeue();
                // 刷新计算数据最新值
                CaculateDrift10Min();
            }

            // 创建消息列表用于广播
            var messages = new List<MasterMessage>();

            // 根据控制器状态驱动试验逻辑
            switch (Status)
            {
                case MasterStatus.Idle:
                    DoIdle();
                    break;

                case MasterStatus.Preparing:
                    DoPreparing();
                    break;

                case MasterStatus.Ready:
                    DoReady();
                    break;

                case MasterStatus.Recording:
                    DoRecording(messages);
                    break;

                case MasterStatus.Complete:
                    DoComplete();
                    break;

                default:
                    break;
            }

            // 触发数据广播事件（在Recording状态下以1秒间隔广播）
            OnDataBroadcast(messages);
        }

        /// <summary>
        /// 空闲状态处理
        /// Requirement 7.1: 系统启动时初始化为Idle状态
        /// </summary>
        private void DoIdle()
        {
            // 空闲状态下不执行特殊逻辑，等待用户启动加热
        }

        /// <summary>
        /// 升温准备状态处理
        /// Requirement 7.2: 开始加热时转换到Preparing状态并开始升温
        /// </summary>
        private void DoPreparing()
        {
            int currentTemp = (int)(_sensorDataCatch.Temp1 * 10);

            // 根据当前温度阶段调整输出功率（软起动）
            if (currentTemp < 3000)
            {
                _apparatusManipulator.SwitchToManual();
                _apparatusManipulator.SetOutputPower(7680);  // 30%输出功率
            }
            else if (currentTemp < 5000)
            {
                _apparatusManipulator.SwitchToManual();
                _apparatusManipulator.SetOutputPower(12800); // 50%输出功率
            }
            else if (currentTemp < 6000)
            {
                _apparatusManipulator.SwitchToManual();
                _apparatusManipulator.SetOutputPower(17920); // 70%输出功率
            }
            else if (currentTemp < 7000)
            {
                _apparatusManipulator.SwitchToManual();
                _apparatusManipulator.SetOutputPower(23040); // 90%输出功率
            }
            else
            {
                // 温度达到700℃以上，切换至PID控温
                _apparatusManipulator.SwitchToPID();

                // 记录PID温控器的实时输出（用于计算恒功率值）
                // Requirement 6.1: 在Ready状态持续记录PID输出值到10分钟队列
                RecordPidOutput();
            }

            // Requirement 7.3: 判断是否达到试验初始条件
            bool criteriasMet = CheckStartCriteria();
            if (criteriasMet)
            {
                Serilog.Log.Information("[仿真] 状态转换: Preparing -> Ready");
                Status = MasterStatus.Ready;
            }
        }


        /// <summary>
        /// 温度平衡状态处理
        /// Requirement 7.3: 温度稳定后转换到Ready状态
        /// </summary>
        private void DoReady()
        {
            // 持续记录PID温控器的实时输出
            // Requirement 6.1: 在Ready状态持续记录PID输出值到10分钟队列
            RecordPidOutput();

            // 持续检查是否仍满足试验初始条件
            if (!CheckStartCriteria())
            {
                // 如果不再满足条件，回退到Preparing状态
                Status = MasterStatus.Preparing;
            }
        }

        /// <summary>
        /// 重写试验开始条件检查
        /// 在仿真模式下简化条件检查，只需温度稳定即可
        /// </summary>
        public override bool CheckStartCriteria()
        {
            // 仿真模式：简化条件检查
            if (_simulator != null)
            {
                // 只检查温度是否在目标范围内且模拟器已稳定
                double temp1 = _sensorDataCatch.Temp1;
                double temp2 = _sensorDataCatch.Temp2;
                
                // 温度在745-755°C范围内且模拟器标记为稳定
                bool tempInRange = temp1 >= 745 && temp1 <= 755 && temp2 >= 745 && temp2 <= 755;
                bool isStable = _simulator.IsStable;
                
                // 添加调试日志
                Serilog.Log.Debug("[仿真] CheckStartCriteria: Temp1={Temp1}°C, Temp2={Temp2}°C, TempInRange={TempInRange}, IsStable={IsStable}", 
                    temp1, temp2, tempInRange, isStable);
                
                if (tempInRange && isStable)
                {
                    Serilog.Log.Information("[仿真] 试验开始条件满足: 温度={Temp1}°C, 稳定={IsStable}", temp1, isStable);
                    return true;
                }
                
                // 如果温度在范围内但还没稳定，输出提示
                if (tempInRange && !isStable)
                {
                    Serilog.Log.Debug("[仿真] 温度已达标，等待稳定...");
                }
                
                return false;
            }
            
            return base.CheckStartCriteria();
        }

        /// <summary>
        /// 试验记录状态处理
        /// Requirement 7.4: 从Ready状态开始记录时转换到Recording状态
        /// Requirement 7.5: 满足终止条件或经过60分钟时转换到Complete状态
        /// </summary>
        /// <param name="messages">消息列表</param>
        private void DoRecording(List<MasterMessage> messages)
        {
            _sensorDataCatch.Timer = Timer;

            // 保存传感器数据至历史记录缓存
            _bufSensorData.Add(new SensorDataCatch()
            {
                Timer = _sensorDataCatch.Timer,
                Temp1 = _sensorDataCatch.Temp1,
                Temp2 = _sensorDataCatch.Temp2,
                TempSuf = _sensorDataCatch.TempSuf,
                TempCen = _sensorDataCatch.TempCen
            });

            // 处理火焰检测事件
            if (_bFlameDetected)
            {
                // 设置火焰捕捉无效（无需继续捕捉）
                _bFlameDetected = false;
                if (_testmaster != null)
                {
                    _testmaster.Flametime = Timer - _iFlameDurTime;
                    _testmaster.Flameduration = _iFlameDurTime;
                }

                // 追加消息
                messages.Add(new MasterMessage()
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    Message = $"检测到持续火焰，持续时间 {_iFlameDurTime} s",
                    FlameTime = Timer,
                    FlameDuration = _iFlameDurTime
                });

                // 触发火焰检测事件
                OnFlameDetected(_iFlameTime, _iFlameDurTime);
            }

            if (_testmaster?.UseFixedDuration == true)
            {
                int targetDurationSeconds = _testmaster.TargetDurationSeconds > 0
                    ? _testmaster.TargetDurationSeconds
                    : 3600;

                if (Timer >= targetDurationSeconds)
                {
                    CompleteTest(targetDurationSeconds, messages, $"本次试验已完成（达到设定时长 {targetDurationSeconds / 60} 分钟）。");
                }
            }
            else
            {
                // Requirement 7.5: 计时到达60分钟，无条件终止本次试验
                if (Timer == 3600)
                {
                    CompleteTest(3600, messages, "本次试验已完成（达到60分钟上限）。");
                }
                // 在试验标准要求的时间点判断是否满足试验终止条件
                else if (Timer == 1800 || Timer == 2100 || Timer == 2400
                    || Timer == 2700 || Timer == 3000 || Timer == 3300)
                {
                    if (CheckTerminateCriteria())
                    {
                        CompleteTest(Timer, messages, "本次试验已完成（满足终止条件）。");
                    }
                }
            }

            // 增加计时器
            Timer++;
        }

        /// <summary>
        /// 完成试验并更新状态
        /// </summary>
        /// <param name="totalTime">试验总时长</param>
        /// <param name="messages">消息列表</param>
        /// <param name="message">完成消息</param>
        private void CompleteTest(int totalTime, List<MasterMessage> messages, string message)
        {
            if (_testmaster != null)
            {
                _testmaster.Totaltesttime = totalTime;
            }

            // 更新控制器状态
            Status = MasterStatus.Complete;

            // 添加完成消息
            messages.Add(new MasterMessage()
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = message
            });
        }


        /// <summary>
        /// 试验完成状态处理
        /// Requirement 7.6: 试验完成后自动转换回Preparing状态
        /// </summary>
        private void DoComplete()
        {
            // 清零计时器
            Timer = 0;

            // Requirement 7.6: 设置控制器状态为Preparing，自动为下一个试验做准备
            Status = MasterStatus.Preparing;

            // 重置10分钟读秒控制变量
            _iCntStable = 0;
            _iCntDrift = 0;
            _iCntDeviation = 0;

            // 切换加热方式为PID控温
            _apparatusManipulator.SwitchToPID();

        }

        #region PID队列管理和恒功率值计算

        /// <summary>
        /// 记录PID输出值到队列
        /// Requirement 6.1: 在Ready状态持续记录PID输出值到10分钟队列
        /// </summary>
        private void RecordPidOutput()
        {
            int pidOutput = _apparatusManipulator.GetPidOutput();
            queuePidOutput.Enqueue(pidOutput);

            // 保持队列最多600个样本（10分钟）
            if (queuePidOutput.Count > 600)
            {
                queuePidOutput.Dequeue();
            }
        }

        /// <summary>
        /// 计算恒功率值（PID输出队列的平均值）
        /// Requirement 6.2: 计算PID输出队列的平均值作为恒功率值
        /// Requirement 6.5: 如果样本少于600个，使用可用样本计算平均值
        /// </summary>
        /// <returns>恒功率值（0-25600）</returns>
        public int CalculateConstantPower()
        {
            if (queuePidOutput.Count == 0)
            {
                // 仿真模式：使用默认恒功率值（约25%输出）
                if (_simulator != null)
                {
                    Serilog.Log.Information("[仿真] PID队列为空，使用默认恒功率值: 6400");
                    return 6400;
                }
                return 0;
            }

            // Requirement 6.5: 使用可用样本计算平均值
            return (int)queuePidOutput.Average();
        }

        /// <summary>
        /// 获取PID输出队列的当前长度
        /// </summary>
        /// <returns>队列长度</returns>
        public int GetPidQueueCount()
        {
            return queuePidOutput.Count;
        }

        /// <summary>
        /// 清空PID输出队列
        /// </summary>
        public void ClearPidQueue()
        {
            queuePidOutput.Clear();
        }

        #endregion

        #region 重载StartRecording以应用恒功率值

        /// <summary>
        /// 开始试验并记录传感器数据
        /// Requirement 6.2: 计算PID输出队列的平均值作为恒功率值
        /// Requirement 6.3: 将恒功率值发送到设备控制器并切换到手动控制模式
        /// </summary>
        /// <returns>是否成功开始记录</returns>
        public override bool StartRecording()
        {
            // Requirement 6.2: 计算恒功率值
            int constantPower = CalculateConstantPower();

            // Requirement 6.3: 设置恒功率值并切换到手动控制模式
            if (_apparatusManipulator.SetOutputPower(Convert.ToUInt16(constantPower))
                && _apparatusManipulator.SwitchToManual())
            {
                // 重置计时器
                Timer = 0;

                // 清空传感器数据缓存
                ClearDataCatch();

                // 重置火焰检测状态
                _bFlameDetected = false;
                _iFlameDurTime = 0;
                _simulator?.StartRecording(Timer);

                // 修改试验控制器状态为Recording
                Status = MasterStatus.Recording;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 停止记录数据
        /// Requirement 6.4: 试验记录停止时自动切换回PID控制模式
        /// Requirement 2.4: 试验记录停止时将火焰视频帧输出到文件
        /// </summary>
        /// <returns>是否成功停止记录</returns>
        public override bool StopRecording()
        {
            _simulator?.StopRecording();

            // Requirement 6.4: 切换加热方式为PID控温
            if (_apparatusManipulator.SwitchToPID())
            {
                // 重置计时器
                Timer = 0;

                // 修改试验控制器状态为Preparing
                Status = MasterStatus.Preparing;

                return true;
            }

            return false;
        }

        #endregion
    }
}
