using System.IO.Ports;
using FluentModbus;
using ISO11820WinForms.Utilities;
using Serilog;

namespace ISO11820WinForms.Services
{
    public sealed class SharedModbusRtuGateway : IModbusRtuGateway
    {
        private readonly string _portName;
        public ModbusSerialSettings Settings { get; }
        private ModbusRtuClient? _client;
        private bool _disposed;

        public SharedModbusRtuGateway(string portName, ModbusSerialSettings? settings = null)
        {
            _portName = portName;
            Settings = settings ?? ModbusSerialSettings.Default;
        }

        public ushort[] ReadHoldingRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            return SerialPortCoordinator.RunExclusive(_portName, () =>
            {
                try
                {
                    return EnsureConnected(timeoutMs)
                        .ReadHoldingRegisters<ushort>(stationNumber, startAddress, registerCount)
                        .ToArray();
                }
                catch
                {
                    ResetClient();
                    throw;
                }
            });
        }

        public ushort[] ReadInputRegisters(byte stationNumber, ushort startAddress, int registerCount, int timeoutMs)
        {
            return SerialPortCoordinator.RunExclusive(_portName, () =>
            {
                try
                {
                    return EnsureConnected(timeoutMs)
                        .ReadInputRegisters<ushort>(stationNumber, startAddress, registerCount)
                        .ToArray();
                }
                catch
                {
                    ResetClient();
                    throw;
                }
            });
        }

        public void WriteSingleRegister(byte stationNumber, ushort address, ushort value, int timeoutMs)
        {
            SerialPortCoordinator.RunExclusive(_portName, () =>
            {
                try
                {
                    EnsureConnected(timeoutMs).WriteSingleRegister(stationNumber, address, value);
                }
                catch
                {
                    ResetClient();
                    throw;
                }
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            SerialPortCoordinator.RunExclusive(_portName, () =>
            {
                ResetClient();
                _disposed = true;
            });
        }

        private ModbusRtuClient EnsureConnected(int timeoutMs)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SharedModbusRtuGateway));
            }

            _client ??= new ModbusRtuClient
            {
                BaudRate = Settings.BaudRate,
                Parity = Settings.Parity,
                StopBits = Settings.StopBits
            };

            _client.ReadTimeout = timeoutMs;
            _client.WriteTimeout = timeoutMs;

            if (!_client.IsConnected)
            {
                _client.Connect(_portName, ModbusEndianness.BigEndian);
                Log.Information("共享 Modbus 串口已连接: {Port}", _portName);
            }

            return _client;
        }

        private void ResetClient()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                if (_client.IsConnected)
                {
                    _client.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("关闭共享 Modbus 串口时忽略异常: {Message}", ex.Message);
            }
            finally
            {
                _client = null;
            }
        }
    }
}
