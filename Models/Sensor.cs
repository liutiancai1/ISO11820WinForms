using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TestServer.Models
{
    //信号类型枚举定义
    public enum SignalType
    {
        A = 0,           //电流信号(安培)
        mA = 1,          //电流信号(毫安)
        V = 2,           //电压信号(伏特)
        mV = 3,          //电压信号(毫伏)
        Digital = 4      //数字信号(数字)
    }

    [Table("sensors")]
    public partial class Sensor
    {
        [Key]
        [Column("sensorid")]
        public int Sensorid { get; set; }
        [Column("sensorname")]
        [StringLength(50)]
        [Unicode(false)]
        public string Sensorname { get; set; } = null!;
        [Column("dispname")]
        [StringLength(100)]
        [Unicode(false)]
        public string Dispname { get; set; } = null!;
        [Column("sensorgroup")]
        [StringLength(50)]
        [Unicode(false)]
        public string Sensorgroup { get; set; } = null!;
        [Column("unit")]
        [StringLength(50)]
        [Unicode(false)]
        public string Unit { get; set; } = null!;
        [Column("discription")]
        [StringLength(100)]
        [Unicode(false)]
        public string Discription { get; set; } = null!;
        [Column("flag")]
        [StringLength(50)]
        [Unicode(false)]
        public string Flag { get; set; } = null!;
        [Column("signalzero")]
        public double Signalzero { get; set; }
        [Column("signalspan")]
        public double Signalspan { get; set; }
        [Column("outputzero")]
        public double Outputzero { get; set; }
        [Column("outputspan")]
        public double Outputspan { get; set; }
        [Column("outputvalue")]
        public double Outputvalue { get; set; }
        [Column("inputvalue")]
        public double Inputvalue { get; set; }
        [Column("signaltype")]
        public byte Signaltype { get; set; }

        public void SetInputValue(double newvalue)
        {
            switch (Signaltype)
            {
                //输入信号为模拟量的情况
                case (byte)SignalType.A:
                case (byte)SignalType.mA:
                case (byte)SignalType.V:
                case (byte)SignalType.mV:
                    Outputvalue = Outputzero + (Outputspan - Outputzero) * ((newvalue - Signalzero) / (Signalspan - Signalzero));
                    /* 判断当前输出值是否在量程范围内,若超出量程则设置传感器状态为false */
                    //if (Outputvalue > Outputspan || Outputvalue < Outputzero)                            
                    //else                            
                    break;
                //输入信号为数字量或温度值的情况
                case (byte)SignalType.Digital:
                    Outputvalue = newvalue;
                    break;
                default:
                    break;
            }
        }
    }
}
