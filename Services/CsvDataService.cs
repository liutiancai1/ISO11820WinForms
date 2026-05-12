using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ISO11820WinForms.Core;
using ISO11820WinForms.Utilities;
using Serilog;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// CSV数据序列化服务
    /// 负责试验传感器数据的CSV序列化和反序列化
    /// Requirements: 8.1, 8.2, 8.3, 8.5
    /// </summary>
    public class CsvDataService
    {
        private readonly ILogger _logger;

        public CsvDataService()
        {
            _logger = Log.ForContext<CsvDataService>();
        }

        /// <summary>
        /// 将传感器数据序列化为CSV文件
        /// Requirement 8.1: 试验记录完成时将传感器数据序列化为CSV文件
        /// Requirement 8.3: 包含Timer、Temp1、Temp2、TempSuf和TempCen列
        /// Requirement 8.5: 使用UTF-8编码和标准CSV格式
        /// </summary>
        /// <param name="data">传感器数据列表</param>
        /// <param name="filePath">输出文件路径</param>
        /// <returns>是否成功序列化</returns>
        public async Task<bool> SerializeAsync(List<SensorDataCatch> data, string filePath)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Requirement 8.5: 使用UTF-8编码
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Encoding = Encoding.UTF8
                };

                await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                await using var csv = new CsvWriter(writer, config);
                
                // Requirement 8.3: 写入包含Timer、Temp1、Temp2、TempSuf和TempCen列的数据
                await csv.WriteRecordsAsync(data);

                _logger.Information("传感器数据已序列化到CSV文件: {FilePath}, 记录数: {Count}", 
                    filePath, data.Count);

                return true;
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "CSV文件写入失败: {FilePath}", filePath);
                throw new FileOperationException("保存传感器数据失败", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "序列化传感器数据时发生异常: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 从CSV文件反序列化传感器数据
        /// Requirement 8.2: 生成试验报告时反序列化CSV数据
        /// Requirement 8.4: 验证CSV格式并优雅地处理解析错误
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <returns>传感器数据列表</returns>
        public async Task<List<SensorDataCatch>> DeserializeAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                _logger.Warning("CSV文件不存在: {FilePath}", filePath);
                throw new FileNotFoundException("CSV文件不存在", filePath);
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    // Requirement 8.4: 优雅地处理解析错误
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    BadDataFound = context =>
                    {
                        _logger.Warning("CSV解析发现无效数据: Row={Row}, Field={Field}", 
                            context.Context.Parser?.Row, context.Field);
                    }
                };

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                var records = new List<SensorDataCatch>();
                
                await foreach (var record in csv.GetRecordsAsync<SensorDataCatch>())
                {
                    records.Add(record);
                }

                _logger.Information("传感器数据已从CSV文件反序列化: {FilePath}, 记录数: {Count}", 
                    filePath, records.Count);

                return records;
            }
            catch (CsvHelperException ex)
            {
                _logger.Error(ex, "CSV格式解析错误: {FilePath}", filePath);
                throw new CsvParseException("CSV文件格式无效", ex);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "CSV文件读取失败: {FilePath}", filePath);
                throw new FileOperationException("读取传感器数据失败", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "反序列化传感器数据时发生异常: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 验证CSV文件格式是否有效
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <returns>是否有效</returns>
        public async Task<bool> ValidateCsvFormatAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                // 读取表头
                await csv.ReadAsync();
                csv.ReadHeader();

                // 验证必需的列是否存在
                var requiredHeaders = new[] { "Timer", "Temp1", "Temp2", "TempSuf", "TempCen" };
                var actualHeaders = csv.HeaderRecord;

                if (actualHeaders == null)
                {
                    return false;
                }

                foreach (var header in requiredHeaders)
                {
                    if (!actualHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.Warning("CSV文件缺少必需的列: {Header}, FilePath={FilePath}", 
                            header, filePath);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证CSV格式时发生异常: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 获取试验数据文件的标准路径
        /// </summary>
        /// <param name="productId">产品编号</param>
        /// <param name="testId">试验编号</param>
        /// <returns>CSV文件路径</returns>
        public static string GetSensorDataFilePath(string productId, string testId)
        {
            return TestDataPathHelper.GetSensorDataFilePath(productId, testId);
        }
    }

    /// <summary>
    /// 文件操作异常
    /// </summary>
    public class FileOperationException : Exception
    {
        public FileOperationException(string message) : base(message) { }
        public FileOperationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// CSV解析异常
    /// </summary>
    public class CsvParseException : Exception
    {
        public CsvParseException(string message) : base(message) { }
        public CsvParseException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
