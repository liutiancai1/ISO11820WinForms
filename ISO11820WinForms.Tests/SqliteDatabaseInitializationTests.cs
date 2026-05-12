using System.Reflection;
using ISO11820WinForms.Models;
using ISO11820WinForms.Services;
using ISO11820WinForms.Utilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ISO11820WinForms.Tests;

public class SqliteDatabaseInitializationTests : IDisposable
{
    private readonly string _tempDirectory;

    public SqliteDatabaseInitializationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:SqlitePath"] = Path.Combine(_tempDirectory, "ISO11820.Tests.db")
        });
    }

    [Fact]
    public void EnsureDatabaseCreated_SeedsDefaultLoginHardwareAndSensors()
    {
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());

        using var context = new ISO11820DbContext();
        Assert.True(context.Database.CanConnect());
        Assert.True(context.Apparatuses.Any(a => a.Apparatusid == 0));
        Assert.True(context.Sensors.Count() >= 17);

        var operatorService = new OperatorService();
        Assert.NotNull(operatorService.AuthenticateUser("admin", "123456"));
    }

    public void Dispose()
    {
        SetConfiguration(null);
    }

    private static void SetConfiguration(IReadOnlyDictionary<string, string?>? values)
    {
        var field = typeof(ConfigurationHelper).GetField("_configuration", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find ConfigurationHelper._configuration.");

        IConfiguration? configuration = null;
        if (values != null)
        {
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        field.SetValue(null, configuration);
    }
}
