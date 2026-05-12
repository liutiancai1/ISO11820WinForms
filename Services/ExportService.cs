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
using OfficeOpenXml.Drawing.Chart;
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var context = new ISO11820DbContext())
                {
                    var testData = await context.Testmasters
                        .Include(t => t.Product)
                        .FirstOrDefaultAsync(t => t.Testid == testId);

                    if (testData == null)
                    {
                        Log.Warning("没有找到试验数据，试验ID: {TestId}", testId);
                        return false;
                    }

                    using (var package = new ExcelPackage())
                    {
                        var infoSheet = package.Workbook.Worksheets.Add("试验信息");
                        CreateTestInfoSheet(infoSheet, testData);

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
        /// 导出记录查询结果到独立Excel文件
        /// </summary>
        public async Task<bool> ExportQueryResultsToExcel(
            IReadOnlyList<Testmaster> records,
            string filePath,
            DateTime startDate,
            DateTime endDate,
            string productId,
            string testId,
            string operatorId)
        {
            try
            {
                Log.Information("开始导出记录查询结果到Excel，记录数: {Count}, 文件路径: {FilePath}", records.Count, filePath);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var sheet = package.Workbook.Worksheets.Add("查询结果");
                    CreateQueryResultsSheet(sheet, records, startDate, endDate, productId, testId, operatorId);

                    var fileInfo = new FileInfo(filePath);
                    await package.SaveAsAsync(fileInfo);
                }

                Log.Information("记录查询结果导出成功");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导出记录查询结果到Excel失败");
                return false;
            }
        }

        /// <summary>
        /// 创建试验信息工作表
        /// </summary>
        private void CreateTestInfoSheet(ExcelWorksheet sheet, Testmaster testData)
        {
            sheet.Cells["A1"].Value = "建筑材料不燃性试验报告";
            sheet.Cells["A1:F1"].Merge = true;
            sheet.Cells["A1:F1"].Style.Font.Size = 20;
            sheet.Cells["A1:F1"].Style.Font.Bold = true;
            sheet.Cells["A1:F1"].Style.Font.Color.SetColor(Color.White);
            sheet.Cells["A1:F1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells["A1:F1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(20, 83, 91));
            sheet.Cells["A1:F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells["A1:F1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Row(1).Height = 34;

            AddReportSubtitle(sheet, 2, $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}");

            AddSectionHeader(sheet, 3, "试验概览");
            AddFieldRow(sheet, 4,
                ("报告编号", testData.Rptno),
                ("试验日期", FormatDateTime(testData.Testdate)),
                ("操作员", testData.Operator));
            AddFieldRow(sheet, 5,
                ("样品编号", testData.Productid),
                ("样品标识", testData.Testid),
                ("试验结果", testData.Phenocode));

            AddSectionHeader(sheet, 7, "样品信息");
            AddFieldRow(sheet, 8,
                ("样品名称", testData.Product?.Productname),
                ("规格型号", testData.Product?.Specific),
                ("样品高度", FormatNumber(testData.Product?.Height, "mm", 1)));
            AddFieldRow(sheet, 9,
                ("样品直径", FormatNumber(testData.Product?.Diameter, "mm", 1)),
                ("初始质量", FormatNumber(testData.Preweight, "g", 2)),
                ("残余质量", FormatNumber(testData.Postweight, "g", 2)));

            AddSectionHeader(sheet, 12, "试验环境");
            AddFieldRow(sheet, 13,
                ("环境温度", FormatNumber(testData.Ambtemp, "℃", 1)),
                ("环境湿度", FormatNumber(testData.Ambhumi, "%", 1)),
                ("设备编号", testData.Apparatusid));
            AddFieldRow(sheet, 14,
                ("设备名称", testData.Apparatusname),
                ("试验依据", testData.According),
                ("试验时长", FormatNumber(testData.Totaltesttime, "s", 0)));

            AddSectionHeader(sheet, 16, "试验结果");
            AddFieldRow(sheet, 17,
                ("火焰时间", FormatNumber(testData.Flametime, "s", 0)),
                ("持续时间", FormatNumber(testData.Flameduration, "s", 0)),
                ("温度升高", FormatNumber(testData.Deltatf, "℃", 1)));
            AddFieldRow(sheet, 18,
                ("质量损失", FormatNumber(testData.Lostweight, "g", 2)),
                ("质量损失率", FormatNumber(testData.LostweightPer, "%", 2)),
                ("现象编码", testData.Phenocode));

            AddSectionHeader(sheet, 20, "备注");
            sheet.Cells["A21:F22"].Merge = true;
            sheet.Cells["A21"].Value = SafeText(ReportPathMemoHelper.GetDisplayMemo(testData.Memo));
            sheet.Cells["A21:F22"].Style.WrapText = true;
            sheet.Cells["A21:F22"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            ApplyThinBorder(sheet.Cells["A21:F22"]);

            ApplyReportPageSetup(sheet);
        }

        private void CreateQueryResultsSheet(
            ExcelWorksheet sheet,
            IReadOnlyList<Testmaster> records,
            DateTime startDate,
            DateTime endDate,
            string productId,
            string testId,
            string operatorId)
        {
            sheet.Cells["A1"].Value = "试验记录查询结果";
            sheet.Cells["A1:I1"].Merge = true;
            sheet.Cells["A1:I1"].Style.Font.Size = 18;
            sheet.Cells["A1:I1"].Style.Font.Bold = true;
            sheet.Cells["A1:I1"].Style.Font.Color.SetColor(Color.White);
            sheet.Cells["A1:I1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells["A1:I1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(20, 83, 91));
            sheet.Cells["A1:I1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Row(1).Height = 32;

            AddReportSubtitle(sheet, 2, $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}，共 {records.Count} 条记录", 9);
            AddSectionHeader(sheet, 3, "查询条件", 9);
            AddFieldRow(sheet, 4,
                ("日期范围", $"{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}"),
                ("样品编号", BlankAsAll(productId)),
                ("样品标识", BlankAsAll(testId)));
            sheet.Cells[4, 7].Value = "操作员";
            sheet.Cells[4, 8, 4, 9].Merge = true;
            sheet.Cells[4, 8].Value = BlankAsAll(operatorId);
            StyleLabelCell(sheet.Cells[4, 7]);
            StyleValueCell(sheet.Cells[4, 8, 4, 9]);

            var headers = new[]
            {
                "试验日期",
                "样品编号",
                "样品标识",
                "样品名称",
                "操作员",
                "试验结果",
                "初始质量(g)",
                "残余质量(g)",
                "试验备注"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[6, i + 1].Value = headers[i];
            }

            using (var range = sheet.Cells["A6:I6"])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.AutoFilter = true;
            }

            int row = 7;
            foreach (var record in records)
            {
                sheet.Cells[row, 1].Value = record.Testdate;
                sheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                sheet.Cells[row, 2].Value = record.Productid;
                sheet.Cells[row, 3].Value = record.Testid;
                sheet.Cells[row, 4].Value = SafeText(record.Product?.Productname);
                sheet.Cells[row, 5].Value = record.Operator;
                sheet.Cells[row, 6].Value = record.Phenocode;
                sheet.Cells[row, 7].Value = record.Preweight;
                sheet.Cells[row, 8].Value = record.Postweight;
                sheet.Cells[row, 9].Value = SafeText(ReportPathMemoHelper.GetDisplayMemo(record.Memo));
                sheet.Cells[row, 7, row, 8].Style.Numberformat.Format = "0.00";
                row++;
            }

            if (records.Count > 0)
            {
                ApplyThinBorder(sheet.Cells[6, 1, row - 1, 9]);
                sheet.Cells[7, 1, row - 1, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                sheet.Cells[7, 9, row - 1, 9].Style.WrapText = true;
            }
            else
            {
                ApplyThinBorder(sheet.Cells["A6:I6"]);
            }

            sheet.View.ShowGridLines = false;
            sheet.View.FreezePanes(7, 1);
            sheet.PrinterSettings.Orientation = eOrientation.Landscape;
            sheet.PrinterSettings.FitToPage = true;
            sheet.PrinterSettings.FitToWidth = 1;
            sheet.PrinterSettings.FitToHeight = 0;

            sheet.Column(1).Width = 20;
            sheet.Column(2).Width = 16;
            sheet.Column(3).Width = 18;
            sheet.Column(4).Width = 24;
            sheet.Column(5).Width = 14;
            sheet.Column(6).Width = 14;
            sheet.Column(7).Width = 14;
            sheet.Column(8).Width = 14;
            sheet.Column(9).Width = 34;
        }

        private static void AddReportSubtitle(ExcelWorksheet sheet, int row, string text, int lastColumn = 6)
        {
            sheet.Cells[row, 1].Value = text;
            sheet.Cells[row, 1, row, lastColumn].Merge = true;
            sheet.Cells[row, 1, row, lastColumn].Style.Font.Color.SetColor(Color.FromArgb(71, 85, 105));
            sheet.Cells[row, 1, row, lastColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        private static void AddSectionHeader(ExcelWorksheet sheet, int row, string text, int lastColumn = 6)
        {
            sheet.Cells[row, 1].Value = text;
            sheet.Cells[row, 1, row, lastColumn].Merge = true;
            sheet.Cells[row, 1, row, lastColumn].Style.Font.Bold = true;
            sheet.Cells[row, 1, row, lastColumn].Style.Font.Color.SetColor(Color.White);
            sheet.Cells[row, 1, row, lastColumn].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[row, 1, row, lastColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
            sheet.Cells[row, 1, row, lastColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            sheet.Cells[row, 1, row, lastColumn].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Row(row).Height = 24;
        }

        private static void AddFieldRow(ExcelWorksheet sheet, int row, params (string Label, string? Value)[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                int labelColumn = i * 2 + 1;
                int valueColumn = labelColumn + 1;
                sheet.Cells[row, labelColumn].Value = fields[i].Label;
                sheet.Cells[row, valueColumn].Value = SafeText(fields[i].Value);
                StyleLabelCell(sheet.Cells[row, labelColumn]);
                StyleValueCell(sheet.Cells[row, valueColumn]);
            }

            sheet.Row(row).Height = 24;
        }

        private static void StyleLabelCell(ExcelRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.Color.SetColor(Color.FromArgb(51, 65, 85));
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(226, 232, 240));
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyThinBorder(range);
        }

        private static void StyleValueCell(ExcelRange range)
        {
            range.Style.Font.Color.SetColor(Color.FromArgb(15, 23, 42));
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyThinBorder(range);
        }

        private static void ApplyThinBorder(ExcelRange range)
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Top.Color.SetColor(Color.FromArgb(203, 213, 225));
            range.Style.Border.Bottom.Color.SetColor(Color.FromArgb(203, 213, 225));
            range.Style.Border.Left.Color.SetColor(Color.FromArgb(203, 213, 225));
            range.Style.Border.Right.Color.SetColor(Color.FromArgb(203, 213, 225));
        }

        private static void ApplyReportPageSetup(ExcelWorksheet sheet)
        {
            sheet.View.ShowGridLines = false;
            sheet.PrinterSettings.Orientation = eOrientation.Portrait;
            sheet.PrinterSettings.FitToPage = true;
            sheet.PrinterSettings.FitToWidth = 1;
            sheet.PrinterSettings.FitToHeight = 0;

            sheet.Column(1).Width = 14;
            sheet.Column(2).Width = 22;
            sheet.Column(3).Width = 14;
            sheet.Column(4).Width = 22;
            sheet.Column(5).Width = 14;
            sheet.Column(6).Width = 22;
        }

        private static string FormatDateTime(DateTime value)
        {
            return value == default ? "-" : value.ToString("yyyy-MM-dd HH:mm");
        }

        private static string FormatNumber(double? value, string unit, int digits)
        {
            return value.HasValue ? $"{value.Value.ToString($"F{digits}")}{unit}" : "-";
        }

        private static string BlankAsAll(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "全部" : value;
        }

        private static string SafeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        public void ExportChartToExcel(PlotModel plotModel, string filePath)
        {
            if (plotModel == null)
            {
                throw new ArgumentNullException(nameof(plotModel));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            var lineSeries = plotModel.Series
                .OfType<OxyPlot.Series.LineSeries>()
                .Where(series => series.Points.Count > 0)
                .ToList();

            if (lineSeries.Count == 0)
            {
                throw new InvalidOperationException("当前温度曲线没有可导出的数据。");
            }

            Log.Information("开始导出图表到Excel，文件路径: {FilePath}", filePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                filePath = Path.ChangeExtension(filePath, ".xlsx");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var chartSheet = package.Workbook.Worksheets.Add("温度曲线");
            var dataSheet = package.Workbook.Worksheets.Add("曲线数据");

            chartSheet.View.ShowGridLines = false;
            chartSheet.Cells["A1:J1"].Merge = true;
            chartSheet.Cells["A1"].Value = string.IsNullOrWhiteSpace(plotModel.Title) ? "温度曲线" : plotModel.Title;
            chartSheet.Cells["A1"].Style.Font.Size = 18;
            chartSheet.Cells["A1"].Style.Font.Bold = true;
            chartSheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            chartSheet.Cells["A2:J2"].Merge = true;
            chartSheet.Cells["A2"].Value = $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            chartSheet.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(71, 85, 105));
            chartSheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            dataSheet.Cells["A1:F1"].Merge = true;
            dataSheet.Cells["A1"].Value = string.IsNullOrWhiteSpace(plotModel.Title) ? "温度曲线数据" : $"{plotModel.Title}数据";
            dataSheet.Cells["A1"].Style.Font.Size = 16;
            dataSheet.Cells["A1"].Style.Font.Bold = true;
            dataSheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            dataSheet.Cells["A2:F2"].Merge = true;
            dataSheet.Cells["A2"].Value = $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            dataSheet.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(71, 85, 105));
            dataSheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            const int headerRow = 4;
            const int compatibilityHeaderRow = 21;
            chartSheet.Row(compatibilityHeaderRow).Hidden = true;
            chartSheet.Cells[compatibilityHeaderRow, 1].Value = "时间(s)";

            dataSheet.Cells[headerRow, 1].Value = "时间(s)";
            dataSheet.Cells[headerRow, 1].Style.Font.Bold = true;

            var xValues = lineSeries
                .SelectMany(series => series.Points.Select(point => point.X))
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            var seriesPointMaps = lineSeries
                .Select(series => series.Points.ToDictionary(point => point.X, point => point.Y))
                .ToList();

            for (int seriesIndex = 0; seriesIndex < lineSeries.Count; seriesIndex++)
            {
                int column = seriesIndex + 2;
                dataSheet.Cells[headerRow, column].Value = string.IsNullOrWhiteSpace(lineSeries[seriesIndex].Title)
                    ? $"曲线{seriesIndex + 1}"
                    : lineSeries[seriesIndex].Title;
                dataSheet.Cells[headerRow, column].Style.Font.Bold = true;
                chartSheet.Cells[compatibilityHeaderRow, column].Value = dataSheet.Cells[headerRow, column].Value;
            }

            int dataStartRow = headerRow + 1;
            for (int rowIndex = 0; rowIndex < xValues.Count; rowIndex++)
            {
                int row = dataStartRow + rowIndex;
                double xValue = xValues[rowIndex];
                dataSheet.Cells[row, 1].Value = xValue;

                for (int seriesIndex = 0; seriesIndex < lineSeries.Count; seriesIndex++)
                {
                    if (seriesPointMaps[seriesIndex].TryGetValue(xValue, out var yValue))
                    {
                        dataSheet.Cells[row, seriesIndex + 2].Value = yValue;
                    }
                }
            }

            using (var headerRange = dataSheet.Cells[headerRow, 1, headerRow, lineSeries.Count + 1])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ApplyThinBorder(headerRange);
            }

            if (xValues.Count > 0)
            {
                using var dataRange = dataSheet.Cells[headerRow + 1, 1, dataStartRow + xValues.Count - 1, lineSeries.Count + 1];
                ApplyThinBorder(dataRange);
            }

            var chart = chartSheet.Drawings.AddChart("temperatureCurveChart", eChartType.XYScatterLinesNoMarkers);
            chart.Title.Text = string.IsNullOrWhiteSpace(plotModel.Title) ? "温度曲线" : plotModel.Title;
            chart.SetPosition(3, 0, 4, 0);
            chart.SetSize(1100, 520);
            chart.Style = eChartStyle.Style4;
            chart.Legend.Position = eLegendPosition.Bottom;
            chart.YAxis.Title.Text = "温度(℃)";
            chart.XAxis.Title.Text = "时间(s)";

            var xRange = dataSheet.Cells[dataStartRow, 1, dataStartRow + xValues.Count - 1, 1];
            for (int seriesIndex = 0; seriesIndex < lineSeries.Count; seriesIndex++)
            {
                int column = seriesIndex + 2;
                var excelSeries = chart.Series.Add(
                    dataSheet.Cells[dataStartRow, column, dataStartRow + xValues.Count - 1, column],
                    xRange);
                excelSeries.Header = dataSheet.Cells[headerRow, column].Value?.ToString();
                ApplyExcelSeriesStyle(excelSeries, lineSeries[seriesIndex]);
            }

            dataSheet.Cells.AutoFitColumns();
            dataSheet.View.FreezePanes(dataStartRow, 1);
            dataSheet.Column(1).Width = Math.Max(dataSheet.Column(1).Width, 12);
            for (int column = 2; column <= lineSeries.Count + 1; column++)
            {
                dataSheet.Column(column).Width = Math.Max(dataSheet.Column(column).Width, 18);
            }

            package.SaveAs(new FileInfo(filePath));
            Log.Information("图表导出到Excel成功");
        }

        private static void ApplyExcelSeriesStyle(ExcelChartSerie excelSeries, OxyPlot.Series.LineSeries sourceSeries)
        {
            var color = Color.FromArgb(
                sourceSeries.Color.A,
                sourceSeries.Color.R,
                sourceSeries.Color.G,
                sourceSeries.Color.B);

            excelSeries.Border.Fill.Color = color;
            excelSeries.Border.Width = Math.Max(2.25, sourceSeries.StrokeThickness);
        }

        /// <summary>
        /// 导出图表到图片文件
        /// </summary>
        /// <param name="plotModel">图表模型</param>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        public void ExportChartToImage(PlotModel plotModel, string filePath, int width = 1200, int height = 600)
        {
            if (plotModel == null)
            {
                throw new ArgumentNullException(nameof(plotModel));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            Log.Information("开始导出图表到图片，文件路径: {FilePath}", filePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                filePath += ".png";
                extension = ".png";
            }

            if (extension == ".png")
            {
                PngExporter.Export(plotModel, filePath, width, height, 96);
                Log.Information("图表导出成功");
                return;
            }

            var imageFormat = extension switch
            {
                ".jpg" => ImageFormat.Jpeg,
                ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => throw new NotSupportedException($"不支持的图片格式: {extension}")
            };

            var exporter = new PngExporter
            {
                Width = width,
                Height = height,
                Resolution = 96
            };

            using var bitmap = exporter.ExportToBitmap(plotModel);
            bitmap.Save(filePath, imageFormat);

            Log.Information("图表导出成功");
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
