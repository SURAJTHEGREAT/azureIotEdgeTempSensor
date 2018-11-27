using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;

namespace EdgeSimulatedTemperatureSensorCore
{
    public class DesiredPropertiesData
    {
        //These are private objects
        private bool _sendData = true;

        private int _sendInterval = 5;

        public DesiredPropertiesData(TwinCollection twinCollection)

        {

            Console.WriteLine($"Updating desired properties {twinCollection.ToJson(Formatting.Indented)}");

            try

            {

                if (twinCollection.Contains("SendData") && twinCollection["SendData"] != null)

                {

                    _sendData = twinCollection["SendData"];

                }



                if (twinCollection.Contains("SendInterval") && twinCollection["SendInterval"] != null)

                {

                    _sendInterval = twinCollection["SendInterval"];

                }

            }

            catch (AggregateException aexc)

            {

                foreach (var exception in aexc.InnerExceptions)

                {

                    Console.WriteLine($"[ERROR] Could not retrieve desired properties {aexc.Message}");

                }

            }

            catch (Exception ex)

            {

                Console.WriteLine($"[ERROR] Reading desired properties failed with {ex.Message}");

            }

            finally

            {

                Console.WriteLine($"Value for SendData = {_sendData}");

                Console.WriteLine($"Value for SendInterval = {_sendInterval}");

            }

        }
        //To access private objects through public methods
        public bool SendData => _sendData;

        public int SendInterval => _sendInterval;
    }
}