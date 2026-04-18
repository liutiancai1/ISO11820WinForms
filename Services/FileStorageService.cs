using System.Text.Json;
using System.Text;
using ISO11820WinForms.Models;
using Serilog;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 文件存储服务
    /// 负责校验记录和参数变更日志的文件存储
    /// </summary>
    public class FileStorageService
    {
        private readonly FileStorageConfiguration _config;
        private readonly ILogger _logger;
        private static readonly object _fileLock = new object();

        public FileStorageService(FileStorageConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = Log.ForContext<FileStorageService>();
        }

        #region 校验记录存储

        /// <summary>
        /// 保存校验记录
        /// </summary>
        public async Task<bool> SaveCalibrationRecordAsync(CalibrationRecord record)
        {
            try
            {
                if (!_config.EnableFileStorage)
                {
                    _logger.Warning("文件存储功能已禁用");
                    return false;
                }

                // 确保目录存在
                var apparatusDir = Path.Combine(_config.CalibrationDirectory, record.ApparatusId.ToString());
                EnsureDirectoryExists(apparatusDir);

                // 生成文件名
                var fileName = $"{record.CalibrationDate:yyyyMMdd_HHmmss}_{record.CalibrationType}.json";
                var filePath = Path.Combine(apparatusDir, fileName);

                // 序列化并保存
                var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                _logger.Information("校验记录已保存: {FilePath}", filePath);

                // 更新索引
                await UpdateCalibrationIndexAsync(record.ApparatusId, new CalibrationRecordSummary
                {
                    Id = record.Id,
                    CalibrationDate = record.CalibrationDate,
                    CalibrationType = record.CalibrationType,
                    Operator = record.Operator,
                    PassedCriteria = record.PassedCriteria,
                    FilePath = filePath
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存校验记录失败");
                return false;
            }
        }

        /// <summary>
        /// 加载校验记录
        /// </summary>
        public async Task<CalibrationRecord?> LoadCalibrationRecordAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Warning("校验记录文件不存在: {FilePath}", filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var record = JsonSerializer.Deserialize<CalibrationRecord>(json);
                
                _logger.Information("校验记录已加载: {FilePath}", filePath);
                return record;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载校验记录失败: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// 更新校验索引
        /// </summary>
        private async Task UpdateCalibrationIndexAsync(int apparatusId, CalibrationRecordSummary summary)
        {
            try
            {
                var apparatusDir = Path.Combine(_config.CalibrationDirectory, apparatusId.ToString());
                var indexPath = Path.Combine(apparatusDir, _config.IndexFileName);

                CalibrationIndex index;
                
                lock (_fileLock)
                {
                    // 读取现有索引
                    if (File.Exists(indexPath))
                    {
                        var json = File.ReadAllText(indexPath, Encoding.UTF8);
                        index = JsonSerializer.Deserialize<CalibrationIndex>(json) ?? new CalibrationIndex();
                    }
                    else
                    {
                        index = new CalibrationIndex();
                    }

                    // 添加或更新记录
                    var existing = index.Records.FirstOrDefault(r => r.Id == summary.Id);
                    if (existing != null)
                    {
                        index.Records.Remove(existing);
                    }
                    index.Records.Add(summary);

                    // 按日期降序排序
                    index.Records = index.Records.OrderByDescending(r => r.CalibrationDate).ToList();

                    // 保存索引
                    var updatedJson = JsonSerializer.Serialize(index, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    File.WriteAllText(indexPath, updatedJson, Encoding.UTF8);
                }

                _logger.Information("校验索引已更新: {IndexPath}", indexPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "更新校验索引失败");
            }
        }

        /// <summary>
        /// 获取校验索引
        /// </summary>
        public async Task<CalibrationIndex> GetCalibrationIndexAsync(int apparatusId)
        {
            try
            {
                var apparatusDir = Path.Combine(_config.CalibrationDirectory, apparatusId.ToString());
                var indexPath = Path.Combine(apparatusDir, _config.IndexFileName);

                if (!File.Exists(indexPath))
                {
                    return new CalibrationIndex();
                }

                var json = await File.ReadAllTextAsync(indexPath, Encoding.UTF8);
                return JsonSerializer.Deserialize<CalibrationIndex>(json) ?? new CalibrationIndex();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "读取校验索引失败");
                return new CalibrationIndex();
            }
        }

        #endregion

        #region 参数变更日志存储

        /// <summary>
        /// 追加参数变更日志
        /// </summary>
        public async Task<bool> AppendParameterChangeLogAsync(ParameterChangeLog log)
        {
            try
            {
                if (!_config.EnableFileStorage)
                {
                    _logger.Warning("文件存储功能已禁用");
                    return false;
                }

                // 确保目录存在
                EnsureDirectoryExists(_config.AuditLogDirectory);

                // 生成文件名（按年份分文件）
                var fileName = $"ParameterChanges_{log.ChangeTime.Year}.csv";
                var filePath = Path.Combine(_config.AuditLogDirectory, fileName);

                // 检查文件是否存在，如果不存在则创建并写入表头
                bool fileExists = File.Exists(filePath);

                lock (_fileLock)
                {
                    using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = !fileExists
                    }))
                    {
                        if (!fileExists)
                        {
                            csv.WriteHeader<ParameterChangeLog>();
                            csv.NextRecord();
                        }

                        csv.WriteRecord(log);
                        csv.NextRecord();
                    }
                }

                _logger.Information("参数变更日志已追加: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "追加参数变更日志失败");
                return false;
            }
        }

        /// <summary>
        /// 读取参数变更日志
        /// </summary>
        public async Task<List<ParameterChangeLog>> ReadParameterChangeLogsAsync(int year)
        {
            try
            {
                var fileName = $"ParameterChanges_{year}.csv";
                var filePath = Path.Combine(_config.AuditLogDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    _logger.Information("参数变更日志文件不存在: {FilePath}", filePath);
                    return new List<ParameterChangeLog>();
                }

                var logs = new List<ParameterChangeLog>();

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    logs = csv.GetRecords<ParameterChangeLog>().ToList();
                }

                _logger.Information("已读取 {Count} 条参数变更日志", logs.Count);
                return logs;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "读取参数变更日志失败");
                return new List<ParameterChangeLog>();
            }
        }

        /// <summary>
        /// 读取指定日期范围的参数变更日志
        /// </summary>
        public async Task<List<ParameterChangeLog>> ReadParameterChangeLogsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allLogs = new List<ParameterChangeLog>();

                // 遍历日期范围内的所有年份
                for (int year = startDate.Year; year <= endDate.Year; year++)
                {
                    var yearLogs = await ReadParameterChangeLogsAsync(year);
                    allLogs.AddRange(yearLogs);
                }

                // 筛选日期范围
                var filteredLogs = allLogs
                    .Where(log => log.ChangeTime >= startDate && log.ChangeTime <= endDate)
                    .OrderByDescending(log => log.ChangeTime)
                    .ToList();

                _logger.Information("已读取日期范围 {StartDate} - {EndDate} 的 {Count} 条参数变更日志", 
                    startDate, endDate, filteredLogs.Count);

                return filteredLogs;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "读取参数变更日志失败");
                return new List<ParameterChangeLog>();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Information("已创建目录: {Directory}", directory);
            }
        }

        #endregion
    }
}
