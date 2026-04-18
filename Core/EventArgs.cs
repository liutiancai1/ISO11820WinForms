using ISO11820_2020.Models;
using System;
using System.Collections.Generic;

namespace ISO11820WinForms.Core
{
    /// <summary>
    /// 数据广播事件参数
    /// 用于TestMaster向UI层广播实时传感器数据和状态信息
    /// </summary>
    public class DataBroadcastEventArgs : EventArgs
    {
        /// <summary>
        /// 试验计时器（秒）
        /// </summary>
        public int Timer { get; set; }

        /// <summary>
        /// 试验控制器ID
        /// </summary>
        public int MasterId { get; set; }

        /// <summary>
        /// 控制器工作模式
        /// </summary>
        public int MasterMode { get; set; }

        /// <summary>
        /// 控制器状态
        /// </summary>
        public int MasterStatus { get; set; }

        /// <summary>
        /// 传感器数据
        /// </summary>
        public SensorDataCatch SensorData { get; set; } = new SensorDataCatch();

        /// <summary>
        /// 计算数据
        /// </summary>
        public CaculateDataCatch CalculateData { get; set; } = new CaculateDataCatch();

        /// <summary>
        /// 控制器消息列表
        /// </summary>
        public List<MasterMessage> Messages { get; set; } = new List<MasterMessage>();

        /// <summary>
        /// 是否检测到火焰
        /// </summary>
        public bool FlameDetected { get; set; }

        /// <summary>
        /// 火焰检测时间（秒）
        /// </summary>
        public int FlameDetectedTime { get; set; }

        /// <summary>
        /// 火焰持续时间（秒）
        /// </summary>
        public int FlameDuration { get; set; }
    }

    /// <summary>
    /// 状态变更事件参数
    /// 用于TestMaster通知UI层状态机状态变化
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 试验控制器ID
        /// </summary>
        public int MasterId { get; set; }

        /// <summary>
        /// 变更前的工作模式
        /// </summary>
        public MasterWorkMode OldMode { get; set; }

        /// <summary>
        /// 变更后的工作模式
        /// </summary>
        public MasterWorkMode NewMode { get; set; }

        /// <summary>
        /// 变更前的状态
        /// </summary>
        public MasterStatus OldStatus { get; set; }

        /// <summary>
        /// 变更后的状态
        /// </summary>
        public MasterStatus NewStatus { get; set; }

        /// <summary>
        /// 状态变更时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 附加消息（可选）
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// 传感器数据接收事件参数
    /// 用于DaqWorker向TestMaster传递采集到的传感器数据
    /// </summary>
    public class SensorDataEventArgs : EventArgs
    {
        /// <summary>
        /// 试验计时器（秒）
        /// </summary>
        public int Timer { get; set; }

        /// <summary>
        /// 炉内温度1（控温温度）
        /// </summary>
        public double Temp1 { get; set; }

        /// <summary>
        /// 炉内温度2
        /// </summary>
        public double Temp2 { get; set; }

        /// <summary>
        /// 表面温度
        /// </summary>
        public double TempSurface { get; set; }

        /// <summary>
        /// 中心温度
        /// </summary>
        public double TempCenter { get; set; }

        /// <summary>
        /// 温度漂移值
        /// </summary>
        public double TempDrift { get; set; }

        /// <summary>
        /// 校准热电偶温度（用于校准模式）
        /// </summary>
        public double TempCalibration { get; set; }

        /// <summary>
        /// 数据采集时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
