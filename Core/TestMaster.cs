using CsvHelper;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using ISO11820_2020.Models;
using ISO11820WinForms.Models;
using TestServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ISO11820WinForms.Core
{
    //定义控制器任务模式枚举
    public enum MasterWorkMode
    {
        Standby = 0,     //待命模式(一般为刚登录系统时)
        Calibration,     //标定、校准模式
        SampleTest       //样品试验模式
    }

    //定义控制器状态枚举
    public enum MasterStatus
    {
        Idle = 0,       //空闲状态(一般为刚登录系统时)
        Preparing,      //试验或标定条件准备中(比如:将温度恒定在某个范围)
        Ready,          //达到进行样品试验或系统标定的条件(比如:温度已经恒定在规定范围)
        Recording,      //开启数据记录
        Complete,       //当前试验结束
        Exception       //发生异常
    }

    //定义传感器采集数据类型(对应试验中的所有需要"采集"的传感器数据)
    public class SensorDataCatch
    {
        /* 传感器数据项 */
        public int Timer { get; set; }      //试验计时器
        public double Temp1 { get; set; }   //炉内温度1(控温温度) 
        public double Temp2 { get; set; }   //炉内温度2
        public double TempSuf { get; set; } //表面温度
        public double TempCen { get; set; } //中心温度
    }

    //定义计算数据类型
    public class CaculateDataCatch
    {
        public double Temp1Drift10Min { get; set; }  //炉内温度2 10Min漂移
        public double Temp2Drift10Min { get; set; }  //炉内温度1 10Min漂移
        public double TempDriftMean { get; set; }  //炉内温度1与炉内温度2平均漂移
    }

    //定义客户端通信缓存对象(WinForms版本不再使用SignalR)
    public class DataCatch
    {
        public int Timer { get; set; } //试验计时器
        public int MasterId { get; set; } //试验控制器ID
        public int MasterMode { get; set; } //控制器工作模式
        public int MasterStatus { get; set; } //控制器状态        
        /* 传感器数据项 */
        public SensorDataCatch sensorDataCatch { get; set; } = new SensorDataCatch();
        /* 计算数据项 */
        public CaculateDataCatch caculateDataCatch { get; set; } = new CaculateDataCatch();
        /* 试验现象记录数据项 */
        public bool FlameDetected { get; set; } = false; //记录是否检测到火焰事件
        public int FlameDetectedTime { get; set; } = 0;     //记录首次检测到持续火焰5s的起火时间
        public int FlameDuration { get; set; } = 0;     //记录火焰持续燃烧时间
        /* 控制器消息记录数据项 */
        public List<MasterMessage> MasterMessages { get; set; } = new List<MasterMessage>();
    }

    /*
     * 类型定义: 该类型定义试验控制器的基本属性与功能
     */
    public class TestMaster : ITestMaster
    {
        /* ====================== 事件定义 ================== */
        
        /// <summary>
        /// 数据广播事件 - 用于向UI层广播实时传感器数据和状态信息
        /// 在Recording状态下以1秒间隔触发
        /// </summary>
        public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

        /// <summary>
        /// 状态变更事件 - 用于通知UI层状态机状态变化
        /// </summary>
        public event EventHandler<StateChangedEventArgs>? StateChanged;

        /// <summary>
        /// 火焰检测事件 - 用于通知UI层检测到持续火焰
        /// </summary>
        public event EventHandler<FlameEventArgs>? FlameDetected;

        /* 控制器内部ID */
        public int MasterId { get; set; }
        /* 控制器工作模式指示变量 */
        private MasterWorkMode _workMode;
        public MasterWorkMode WorkMode 
        { 
            get => _workMode;
            set
            {
                if (_workMode != value)
                {
                    var oldMode = _workMode;
                    _workMode = value;
                    OnStateChanged(oldMode, value, Status, Status);
                }
            }
        }
        /* 控制器工作状态指示变量 */
        private MasterStatus _status;
        public MasterStatus Status 
        { 
            get => _status;
            set
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    OnStateChanged(WorkMode, WorkMode, oldStatus, value);
                }
            }
        }
        /* 试验或系统标定计时器变量 */
        public int Timer { get; set; }
        /* 试验数据定时记录计时器(用于样品试验过程中广播实时计算值) */
        protected System.Threading.Timer _timer;
        /* 传感器对象集合 */
        protected SensorDictionary _sensors;
        
        /* [Recording]状态所需数据结构 */
        // 试验数据缓存(包括实时传感器数据,计算数据以及控制器消息)
        protected List<SensorDataCatch> _bufSensorData;
        /* 试验设备操作对象 */
        protected ApparatusManipulator _apparatusManipulator;
        /* 视频实时分析对象 */
        protected FlameAnalyzer? _flameAnalyzer;
        /* 指示本次试验过程是否已经检测到持续火焰事件(每次试验只捕捉一次持续火焰事件) */
        protected bool _bFlameDetected;
        /* 当前试验过程检测到的火焰起火时间及持续时间(秒) */
        protected DateTime _iFlameTime;
        protected int _iFlameDurTime;

        /* 本次试验的产品数据及试样数据缓存 */
        protected Productmaster? _productMaster;
        protected Testmaster? _testmaster;

        /* [Recording]状态 与 [Preparing]状态 与 [Ready]状态 共通数据结构 */
        //传感器采集数据缓存
        protected SensorDataCatch _sensorDataCatch;
        //计算数据缓存
        protected CaculateDataCatch _caculateDataCatch;
        // 用于计算试验开始条件及终止条件的数据缓存: 10min温度漂移
        protected Queue<double> xData10Min;   //试验计时缓存队列
        protected Queue<double> y1Data10Min;  //炉内温度1缓存队列
        protected Queue<double> y2Data10Min;  //炉内温度2缓存队列

        /* [Preparing]状态与[Ready]状态所需数据结构 */
        // 用于计算试验开始条件的数据缓存: 10Min内温度范围是否稳定(750℃±5)
        protected int _iCntStable;    //10Min温度稳定读秒,当前秒满足范围则减1,否则重置为600
        protected int _iCntDeviation; //10Min温度偏离读秒,当前秒满足范围则减1,否则重置为600
        protected int _iCntDrift;     //10Min温度漂移读秒,当前秒满足范围则减1,否则重置为600
        // PID温度控制器连续10分钟的输出值缓存(0-25600对应0-100%)
        protected Queue<int> queuePidOutput;

        /* 构造函数 */
        public TestMaster(SensorDictionary sensors, ApparatusManipulator apparatusManipulator)
        {
            _sensors = sensors;
            _apparatusManipulator = apparatusManipulator;
            _timer = new System.Threading.Timer(DoWork);
            _bufSensorData = new List<SensorDataCatch>();
            xData10Min = new Queue<double>();
            queuePidOutput = new Queue<int>();

            _sensorDataCatch = new SensorDataCatch();
            _caculateDataCatch = new CaculateDataCatch();

            //初始化火焰事件指示变量(默认为没有发生持续火焰)及持续时间
            _bFlameDetected = false;
            _iFlameDurTime = 0;

            //初始化用于漂移计算的时间序列
            for (int i = 0; i < 600; i++)
            {
                xData10Min.Enqueue(i);
            }
            y1Data10Min = new Queue<double>();
            y2Data10Min = new Queue<double>();
            //初始化读秒器
            _iCntStable = 0;
            _iCntDeviation = 0;
            _iCntDrift = 0;

            //初始化控制器工作模式及状态（使用字段直接赋值避免触发事件）
            _workMode = MasterWorkMode.Standby;
            _status = MasterStatus.Idle;
        }

        /* ====================== 事件触发辅助方法 ================== */

        /// <summary>
        /// 触发数据广播事件
        /// </summary>
        /// <param name="messages">控制器消息列表（可选）</param>
        protected virtual void OnDataBroadcast(List<MasterMessage>? messages = null)
        {
            var args = new DataBroadcastEventArgs
            {
                Timer = Timer,
                MasterId = MasterId,
                MasterMode = (int)WorkMode,
                MasterStatus = (int)Status,
                SensorData = new SensorDataCatch
                {
                    Timer = _sensorDataCatch.Timer,
                    Temp1 = _sensorDataCatch.Temp1,
                    Temp2 = _sensorDataCatch.Temp2,
                    TempSuf = _sensorDataCatch.TempSuf,
                    TempCen = _sensorDataCatch.TempCen
                },
                CalculateData = new CaculateDataCatch
                {
                    Temp1Drift10Min = _caculateDataCatch.Temp1Drift10Min,
                    Temp2Drift10Min = _caculateDataCatch.Temp2Drift10Min,
                    TempDriftMean = _caculateDataCatch.TempDriftMean
                },
                Messages = messages ?? new List<MasterMessage>(),
                FlameDetected = _bFlameDetected,
                FlameDetectedTime = _bFlameDetected ? (int)(_iFlameTime - DateTime.Now).TotalSeconds : 0,
                FlameDuration = _iFlameDurTime
            };

            DataBroadcast?.Invoke(this, args);
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        /// <param name="oldMode">变更前的工作模式</param>
        /// <param name="newMode">变更后的工作模式</param>
        /// <param name="oldStatus">变更前的状态</param>
        /// <param name="newStatus">变更后的状态</param>
        /// <param name="message">附加消息（可选）</param>
        protected virtual void OnStateChanged(MasterWorkMode oldMode, MasterWorkMode newMode, 
            MasterStatus oldStatus, MasterStatus newStatus, string? message = null)
        {
            var args = new StateChangedEventArgs
            {
                MasterId = MasterId,
                OldMode = oldMode,
                NewMode = newMode,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Timestamp = DateTime.Now,
                Message = message
            };

            StateChanged?.Invoke(this, args);
        }

        /// <summary>
        /// 触发火焰检测事件
        /// </summary>
        /// <param name="time">起火时间</param>
        /// <param name="duration">持续燃烧时间（秒）</param>
        protected virtual void OnFlameDetected(DateTime time, int duration)
        {
            var args = new FlameEventArgs
            {
                Time = time,
                Duration = duration
            };

            FlameDetected?.Invoke(this, args);
        }

        /// <summary>
        /// 处理FlameAnalyzer的火焰检测事件
        /// </summary>
        protected virtual void HandleFlameDetected(object? sender, FlameEventArgs e)
        {
            if (!_bFlameDetected)
            {
                _bFlameDetected = true;
                _iFlameTime = e.Time;
                _iFlameDurTime = e.Duration;
                
                // 转发火焰事件到UI层
                OnFlameDetected(e.Time, e.Duration);
            }
        }

        /* ====================== 实现试验控制器通用接口方法 ================== */

        /* 控制器初始化函数 */
        public bool OnInitialized()
        {
             Console.WriteLine("连接PID温度控制器");
            //连接PID控温器
            if (_apparatusManipulator.EstablishConnection())
            {
                //启动试验控制器并设置状态为[Idle]           
                Status = MasterStatus.Idle;
                _timer?.Change(0, 1000);
                return true;
            }

            return false;
        }

        /* 控制器工作函数 */
        protected virtual void DoWork(object? state)
        {
        }

        /* 
         * 功能: 计算10min炉内温度漂移
        */
        protected void CaculateDrift10Min()
        {
            double[] xArray = xData10Min.ToArray();   //时间数据序列
            double[] y1Array = y1Data10Min.ToArray(); //炉内温度1数据序列
            double[] y2Array = y2Data10Min.ToArray(); //炉内温度2数据序列
            //拟合曲线,取得斜率与截距
            (_, double slope1) = Fit.Line(xArray, y1Array); //炉内温度1拟合曲线参数
            (_, double slope2) = Fit.Line(xArray, y2Array); //炉内温度2拟合曲线参数
            //计算温度漂移值
            _caculateDataCatch.Temp1Drift10Min = Math.Abs(slope1 * 599);
            _caculateDataCatch.Temp2Drift10Min = Math.Abs(slope2 * 599);
            _caculateDataCatch.TempDriftMean =
                (_caculateDataCatch.Temp1Drift10Min + _caculateDataCatch.Temp2Drift10Min) / 2;
        }

        /*
         * 功能: 清理试验数据缓存
         */
        protected void ClearDataCatch()
        {
            _bufSensorData.Clear();
        }

        public void CreateCalibration()
        {
            throw new NotImplementedException();
        }

        public void CreateTest()
        {
            throw new NotImplementedException();
        }

        /*
         * 功能: 开始试验并记录传感器数据
         *       1.设置当前试验使用的恒功率值为: PID温度控制器连续10分钟的输出值的平均值
         *       2.向试验设备控制器发送指令,切换加热方式为手动控制方式
         */
        public bool StartRecording()
        {
            /* 开始样品试验前的初始化工作 */
            if (_apparatusManipulator.SetOutputPower(Convert.ToUInt16(queuePidOutput.Average()))
                && _apparatusManipulator.SwitchToManual())
            {
                //重置计时器
                Timer = 0;
                //修改试验控制器状态为[Recording]
                Status = MasterStatus.Recording;
                return true;
            }
            return false;
        }

        public bool StopRecording()
        {
            //向试验设备控制器发送指令,切换加热方式为PID控温            
            if (_apparatusManipulator.SwitchToPID())
            {
                //重置计时器
                Timer = 0;
                //修改试验控制器状态为[Preparing]
                Status = MasterStatus.Preparing;
                return true;
            }
            return false;
        }

        /*
         * 功能: 启动不燃炉加热
         */
        public async Task<int> StartHeatingAsync()
        {
            Console.WriteLine("启动试验炉加热");
            var ret = await Task.Run(() => _apparatusManipulator.StartHeating());
            if(ret)
            {
                Status = MasterStatus.Preparing;
            }            
            return ret ? 0 : -1;
        }

        /*
         * 功能: 停止不燃炉加热
         */
        public int StopHeating()
        {
            var ret = _apparatusManipulator.StopHeating();
            if(ret)
            {
                Status = MasterStatus.Idle;
            }            
            return ret ? 0 : -1;
        }

        public virtual void SetProductData(Productmaster prodmaster)
        {
            _productMaster = prodmaster;
        }

        public virtual Productmaster GetProductData()
        {
            return _productMaster!;
        }

        public virtual void ResetProductData()
        {
            _productMaster = null;
        }

        public virtual void SetTestData(Testmaster testmaster)
        {
            _testmaster = testmaster;
        }

        public virtual Testmaster GetTestData()
        {
            return _testmaster!;
        }

        public virtual void ResetTestData()
        {
            _testmaster = null;
        }

        public void SetPhenomenon(string phenocode, string memo)
        {
            throw new NotImplementedException();
        }

        /*
         * 功能: 判断试验条件是否满足(执行标准ISO 11820)
         */
        public virtual bool CheckStartCriteria()
        {
            //炉内温度1和炉内温度2取整临时变量
            int temp1, temp2;
            //10Min内,炉内温度1和炉内温度2最大值与平均值临时变量
            double max1, max2, average1, average2;
            /* 计算是否达到试验初始条件 */
            //10分钟后,漂移值缓存满,开始计算漂移值
            if (y1Data10Min.Count >= 600)
            {
                //计算温度范围条件
                temp1 = (int)(_sensorDataCatch.Temp1 * 10);
                temp2 = (int)(_sensorDataCatch.Temp2 * 10);
                if ((temp1 <= 7550 && temp1 >= 7450) && (temp2 <= 7550 && temp2 >= 7450))
                {
                    if (_iCntStable > 0) _iCntStable--;
                }
                else
                {
                    _iCntStable = 600;
                }
                //计算温度漂移条件
                if ((int)(_caculateDataCatch.Temp1Drift10Min * 10) <= 20
                    && (int)(_caculateDataCatch.Temp2Drift10Min * 10) <= 20)
                {
                    if (_iCntDrift > 0) _iCntDrift--;
                }
                else
                {
                    _iCntDrift = 600;
                }
                //计算温度偏差条件
                max1 = y1Data10Min.Max();
                max2 = y2Data10Min.Max();
                average1 = y1Data10Min.Average();
                average2 = y2Data10Min.Average();
                if ((int)(max1 - average1) <= 10 && (int)(max2 - average2) <= 10)
                {
                    if (_iCntDeviation > 0) _iCntDeviation--;
                }
                else
                {
                    _iCntDeviation = 600;
                }
                //判断是否达到试验初始条件并修改控制器状态            
                return _iCntStable == 0 && _iCntDrift == 0 && _iCntDeviation == 0;
            }
            //未满10Min的情况,默认返回false
            return false;
        }

        /*
         * 功能: 判断试验终止条件是否满足
         */
        public virtual bool CheckTerminateCriteria()
        {
            //判断试验终止条件是否满足 
            return ((int)(_caculateDataCatch.Temp1Drift10Min * 10) <= 20 &&
                 (int)(_caculateDataCatch.Temp2Drift10Min * 10) <= 20) ? true : false;
        }

        /*
         * 功能: 试验计时结束后数据处理函数
         * Requirement 8.1: 试验记录完成时将传感器数据序列化为CSV文件
         */
        public virtual async Task PostTestProcess()
        {
            /* 创建本地存储目录 */
            string prodpath = $"D:\\ISO11820\\{_testmaster!.Productid}";
            string smppath = $"{prodpath}\\{_testmaster.Testid}";
            string datapath = $"{smppath}\\data";
            string rptpath = $"{smppath}\\report";
            try
            {
                /* 创建本次试验结果文件的存储目录 */
                Directory.CreateDirectory(prodpath);
                Directory.CreateDirectory(smppath);
                Directory.CreateDirectory(datapath);
                Directory.CreateDirectory(rptpath);

                /* Requirement 8.1: 使用CsvDataService保存本次试验数据文件 */
                var csvService = new ISO11820WinForms.Services.CsvDataService();
                var csvFilePath = ISO11820WinForms.Services.CsvDataService.GetSensorDataFilePath(
                    _testmaster.Productid, _testmaster.Testid);
                await csvService.SerializeAsync(_bufSensorData, csvFilePath);

                // 更新本次试验数据至试验数据库（记录已在新建试验时创建）
                using (var ctx = new ISO11820DbContext())
                {
                    // 查找已存在的记录并更新
                    var existingTest = await ctx.Testmasters
                        .FirstOrDefaultAsync(t => t.Productid == _testmaster.Productid && t.Testid == _testmaster.Testid);
                    
                    if (existingTest != null)
                    {
                        // 更新试验后数据
                        existingTest.Postweight = _testmaster.Postweight;
                        existingTest.Phenocode = _testmaster.Phenocode;
                        existingTest.Flametime = _testmaster.Flametime;
                        existingTest.Flameduration = _testmaster.Flameduration;
                        existingTest.Totaltesttime = _testmaster.Totaltesttime;
                        existingTest.Maxtf1 = _testmaster.Maxtf1;
                        existingTest.Maxtf1Time = _testmaster.Maxtf1Time;
                        existingTest.Finaltf1 = _testmaster.Finaltf1;
                        existingTest.Flag = "10000000"; // 标记试验已完成
                        await ctx.SaveChangesAsync();
                    }
                    else
                    {
                        // 如果记录不存在（异常情况），则添加新记录
                        ctx.Testmasters.Add(_testmaster);
                        await ctx.SaveChangesAsync();
                    }
                }

                // 在后台线程生成报告（不阻塞主流程）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await GenerateTestReportAsync(_testmaster.Productid, _testmaster.Testid);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不影响试验数据保存
                        Serilog.Log.Error(ex, "生成试验报告失败: ProductId={ProductId}, TestId={TestId}", 
                            _testmaster.Productid, _testmaster.Testid);
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 生成试验报告（在后台线程执行）
        /// </summary>
        private async Task GenerateTestReportAsync(string productId, string testId)
        {
            try
            {
                // 获取配置服务
                var configService = ISO11820WinForms.Services.ConfigurationService.Instance;
                var reportConfig = configService.ReportConfig;

                // 创建报告服务
                using var dbContext = new ISO11820DbContext();
                var logger = Serilog.Log.ForContext<TestMaster>();
                var reportService = new ISO11820WinForms.Services.ReportService(
                    reportConfig, 
                    dbContext, 
                    logger);

                // 生成报告
                var result = await reportService.GenerateTestReportAsync(
                    productId, 
                    testId, 
                    TestServer.Models.ReportFormat.ExcelAndPdf);

                if (result.Success)
                {
                    logger.Information("试验报告生成成功: Excel={ExcelPath}, PDF={PdfPath}", 
                        result.ExcelFilePath, result.PdfFilePath);
                }
                else
                {
                    logger.Warning("试验报告生成失败: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "生成试验报告时发生异常");
                throw;
            }
        }

        /* ======================= ISO11820 独有的函数 ================ */
        /* 取得当前试验控制器对应的传感器数据当前值 */
        protected virtual void FetchSensorData()
        {
        }

        /*
         * 功能: 设置当前试验样品的残余质量
         */
        public void SetPostTestData(string phenocode, int flametime, int flamedur, double mass)
        {
            _testmaster!.Phenocode = phenocode;
            _testmaster.Flametime = flametime;
            _testmaster.Flameduration = flamedur;
            _testmaster.Postweight = mass;
        }

        /*
         * 功能: 设置设备参数
         * 参数:
         *      apparatusId - 设备编号
         *      apparatusName - 设备名称
         *      checkDateFrom - 检定日期（起始）
         *      checkDateTo - 检定日期（终止）
         *      pidPort - PID 端口
         *      constPower - 恒功率值
         */
        public async Task<bool> SetApparatusParam(string apparatusId, string apparatusName, DateTime checkDateFrom, DateTime checkDateTo, string pidPort, int constPower)
        {
            try
            {
                // 更新数据库中的设备参数
                using (var ctx = new ISO11820DbContext())
                {
                    // 查找设备记录（假设 MasterId 对应 apparatusid）
                    var apparatus = await ctx.Apparatuses.FindAsync(MasterId);
                    
                    if (apparatus != null)
                    {
                        // 更新设备参数
                        apparatus.Innernumber = apparatusId;
                        apparatus.Apparatusname = apparatusName;
                        apparatus.Checkdatef = checkDateFrom;
                        apparatus.Checkdatet = checkDateTo;
                        apparatus.Pidport = pidPort;
                        apparatus.Constpower = constPower;

                        // 保存更改
                        await ctx.SaveChangesAsync();
                        
                        // 性能优化：清除缓存以确保下次获取最新数据
                        ISO11820WinForms.Services.CacheService.Instance.ClearApparatusCache(MasterId);
                        
                        return true;
                    }
                    else
                    {
                        // 如果设备记录不存在，创建新记录
                        var newApparatus = new Apparatus
                        {
                            Apparatusid = MasterId,
                            Innernumber = apparatusId,
                            Apparatusname = apparatusName,
                            Checkdatef = checkDateFrom,
                            Checkdatet = checkDateTo,
                            Pidport = pidPort,
                            Constpower = constPower,
                            Powerport = "COM2"  // 默认值
                        };
                        ctx.Apparatuses.Add(newApparatus);
                        await ctx.SaveChangesAsync();
                        
                        // 性能优化：清除缓存以确保下次获取最新数据
                        ISO11820WinForms.Services.CacheService.Instance.ClearApparatusCache(MasterId);
                        
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /*
         * 功能: 获取设备参数
         * 性能优化：使用缓存服务减少数据库访问
         * 返回: 设备参数对象，如果不存在则返回 null
         */
        public async Task<Apparatus?> GetApparatusParam()
        {
            try
            {
                // 使用缓存服务获取设备参数
                return await ISO11820WinForms.Services.CacheService.Instance.GetApparatusAsync(MasterId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
