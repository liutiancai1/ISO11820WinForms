namespace ISO11820WinForms.Services
{
    public interface IModbusRtuGateway : IDisposable
    {
        ModbusSerialSettings Settings { get; }
        ushort[] ReadHoldingRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs);
        ushort[] ReadInputRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs);
        void WriteSingleRegister(byte stationNumber, ushort address, ushort value, int timeoutMs);
    }
}
