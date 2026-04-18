using System.Collections.Generic;
using System.Linq;
using TestServer.Models;

namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 汇总统计数据
    /// </summary>
    public class SummaryStatistics
    {
        /// <summary>
        /// 总试验数
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// 通过数
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// 不通过数
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// 平均最高温度（样品温升）
        /// </summary>
        public double AvgMaxTemp { get; set; }

        /// <summary>
        /// 平均终平衡温度
        /// </summary>
        public double AvgFinalTemp { get; set; }

        /// <summary>
        /// 平均试验时长（秒）
        /// </summary>
        public double AvgTestDuration { get; set; }

        /// <summary>
        /// 平均失重率
        /// </summary>
        public double AvgLostWeightPercent { get; set; }

        /// <summary>
        /// 最大样品温升
        /// </summary>
        public double MaxTempRise { get; set; }

        /// <summary>
        /// 最小样品温升
        /// </summary>
        public double MinTempRise { get; set; }

        /// <summary>
        /// 被排除的不完整记录数
        /// </summary>
        public int ExcludedIncompleteCount { get; set; }

        /// <summary>
        /// 被排除的不完整记录ID列表
        /// </summary>
        public List<string> ExcludedTestIds { get; set; } = new();

        /// <summary>
        /// 从试验数据列表计算汇总统计
        /// </summary>
        /// <param name="testDataList">试验数据列表</param>
        /// <returns>汇总统计结果</returns>
        public static SummaryStatistics Calculate(List<TestReportData> testDataList)
        {
            if (testDataList == null || testDataList.Count == 0)
            {
                return new SummaryStatistics();
            }

            var stats = new SummaryStatistics
            {
                TotalTests = testDataList.Count
            };

            // 计算通过/不通过数量
            // 根据 ISO 11820 标准：
            // - 样品温升 ≤ 50°C
            // - 失重率 ≤ 50%
            // - 无持续火焰（火焰持续时间 < 5秒）
            foreach (var data in testDataList)
            {
                var test = data.TestInfo;
                bool passed = test.Deltatf <= 50 && 
                              test.LostweightPer <= 50 && 
                              test.Flameduration < 5;
                
                if (passed)
                {
                    stats.PassedTests++;
                }
                else
                {
                    stats.FailedTests++;
                }
            }

            // 计算平均值
            var tests = testDataList.Select(d => d.TestInfo).ToList();
            
            stats.AvgMaxTemp = tests.Average(t => t.Deltatf);
            stats.AvgFinalTemp = tests.Average(t => t.Finaltf1);
            stats.AvgTestDuration = tests.Average(t => t.Totaltesttime);
            stats.AvgLostWeightPercent = tests.Average(t => t.LostweightPer);

            // 计算最大/最小温升
            stats.MaxTempRise = tests.Max(t => t.Deltatf);
            stats.MinTempRise = tests.Min(t => t.Deltatf);

            return stats;
        }

        /// <summary>
        /// 从试验数据列表计算汇总统计（带不完整记录过滤）
        /// </summary>
        /// <param name="testDataList">试验数据列表</param>
        /// <param name="excludedIds">输出：被排除的试验ID列表</param>
        /// <returns>汇总统计结果</returns>
        public static SummaryStatistics CalculateWithFilter(
            List<TestReportData> testDataList, 
            out List<string> excludedIds)
        {
            excludedIds = new List<string>();

            if (testDataList == null || testDataList.Count == 0)
            {
                return new SummaryStatistics();
            }

            // 过滤不完整的记录
            var completeTests = new List<TestReportData>();
            foreach (var data in testDataList)
            {
                if (IsTestComplete(data))
                {
                    completeTests.Add(data);
                }
                else
                {
                    excludedIds.Add($"{data.TestInfo.Productid}|{data.TestInfo.Testid}");
                }
            }

            var stats = Calculate(completeTests);
            stats.ExcludedIncompleteCount = excludedIds.Count;
            stats.ExcludedTestIds = excludedIds;

            return stats;
        }

        /// <summary>
        /// 检查试验记录是否完整
        /// </summary>
        /// <param name="data">试验数据</param>
        /// <returns>是否完整</returns>
        private static bool IsTestComplete(TestReportData data)
        {
            if (data?.TestInfo == null)
            {
                return false;
            }

            var test = data.TestInfo;

            // 检查必要字段是否有效
            // 试验时间必须大于0
            if (test.Totaltesttime <= 0)
            {
                return false;
            }

            // 产品ID和试验ID不能为空
            if (string.IsNullOrWhiteSpace(test.Productid) || 
                string.IsNullOrWhiteSpace(test.Testid))
            {
                return false;
            }

            return true;
        }
    }
}
