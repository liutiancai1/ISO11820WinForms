using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ISO11820WinForms.Models;
using Serilog;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 数据库连接辅助类
    /// 提供数据库连接检测和重试机制
    /// </summary>
    public static class DatabaseHelper
    {
        private const int DEFAULT_MAX_RETRIES = 3;
        private const int DEFAULT_RETRY_DELAY_MS = 1000;

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        public static bool TestConnection()
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    // 尝试打开连接
                    return context.Database.CanConnect();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 异步测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    // 尝试打开连接
                    return await context.Database.CanConnectAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 获取数据库连接状态信息
        /// </summary>
        /// <returns>连接状态信息</returns>
        public static (bool isConnected, string message) GetConnectionStatus()
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    if (context.Database.CanConnect())
                    {
                        var connectionString = context.Database.GetConnectionString();
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        return (true, $"已连接到数据库: {builder.DataSource}\\{builder.InitialCatalog}");
                    }
                    else
                    {
                        return (false, "无法连接到数据库");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取数据库连接状态失败");
                return (false, $"数据库连接错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 带重试机制执行数据库操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        /// <returns>操作结果</returns>
        public static T ExecuteWithRetry<T>(Func<T> operation, int maxRetries = DEFAULT_MAX_RETRIES, int retryDelayMs = DEFAULT_RETRY_DELAY_MS)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount <= maxRetries)
            {
                try
                {
                    return operation();
                }
                catch (SqlException sqlEx)
                {
                    lastException = sqlEx;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        Log.Error(sqlEx, "数据库操作失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                        throw;
                    }

                    Log.Warning(sqlEx, "数据库操作失败，正在重试 ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
                    Thread.Sleep(retryDelayMs);
                }
                catch (DbUpdateException dbEx)
                {
                    lastException = dbEx;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        Log.Error(dbEx, "数据库更新失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                        throw;
                    }

                    Log.Warning(dbEx, "数据库更新失败，正在重试 ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
                    Thread.Sleep(retryDelayMs);
                }
            }

            // 如果所有重试都失败，抛出最后一个异常
            throw lastException ?? new Exception("数据库操作失败");
        }

        /// <summary>
        /// 带重试机制异步执行数据库操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = DEFAULT_MAX_RETRIES, int retryDelayMs = DEFAULT_RETRY_DELAY_MS)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount <= maxRetries)
            {
                try
                {
                    return await operation();
                }
                catch (SqlException sqlEx)
                {
                    lastException = sqlEx;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        Log.Error(sqlEx, "数据库操作失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                        throw;
                    }

                    Log.Warning(sqlEx, "数据库操作失败，正在重试 ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
                    await Task.Delay(retryDelayMs);
                }
                catch (DbUpdateException dbEx)
                {
                    lastException = dbEx;
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        Log.Error(dbEx, "数据库更新失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                        throw;
                    }

                    Log.Warning(dbEx, "数据库更新失败，正在重试 ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
                    await Task.Delay(retryDelayMs);
                }
            }

            // 如果所有重试都失败，抛出最后一个异常
            throw lastException ?? new Exception("数据库操作失败");
        }

        /// <summary>
        /// 带重试机制执行无返回值的数据库操作
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        public static void ExecuteWithRetry(Action operation, int maxRetries = DEFAULT_MAX_RETRIES, int retryDelayMs = DEFAULT_RETRY_DELAY_MS)
        {
            ExecuteWithRetry<object>(() =>
            {
                operation();
                return null;
            }, maxRetries, retryDelayMs);
        }

        /// <summary>
        /// 带重试机制异步执行无返回值的数据库操作
        /// </summary>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelayMs">重试延迟（毫秒）</param>
        public static async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = DEFAULT_MAX_RETRIES, int retryDelayMs = DEFAULT_RETRY_DELAY_MS)
        {
            await ExecuteWithRetryAsync<object>(async () =>
            {
                await operation();
                return null;
            }, maxRetries, retryDelayMs);
        }

        /// <summary>
        /// 检查数据库连接并在失败时提示用户
        /// </summary>
        /// <param name="showMessageOnSuccess">成功时是否显示消息</param>
        /// <returns>连接是否成功</returns>
        public static bool CheckConnectionWithPrompt(bool showMessageOnSuccess = false)
        {
            var (isConnected, message) = GetConnectionStatus();

            if (isConnected)
            {
                Log.Information("数据库连接检查成功: {Message}", message);
                
                if (showMessageOnSuccess)
                {
                    ExceptionHandler.ShowSuccess(message, "数据库连接");
                }
                
                return true;
            }
            else
            {
                Log.Error("数据库连接检查失败: {Message}", message);
                
                ExceptionHandler.ShowWarning(
                    $"{message}\n\n请检查：\n" +
                    "• SQL Server 服务是否正在运行\n" +
                    "• 数据库连接字符串是否正确\n" +
                    "• 网络连接是否正常\n" +
                    "• 防火墙设置是否允许连接",
                    "数据库连接失败");
                
                return false;
            }
        }

        /// <summary>
        /// 异步检查数据库连接并在失败时提示用户
        /// </summary>
        /// <param name="showMessageOnSuccess">成功时是否显示消息</param>
        /// <returns>连接是否成功</returns>
        public static async Task<bool> CheckConnectionWithPromptAsync(bool showMessageOnSuccess = false)
        {
            bool isConnected = await TestConnectionAsync();

            if (isConnected)
            {
                var (_, message) = GetConnectionStatus();
                Log.Information("数据库连接检查成功: {Message}", message);
                
                if (showMessageOnSuccess)
                {
                    ExceptionHandler.ShowSuccess(message, "数据库连接");
                }
                
                return true;
            }
            else
            {
                Log.Error("数据库连接检查失败");
                
                ExceptionHandler.ShowWarning(
                    "无法连接到数据库。\n\n请检查：\n" +
                    "• SQL Server 服务是否正在运行\n" +
                    "• 数据库连接字符串是否正确\n" +
                    "• 网络连接是否正常\n" +
                    "• 防火墙设置是否允许连接",
                    "数据库连接失败");
                
                return false;
            }
        }

        /// <summary>
        /// 确保数据库已创建
        /// </summary>
        /// <returns>数据库是否已创建或成功创建</returns>
        public static bool EnsureDatabaseCreated()
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    bool created = context.Database.EnsureCreated();
                    
                    if (created)
                    {
                        Log.Information("数据库已创建");
                    }
                    else
                    {
                        Log.Information("数据库已存在");
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "确保数据库创建失败");
                return false;
            }
        }

        /// <summary>
        /// 异步确保数据库已创建
        /// </summary>
        /// <returns>数据库是否已创建或成功创建</returns>
        public static async Task<bool> EnsureDatabaseCreatedAsync()
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    bool created = await context.Database.EnsureCreatedAsync();
                    
                    if (created)
                    {
                        Log.Information("数据库已创建");
                    }
                    else
                    {
                        Log.Information("数据库已存在");
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "确保数据库创建失败");
                return false;
            }
        }
    }
}
