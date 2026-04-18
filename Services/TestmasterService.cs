using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestServer.Models;
using ISO11820WinForms.Models;
using ISO11820WinForms.Global;
using ISO11820WinForms.Utilities;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 试验主数据服务层 - 处理新建试验的业务逻辑
    /// </summary>
    public class TestmasterService
    {
        /// <summary>
        /// 创建新的试验记录（完全参考Web版实现）
        /// 性能优化：使用异步方法提高响应性
        /// </summary>
        /// <param name="productData">产品信息</param>
        /// <param name="testData">试验信息</param>
        /// <returns>返回(成功标志, 消息, 样品编号, 试验编号)</returns>
        public async Task<(bool success, string message, string productId, string testId)> CreateNewTestAsync(
            Productmaster productData,
            Testmaster testData)
        {
            try
            {
                // 使用数据库重试机制执行操作
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        // 步顷1：先关联到TestMaster内存缓存（参考Web版第316行）
                        AssociateToTestMaster(productData, testData);

                        // 步骤2：检查产品是否首次创建（参考Web版第320-324行）
                        // 性能优化：使用异步方法
                        if (!await context.Productmasters.AnyAsync(prod => prod.Productid == productData.Productid))
                        {
                            context.Productmasters.Add(productData);
                            await context.SaveChangesAsync();
                        }

                        // 步骤3：检查试验编号是否重复（参考Web版第326行）
                        // 性能优化：使用异步方法
                        if (!await context.Testmasters.AnyAsync(test => test.Productid == productData.Productid && test.Testid == testData.Testid))
                        {
                            // 试验编号没有重复，添加试验记录到数据库
                            context.Testmasters.Add(testData);
                            await context.SaveChangesAsync();
                            
                            // 创建成功
                            string successMsg = $"创建新试验成功。样品编号: [ {testData.Productid} ], 样品标识: [ {testData.Testid} ]";
                            return (true, successMsg, testData.Productid, testData.Testid);
                        }
                        else
                        {
                            // 试验编号重复
                            return (false, $"样品标识号 [ {testData.Testid} ] 重复，请检查输入。", "", "");
                        }
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                return (false, $"数据库操作失败: {ex.Message}\n\n请检查数据库连接。", "", "");
            }
        }

        /// <summary>
        /// 创建新的试验记录（同步版本，保持向后兼容）
        /// </summary>
        [Obsolete("请使用 CreateNewTestAsync 异步方法以获得更好的性能")]
        public (bool success, string message, string productId, string testId) CreateNewTest(
            Productmaster productData,
            Testmaster testData)
        {
            return CreateNewTestAsync(productData, testData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 验证试验数据输入（业务规则验证）
        /// </summary>
        public (bool isValid, string errorMessage) ValidateTestData(
            string ambTemp, string ambHumi,
            string productId, string testId, string productName,
            string height, string diameter, string weight,
            string operatorName)
        {
            // 1. 验证环境信息
            if (!float.TryParse(ambTemp, out float temp))
            {
                return (false, "环境温度格式不正确。\n请输入有效的数字（例如：25.5）。");
            }

            if (temp < -50 || temp > 100)
            {
                return (false, "环境温度超出合理范围。\n请输入 -50℃ 到 100℃ 之间的温度值。");
            }

            if (!float.TryParse(ambHumi, out float humi))
            {
                return (false, "环境湿度格式不正确。\n请输入有效的数字（例如：60）。");
            }

            if (humi < 0 || humi > 100)
            {
                return (false, "环境湿度超出合理范围。\n请输入 0% 到 100% 之间的湿度值。");
            }

            // 2. 验证试样信息
            if (string.IsNullOrWhiteSpace(productId))
            {
                return (false, "试样编号不能为空。\n请输入试样编号以标识本次试验的样品。");
            }

            if (string.IsNullOrWhiteSpace(testId))
            {
                return (false, "样品标识号不能为空。\n请输入唯一的样品标识号。");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                return (false, "产品名称不能为空。\n请输入产品的完整名称。");
            }

            // 3. 验证尺寸和质量
            if (!float.TryParse(height, out float h))
            {
                return (false, "样品高度格式不正确。\n请输入有效的数字（例如：50）。");
            }

            if (h <= 0 || h > 1000)
            {
                return (false, "样品高度超出合理范围。\n请输入 0 到 1000mm 之间的高度值。");
            }

            if (!float.TryParse(diameter, out float d))
            {
                return (false, "样品直径格式不正确。\n请输入有效的数字（例如：45）。");
            }

            if (d <= 0 || d > 1000)
            {
                return (false, "样品直径超出合理范围。\n请输入 0 到 1000mm 之间的直径值。");
            }

            if (!float.TryParse(weight, out float w))
            {
                return (false, "样品质量格式不正确。\n请输入有效的数字（例如：100.5）。");
            }

            if (w <= 0 || w > 10000)
            {
                return (false, "样品质量超出合理范围。\n请输入 0 到 10000g 之间的质量值。");
            }

            // 4. 验证试验人员
            if (string.IsNullOrWhiteSpace(operatorName))
            {
                return (false, "试验人员不能为空。\n请输入操作员姓名。");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 按日期范围查询试验记录
        /// 性能优化：使用异步方法和索引优化查询
        /// </summary>
        /// <param name="startDate">开始日期（包含）</param>
        /// <param name="endDate">结束日期（包含）</param>
        /// <returns>符合条件的试验记录列表</returns>
        public async Task<List<Testmaster>> QueryTestsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        // 查询日期范围内的试验记录（包含起始和结束日期）
                        // 性能优化：使用索引字段 Testdate 进行查询
                        var tests = await context.Testmasters
                            .Include(t => t.Product)
                            .Where(t => t.Testdate >= startDate && t.Testdate <= endDate)
                            .OrderByDescending(t => t.Testdate)
                            .ThenBy(t => t.Productid)
                            .ToListAsync();

                        return tests;
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"查询试验记录失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 按日期范围查询试验记录（同步版本，保持向后兼容）
        /// </summary>
        [Obsolete("请使用 QueryTestsByDateRangeAsync 异步方法以获得更好的性能")]
        public List<Testmaster> QueryTestsByDateRange(DateTime startDate, DateTime endDate)
        {
            return QueryTestsByDateRangeAsync(startDate, endDate).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 按样品编号查询试验记录（返回包含产品信息的DTO）
        /// 直接查询 testmaster 表并关联 productmaster 表
        /// </summary>
        /// <param name="productId">样品编号</param>
        /// <returns>符合条件的试验信息列表</returns>
        public async Task<List<TestInfoDto>> QueryByProductIdAsync(string productId)
        {
            try
            {
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        var tests = await context.Testmasters
                            .Include(t => t.Product)
                            .Where(t => t.Productid == productId)
                            .OrderByDescending(t => t.Testdate)
                            .ToListAsync();

                        return tests.Select(TestInfoDto.FromTestmaster).ToList();
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"查询试验记录失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 按日期范围查询试验记录（返回包含产品信息的DTO）
        /// 直接查询 testmaster 表并关联 productmaster 表
        /// 符合设计文档接口定义
        /// </summary>
        /// <param name="from">开始日期（包含）</param>
        /// <param name="to">结束日期（包含）</param>
        /// <returns>符合条件的试验信息列表</returns>
        public Task<List<TestInfoDto>> QueryByDateRangeAsync(DateTime from, DateTime to)
        {
            return QueryTestInfoByDateRangeAsync(from, to);
        }

        /// <summary>
        /// 按日期范围查询试验记录（返回包含产品信息的DTO）
        /// 直接查询 testmaster 表并关联 productmaster 表
        /// </summary>
        /// <param name="startDate">开始日期（包含）</param>
        /// <param name="endDate">结束日期（包含）</param>
        /// <returns>符合条件的试验信息列表</returns>
        public async Task<List<TestInfoDto>> QueryTestInfoByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        var tests = await context.Testmasters
                            .Include(t => t.Product)
                            .Where(t => t.Testdate >= startDate && t.Testdate <= endDate)
                            .OrderByDescending(t => t.Testdate)
                            .ThenBy(t => t.Productid)
                            .ToListAsync();

                        return tests.Select(TestInfoDto.FromTestmaster).ToList();
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"查询试验记录失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 组合查询试验记录（返回包含产品信息的DTO）
        /// 直接查询 testmaster 表并关联 productmaster 表
        /// </summary>
        /// <param name="productId">样品编号（可选）</param>
        /// <param name="startDate">开始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
        /// <returns>符合条件的试验信息列表</returns>
        public async Task<List<TestInfoDto>> QueryAsync(string? productId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        var query = context.Testmasters
                            .Include(t => t.Product)
                            .AsQueryable();

                        // 按样品编号过滤
                        if (!string.IsNullOrWhiteSpace(productId))
                        {
                            query = query.Where(t => t.Productid == productId);
                        }

                        // 按日期范围过滤
                        if (startDate.HasValue)
                        {
                            query = query.Where(t => t.Testdate >= startDate.Value);
                        }
                        if (endDate.HasValue)
                        {
                            query = query.Where(t => t.Testdate <= endDate.Value);
                        }

                        var tests = await query
                            .OrderByDescending(t => t.Testdate)
                            .ThenBy(t => t.Productid)
                            .ToListAsync();

                        return tests.Select(TestInfoDto.FromTestmaster).ToList();
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"查询试验记录失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取试验详情
        /// </summary>
        /// <param name="productId">样品编号</param>
        /// <param name="testId">试验编号</param>
        /// <returns>试验详情</returns>
        public async Task<Testmaster?> GetTestDetailAsync(string productId, string testId)
        {
            try
            {
                return await DatabaseHelper.ExecuteWithRetryAsync(async () =>
                {
                    using (var context = new ISO11820DbContext())
                    {
                        return await context.Testmasters
                            .Include(t => t.Product)
                            .FirstOrDefaultAsync(t => t.Productid == productId && t.Testid == testId);
                    }
                }, maxRetries: 3, retryDelayMs: 1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"获取试验详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 关联产品和试验数据到TestMaster（参考Web版业务逻辑）
        /// </summary>
        private void AssociateToTestMaster(Productmaster productData, Testmaster testData)
        {
            if (SystemContext.Current.Masters.DictTestMaster.ContainsKey(0))
            {
                var master = SystemContext.Current.Masters.DictTestMaster[0];
                master.SetProductData(productData);
                master.SetTestData(testData);
            }
        }
    }
}