using System.Collections.Concurrent;

namespace ISO11820WinForms.Utilities
{
    public static class SerialPortCoordinator
    {
        private static readonly ConcurrentDictionary<string, object> PortLocks = new(StringComparer.OrdinalIgnoreCase);

        public static void RunExclusive(string? portName, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            lock (GetPortLock(portName))
            {
                action();
            }
        }

        public static T RunExclusive<T>(string? portName, Func<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            lock (GetPortLock(portName))
            {
                return action();
            }
        }

        public static string NormalizePortName(string? portName)
        {
            return string.IsNullOrWhiteSpace(portName)
                ? string.Empty
                : portName.Trim().ToUpperInvariant();
        }

        public static bool IsSamePort(string? leftPort, string? rightPort)
        {
            return NormalizePortName(leftPort) == NormalizePortName(rightPort);
        }

        private static object GetPortLock(string? portName)
        {
            var normalizedPort = NormalizePortName(portName);
            return PortLocks.GetOrAdd(normalizedPort, _ => new object());
        }
    }
}
