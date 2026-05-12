using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using TestServer.Models;

namespace ISO11820WinForms.Services
{
    public static class TemperatureCurveExportService
    {
        private const string DataSheetName = "温度数据";
        private const string CurveSheetName = "温度曲线";

        public static void AddTemperatureDataWorksheet(ExcelPackage package, IReadOnlyList<SensorDataPoint> sensorData)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (sensorData == null || sensorData.Count == 0)
            {
                return;
            }

            var existingSheet = package.Workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == DataSheetName);
            if (existingSheet != null)
            {
                package.Workbook.Worksheets.Delete(existingSheet);
            }

            var sheet = package.Workbook.Worksheets.Add(DataSheetName);
            sheet.View.ShowGridLines = false;
            sheet.Cells["A1:E1"].Merge = true;
            sheet.Cells["A1"].Value = "原始温度数据";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            WriteTemperatureDataTable(sheet, sensorData);
        }

        public static void AddTemperatureCurveWorksheet(ExcelPackage package, IReadOnlyList<SensorDataPoint> sensorData)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (sensorData == null || sensorData.Count == 0)
            {
                return;
            }

            var existingSheet = package.Workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == CurveSheetName);
            if (existingSheet != null)
            {
                package.Workbook.Worksheets.Delete(existingSheet);
            }

            var sheet = package.Workbook.Worksheets.Add(CurveSheetName);
            sheet.View.ShowGridLines = false;
            sheet.Cells["A1:J1"].Merge = true;
            sheet.Cells["A1"].Value = "温度曲线";
            sheet.Cells["A1"].Style.Font.Size = 18;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            WriteTemperatureDataTable(sheet, sensorData);

            var chart = sheet.Drawings.AddChart("temperatureCurveChart", eChartType.XYScatterLinesNoMarkers);
            chart.Title.Text = "温度曲线";
            chart.SetPosition(1, 0, 6, 0);
            chart.SetSize(1100, 520);
            chart.Style = eChartStyle.Style4;
            chart.Legend.Position = eLegendPosition.Bottom;
            chart.XAxis.Title.Text = "时间(s)";
            chart.YAxis.Title.Text = "温度(℃)";

            const int dataStartRow = 5;
            var dataEndRow = dataStartRow + sensorData.Count - 1;
            var xRange = sheet.Cells[dataStartRow, 1, dataEndRow, 1];
            AddSeries(chart, sheet, "炉内温度1", dataStartRow, dataEndRow, 2, xRange, Color.FromArgb(37, 99, 235));
            AddSeries(chart, sheet, "炉内温度2", dataStartRow, dataEndRow, 3, xRange, Color.FromArgb(220, 38, 38));
            AddSeries(chart, sheet, "表面温度", dataStartRow, dataEndRow, 4, xRange, Color.FromArgb(22, 163, 74));
            AddSeries(chart, sheet, "中心温度", dataStartRow, dataEndRow, 5, xRange, Color.FromArgb(234, 179, 8));

            sheet.View.FreezePanes(dataStartRow, 1);
        }

        public static void ExportTemperatureCurveImage(
            IReadOnlyList<SensorDataPoint> sensorData,
            string filePath,
            int width = 1200,
            int height = 600)
        {
            if (sensorData == null || sensorData.Count == 0)
            {
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var model = CreatePlotModel(sensorData);
            PngExporter.Export(model, filePath, width, height, 96);
        }

        private static void AddSeries(
            ExcelChart chart,
            ExcelWorksheet sheet,
            string title,
            int startRow,
            int endRow,
            int column,
            ExcelRange xRange,
            Color color)
        {
            var series = chart.Series.Add(sheet.Cells[startRow, column, endRow, column], xRange);
            series.Header = title;
            series.Border.Fill.Color = color;
            series.Border.Width = 2.25;
        }

        private static void WriteTemperatureDataTable(ExcelWorksheet sheet, IReadOnlyList<SensorDataPoint> sensorData)
        {
            const int headerRow = 4;
            sheet.Cells[headerRow, 1].Value = "时间(s)";
            sheet.Cells[headerRow, 2].Value = "炉内温度1";
            sheet.Cells[headerRow, 3].Value = "炉内温度2";
            sheet.Cells[headerRow, 4].Value = "表面温度";
            sheet.Cells[headerRow, 5].Value = "中心温度";

            using (var headerRange = sheet.Cells[headerRow, 1, headerRow, 5])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 118, 110));
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            var dataStartRow = headerRow + 1;
            for (var index = 0; index < sensorData.Count; index++)
            {
                var row = dataStartRow + index;
                var data = sensorData[index];
                sheet.Cells[row, 1].Value = data.TimeStamp;
                sheet.Cells[row, 2].Value = data.Tf1;
                sheet.Cells[row, 3].Value = data.Tf2;
                sheet.Cells[row, 4].Value = data.Ts;
                sheet.Cells[row, 5].Value = data.Tc;
            }

            var dataEndRow = dataStartRow + sensorData.Count - 1;
            sheet.Cells[headerRow, 1, dataEndRow, 5].AutoFitColumns();
            sheet.View.FreezePanes(dataStartRow, 1);
        }

        private static PlotModel CreatePlotModel(IReadOnlyList<SensorDataPoint> sensorData)
        {
            var model = new PlotModel
            {
                Title = "温度曲线",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Gray
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间(s)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度(℃)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            var tf1 = CreateLineSeries("炉内温度1", OxyColor.FromRgb(37, 99, 235));
            var tf2 = CreateLineSeries("炉内温度2", OxyColor.FromRgb(220, 38, 38));
            var ts = CreateLineSeries("表面温度", OxyColor.FromRgb(22, 163, 74));
            var tc = CreateLineSeries("中心温度", OxyColor.FromRgb(234, 179, 8));

            foreach (var data in sensorData)
            {
                tf1.Points.Add(new DataPoint(data.TimeStamp, data.Tf1));
                tf2.Points.Add(new DataPoint(data.TimeStamp, data.Tf2));
                ts.Points.Add(new DataPoint(data.TimeStamp, data.Ts));
                tc.Points.Add(new DataPoint(data.TimeStamp, data.Tc));
            }

            model.Series.Add(tf1);
            model.Series.Add(tf2);
            model.Series.Add(ts);
            model.Series.Add(tc);
            return model;
        }

        private static LineSeries CreateLineSeries(string title, OxyColor color)
        {
            return new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 2
            };
        }
    }
}
