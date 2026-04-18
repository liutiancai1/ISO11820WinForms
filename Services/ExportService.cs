using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OxyPlot;
using OxyPlot.WindowsForms;
using Serilog;
using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using TestServer.Models;
using Microsoft.EntityFrameworkCore;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 数据导出服务类
    /// 提供CSV、Excel、图片等格式的数据导出功能
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// 导出试验数据到CSV文件
        /// </summary>
        /// <param name="testId">试验ID</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ExportTemperatureDataToCsv(string testId, string filePath)
        {
            try
            {
                Log.Information("开始导出试验数据到CSV，试验ID: {TestId}, 文件路径: {FilePath}", testId, filePath);

                // 从数据库查询试验数据
                using (var context = new ISO11820DbContext())
                {
                    var records = await context.Testmasters
                        .Include(t => t.Product)
                        .Where(t => t.Testid == testId)
                        .ToListAsync();

                    var testData = records
                        .Select(t => new
                        {
                            样品编号 = t.Productid,
                            样品标识 = t.Testid,
                            样品名称 = t.Product.Productname,
                            规格型号 = t.Product.Specific,
                            试验日期 = t.Testdate,
                            操作员 = t.Operator,
                            环境温度 = t.Ambtemp,
                            环境湿度 = t.Ambhumi,
                            样品高度 = t.Product.Height,
                            样品直径 = t.Product.Diameter,
                            初始质量 = t.Preweight,
                            残余质量 = t.Postweight,
                            现象编码 = t.Phenocode,
                            火焰时间 = t.Flametime,
                            持续时间 = t.Flameduration,
                            温度升高 = t.Deltatf,
                            质量损失率 = t.LostweightPer,
                            试验备注 = ReportPathMemoHelper.GetDisplayMemo(t.Memo)
                        })
                        .ToList();

                    if (testData.Count == 0)
                    {
                        Log.Warning("没有找到试验数据，试验ID: {TestId}", testId);
                        return false;
                    }

                    // 写入CSV文件
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ","
                    };

                    using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                    using (var csv = new CsvWriter(writer, config))
                    {
                        csv.WriteRecords(testData);
                    }

                    Log.Information("试验数据导出成功，共 {Count} 条记录", testData.Count);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出试验数据到CSV失败");
                return false;
            }
        }

        /// <summary>
        /// 导出试验报告到Excel文件
        /// </summary>
        /// <param name="testId">试验ID</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ExportTestReportToExcel(string testId, string filePath)
        {
            try
            {
                Log.Information("开始导出试验报告到Excel，试验ID: {TestId}, 文件路径: {FilePath}", testId, filePath);

                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var context = new ISO11820DbContext())
                {
                    // 查询试验数据
                    var testData = await context.Testmasters
                        .Include(t => t.Product)
                        .FirstOrDefaultAsync(t => t.Testid == testId);

                    if (testData == null)
                    {
                        Log.Warning("没有找到试验数据，试验ID: {TestId}", testId);
                        return false;
                    }

                    // 创建Excel文件
                    using (var package = new ExcelPackage())
                    {
                        // 添加试验信息工作表
                        var infoSheet = package.Workbook.Worksheets.Add("试验信息");
                        CreateTestInfoSheet(infoSheet, testData);

                        // 保存文件
                        var fileInfo = new FileInfo(filePath);
                        await package.SaveAsAsync(fileInfo);
                    }

                    Log.Information("试验报告导出成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出试验报告到Excel失败");
                return false;
            }
        }

        /// <summary>
        /// 创建试验信息工作表
        /// </summary>
        private void CreateTestInfoSheet(ExcelWorksheet sheet, Testmaster testData)
        {
            // 设置标题
            sheet.Cells["A1"].Value = "建筑材料不燃性试验报告";
            sheet.Cells["A1:D1"].Merge = true;
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Row(1).Height = 30;

            // 设置试验信息
            int row = 3;
            AddInfoRow(sheet, ref row, "样品编号", testData.Productid);
            AddInfoRow(sheet, ref row, "样品标识", testData.Testid);
            AddInfoRow(sheet, ref row, "样品名称", testData.Product?.Productname);
            AddInfoRow(sheet, ref row, "规格型号", testData.Product?.Specific);
            AddInfoRow(sheet, ref row, "试验日期", testData.Testdate.ToString("yyyy-MM-dd"));
            AddInfoRow(sheet, ref row, "操作员", testData.Operator);
            AddInfoRow(sheet, ref row, "环境温度", testData.Ambtemp.ToString("F1") + "℃");
            AddInfoRow(sheet, ref row, "环境湿度", testData.Ambhumi.ToString("F1") + "%");
            AddInfoRow(sheet, ref row, "样品高度", testData.Product?.Height.ToString("F1") + "mm");
            AddInfoRow(sheet, ref row, "样品直径", testData.Product?.Diameter.ToString("F1") + "mm");
            AddInfoRow(sheet, ref row, "初始质量", testData.Preweight.ToString("F2") + "g");
            AddInfoRow(sheet, ref row, "残余质量", testData.Postweight.ToString("F2") + "g");
            AddInfoRow(sheet, ref row, "现象编码", testData.Phenocode);
            AddInfoRow(sheet, ref row, "火焰时间", testData.Flametime.ToString("F1") + "s");
            AddInfoRow(sheet, ref row, "持续时间", testData.Flameduration.ToString("F1") + "s");
            AddInfoRow(sheet, ref row, "温度升高", testData.Deltatf.ToString("F1") + "℃");
            AddInfoRow(sheet, ref row, "质量损失率", testData.LostweightPer.ToString("F2") + "%");
            AddInfoRow(sheet, ref row, "设备编号", testData.Apparatusid);
            AddInfoRow(sheet, ref row, "设备名称", testData.Apparatusname);

            // 设置列宽
            sheet.Column(1).Width = 20;
            sheet.Column(2).Width = 40;
        }

        /// <summary>
        /// 添加信息行
        /// </summary>
        private void AddInfoRow(ExcelWorksheet sheet, ref int row, string label, string? value)
        {
            sheet.Cells[row, 1].Value = label;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 230, 241));
            
            sheet.Cells[row, 2].Value = value ?? "";
            
            row++;
        }



        /// <summary>
        /// 导出图表到图片文件
        /// </summary>
        /// <param name="plotModel">图表模型</param>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>是否成功</returns>
        public bool ExportChartToImage(PlotModel plotModel, string filePath, int width = 1200, int height = 600)
        {
            try
            {
                Log.Information("开始导出图表到图片，文件路径: {FilePath}", filePath);

                // 确定图片格式
                var extension = Path.GetExtension(filePath).ToLower();
                ImageFormat format;

                switch (extension)
                {
                    case ".png":
                        format = ImageFormat.Png;
                        break;
                    case ".jpg":
                    case ".jpeg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                    default:
                        format = ImageFormat.Png;
                        break;
                }

                // 使用OxyPlot的PngExporter导出图片
                using (var bitmap = new System.Drawing.Bitmap(width, height))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.White);
                        
                        // 创建临时PlotView来渲染图表
                        using (var plotView = new PlotView())
                        {
                            plotView.Model = plotModel;
                            plotView.Width = width;
                            plotView.Height = height;
                            
                            // 渲染到bitmap
                            var rect = new Rectangle(0, 0, width, height);
                            plotView.DrawToBitmap(bitmap, rect);
                        }
                    }

                    // 保存图片
                    bitmap.Save(filePath, format);
                }

                Log.Information("图表导出成功");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出图表到图片失败");
                return false;
            }
        }

        /// <summary>
        /// 批量导出试验数据
        /// </summary>
        /// <param name="testIds">试验ID列表</param>
        /// <param name="exportFolder">导出文件夹路径</param>
        /// <param name="exportCsv">是否导出CSV</param>
        /// <param name="exportExcel">是否导出Excel</param>
        /// <param name="progress">进度回调</param>
        /// <returns>成功导出的数量</returns>
        public async Task<int> BatchExportTestData(
            List<string> testIds, 
            string exportFolder, 
            bool exportCsv, 
            bool exportExcel,
            IProgress<int>? progress = null)
        {
            try
            {
                Log.Information("开始批量导出试验数据，共 {Count} 个试验", testIds.Count);

                int successCount = 0;
                int currentIndex = 0;

                foreach (var testId in testIds)
                {
                    try
                    {
                        // 导出CSV
                        if (exportCsv)
                        {
                            var csvPath = Path.Combine(exportFolder, $"{testId}_温度数据.csv");
                            if (await ExportTemperatureDataToCsv(testId, csvPath))
                            {
                                successCount++;
                            }
                        }

                        // 导出Excel
                        if (exportExcel)
                        {
                            var excelPath = Path.Combine(exportFolder, $"{testId}_试验报告.xlsx");
                            if (await ExportTestReportToExcel(testId, excelPath))
                            {
                                successCount++;
                            }
                        }

                        // 报告进度
                        currentIndex++;
                        progress?.Report((int)((double)currentIndex / testIds.Count * 100));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "批量导出失败，试验ID: {TestId}", testId);
                    }
                }

                Log.Information("批量导出完成，成功 {SuccessCount} 个", successCount);
                return successCount;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "批量导出试验数据失败");
                return 0;
            }
        }
    }
}
