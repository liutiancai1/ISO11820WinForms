using System.Text.Json.Nodes;
using ISO11820WinForms.Services;
using Xunit;

namespace ISO11820WinForms.Tests;

public class SimulationModeConfigServiceTests
{
    [Fact]
    public void SetSimulationMode_UpdatesAllSimulationSwitches()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"iso11820-simulation-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile,
            "{\n" +
            "  \"Simulation\": {\n" +
            "    \"EnableSimulation\": true,\n" +
            "    \"SimulateSensors\": true,\n" +
            "    \"SimulatePidController\": true,\n" +
            "    \"InitialFurnaceTemp\": 720.0\n" +
            "  }\n" +
            "}\n");

        try
        {
            var service = new SimulationModeConfigService(tempFile);

            service.SetSimulationMode(false);

            var json = JsonNode.Parse(File.ReadAllText(tempFile))!;
            Assert.False(json["Simulation"]!["EnableSimulation"]!.GetValue<bool>());
            Assert.False(json["Simulation"]!["SimulateSensors"]!.GetValue<bool>());
            Assert.False(json["Simulation"]!["SimulatePidController"]!.GetValue<bool>());

            service.SetSimulationMode(true);

            json = JsonNode.Parse(File.ReadAllText(tempFile))!;
            Assert.True(json["Simulation"]!["EnableSimulation"]!.GetValue<bool>());
            Assert.True(json["Simulation"]!["SimulateSensors"]!.GetValue<bool>());
            Assert.True(json["Simulation"]!["SimulatePidController"]!.GetValue<bool>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ToggleSimulationMode_ReturnsNewMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"iso11820-simulation-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile,
            "{\n" +
            "  \"Simulation\": {\n" +
            "    \"EnableSimulation\": false,\n" +
            "    \"SimulateSensors\": false,\n" +
            "    \"SimulatePidController\": false\n" +
            "  }\n" +
            "}\n");

        try
        {
            var service = new SimulationModeConfigService(tempFile);

            var enabled = service.ToggleSimulationMode();

            Assert.True(enabled);
            Assert.True(service.IsSimulationModeEnabled());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
