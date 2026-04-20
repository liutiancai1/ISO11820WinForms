using ISO11820WinForms.Core;
using Xunit;

namespace ISO11820WinForms.Tests;

public class ApparatusManipulatorConfigurationTests
{
    [Fact]
    public void Constructor_WhenPidStationProvided_UsesThatUnitIdentifier()
    {
        var manipulator = new ApparatusManipulator("COM9", "COM9", 6, 750, 2);

        Assert.Equal(2, manipulator.UnitIdentifier);
    }
}
