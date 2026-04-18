using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using TestServer.Models;
using ISO11820WinForms.Models;
using OfficeOpenXml;
using ClosedXML.Excel;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using SummaryStatistics = ISO11820WinForms.Models.SummaryStatistics;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 系统字体解析器 - 用于 PDFsharp 6.x
    /// </summary>
    public class SystemFontResolver : IFontResolver
    {
        public string DefaultFontName => "Microsoft YaHei UI";

        public byte[]? GetFont(string faceName)
        {
            // 获取 Windows 字体目录
            var fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            
            // 字体文件映射
            var fontFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft YaHei UI", "msyh.ttc" },
                { "Microsoft YaHei UI#Bold", "msyhbd.ttc" },
                { "Microsoft YaHei", "msyh.ttc" },
                { "Microsoft YaHei#Bold", "msyhbd.ttc" },
                { "SimSun", "simsun.ttc" },
                { "SimHei", "simhei.ttf" },
                { "Arial", "arial.ttf" },
                { "Arial#Bold", "arialbd.ttf" },
            };

            if (fontFiles.TryGetValue(faceName, out var fileName))
            {
                var fontPath = Path.Combine(fontDir, fileName);
                if (File.Exists(fontPath))
                {
                    return File.ReadAllBytes(fontPath);
                }
            }

            // 尝试直接查找字体文件
            var directPath = Path.Combine(fontDir, faceName + ".ttf");
            if (File.Exists(directPath))
            {
                return File.ReadAllBytes(directPath);
            }

            directPath = Path.Combine(fontDir, faceName + ".ttc");
            if (File.Exists(directPath))
            {
                return File.ReadAllBytes(directPath);
            }

            return null;
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // 构建字体名称
            var faceName = familyName;
            if (isBold)
            {
                faceName += "#Bold";
            }

            // 检查是否有对应的字体
            var fontData = GetFont(faceName);
            if (fontData != null)
            {
                return new FontResolverInfo(faceName);
            }

            // 回退到默认字体
            fontData = GetFont(familyName);
            if (fontData != null)
            {
                return new FontResolverInfo(familyName);
            }

            // 最后回退到 Arial
            return new FontResolverInfo("Arial");
        }
    }

    /// <summary>
    /// 报告模板引擎
    /// </summary>
    public class ReportTemplateEngine
    {
        private readonly ReportConfiguration _config;
        private readonly ILogger _logger;
        private static bool _fontResolverInitialized = false;
        private static readonly object _fontResolverLock = new object();

        public ReportTemplateEngine(ReportConfiguration config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 设置 EPPlus 许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 初始化字体解析器（只需要初始化一次）
            InitializeFontResolver();
        }

        /// <summary>
        /// 初始化字体解析器
        /// </summary>
        private void InitializeFontResolver()
        {
            lock (_fontResolverLock)
            {
                if (!_fontResolverInitialized)
                {
                    try
                    {
                        GlobalFontSettings.FontResolver = new SystemFontResolver();
                        _fontResolverInitialized = true;
                        _logger.Information("PDFsharp 字体解析器初始化成功");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "PDFsharp 字体解析器初始化失败，PDF 导出可能不可用");
                    }
                }
            }
        }

        /// <summary>
        /// 生成 Excel 报告
        /// </summary>
        /// <param name="reportData">报告数据</param>
        /// <returns>Excel 文件路径</returns>
        public async Task<string> GenerateExcelReportAsync(TestReportData reportData)
        {
            _logger.Information("开始生成 Excel 报告");

            // 验证模板文件
            if (!File.Exists(_config.TemplateFilePath))
            {
                throw new FileNotFoundException($"报告模板文件不存在: {_config.TemplateFilePath}");
            }

            // 确保输出目录存在
            if (!Directory.Exists(_config.OutputDirectory))
            {
                Directory.CreateDirectory(_config.OutputDirectory);
                _logger.Information("创建输出目录: {OutputDirectory}", _config.OutputDirectory);
            }

            // 生成输出文件名
            var fileName = $"TestReport_{reportData.TestInfo.Productid}_{reportData.TestInfo.Testid}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var outputPath = Path.Combine(_config.OutputDirectory, fileName);

            // 复制模板文件到输出路径
            File.Copy(_config.TemplateFilePath, outputPath, true);

            // 填充模板数据
            await FillTemplateDataAsync(outputPath, reportData);

            _logger.Information("Excel 报告生成完成: {OutputPath}", outputPath);
            return outputPath;
        }

        /// <summary>
        /// 填充模板数据
        /// </summary>
        private async Task FillTemplateDataAsync(string filePath, TestReportData reportData)
        {
            await Task.Run(() =>
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0]; // 假设第一个工作表是报告模板

                // 填充样品信息
                FillProductInfo(worksheet, reportData);

                // 填充试验信息
                FillTestInfo(worksheet, reportData);

                // 填充设备信息
                FillApparatusInfo(worksheet, reportData);

                // 填充温度数据
                FillTemperatureData(worksheet, reportData);

                // 填充试验现象
                FillTestPhenomena(worksheet, reportData);

                // 计算公式
                CalculateFormulas(worksheet, reportData);

                // 保存文件
                package.Save();
                _logger.Information("模板数据填充完成");
            });
        }

        /// <summary>
        /// 计算公式字段
        /// </summary>
        private void CalculateFormulas(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                _logger.Information("开始计算公式字段");

                var sensorData = reportData.SensorData;
                if (sensorData == null || sensorData.Count == 0)
                {
                    _logger.Warning("传感器数据为空，跳过公式计算");
                    return;
                }

                // 计算温度漂移率（Temperature Drift Rate）
                CalculateTemperatureDriftRate(worksheet, sensorData);

                // 计算平均温度
                CalculateAverageTemperatures(worksheet, sensorData);

                // 计算标准偏差
                CalculateStandardDeviations(worksheet, sensorData);

                _logger.Information("公式计算完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "计算公式时出错");
            }
        }

        /// <summary>
        /// 计算温度漂移率
        /// </summary>
        private void CalculateTemperatureDriftRate(ExcelWorksheet worksheet, List<SensorDataPoint> sensorData)
        {
            try
            {
                if (sensorData.Count < 2)
                {
                    return;
                }

                // 获取最后10分钟的数据（假设每秒一个数据点）
                int lastMinutes = 10;
                int dataPoints = Math.Min(lastMinutes * 60, sensorData.Count);
                var lastData = sensorData.Skip(sensorData.Count - dataPoints).ToList();

                // 计算炉壁温度1的漂移率
                double tf1DriftRate = CalculateDriftRate(lastData.Select(d => d.Tf1).ToList());
                SetCellValue(worksheet, "F20", tf1DriftRate);

                // 计算炉壁温度2的漂移率
                double tf2DriftRate = CalculateDriftRate(lastData.Select(d => d.Tf2).ToList());
                SetCellValue(worksheet, "G20", tf2DriftRate);

                // 计算样品温度的漂移率
                double tsDriftRate = CalculateDriftRate(lastData.Select(d => d.Ts).ToList());
                SetCellValue(worksheet, "H20", tsDriftRate);

                // 计算中心温度的漂移率
                double tcDriftRate = CalculateDriftRate(lastData.Select(d => d.Tc).ToList());
                SetCellValue(worksheet, "I20", tcDriftRate);

                _logger.Debug("温度漂移率计算完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "计算温度漂移率时出错");
            }
        }

        /// <summary>
        /// 计算漂移率（使用线性回归）
        /// </summary>
        private double CalculateDriftRate(List<double> temperatures)
        {
            if (temperatures.Count < 2)
            {
                return 0;
            }

            int n = temperatures.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = temperatures[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            // 计算斜率（漂移率）
            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            
            // 转换为每分钟的漂移率（假设数据点间隔为1秒）
            double driftRatePerMinute = slope * 60;

            return Math.Round(driftRatePerMinute, 2);
        }

        /// <summary>
        /// 计算平均温度
        /// </summary>
        private void CalculateAverageTemperatures(ExcelWorksheet worksheet, List<SensorDataPoint> sensorData)
        {
            try
            {
                // 计算平均炉壁温度1
                double avgTf1 = sensorData.Average(d => d.Tf1);
                SetCellValue(worksheet, "F21", Math.Round(avgTf1, 2));

                // 计算平均炉壁温度2
                double avgTf2 = sensorData.Average(d => d.Tf2);
                SetCellValue(worksheet, "G21", Math.Round(avgTf2, 2));

                // 计算平均样品温度
                double avgTs = sensorData.Average(d => d.Ts);
                SetCellValue(worksheet, "H21", Math.Round(avgTs, 2));

                // 计算平均中心温度
                double avgTc = sensorData.Average(d => d.Tc);
                SetCellValue(worksheet, "I21", Math.Round(avgTc, 2));

                _logger.Debug("平均温度计算完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "计算平均温度时出错");
            }
        }

        /// <summary>
        /// 计算标准偏差
        /// </summary>
        private void CalculateStandardDeviations(ExcelWorksheet worksheet, List<SensorDataPoint> sensorData)
        {
            try
            {
                // 计算炉壁温度1的标准偏差
                double stdTf1 = CalculateStandardDeviation(sensorData.Select(d => d.Tf1).ToList());
                SetCellValue(worksheet, "F22", Math.Round(stdTf1, 2));

                // 计算炉壁温度2的标准偏差
                double stdTf2 = CalculateStandardDeviation(sensorData.Select(d => d.Tf2).ToList());
                SetCellValue(worksheet, "G22", Math.Round(stdTf2, 2));

                // 计算样品温度的标准偏差
                double stdTs = CalculateStandardDeviation(sensorData.Select(d => d.Ts).ToList());
                SetCellValue(worksheet, "H22", Math.Round(stdTs, 2));

                // 计算中心温度的标准偏差
                double stdTc = CalculateStandardDeviation(sensorData.Select(d => d.Tc).ToList());
                SetCellValue(worksheet, "I22", Math.Round(stdTc, 2));

                _logger.Debug("标准偏差计算完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "计算标准偏差时出错");
            }
        }

        /// <summary>
        /// 计算标准偏差
        /// </summary>
        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2)
            {
                return 0;
            }

            double avg = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            double variance = sumOfSquares / (values.Count - 1);
            return Math.Sqrt(variance);
        }

        /// <summary>
        /// 填充产品信息
        /// </summary>
        private void FillProductInfo(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                var product = reportData.ProductInfo;
                
                // 根据模板的实际单元格位置填充数据
                // 这里使用常见的报告格式，实际位置需要根据模板调整
                SetCellValue(worksheet, "B2", product.Productid);        // 产品编号
                SetCellValue(worksheet, "B3", product.Productname);      // 产品名称
                SetCellValue(worksheet, "B4", product.Specific);         // 规格型号
                SetCellValue(worksheet, "B5", product.Diameter);         // 直径
                SetCellValue(worksheet, "B6", product.Height);           // 高度

                _logger.Debug("产品信息填充完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "填充产品信息时出错");
            }
        }

        /// <summary>
        /// 填充试验信息
        /// </summary>
        private void FillTestInfo(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                var test = reportData.TestInfo;

                SetCellValue(worksheet, "E2", test.Testid);              // 试验编号
                SetCellValue(worksheet, "E3", test.Testdate);            // 试验日期
                SetCellValue(worksheet, "E4", test.Operator);            // 操作员
                SetCellValue(worksheet, "E5", test.According);           // 依据标准
                SetCellValue(worksheet, "E6", test.Rptno);               // 报告编号

                SetCellValue(worksheet, "B8", test.Ambtemp);             // 环境温度
                SetCellValue(worksheet, "B9", test.Ambhumi);             // 环境湿度
                SetCellValue(worksheet, "B10", test.Totaltesttime);      // 试验时长

                SetCellValue(worksheet, "E8", test.Preweight);           // 试验前质量
                SetCellValue(worksheet, "E9", test.Postweight);          // 试验后质量
                SetCellValue(worksheet, "E10", test.Lostweight);         // 失重
                SetCellValue(worksheet, "E11", test.LostweightPer);      // 失重率

                _logger.Debug("试验信息填充完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "填充试验信息时出错");
            }
        }

        /// <summary>
        /// 填充设备信息
        /// </summary>
        private void FillApparatusInfo(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                var apparatus = reportData.ApparatusInfo;
                if (apparatus == null)
                {
                    _logger.Warning("设备信息为空");
                    return;
                }

                SetCellValue(worksheet, "B12", apparatus.Innernumber);   // 设备编号
                SetCellValue(worksheet, "B13", apparatus.Apparatusname); // 设备名称
                SetCellValue(worksheet, "B14", apparatus.Checkdatef);    // 校验起始日期
                SetCellValue(worksheet, "B15", apparatus.Checkdatet);    // 校验截止日期
                SetCellValue(worksheet, "E12", reportData.TestInfo.Constpower); // 恒功率值

                _logger.Debug("设备信息填充完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "填充设备信息时出错");
            }
        }

        /// <summary>
        /// 填充温度数据
        /// </summary>
        private void FillTemperatureData(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                var sensorData = reportData.SensorData;
                if (sensorData == null || sensorData.Count == 0)
                {
                    _logger.Warning("传感器数据为空");
                    return;
                }

                // 从第18行开始填充温度数据（假设模板从这里开始）
                int startRow = 18;
                for (int i = 0; i < sensorData.Count; i++)
                {
                    var data = sensorData[i];
                    int row = startRow + i;

                    worksheet.Cells[row, 1].Value = data.TimeStamp;  // 时间戳
                    worksheet.Cells[row, 2].Value = data.Tf1;        // 炉壁温度1
                    worksheet.Cells[row, 3].Value = data.Tf2;        // 炉壁温度2
                    worksheet.Cells[row, 4].Value = data.Ts;         // 样品温度
                    worksheet.Cells[row, 5].Value = data.Tc;         // 中心温度
                }

                _logger.Debug("温度数据填充完成，共 {Count} 条记录", sensorData.Count);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "填充温度数据时出错");
            }
        }

        /// <summary>
        /// 填充试验现象
        /// </summary>
        private void FillTestPhenomena(ExcelWorksheet worksheet, TestReportData reportData)
        {
            try
            {
                var test = reportData.TestInfo;

                // 填充试验现象代码和描述
                SetCellValue(worksheet, "B16", test.Phenocode);          // 现象代码
                
                // 填充火焰相关数据
                SetCellValue(worksheet, "E13", test.Flametime);          // 火焰出现时间
                SetCellValue(worksheet, "E14", test.Flameduration);      // 火焰持续时间

                // 填充最大温度数据
                SetCellValue(worksheet, "B17", test.Maxtf1);             // 最大炉壁温度1
                SetCellValue(worksheet, "C17", test.Maxtf2);             // 最大炉壁温度2
                SetCellValue(worksheet, "D17", test.Maxts);              // 最大样品温度
                SetCellValue(worksheet, "E17", test.Maxtc);              // 最大中心温度

                // 填充最终温度数据
                SetCellValue(worksheet, "B18", test.Finaltf1);           // 最终炉壁温度1
                SetCellValue(worksheet, "C18", test.Finaltf2);           // 最终炉壁温度2
                SetCellValue(worksheet, "D18", test.Finalts);            // 最终样品温度
                SetCellValue(worksheet, "E18", test.Finaltc);            // 最终中心温度

                // 填充温升数据
                SetCellValue(worksheet, "B19", test.Deltatf1);           // 炉壁温升1
                SetCellValue(worksheet, "C19", test.Deltatf2);           // 炉壁温升2
                SetCellValue(worksheet, "D19", test.Deltatf);            // 样品温升
                SetCellValue(worksheet, "E19", test.Deltatc);            // 中心温升

                _logger.Debug("试验现象填充完成");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "填充试验现象时出错");
            }
        }

        /// <summary>
        /// 设置单元格值（辅助方法）
        /// </summary>
        private void SetCellValue(ExcelWorksheet worksheet, string address, object? value)
        {
            if (value != null)
            {
                worksheet.Cells[address].Value = value;
            }
        }

        /// <summary>
        /// 生成汇总报告
        /// </summary>
        /// <param name="reportDataList">报告数据列表</param>
        /// <returns>Excel 文件路径</returns>
        public async Task<string> GenerateSummaryReportAsync(List<TestReportData> reportDataList)
        {
            var stats = SummaryStatistics.Calculate(reportDataList);
            return await GenerateSummaryReportAsync(reportDataList, stats);
        }

        /// <summary>
        /// 生成汇总报告（带统计数据）
        /// </summary>
        /// <param name="reportDataList">报告数据列表</param>
        /// <param name="summaryStats">汇总统计数据</param>
        /// <returns>Excel 文件路径</returns>
        public async Task<string> GenerateSummaryReportAsync(
            List<TestReportData> reportDataList, 
            SummaryStatistics summaryStats)
        {
            _logger.Information("开始生成汇总报告，试验数量: {Count}", reportDataList.Count);

            // 确保输出目录存在
            if (!Directory.Exists(_config.OutputDirectory))
            {
                Directory.CreateDirectory(_config.OutputDirectory);
            }

            // 生成输出文件名
            var fileName = $"SummaryReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var outputPath = Path.Combine(_config.OutputDirectory, fileName);

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                
                // 创建汇总统计工作表
                var summarySheet = package.Workbook.Worksheets.Add("汇总统计");
                FillSummaryStatisticsSheet(summarySheet, summaryStats);

                // 创建试验详情工作表
                var detailSheet = package.Workbook.Worksheets.Add("试验详情");
                FillTestDetailsSheet(detailSheet, reportDataList);

                // 保存文件
                package.SaveAs(new FileInfo(outputPath));
                _logger.Information("汇总报告已保存: {OutputPath}", outputPath);
            });

            _logger.Information("汇总报告生成完成: {OutputPath}", outputPath);
            return outputPath;
        }

        /// <summary>
        /// 填充汇总统计工作表
        /// </summary>
        private void FillSummaryStatisticsSheet(ExcelWorksheet worksheet, SummaryStatistics stats)
        {
            // 设置标题
            worksheet.Cells["A1"].Value = "ISO11820 试验汇总统计报告";
            worksheet.Cells["A1:D1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // 生成日期
            worksheet.Cells["A2"].Value = $"生成日期: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Cells["A2:D2"].Merge = true;

            // 统计数据区域
            int row = 4;
            
            // 试验数量统计
            worksheet.Cells[row, 1].Value = "试验数量统计";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            row++;

            worksheet.Cells[row, 1].Value = "总试验数";
            worksheet.Cells[row, 2].Value = stats.TotalTests;
            row++;

            worksheet.Cells[row, 1].Value = "通过数";
            worksheet.Cells[row, 2].Value = stats.PassedTests;
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(System.Drawing.Color.Green);
            row++;

            worksheet.Cells[row, 1].Value = "不通过数";
            worksheet.Cells[row, 2].Value = stats.FailedTests;
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            row++;

            worksheet.Cells[row, 1].Value = "通过率";
            worksheet.Cells[row, 2].Value = stats.TotalTests > 0 
                ? $"{(double)stats.PassedTests / stats.TotalTests * 100:F1}%" 
                : "N/A";
            row += 2;

            // 温度统计
            worksheet.Cells[row, 1].Value = "温度统计";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            row++;

            worksheet.Cells[row, 1].Value = "平均最高温升 (°C)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.AvgMaxTemp, 2);
            row++;

            worksheet.Cells[row, 1].Value = "平均终平衡温度 (°C)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.AvgFinalTemp, 2);
            row++;

            worksheet.Cells[row, 1].Value = "最大温升 (°C)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.MaxTempRise, 2);
            row++;

            worksheet.Cells[row, 1].Value = "最小温升 (°C)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.MinTempRise, 2);
            row += 2;

            // 其他统计
            worksheet.Cells[row, 1].Value = "其他统计";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            row++;

            worksheet.Cells[row, 1].Value = "平均失重率 (%)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.AvgLostWeightPercent, 2);
            row++;

            worksheet.Cells[row, 1].Value = "平均试验时长 (秒)";
            worksheet.Cells[row, 2].Value = Math.Round(stats.AvgTestDuration, 0);
            row++;

            // 排除记录信息
            if (stats.ExcludedIncompleteCount > 0)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "排除的不完整记录";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                row++;

                worksheet.Cells[row, 1].Value = "排除数量";
                worksheet.Cells[row, 2].Value = stats.ExcludedIncompleteCount;
                row++;

                if (stats.ExcludedTestIds.Count > 0)
                {
                    worksheet.Cells[row, 1].Value = "排除的试验ID";
                    worksheet.Cells[row, 2].Value = string.Join(", ", stats.ExcludedTestIds);
                }
            }

            // 自动调整列宽
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 20;
        }

        /// <summary>
        /// 填充试验详情工作表
        /// </summary>
        private void FillTestDetailsSheet(ExcelWorksheet worksheet, List<TestReportData> reportDataList)
        {
            // 设置标题
            worksheet.Cells["A1"].Value = "ISO11820 试验详情列表";
            worksheet.Cells["A1:M1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // 设置列标题
            int headerRow = 3;
            worksheet.Cells[headerRow, 1].Value = "序号";
            worksheet.Cells[headerRow, 2].Value = "产品编号";
            worksheet.Cells[headerRow, 3].Value = "产品名称";
            worksheet.Cells[headerRow, 4].Value = "试验编号";
            worksheet.Cells[headerRow, 5].Value = "试验日期";
            worksheet.Cells[headerRow, 6].Value = "操作员";
            worksheet.Cells[headerRow, 7].Value = "设备名称";
            worksheet.Cells[headerRow, 8].Value = "恒功率(W)";
            worksheet.Cells[headerRow, 9].Value = "失重率(%)";
            worksheet.Cells[headerRow, 10].Value = "样品温升(°C)";
            worksheet.Cells[headerRow, 11].Value = "火焰时间(s)";
            worksheet.Cells[headerRow, 12].Value = "火焰持续(s)";
            worksheet.Cells[headerRow, 13].Value = "结果";

            // 设置列标题样式
            using (var range = worksheet.Cells[headerRow, 1, headerRow, 13])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // 填充数据
            int dataRow = headerRow + 1;
            for (int i = 0; i < reportDataList.Count; i++)
            {
                var data = reportDataList[i];
                var test = data.TestInfo;
                var product = data.ProductInfo;

                // 判断是否通过
                bool passed = test.Deltatf <= 50 && 
                              test.LostweightPer <= 50 && 
                              test.Flameduration < 5;

                worksheet.Cells[dataRow, 1].Value = i + 1;
                worksheet.Cells[dataRow, 2].Value = product?.Productid ?? "";
                worksheet.Cells[dataRow, 3].Value = product?.Productname ?? "";
                worksheet.Cells[dataRow, 4].Value = test.Testid;
                worksheet.Cells[dataRow, 5].Value = test.Testdate.ToString("yyyy-MM-dd");
                worksheet.Cells[dataRow, 6].Value = test.Operator;
                worksheet.Cells[dataRow, 7].Value = test.Apparatusname;
                worksheet.Cells[dataRow, 8].Value = test.Constpower;
                worksheet.Cells[dataRow, 9].Value = Math.Round(test.LostweightPer, 2);
                worksheet.Cells[dataRow, 10].Value = Math.Round(test.Deltatf, 2);
                worksheet.Cells[dataRow, 11].Value = test.Flametime;
                worksheet.Cells[dataRow, 12].Value = test.Flameduration;
                worksheet.Cells[dataRow, 13].Value = passed ? "通过" : "不通过";

                // 设置结果列颜色
                worksheet.Cells[dataRow, 13].Style.Font.Color.SetColor(
                    passed ? System.Drawing.Color.Green : System.Drawing.Color.Red);

                dataRow++;
            }

            // 添加统计行
            int statsRow = dataRow + 1;
            worksheet.Cells[statsRow, 1].Value = "统计";
            worksheet.Cells[statsRow, 1].Style.Font.Bold = true;

            // 计算统计数据
            if (reportDataList.Count > 0)
            {
                // 平均值
                worksheet.Cells[statsRow, 8].Value = "平均";
                worksheet.Cells[statsRow, 9].Formula = $"AVERAGE(I{headerRow + 1}:I{dataRow - 1})";
                worksheet.Cells[statsRow, 10].Formula = $"AVERAGE(J{headerRow + 1}:J{dataRow - 1})";
                worksheet.Cells[statsRow, 11].Formula = $"AVERAGE(K{headerRow + 1}:K{dataRow - 1})";
                worksheet.Cells[statsRow, 12].Formula = $"AVERAGE(L{headerRow + 1}:L{dataRow - 1})";

                // 最大值
                statsRow++;
                worksheet.Cells[statsRow, 8].Value = "最大";
                worksheet.Cells[statsRow, 9].Formula = $"MAX(I{headerRow + 1}:I{dataRow - 1})";
                worksheet.Cells[statsRow, 10].Formula = $"MAX(J{headerRow + 1}:J{dataRow - 1})";
                worksheet.Cells[statsRow, 11].Formula = $"MAX(K{headerRow + 1}:K{dataRow - 1})";
                worksheet.Cells[statsRow, 12].Formula = $"MAX(L{headerRow + 1}:L{dataRow - 1})";

                // 最小值
                statsRow++;
                worksheet.Cells[statsRow, 8].Value = "最小";
                worksheet.Cells[statsRow, 9].Formula = $"MIN(I{headerRow + 1}:I{dataRow - 1})";
                worksheet.Cells[statsRow, 10].Formula = $"MIN(J{headerRow + 1}:J{dataRow - 1})";
                worksheet.Cells[statsRow, 11].Formula = $"MIN(K{headerRow + 1}:K{dataRow - 1})";
                worksheet.Cells[statsRow, 12].Formula = $"MIN(L{headerRow + 1}:L{dataRow - 1})";
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        /// <summary>
        /// 生成预览报告
        /// </summary>
        /// <param name="reportData">报告数据</param>
        /// <returns>预览文件路径</returns>
        public async Task<string> GeneratePreviewReportAsync(TestReportData reportData)
        {
            _logger.Information("开始生成预览报告");

            // 验证模板文件
            if (!File.Exists(_config.TemplateFilePath))
            {
                throw new FileNotFoundException($"报告模板文件不存在: {_config.TemplateFilePath}");
            }

            // 生成临时文件路径
            var tempPath = Path.Combine(Path.GetTempPath(), $"Preview_{reportData.TestInfo.Testid}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");

            // 复制模板文件到临时路径
            File.Copy(_config.TemplateFilePath, tempPath, true);

            // 填充模板数据（与正式报告相同的逻辑）
            await FillTemplateDataAsync(tempPath, reportData);

            _logger.Information("预览报告生成完成: {TempPath}", tempPath);
            return tempPath;
        }

        /// <summary>
        /// 导出为 PDF
        /// </summary>
        /// <param name="excelFilePath">Excel 文件路径</param>
        /// <returns>PDF 文件路径</returns>
        public async Task<string> ExportToPdfAsync(string excelFilePath)
        {
            _logger.Information("开始导出 PDF: {ExcelFilePath}", excelFilePath);

            if (!File.Exists(excelFilePath))
            {
                throw new FileNotFoundException($"Excel 文件不存在: {excelFilePath}");
            }

            // 生成 PDF 文件路径
            var pdfPath = Path.ChangeExtension(excelFilePath, ".pdf");

            await Task.Run(() =>
            {
                try
                {
                    // 使用 ClosedXML 读取 Excel 文件
                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheet(1);

                    // 创建 PDF 文档
                    using var document = new PdfDocument();
                    document.Info.Title = "试验报告";
                    document.Info.Author = "ISO11820 系统";
                    document.Info.Subject = "试验报告";

                    // 添加页面
                    var page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    page.Orientation = PdfSharp.PageOrientation.Portrait;

                    // 创建绘图对象
                    using var gfx = XGraphics.FromPdfPage(page);
                    
                    // 使用系统字体（与登录界面一致）
                    var font = new XFont("Microsoft YaHei UI", 10, XFontStyleEx.Regular);
                    var titleFont = new XFont("Microsoft YaHei UI", 14, XFontStyleEx.Bold);

                    // 绘制标题
                    gfx.DrawString("ISO11820 试验报告", titleFont, XBrushes.Black,
                        new XRect(0, 40, page.Width.Point, 30), XStringFormats.TopCenter);

                    // 获取使用的范围
                    var usedRange = worksheet.RangeUsed();
                    if (usedRange != null)
                    {
                        double yPosition = 80;
                        double lineHeight = 20;
                        double leftMargin = 50;
                        double columnWidth = 150;

                        // 遍历单元格并绘制内容
                        for (int row = 1; row <= Math.Min(usedRange.RowCount(), 50); row++)
                        {
                            double xPosition = leftMargin;

                            for (int col = 1; col <= Math.Min(usedRange.ColumnCount(), 5); col++)
                            {
                                var cell = worksheet.Cell(row, col);
                                var value = cell.GetString();

                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    // 绘制单元格内容
                                    gfx.DrawString(value, font, XBrushes.Black,
                                        new XRect(xPosition, yPosition, columnWidth, lineHeight),
                                        XStringFormats.TopLeft);
                                }

                                xPosition += columnWidth;
                            }

                            yPosition += lineHeight;

                            // 如果超出页面，添加新页面
                            if (yPosition > page.Height.Point - 50)
                            {
                                page = document.AddPage();
                                page.Size = PdfSharp.PageSize.A4;
                                gfx.Dispose();
                                var newGfx = XGraphics.FromPdfPage(page);
                                yPosition = 50;
                            }
                        }
                    }

                    // 保存 PDF 文件
                    document.Save(pdfPath);
                    _logger.Information("PDF 文件已保存: {PdfPath}", pdfPath);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出 PDF 时出错");
                    throw;
                }
            });

            _logger.Information("PDF 导出完成: {PdfPath}", pdfPath);
            return pdfPath;
        }
    }
}
