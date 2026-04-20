using ISO11820WinForms.Core;
using Xunit;

namespace ISO11820WinForms.Tests;

public class TestMasterInitializationTests
{
    [Fact]
    public void OnInitialized_WhenManipulatorConnects_ReturnsTrue()
    {
        var master = new TestMaster(new SensorDictionary(), new FakeManipulator(true));

        var result = master.OnInitialized();

        Assert.True(result);
    }

    [Fact]
    public void OnInitialized_WhenManipulatorConnectFails_ReturnsFalse()
    {
        var master = new TestMaster(new SensorDictionary(), new FakeManipulator(false));

        var result = master.OnInitialized();

        Assert.False(result);
    }

    private sealed class FakeManipulator : ApparatusManipulator
    {
        private readonly bool _establishResult;

        public FakeManipulator(bool establishResult)
            : base("COM9", "COM9", 6, 750)
        {
            _establishResult = establishResult;
        }

        public override bool EstablishConnection()
        {
            return _establishResult;
        }
    }
}
