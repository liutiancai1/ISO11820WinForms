using System.IO.Ports;

namespace ISO11820WinForms.Services
{
    public sealed record ModbusSerialSettings(int BaudRate, Parity Parity, StopBits StopBits)
    {
        public static ModbusSerialSettings Default { get; } = new(9600, Parity.None, StopBits.One);

        public string ToDisplayString()
        {
            return $"{BaudRate}, 8{FormatParity(Parity)}{FormatStopBits(StopBits)}";
        }

        private static string FormatParity(Parity parity)
        {
            return parity switch
            {
                Parity.Even => "E",
                Parity.Odd => "O",
                _ => "N"
            };
        }

        private static string FormatStopBits(StopBits stopBits)
        {
            return stopBits switch
            {
                StopBits.Two => "2",
                _ => "1"
            };
        }
    }
}
