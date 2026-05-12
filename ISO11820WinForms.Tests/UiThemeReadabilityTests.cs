using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using ISO11820WinForms.Global;
using Xunit;

namespace ISO11820WinForms.Tests;

public class UiThemeReadabilityTests
{
    private static readonly Assembly AppAssembly = typeof(SystemContext).Assembly;
    private static readonly Type UiThemeType =
        AppAssembly.GetType("ISO11820WinForms.UI.UiTheme")
        ?? throw new InvalidOperationException("Unable to find UiTheme type.");

    private static readonly Type ButtonToneType =
        AppAssembly.GetType("ISO11820WinForms.UI.ButtonTone")
        ?? throw new InvalidOperationException("Unable to find ButtonTone type.");

    [Fact]
    public void IndustrialPalette_UsesRequestedLightMonitoringColors()
    {
        Assert.Equal(Color.FromArgb(0xec, 0xee, 0xf1), GetThemeColor("AppBackground"));
        Assert.Equal(Color.FromArgb(0x33, 0x44, 0x55), GetThemeColor("Ink"));
        Assert.Equal(Color.FromArgb(0x66, 0x77, 0x88), GetThemeColor("InkMuted"));
        Assert.Equal(Color.FromArgb(0x3a, 0x6b, 0x54), GetThemeColor("Accent"));
        Assert.Equal(Color.FromArgb(0xa0, 0x55, 0x41), GetThemeColor("Danger"));
        Assert.Equal(Color.FromArgb(0x6c, 0x78, 0x86), GetThemeColor("Neutral"));
    }

    [Fact]
    public void StyleDataGridView_UsesIndustrialTablePalette()
    {
        using var grid = new DataGridView();

        InvokeStyleDataGridView(grid);

        Assert.Equal(Color.White, grid.BackgroundColor);
        Assert.Equal(Color.FromArgb(0xe2, 0xe6, 0xed), grid.GridColor);
        Assert.Equal(Color.White, grid.DefaultCellStyle.BackColor);
        Assert.Equal(Color.FromArgb(0xf7, 0xf9, 0xfc), grid.AlternatingRowsDefaultCellStyle.BackColor);
        Assert.Equal(Color.FromArgb(0xd6, 0xe4, 0xff), grid.DefaultCellStyle.SelectionBackColor);
        Assert.Equal(Color.FromArgb(0x1f, 0x2a, 0x44), grid.ColumnHeadersDefaultCellStyle.BackColor);
        Assert.Equal(Color.White, grid.ColumnHeadersDefaultCellStyle.ForeColor);
    }

    [Fact]
    public void StyleButton_WhenCompact_UsesReadableBoldFont()
    {
        var button = new Button();

        InvokeStyleButton(button, "Primary", compact: true);

        Assert.True(button.Font.Bold);
        Assert.True(button.Font.Size >= 11F, $"Expected compact button font >= 11pt, actual: {button.Font.Size}pt");
        Assert.True(button.Padding.Left >= 10);
        Assert.True(button.Padding.Right >= 10);
    }

    [Fact]
    public void StyleButton_WhenCompact_ExpandsWidthForFourCharacterLabels()
    {
        var button = new Button
        {
            Text = "新建试验",
            Width = 100
        };

        InvokeStyleButton(button, "Warning", compact: true);

        var textWidth = TextRenderer.MeasureText(button.Text, button.Font).Width;
        var requiredWidth = textWidth + button.Padding.Left + button.Padding.Right + 18;

        Assert.True(button.MinimumSize.Width >= 118);
        Assert.True(button.MinimumSize.Width >= requiredWidth, $"Expected minimum width >= {requiredWidth}, actual: {button.MinimumSize.Width}");
        Assert.True(button.Width >= button.MinimumSize.Width, $"Expected button width >= minimum width, actual: {button.Width} < {button.MinimumSize.Width}");
    }

    [Theory]
    [InlineData("Primary")]
    [InlineData("Secondary")]
    [InlineData("Warning")]
    [InlineData("Danger")]
    [InlineData("Neutral")]
    public void StyleButton_UsesAccessibleTextContrast(string toneName)
    {
        var button = new Button();

        InvokeStyleButton(button, toneName, compact: true);

        var contrastRatio = CalculateContrastRatio(button.BackColor, button.ForeColor);
        Assert.True(
            contrastRatio >= 4.5,
            $"Expected contrast ratio >= 4.5 for tone {toneName}, actual: {contrastRatio:F2}");
    }

    [Fact]
    public void StyleMenuStrip_UsesReadableMenuFonts()
    {
        var menuStrip = new MenuStrip();
        var menuItem = new ToolStripMenuItem("样品试验");
        menuStrip.Items.Add(menuItem);

        InvokeStyleMenuStrip(menuStrip, compact: false);
        Assert.True(menuItem.Font.Size >= 11F, $"Expected main menu font >= 11pt, actual: {menuItem.Font.Size}pt");

        InvokeStyleMenuStrip(menuStrip, compact: true);
        Assert.True(menuItem.Font.Size >= 10F, $"Expected compact menu font >= 10pt, actual: {menuItem.Font.Size}pt");
    }

    private static void InvokeStyleButton(Button button, string toneName, bool compact)
    {
        var tone = Enum.Parse(ButtonToneType, toneName);
        var method = UiThemeType.GetMethod("StyleButton", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to find StyleButton method.");

        method.Invoke(null, new object[] { button, tone, compact });
    }

    private static void InvokeStyleMenuStrip(MenuStrip menuStrip, bool compact)
    {
        var method = UiThemeType.GetMethod("StyleMenuStrip", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to find StyleMenuStrip method.");

        method.Invoke(null, new object[] { menuStrip, compact });
    }

    private static void InvokeStyleDataGridView(DataGridView grid)
    {
        var method = UiThemeType.GetMethod("StyleDataGridView", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to find StyleDataGridView method.");

        method.Invoke(null, new object[] { grid });
    }

    private static Color GetThemeColor(string fieldName)
    {
        var field = UiThemeType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Unable to find UiTheme.{fieldName}.");

        return (Color)field.GetValue(null)!;
    }

    private static double CalculateContrastRatio(Color background, Color foreground)
    {
        var backgroundLuminance = CalculateRelativeLuminance(background);
        var foregroundLuminance = CalculateRelativeLuminance(foreground);
        var lighter = Math.Max(backgroundLuminance, foregroundLuminance);
        var darker = Math.Min(backgroundLuminance, foregroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double CalculateRelativeLuminance(Color color)
    {
        static double ConvertChannel(int channel)
        {
            var normalized = channel / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return
            0.2126 * ConvertChannel(color.R) +
            0.7152 * ConvertChannel(color.G) +
            0.0722 * ConvertChannel(color.B);
    }
}
