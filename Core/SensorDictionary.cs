using ISO11820_2020.Models;
using TestServer.Models;

namespace ISO11820WinForms.Core
{
    /* 定义传感器集合类型 */
    public class SensorDictionary
    {
        //传感器集合
        public Dictionary<int, Sensor> Sensors { get; set; }

        public SensorDictionary()
        {
            Sensors = new Dictionary<int, Sensor>();
        }
    }
}
