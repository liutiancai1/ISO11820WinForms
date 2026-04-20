using ISO11820WinForms.Utilities;
using Xunit;

namespace ISO11820WinForms.Tests;

public class ConfigurationHelperTests
{
    [Fact]
    public void NormalizeSensorRegisterStartAddress_WhenModbusStartAddressIsZero_KeepsZero()
    {
        var result = ConfigurationHelper.NormalizeSensorRegisterStartAddress("ModbusRtu", 0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void NormalizeSensorRegisterStartAddress_WhenModbusStartAddressIsPositive_KeepsOriginalValue()
    {
        var result = ConfigurationHelper.NormalizeSensorRegisterStartAddress("ModbusRtu", 5);

        Assert.Equal(5, result);
    }
}
