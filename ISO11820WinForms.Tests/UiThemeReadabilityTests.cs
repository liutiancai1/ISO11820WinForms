using System.Reflection;
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
    public void StyleButton_WhenCompact_UsesReadableBoldFont()
    {
        var button = new Button();

        InvokeStyleButton(button, "Primary", compact: true);

        Assert.True(button.Font.Bold);
        Assert.True(button.Font.Size >= 11F, $"Expected compact button font >= 11pt, actual: {button.Font.Size}pt");
        Assert.True(button.Padding.Left >= 12);
        Assert.True(button.Padding.Right >= 12);
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
