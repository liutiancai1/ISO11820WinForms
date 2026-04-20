using System.IO.Ports;
using ISO11820WinForms.Services;
using Xunit;

namespace ISO11820WinForms.Tests;

public class AdamModbusSerialSettingsDetectorTests
{
    [Fact]
    public void Detect_WhenDefaultHoldingRegistersReadable_ReturnsDefaultSettings()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();
        detector.AddHoldingResponse(ModbusSerialSettings.Default, 1, 1, new ushort[] { 1, 2, 3, 4 });

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.True(result.IsDetected);
        Assert.Equal(ModbusSerialSettings.Default, result.Settings);
        Assert.Equal((byte)1, result.StationNumber);
    }

    [Fact]
    public void Detect_WhenOnlyEvenParityInputRegistersReadable_ReturnsEvenParitySettings()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();
        var evenParity = new ModbusSerialSettings(9600, Parity.Even, StopBits.One);
        detector.AddInputResponse(evenParity, 1, 1, new ushort[] { 1, 2, 3, 4 });

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.True(result.IsDetected);
        Assert.Equal(evenParity, result.Settings);
        Assert.Equal((byte)1, result.StationNumber);
    }

    [Fact]
    public void Detect_WhenNoCandidateResponds_ReturnsDefaultAndNotDetected()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.False(result.IsDetected);
        Assert.Equal(ModbusSerialSettings.Default, result.Settings);
    }

    [Fact]
    public void Detect_WhenConfiguredStartAddressDoesNotRespondButZeroBasedAddressDoes_StillDetectsDevice()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();
        detector.AddHoldingResponse(ModbusSerialSettings.Default, 1, 0, new ushort[] { 1, 2, 3, 4 });

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.True(result.IsDetected);
        Assert.Equal(ModbusSerialSettings.Default, result.Settings);
        Assert.Equal((byte)1, result.StationNumber);
    }

    [Fact]
    public void Detect_WhenConfiguredStationDoesNotRespondButStationTwentyThreeDoes_ReturnsNotDetected()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();
        detector.AddInputResponse(ModbusSerialSettings.Default, 23, 1, new ushort[] { 1, 2, 3, 4 });

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.False(result.IsDetected);
        Assert.Equal(ModbusSerialSettings.Default, result.Settings);
        Assert.Equal((byte)1, result.StationNumber);
        Assert.Equal((ushort)1, result.StartAddress);
        Assert.Null(result.RegisterKind);
    }

    [Fact]
    public void Detect_WhenPidStationResponds_ReturnsNotDetected()
    {
        var detector = new TestableAdamModbusSerialSettingsDetector();
        detector.AddHoldingResponse(ModbusSerialSettings.Default, 2, 1, new ushort[] { 10, 20, 30, 40 });

        var result = detector.Detect("COM9", 1, 1, 8, 1000);

        Assert.False(result.IsDetected);
        Assert.Equal(ModbusSerialSettings.Default, result.Settings);
        Assert.Equal((byte)1, result.StationNumber);
    }

    private sealed class TestableAdamModbusSerialSettingsDetector : AdamModbusSerialSettingsDetector
    {
        private readonly Dictionary<(ModbusSerialSettings Settings, byte StationNumber, ushort StartAddress), ushort[]> _holdingResponses = new();
        private readonly Dictionary<(ModbusSerialSettings Settings, byte StationNumber, ushort StartAddress), ushort[]> _inputResponses = new();

        public void AddHoldingResponse(ModbusSerialSettings settings, byte stationNumber, ushort startAddress, ushort[] response)
        {
            _holdingResponses[(settings, stationNumber, startAddress)] = response;
        }

        public void AddInputResponse(ModbusSerialSettings settings, byte stationNumber, ushort startAddress, ushort[] response)
        {
            _inputResponses[(settings, stationNumber, startAddress)] = response;
        }

        protected override ushort[] ReadHoldingRegisters(
            string portName,
            ModbusSerialSettings settings,
            byte stationNumber,
            ushort startAddress,
            int registerCount,
            int timeoutMs)
        {
            if (_holdingResponses.TryGetValue((settings, stationNumber, startAddress), out var response))
            {
                return response;
            }

            throw new TimeoutException("holding timeout");
        }

        protected override ushort[] ReadInputRegisters(
            string portName,
            ModbusSerialSettings settings,
            byte stationNumber,
            ushort startAddress,
            int registerCount,
            int timeoutMs)
        {
            if (_inputResponses.TryGetValue((settings, stationNumber, startAddress), out var response))
            {
                return response;
            }

            throw new TimeoutException("input timeout");
        }
    }
}
