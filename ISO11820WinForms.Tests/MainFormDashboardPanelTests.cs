using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using ISO11820WinForms.Core;
using ISO11820WinForms.Forms;
using ISO11820WinForms.Global;
using ISO11820WinForms.Utilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ISO11820WinForms.Tests;

public class MainFormDashboardPanelTests : IDisposable
{
    private readonly string _tempDirectory;

    public MainFormDashboardPanelTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ISO11820WinFormsTests", Guid.NewGuid().ToString("N"));
        SetConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:SqlitePath"] = Path.Combine(_tempDirectory, "ISO11820.Tests.db"),
            ["Simulation:EnableSimulation"] = "false",
            ["Simulation:SimulateSensors"] = "false",
            ["Simulation:SimulatePidController"] = "false",
            ["FileStorage:BaseDirectory"] = Path.Combine(_tempDirectory, "ISO11820")
        });
        ResetSystemContext();
        Assert.True(DatabaseHelper.EnsureDatabaseCreated());
    }

    public void Dispose()
    {
        ResetSystemContext();
        SetConfiguration(null);
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void CreateConnectionStatusPanel_IncludesCompactInformationSections()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;

            Assert.NotNull(panel.Controls["SharedPortCard"]);
            Assert.NotNull(panel.Controls["TestStateCard"]);
            Assert.NotNull(panel.Controls["RuntimeSummaryCard"]);
            Assert.NotNull(panel.Controls["RecentStatusCard"]);
            Assert.NotNull(panel.Controls["ConnectionNote"]);
        });
    }

    [Fact]
    public void CreateConnectionStatusPanel_ConnectionNoteProvidesDiagnosticsButton()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;
            var note = panel.Controls["ConnectionNote"];

            Assert.NotNull(note);
            Assert.Contains(GetControls<Button>(note!), button =>
                button.Name == "HardwareDiagnosticsButton" && button.Text.Contains("诊断"));
        });
    }

    [Fact]
    public void CreateConnectionStatusPanel_ConnectionNoteProvidesModeSwitchButton()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;
            var note = panel.Controls["ConnectionNote"];

            Assert.NotNull(note);
            Assert.Contains(GetControls<Button>(note!), button => button.Name == "HardwareModeSwitchButton");
            Assert.Contains(GetControls<Label>(note!), label => label.Name == "HardwareModeSummaryLabel");
        });
    }

    [Fact]
    public void CreateConnectionStatusPanel_DoesNotForceScrollbar()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;

            Assert.False(panel.AutoScroll);
        });
    }

    [Fact]
    public void CreateConnectionStatusPanel_UsesClearRecordStateText()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;
            var labelTexts = GetLabels(panel).Select(label => label.Text).ToList();

            Assert.Contains("未记录", labelTexts);
            Assert.DoesNotContain("待机", labelTexts);
        });
    }

    [Fact]
    public void PositionConnectionStatusControls_UsesAvailableVerticalSpace()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var createMethod = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");
            var positionMethod = typeof(MainForm).GetMethod("PositionConnectionStatusControls", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find PositionConnectionStatusControls.");
            var panelField = typeof(MainForm).GetField("_connectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find _connectionStatusPanel.");

            using var panel = (Panel)createMethod.Invoke(form, Array.Empty<object>())!;
            panel.Size = new Size(500, 900);
            panelField.SetValue(form, panel);

            positionMethod.Invoke(form, Array.Empty<object>());

            Assert.True(panel.Controls["RuntimeSummaryCard"].Height >= 150);
            Assert.True(panel.Controls["ConnectionNote"].Bottom >= 780);
        });
    }

    [Fact]
    public void PositionConnectionStatusControls_CompactHeightKeepsCardsVisible()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var createMethod = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");
            var positionMethod = typeof(MainForm).GetMethod("PositionConnectionStatusControls", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find PositionConnectionStatusControls.");
            var panelField = typeof(MainForm).GetField("_connectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find _connectionStatusPanel.");

            using var panel = (Panel)createMethod.Invoke(form, Array.Empty<object>())!;
            panel.Size = new Size(500, 647);
            panelField.SetValue(form, panel);

            positionMethod.Invoke(form, Array.Empty<object>());

            Assert.True(panel.Controls["RuntimeSummaryCard"].Height >= 132);
            Assert.True(panel.Controls["ConnectionNote"].Bottom <= panel.ClientSize.Height - 8);
        });
    }

    [Fact]
    public void CreateConnectionStatusPanel_CriticalCardsUseLayoutContainers()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateConnectionStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateConnectionStatusPanel.");

            using var panel = (Panel)method.Invoke(form, Array.Empty<object>())!;
            var sharedCard = panel.Controls["SharedPortCard"];
            var stateCard = panel.Controls["TestStateCard"];

            Assert.Contains(sharedCard.Controls.OfType<TableLayoutPanel>(), layout => layout.Dock == DockStyle.Fill);
            Assert.Contains(stateCard.Controls.OfType<TableLayoutPanel>(), layout => layout.Dock == DockStyle.Fill);
            Assert.Empty(sharedCard.Controls.OfType<Label>());
            Assert.Empty(stateCard.Controls.OfType<Label>());
        });
    }

    [Fact]
    public void CreateOperationStatusPanel_UsesLayoutContainer()
    {
        RunSta(() =>
        {
            var form = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            var method = typeof(MainForm).GetMethod("CreateOperationStatusPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to find CreateOperationStatusPanel.");

            using var panel = (Control)method.Invoke(form, Array.Empty<object>())!;

            Assert.Contains(panel.Controls.OfType<TableLayoutPanel>(), layout => layout.Dock == DockStyle.Fill);
            Assert.Empty(panel.Controls.OfType<Label>());
        });
    }

    [Fact]
    public void GetStatusText_UsesClearIdleText()
    {
        var method = typeof(MainForm).GetMethod("GetStatusText", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find GetStatusText.");

        var text = (string)method.Invoke(null, new object[] { MasterStatus.Idle })!;

        Assert.Equal("\u672A\u5F00\u59CB", text);
        Assert.NotEqual("\u5F85\u673A", text);
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
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

    private static void ResetSystemContext()
    {
        var field = typeof(SystemContext).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to find SystemContext._instance.");
        field.SetValue(null, null);
    }

    private static IEnumerable<Label> GetLabels(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label label)
            {
                yield return label;
            }

            foreach (var nested in GetLabels(child))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<TControl> GetControls<TControl>(Control root)
        where TControl : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is TControl target)
            {
                yield return target;
            }

            foreach (var nested in GetControls<TControl>(child))
            {
                yield return nested;
            }
        }
    }
}
