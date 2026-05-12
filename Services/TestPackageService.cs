using ISO11820WinForms.Utilities;

namespace ISO11820WinForms.Services
{
    public class TestPackageService
    {
        private const string PackageRootFolderName = "TestPackages";

        private readonly string _packageRoot;

        public TestPackageService(string? packageRoot = null)
        {
            _packageRoot = string.IsNullOrWhiteSpace(packageRoot)
                ? Path.Combine(GetDefaultPackageRoot(), PackageRootFolderName)
                : packageRoot;
        }

        public string CreatePackage(
            string productId,
            string testId,
            string? excelReportPath,
            string? pdfReportPath,
            string? sensorDataPath,
            DateTime? createdAt = null,
            string? temperatureCurveImagePath = null)
        {
            var packageTime = createdAt ?? DateTime.Now;
            var safeProductId = SanitizePathPart(productId);
            var safeTestId = SanitizePathPart(testId);
            var packagePrefix = $"{safeProductId}_{safeTestId}";
            var dayFolder = Path.Combine(_packageRoot, packageTime.ToString("yyyyMMdd"));
            var packageName = $"{packagePrefix}_{packageTime:yyyyMMdd_HHmmss_fff}";
            var packageDirectory = GetUniqueDirectory(dayFolder, packageName);

            Directory.CreateDirectory(packageDirectory);
            CopyIfExists(excelReportPath, Path.Combine(packageDirectory, $"{packagePrefix}_report.xlsx"));
            CopyIfExists(pdfReportPath, Path.Combine(packageDirectory, $"{packagePrefix}_report.pdf"));
            CopyIfExists(sensorDataPath, Path.Combine(packageDirectory, $"{packagePrefix}_sensordata.csv"));
            CopyIfExists(temperatureCurveImagePath, Path.Combine(packageDirectory, $"{packagePrefix}_temperature_curve.png"));

            return packageDirectory;
        }

        private static string GetDefaultPackageRoot()
        {
            var outputDirectory = ConfigurationHelper.GetReportConfiguration().OutputDirectory;
            return string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports")
                : outputDirectory;
        }

        private static string GetUniqueDirectory(string parentDirectory, string packageName)
        {
            Directory.CreateDirectory(parentDirectory);

            var candidate = Path.Combine(parentDirectory, packageName);
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }

            for (var index = 2; ; index++)
            {
                candidate = Path.Combine(parentDirectory, $"{packageName}_{index:00}");
                if (!Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        private static void CopyIfExists(string? sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        private static string SanitizePathPart(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(value
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
        }
    }
}
