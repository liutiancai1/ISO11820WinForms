using ISO11820WinForms.Models;
using ISO11820WinForms.Utilities;
using Xunit;

namespace ISO11820WinForms.Tests.Utilities;

public class TestDataPathHelperTests
{
    [Fact]
    public void GetPreferredDataFilePath_UsesConfiguredTestDataDirectory()
    {
        var config = new FileStorageConfiguration
        {
            BaseDirectory = @"D:\ISO11820",
            TestDataDirectory = @"D:\ISO11820\TestData"
        };

        var path = TestDataPathHelper.GetPreferredDataFilePath(config, "P001", "T001", "sensordata.csv");

        Assert.Equal(
            Path.Combine(@"D:\ISO11820\TestData", "P001", "T001", "data", "sensordata.csv"),
            path);
    }

    [Fact]
    public void ResolveExistingDataFilePath_FallsBackToLegacyPath_WhenPreferredPathDoesNotExist()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var config = new FileStorageConfiguration
            {
                BaseDirectory = Path.Combine(root, "legacy"),
                TestDataDirectory = Path.Combine(root, "preferred")
            };

            var legacyPath = TestDataPathHelper.GetLegacyDataFilePath(config, "P001", "T001", "sensordata.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
            File.WriteAllText(legacyPath, "legacy");

            var resolvedPath = TestDataPathHelper.ResolveExistingDataFilePath(config, "P001", "T001", "sensordata.csv");

            Assert.Equal(legacyPath, resolvedPath);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
