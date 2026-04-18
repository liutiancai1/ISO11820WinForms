using ISO11820WinForms.Models;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 试验数据文件路径辅助类
    /// </summary>
    public static class TestDataPathHelper
    {
        private const string DataFolderName = "data";
        private const string SensorDataFileName = "sensordata.csv";
        private const string FlameVideoFileName = "flame.avi";
        private const string ReportPathsFileName = "report-paths.json";

        public static string GetSensorDataFilePath(string productId, string testId)
        {
            return GetPreferredDataFilePath(ConfigurationHelper.GetFileStorageConfiguration(), productId, testId, SensorDataFileName);
        }

        public static string ResolveExistingSensorDataFilePath(string productId, string testId)
        {
            return ResolveExistingDataFilePath(ConfigurationHelper.GetFileStorageConfiguration(), productId, testId, SensorDataFileName);
        }

        public static string GetFlameVideoPath(string productId, string testId)
        {
            return GetPreferredDataFilePath(ConfigurationHelper.GetFileStorageConfiguration(), productId, testId, FlameVideoFileName);
        }

        public static string GetReportPathsFilePath(string productId, string testId)
        {
            return GetPreferredDataFilePath(ConfigurationHelper.GetFileStorageConfiguration(), productId, testId, ReportPathsFileName);
        }

        public static string GetPreferredDataFilePath(
            FileStorageConfiguration config,
            string productId,
            string testId,
            string fileName)
        {
            return Path.Combine(GetPreferredTestDataRoot(config), productId, testId, DataFolderName, fileName);
        }

        public static string GetLegacyDataFilePath(
            FileStorageConfiguration config,
            string productId,
            string testId,
            string fileName)
        {
            return Path.Combine(GetLegacyTestDataRoot(config), productId, testId, DataFolderName, fileName);
        }

        public static string ResolveExistingDataFilePath(
            FileStorageConfiguration config,
            string productId,
            string testId,
            string fileName)
        {
            var preferredPath = GetPreferredDataFilePath(config, productId, testId, fileName);
            if (File.Exists(preferredPath))
            {
                return preferredPath;
            }

            var legacyPath = GetLegacyDataFilePath(config, productId, testId, fileName);
            if (!string.Equals(preferredPath, legacyPath, StringComparison.OrdinalIgnoreCase) && File.Exists(legacyPath))
            {
                return legacyPath;
            }

            return preferredPath;
        }

        private static string GetPreferredTestDataRoot(FileStorageConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!string.IsNullOrWhiteSpace(config.TestDataDirectory))
            {
                return config.TestDataDirectory;
            }

            if (!string.IsNullOrWhiteSpace(config.BaseDirectory))
            {
                return Path.Combine(config.BaseDirectory, "TestData");
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "TestData");
        }

        private static string GetLegacyTestDataRoot(FileStorageConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!string.IsNullOrWhiteSpace(config.BaseDirectory))
            {
                return config.BaseDirectory;
            }

            return GetPreferredTestDataRoot(config);
        }
    }
}
