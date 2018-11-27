using System;

namespace EdgeSimulatedTemperatureSensorCore
{
    public class DataGenerationPolicy
    {
        private static readonly Random rnd = new Random();

        private double _normal;

        public double MachineTemperatureMin { get; private set; }

        public double MachineTemperatureMax { get; private set; }

        public double MachinePressureMin { get; private set; }

        public double MachinePressureMax { get; private set; }

        public double AmbientTemperature { get; private set; }

        public int HumidityPercentMin { get; private set; }

        public int HumidityPercentMax { get; set; }

        public DataGenerationPolicy()
        {
            MachineTemperatureMin = 21;

            MachineTemperatureMax = 100;

            MachinePressureMin = 1;

            MachinePressureMax = 10;

            AmbientTemperature = 21;

            HumidityPercentMin = 24;

            HumidityPercentMax = 27;

            _normal = (MachinePressureMax - MachinePressureMin) / (MachineTemperatureMax - MachineTemperatureMin);
        }

        public double CalculateAmbientTemperature()
        {
            return AmbientTemperature + rnd.NextDouble() - 0.5;
        }

        public int CalculateHumidity()
        {
            return rnd.Next(HumidityPercentMin, HumidityPercentMax);
        }

        public double CalculatePressure(double currentTemperature)
        {
            return MachinePressureMin + ((currentTemperature - MachineTemperatureMin) * _normal);
        }

        //double? means to convert currentTemperature null to double
        public double CalculateMachineTemperature(double? currentTemperature = null)

        {
            // The ?? operator is called the null-coalescing operator. It returns the left-hand operand if the operand is not null; otherwise it returns the right hand operand.
            var current = currentTemperature ?? MachineTemperatureMin;

            if (current > MachineTemperatureMax)

            {

                current += rnd.NextDouble() - 0.5; // add value between [-0.5..0.5]

            }
            else

            {

                current += -0.25 + (rnd.NextDouble() * 1.5); // add value between [-0.25..1.25] - avg +0.5

            }
            return current;
        }
    }
}