using System.IO.Ports;
using FluentModbus;
using Serilog;

namespace ISO11820WinForms.Services
{
    public class AdamModbusSerialSettingsDetector
    {
        private static readonly ModbusSerialSettings[] CandidateSettings =
        {
            ModbusSerialSettings.Default,
            new(9600, Parity.Even, StopBits.One),
            new(9600, Parity.Odd, StopBits.One)
        };

        public ModbusSerialDetectionResult Detect(
            string portName,
            byte stationNumber,
            ushort startAddress,
            int registerCount,
            int timeoutMs)
        {
            foreach (var settings in CandidateSettings)
            {
                foreach (var candidateStartAddress in GetCandidateStartAddresses(startAddress))
                {
                    try
                    {
                        var holdingRegisters = ReadHoldingRegisters(
                            portName,
                            settings,
                            stationNumber,
                            candidateStartAddress,
                            registerCount,
                            timeoutMs);

                        if (holdingRegisters.Length > 0)
                        {
                            Log.Information(
                                "ADAM 自动探测成功: 串口参数={SerialSettings}, 站号={StationNumber}, 起始地址={StartAddress}, 寄存器类型=Holding Registers",
                                settings.ToDisplayString(),
                                stationNumber,
                                candidateStartAddress);
                            return new ModbusSerialDetectionResult(
                                true,
                                settings,
                                stationNumber,
                                candidateStartAddress,
                                "HoldingRegisters");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(
                            "ADAM 自动探测 Holding Registers 失败: 串口参数={SerialSettings}, 站号={StationNumber}, 起始地址={StartAddress}, 原因={Message}",
                            settings.ToDisplayString(),
                            stationNumber,
                            candidateStartAddress,
                            ex.Message);
                    }

                    try
                    {
                        var inputRegisters = ReadInputRegisters(
                            portName,
                            settings,
                            stationNumber,
                            candidateStartAddress,
                            registerCount,
                            timeoutMs);

                        if (inputRegisters.Length > 0)
                        {
                            Log.Information(
                                "ADAM 自动探测成功: 串口参数={SerialSettings}, 站号={StationNumber}, 起始地址={StartAddress}, 寄存器类型=Input Registers",
                                settings.ToDisplayString(),
                                stationNumber,
                                candidateStartAddress);
                            return new ModbusSerialDetectionResult(
                                true,
                                settings,
                                stationNumber,
                                candidateStartAddress,
                                "InputRegisters");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(
                            "ADAM 自动探测 Input Registers 失败: 串口参数={SerialSettings}, 站号={StationNumber}, 起始地址={StartAddress}, 原因={Message}",
                            settings.ToDisplayString(),
                            stationNumber,
                            candidateStartAddress,
                            ex.Message);
                    }
                }
            }

            Log.Warning(
                "ADAM 自动探测未找到可用串口参数，回退到默认参数 {SerialSettings}",
                ModbusSerialSettings.Default.ToDisplayString());
            return new ModbusSerialDetectionResult(false, ModbusSerialSettings.Default, stationNumber, startAddress);
        }

        protected virtual ushort[] ReadHoldingRegisters(
            string portName,
            ModbusSerialSettings settings,
            byte stationNumber,
            ushort startAddress,
            int registerCount,
            int timeoutMs)
        {
            using var client = CreateClient(settings, timeoutMs);
            client.Connect(portName, ModbusEndianness.BigEndian);
            return client.ReadHoldingRegisters<ushort>(stationNumber, startAddress, registerCount).ToArray();
        }

        protected virtual ushort[] ReadInputRegisters(
            string portName,
            ModbusSerialSettings settings,
            byte stationNumber,
            ushort startAddress,
            int registerCount,
            int timeoutMs)
        {
            using var client = CreateClient(settings, timeoutMs);
            client.Connect(portName, ModbusEndianness.BigEndian);
            return client.ReadInputRegisters<ushort>(stationNumber, startAddress, registerCount).ToArray();
        }

        private static IEnumerable<ushort> GetCandidateStartAddresses(ushort configuredStartAddress)
        {
            yield return configuredStartAddress;

            if (configuredStartAddress != 0)
            {
                yield return 0;
            }

            if (configuredStartAddress != 1)
            {
                yield return 1;
            }
        }

        private static ModbusRtuClient CreateClient(ModbusSerialSettings settings, int timeoutMs)
        {
            return new ModbusRtuClient
            {
                BaudRate = settings.BaudRate,
                Parity = settings.Parity,
                StopBits = settings.StopBits,
                ReadTimeout = timeoutMs,
                WriteTimeout = timeoutMs
            };
        }
    }
}
