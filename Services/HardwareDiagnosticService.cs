using ISO11820WinForms.Core;

namespace ISO11820WinForms.Services
{
    public sealed record HardwareDiagnosticResult(
        string DeviceName,
        bool Success,
        string PortName,
        string Detail,
        string? ErrorMessage = null);

    public sealed class HardwareDiagnosticService
    {
        private const ushort PidCurrentTemperatureAddress = 0x0102;

        public HardwareDiagnosticResult ProbePid(ApparatusManipulator? manipulator)
        {
            if (manipulator == null)
            {
                return new HardwareDiagnosticResult("PID 控制器", false, "未配置", "PID 控制器未初始化。");
            }

            if (manipulator.IsSimulationMode)
            {
                return new HardwareDiagnosticResult(
                    "PID 控制器",
                    true,
                    manipulator.PidPortName,
                    "仿真模式：PID 命令不访问真实串口。");
            }

            var (success, value) = manipulator.SendPidModuleCmd(OperationType.Read, PidCurrentTemperatureAddress, 0);
            if (success)
            {
                return new HardwareDiagnosticResult(
                    "PID 控制器",
                    true,
                    manipulator.PidPortName,
                    $"读取当前温度成功：{value / 10.0:F1} ℃。");
            }

            return new HardwareDiagnosticResult(
                "PID 控制器",
                false,
                manipulator.PidPortName,
                "读取当前温度寄存器失败。",
                manipulator.LastConnectionError);
        }

        public HardwareDiagnosticResult ProbeSensor(DaqWorker? worker)
        {
            if (worker == null)
            {
                return new HardwareDiagnosticResult("ADAM 采集", false, "未配置", "采集服务未初始化。");
            }

            if (worker.IsSimulationMode)
            {
                return new HardwareDiagnosticResult(
                    "ADAM 采集",
                    true,
                    worker.SensorPortName,
                    "仿真模式：采集数据由模拟器生成。");
            }

            var success = worker.ProbeNow();
            if (success)
            {
                return new HardwareDiagnosticResult(
                    "ADAM 采集",
                    true,
                    worker.SensorPortName,
                    "采集模块读取成功。");
            }

            return new HardwareDiagnosticResult(
                "ADAM 采集",
                false,
                worker.SensorPortName,
                "采集模块读取失败。",
                worker.LastConnectionError);
        }

        public IReadOnlyList<HardwareDiagnosticResult> ProbeAll(DaqWorker? worker, ApparatusManipulator? manipulator)
        {
            return new[]
            {
                ProbePid(manipulator),
                ProbeSensor(worker)
            };
        }
    }
}
