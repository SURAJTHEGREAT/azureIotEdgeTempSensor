using System;

namespace EdgeSimulatedTemperatureSensorCore
{
    public class TemperatureDataFactory
    {
        private static readonly Random rand = new Random();

        private static double CurrentMachineTemperature;

        public static MessageBody CreateTemperatureData(int counter, DataGenerationPolicy policy, bool reset = false)
        {
            if (reset)

            {
                //send NULL current temperature - to get current += -0.25 + (rnd.NextDouble() * 1.5);
                //since static can be accessed as such
                TemperatureDataFactory.CurrentMachineTemperature = policy.CalculateMachineTemperature();

            }
            else

            {

                TemperatureDataFactory.CurrentMachineTemperature =

                    policy.CalculateMachineTemperature(TemperatureDataFactory.CurrentMachineTemperature);

            }

            var machinePressure = policy.CalculatePressure(TemperatureDataFactory.CurrentMachineTemperature);

            var ambientTemperature = policy.CalculateAmbientTemperature();

            var ambientHumidity = policy.CalculateHumidity();

            var messageBody = new MessageBody

            {
                Machine = new Machine

                {

                    Temperature = TemperatureDataFactory.CurrentMachineTemperature,

                    Pressure = machinePressure

                },

                Ambient = new Ambient

                {

                    Temperature = ambientTemperature,

                    Humidity = ambientHumidity

                },

                TimeCreated = string.Format("{0:O}", DateTime.Now)

            };
            return messageBody;
        }
    }
}