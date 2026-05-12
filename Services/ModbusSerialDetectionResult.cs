namespace ISO11820WinForms.Services
{
    public sealed record ModbusSerialDetectionResult(
        bool IsDetected,
        ModbusSerialSettings Settings,
        byte StationNumber,
        ushort StartAddress,
        string? RegisterKind = null);
}
