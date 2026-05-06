using ISO11820WinForms.Forms.Controls;
using ISO11820WinForms.Services;
using Xunit;

namespace ISO11820WinForms.Tests;

public class CalibrationViewTests
{
    [Fact]
    public void CalculateSurfaceUniformity_ReturnsLegacyWebFormulaValues()
    {
        var values = new Dictionary<string, double>
        {
            ["A1"] = 750,
            ["A2"] = 753,
            ["A3"] = 747,
            ["B1"] = 752,
            ["B2"] = 751,
            ["B3"] = 749,
            ["C1"] = 748,
            ["C2"] = 750,
            ["C3"] = 754
        };

        var result = CalibrationCalculationService.CalculateSurfaceUniformity(values);

        Assert.Equal(750.4444, result.TAvg, 4);
        Assert.Equal(750.0000, result.TAvgAxis1, 4);
        Assert.Equal(751.3333, result.TAvgAxis2, 4);
        Assert.Equal(750.0000, result.TAvgAxis3, 4);
        Assert.Equal(750.0000, result.TAvgLevelA, 4);
        Assert.Equal(750.6667, result.TAvgLevelB, 4);
        Assert.Equal(750.6667, result.TAvgLevelC, 4);
        Assert.Equal(0.0592, result.TDevAxis1, 4);
        Assert.Equal(0.1184, result.TDevAxis2, 4);
        Assert.Equal(0.0592, result.TDevAxis3, 4);
        Assert.Equal(0.0592, result.TDevLevelA, 4);
        Assert.Equal(0.0296, result.TDevLevelB, 4);
        Assert.Equal(0.0296, result.TDevLevelC, 4);
        Assert.Equal(0.0790, result.TAvgDevAxis, 4);
        Assert.Equal(0.0395, result.TAvgDevLevel, 4);
    }

    [Fact]
    public void RecordCenterTemperature_UpdatesRecordedPositionTextBox()
    {
        RunOnStaThread(() =>
        {
            using var view = new CalibrationView();
            view.SetCalibrationTemperature(720.2);

            var combo = view.Controls.Find("cmbCenterPosition", true).OfType<ComboBox>().Single();
            combo.SelectedItem = "145(mm)";

            var button = view.Controls.Find("btnRecordCenter", true).OfType<Button>().Single();
            button.PerformClick();

            var textBox = view.Controls.Find("txtCenter145", true).OfType<TextBox>().Single();
            Assert.Equal("720.2", textBox.Text);
        });
    }

    private static void RunOnStaThread(Action action)
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
}
