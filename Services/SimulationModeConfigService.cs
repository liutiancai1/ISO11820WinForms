using System.Text.Json;
using System.Text.Json.Nodes;

namespace ISO11820WinForms.Services
{
    public sealed class SimulationModeConfigService
    {
        private readonly string _configPath;

        public SimulationModeConfigService(string? configPath = null)
        {
            _configPath = string.IsNullOrWhiteSpace(configPath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")
                : configPath;
        }

        public bool IsSimulationModeEnabled()
        {
            var root = LoadRoot();
            return root["Simulation"]?["EnableSimulation"]?.GetValue<bool>() == true;
        }

        public bool ToggleSimulationMode()
        {
            var enabled = !IsSimulationModeEnabled();
            SetSimulationMode(enabled);
            return enabled;
        }

        public void SetSimulationMode(bool enabled)
        {
            var root = LoadRoot();
            var simulation = root["Simulation"] as JsonObject;
            if (simulation == null)
            {
                simulation = new JsonObject();
                root["Simulation"] = simulation;
            }

            simulation["EnableSimulation"] = enabled;
            simulation["SimulateSensors"] = enabled;
            simulation["SimulatePidController"] = enabled;

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configPath, root.ToJsonString(options));
        }

        private JsonObject LoadRoot()
        {
            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException("未找到 appsettings.json 配置文件。", _configPath);
            }

            var node = JsonNode.Parse(File.ReadAllText(_configPath));
            if (node is not JsonObject root)
            {
                throw new InvalidOperationException("appsettings.json 根节点必须是 JSON 对象。");
            }

            return root;
        }
    }
}
