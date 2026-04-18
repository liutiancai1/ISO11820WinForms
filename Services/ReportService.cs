using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TestServer.Models;
using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using SummaryStatistics = ISO11820WinForms.Models.SummaryStatistics;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 报告生成服务
    /// </summary>
    public class ReportService
    {
        private readonly ReportConfiguration _config;
        private readonly ILogger _logger;
        private readonly ISO11820WinForms.Models.ISO11820DbContext _dbContext;
        private readonly ReportTemplateEngine _templateEngine;

        public ReportService(
            ReportConfiguration config,
            ISO11820WinForms.Models.ISO11820DbContext dbContext,
            ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateEngine = new ReportTemplateEngine(config, logger);
        }

        /// <summary>
        /// 生成单个试验报告
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <param name="testId">试验ID</param>
        /// <param name="format">报告格式</param>
        /// <returns>报告生成结果</returns>
        public async Task<ReportResult> GenerateTestReportAsync(
            string productId,
            string testId,
            ReportFormat format = ReportFormat.ExcelAndPdf)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("开始生成试验报告: ProductId={ProductId}, TestId={TestId}, Format={Format}",
                productId, testId, format);

            try
            {
                // 1. 加载试验数据
                var reportData = await LoadTestDataAsync(productId, testId);
                if (reportData == null)
                {
                    return new ReportResult
                    {
                        Success = false,
                        ErrorMessage = $"未找到试验记录: ProductId={productId}, TestId={testId}"
                    };
                }

                // 2. 生成 Excel 报告
                string? excelPath = null;
                if (format == ReportFormat.ExcelOnly || format == ReportFormat.ExcelAndPdf)
                {
                    excelPath = await _templateEngine.GenerateExcelReportAsync(reportData);
                    _logger.Information("Excel 报告生成成功: {ExcelPath}", excelPath);
                }

                // 3. 生成 PDF 报告
                string? pdfPath = null;
                if (format == ReportFormat.PdfOnly || format == ReportFormat.ExcelAndPdf)
                {
                    if (excelPath != null)
                    {
                        pdfPath = await _templateEngine.ExportToPdfAsync(excelPath);
                        _logger.Information("PDF 报告生成成功: {PdfPath}", pdfPath);
                    }
                    else
                    {
                        // 如果只生成 PDF，需要先生成临时 Excel
                        var tempExcelPath = await _templateEngine.GenerateExcelReportAsync(reportData);
                        pdfPath = await _templateEngine.ExportToPdfAsync(tempExcelPath);
                        // 删除临时 Excel 文件
                        if (File.Exists(tempExcelPath))
                        {
                            File.Delete(tempExcelPath);
                        }
                        _logger.Information("PDF 报告生成成功: {PdfPath}", pdfPath);
                    }
                }

                // 4. 保存报告路径到数据库
                await SaveReportPathsAsync(productId, testId, excelPath, pdfPath);

                stopwatch.Stop();
                _logger.Information("报告生成完成，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new ReportResult
                {
                    Success = true,
                    ExcelFilePath = excelPath,
                    PdfFilePath = pdfPath,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "生成报告失败: ProductId={ProductId}, TestId={TestId}", productId, testId);
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = $"生成报告失败: {ex.Message}",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// 生成汇总报告
        /// </summary>
        /// <param name="testIds">试验ID列表（格式：ProductId, TestId）</param>
        /// <param name="format">报告格式</param>
        /// <returns>报告生成结果</returns>
        public async Task<ReportResult> GenerateSummaryReportAsync(
            List<(string ProductId, string TestId)> testIds,
            ReportFormat format = ReportFormat.ExcelAndPdf)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("开始生成汇总报告，试验数量: {Count}", testIds.Count);

            try
            {
                // 1. 加载所有试验数据
                var reportDataList = new List<TestReportData>();
                foreach (var (productId, testId) in testIds)
                {
                    var data = await LoadTestDataAsync(productId, testId);
                    if (data != null)
                    {
                        reportDataList.Add(data);
                    }
                }

                if (reportDataList.Count == 0)
                {
                    return new ReportResult
                    {
                        Success = false,
                        ErrorMessage = "没有找到有效的试验数据"
                    };
                }

                // 2. 计算汇总统计（带不完整记录过滤）
                var summaryStats = CalculateSummaryStatistics(reportDataList, out var excludedIds);
                
                // 如果有被排除的记录，记录日志
                if (excludedIds.Count > 0)
                {
                    _logger.Warning("汇总报告排除了 {Count} 条不完整记录: {ExcludedIds}", 
                        excludedIds.Count, string.Join(", ", excludedIds));
                }

                // 3. 生成汇总 Excel 报告
                string? excelPath = null;
                if (format == ReportFormat.ExcelOnly || format == ReportFormat.ExcelAndPdf)
                {
                    excelPath = await _templateEngine.GenerateSummaryReportAsync(reportDataList, summaryStats);
                    _logger.Information("汇总 Excel 报告生成成功: {ExcelPath}", excelPath);
                }

                // 4. 生成 PDF 报告
                string? pdfPath = null;
                if (format == ReportFormat.PdfOnly || format == ReportFormat.ExcelAndPdf)
                {
                    if (excelPath != null)
                    {
                        pdfPath = await _templateEngine.ExportToPdfAsync(excelPath);
                        _logger.Information("汇总 PDF 报告生成成功: {PdfPath}", pdfPath);
                    }
                }

                stopwatch.Stop();
                _logger.Information("汇总报告生成完成，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new ReportResult
                {
                    Success = true,
                    ExcelFilePath = excelPath,
                    PdfFilePath = pdfPath,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    ExcludedTestIds = excludedIds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "生成汇总报告失败");
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = $"生成汇总报告失败: {ex.Message}",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// 计算汇总统计数据
        /// </summary>
        /// <param name="testDataList">试验数据列表</param>
        /// <param name="excludedIds">被排除的不完整记录ID列表</param>
        /// <returns>汇总统计结果</returns>
        public SummaryStatistics CalculateSummaryStatistics(
            List<TestReportData> testDataList,
            out List<string> excludedIds)
        {
            return SummaryStatistics.CalculateWithFilter(testDataList, out excludedIds);
        }

        /// <summary>
        /// 计算汇总统计数据（不过滤）
        /// </summary>
        /// <param name="testDataList">试验数据列表</param>
        /// <returns>汇总统计结果</returns>
        public SummaryStatistics CalculateSummaryStatistics(List<TestReportData> testDataList)
        {
            return SummaryStatistics.Calculate(testDataList);
        }

        /// <summary>
        /// 生成预览报告
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <param name="testId">试验ID</param>
        /// <returns>预览报告文件路径</returns>
        public async Task<string> GeneratePreviewReportAsync(string productId, string testId)
        {
            _logger.Information("生成预览报告: ProductId={ProductId}, TestId={TestId}", productId, testId);

            try
            {
                // 加载试验数据
                var reportData = await LoadTestDataAsync(productId, testId);
                if (reportData == null)
                {
                    throw new InvalidOperationException($"未找到试验记录: ProductId={productId}, TestId={testId}");
                }

                // 生成临时 Excel 文件
                var previewPath = await _templateEngine.GeneratePreviewReportAsync(reportData);
                _logger.Information("预览报告生成成功: {PreviewPath}", previewPath);

                return previewPath;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "生成预览报告失败: ProductId={ProductId}, TestId={TestId}", productId, testId);
                throw;
            }
        }

        /// <summary>
        /// 重试失败的报告生成
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <param name="testId">试验ID</param>
        /// <returns>报告生成结果</returns>
        public async Task<ReportResult> RetryReportGenerationAsync(string productId, string testId)
        {
            _logger.Information("重试生成报告: ProductId={ProductId}, TestId={TestId}", productId, testId);
            return await GenerateTestReportAsync(productId, testId);
        }

        /// <summary>
        /// 加载试验数据
        /// Requirement 8.2: 生成试验报告时反序列化CSV数据
        /// </summary>
        private async Task<TestReportData?> LoadTestDataAsync(string productId, string testId)
        {
            try
            {
                // 查询试验主记录
                var testmaster = await _dbContext.Testmasters
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.Productid == productId && t.Testid == testId);

                if (testmaster == null)
                {
                    _logger.Warning("未找到试验记录: ProductId={ProductId}, TestId={TestId}", productId, testId);
                    return null;
                }

                // 查询设备信息
                var apparatus = await _dbContext.Apparatuses
                    .FirstOrDefaultAsync(a => a.Innernumber == testmaster.Apparatusid);

                if (apparatus == null)
                {
                    _logger.Warning("未找到设备信息: ApparatusId={ApparatusId}", testmaster.Apparatusid);
                }

                // Requirement 8.2: 使用CsvDataService从CSV文件反序列化传感器数据
                var sensorData = new List<SensorDataPoint>();
                var csvService = new CsvDataService();
                var csvFilePath = TestDataPathHelper.ResolveExistingSensorDataFilePath(productId, testId);
                
                if (File.Exists(csvFilePath))
                {
                    try
                    {
                        var rawSensorData = await csvService.DeserializeAsync(csvFilePath);
                        // 转换为SensorDataPoint格式（映射属性名）
                        sensorData = rawSensorData.Select(s => new SensorDataPoint
                        {
                            TimeStamp = s.Timer,
                            Tf1 = s.Temp1,
                            Tf2 = s.Temp2,
                            Ts = s.TempSuf,
                            Tc = s.TempCen
                        }).ToList();
                        
                        _logger.Information("已从CSV文件加载传感器数据: {FilePath}, 记录数: {Count}", 
                            csvFilePath, sensorData.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "从CSV文件加载传感器数据失败: {FilePath}", csvFilePath);
                        // 继续执行，使用空列表
                    }
                }
                else
                {
                    _logger.Warning("传感器数据CSV文件不存在: {FilePath}", csvFilePath);
                }

                return new TestReportData
                {
                    TestInfo = testmaster,
                    ProductInfo = testmaster.Product,
                    ApparatusInfo = apparatus!,
                    SensorData = sensorData
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载试验数据失败: ProductId={ProductId}, TestId={TestId}", productId, testId);
                return null;
            }
        }

        /// <summary>
        /// 保存报告路径到试验数据目录
        /// </summary>
        private async Task SaveReportPathsAsync(
            string productId,
            string testId,
            string? excelPath,
            string? pdfPath)
        {
            try
            {
                var reportPathsFilePath = TestDataPathHelper.GetReportPathsFilePath(productId, testId);
                var reportPathsDirectory = Path.GetDirectoryName(reportPathsFilePath);
                if (!string.IsNullOrWhiteSpace(reportPathsDirectory))
                {
                    Directory.CreateDirectory(reportPathsDirectory);
                }

                TestReportPaths paths;
                if (File.Exists(reportPathsFilePath))
                {
                    try
                    {
                        var existingContent = await File.ReadAllTextAsync(reportPathsFilePath);
                        paths = JsonSerializer.Deserialize<TestReportPaths>(existingContent) ?? new TestReportPaths();
                    }
                    catch
                    {
                        paths = new TestReportPaths();
                    }
                }
                else
                {
                    paths = new TestReportPaths();
                }

                if (excelPath != null)
                {
                    paths.ExcelReportPath = excelPath;
                }
                if (pdfPath != null)
                {
                    paths.PdfReportPath = pdfPath;
                }

                var content = JsonSerializer.Serialize(paths);
                await File.WriteAllTextAsync(reportPathsFilePath, content);

                _logger.Information("报告路径已保存到文件: {FilePath}", reportPathsFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存报告路径失败");
                // 不抛出异常，因为这不是关键错误
            }
        }
    }
}
