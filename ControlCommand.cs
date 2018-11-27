using Newtonsoft.Json;

namespace EdgeSimulatedTemperatureSensorCore
{
    public  class ControlCommand
    {
        [JsonProperty("command")]

        public ControlCommandEnum Command { get; set; }
    }

    public enum ControlCommandEnum
    {
        Reset = 0,

        Noop = 1
    };
}