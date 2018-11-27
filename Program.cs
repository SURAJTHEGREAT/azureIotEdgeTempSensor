using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace EdgeSimulatedTemperatureSensorCore
{
    class Program

    {
        //he volatile keyword indicates that a field might be modified by multiple threads that are executing at the same time.
        private static volatile DesiredPropertiesData desiredPropertiesData;
        private static volatile bool IsReset = false;
        private static int counter;
        private static DataGenerationPolicy generationPolicy = new DataGenerationPolicy();
        static async Task Main(string[] args)
        {
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable

            var connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");
            
            // Cert verification is not yet fully functional when using Windows OS for the container

            //var bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            //if (!bypassCertVerification) InstallCert();

            //Start the initiate method
            await Init(connectionString);

            // Wait until the app unloads or is cancelled

            var cts = new CancellationTokenSource();

            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();

            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

            await WhenCancelled(cts.Token);

        }

        /// <summary>

        /// Handles cleanup operations when app is cancelled or unloads

        /// </summary>

        public static Task WhenCancelled(CancellationToken cancellationToken)

        {

            var tcs = new TaskCompletionSource<bool>();

            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);

            return tcs.Task;

        }


        static async Task Init(string connectionString)
        {
            //Create a module client with MQTT
            Console.WriteLine("Connection String {0}", connectionString);
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
/*
            if (bypassCertVerification)

            {

                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            }
*/

            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime

            //var ioTHubModuleClient = ModuleClient.CreateFromConnectionString(connectionString, settings);

            // Open a connection to the Edge runtime - create from env with transport type (https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.moduleclient.createfromenvironmentasync?view=azure-dotnet)
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

            await ioTHubModuleClient.OpenAsync();

            Console.WriteLine("IoT Hub module client initialized.");

            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();

            var moduleTwinCollection = moduleTwin.Properties.Desired;

            desiredPropertiesData = new DesiredPropertiesData(moduleTwinCollection);

            // callback for updating desired properties through the portal or rest api - the collection passed is twin collection

            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // this direct method will allow to reset the temperature sensor values back to their initial state - send via rest api

            await ioTHubModuleClient.SetMethodHandlerAsync("reset", ResetMethod, null);

            // we don't pass ioTHubModuleClient as we're not sending any messages out to the message bus

            await ioTHubModuleClient.SetInputMessageHandlerAsync("control", ControlMessageHandler, null);

            // as this runs in a loop we don't await

            SendSimulationData(ioTHubModuleClient);


        }

        private static Task<MessageResponse> ControlMessageHandler(Message message, object userContext)
        {
            var messageBytes = message.GetBytes();

            var messageString = Encoding.UTF8.GetString(messageBytes);
                      
            Console.WriteLine($"Received message Body: [{messageString}]");

            try
            {
                var messages = JsonConvert.DeserializeObject<ControlCommand[]>(messageString);
                foreach (ControlCommand messageBody in messages)
                {
                    if (messageBody.Command == ControlCommandEnum.Reset)

                    {

                        Console.WriteLine("Resetting temperature sensor..");

                        IsReset = true;

                    }

                    else

                    {

                        //NoOp

                        Console.WriteLine("Received NOOP message");

                    }
                }
            }
            catch (Exception ex)

            {

                Console.WriteLine($"Failed to deserialize control command with exception: [{ex.Message}]");

            }
            return Task.FromResult(MessageResponse.Completed);
        }

        static void InstallCert()
        {
            var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");

            if (string.IsNullOrWhiteSpace(certPath))

            {

                // We cannot proceed further without a proper cert file

                Console.WriteLine($"Missing path to certificate collection file: {certPath}");

                throw new InvalidOperationException("Missing path to certificate file.");

            }

            else if (!File.Exists(certPath))

            {

                // We cannot proceed further without a proper cert file

                Console.WriteLine($"Missing path to certificate collection file: {certPath}");

                throw new InvalidOperationException("Missing certificate file.");

            }

            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);

            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));

            Console.WriteLine("Added Cert: " + certPath);

            store.Close();
        }
        private static Task OnDesiredPropertiesUpdate(TwinCollection twinCollection, object userContext)
        {
            desiredPropertiesData = new DesiredPropertiesData(twinCollection);
            return Task.CompletedTask;
        }
        private static Task<MethodResponse> ResetMethod(MethodRequest request, object userContext)
        {
            var response = new MethodResponse((int)HttpStatusCode.OK);
            Console.WriteLine("Received reset command via direct method invocation");

            Console.WriteLine("Resetting temperature sensor...");

            IsReset = true;

            return Task.FromResult(response);
            
        }
        private static async Task SendSimulationData(ModuleClient deviceClient)
        {
            while (true)

            {

                try

                {

                    if (desiredPropertiesData.SendData)

                    {

                        counter++;

                        if (counter == 1)

                        {

                            // first time execution needs to reset the data factory

                            IsReset = true;

                        }

                        var messageBody = TemperatureDataFactory.CreateTemperatureData(counter, generationPolicy, IsReset);

                        IsReset = false;
                        var messageString = JsonConvert.SerializeObject(messageBody);
                        var messageBytes = Encoding.UTF8.GetBytes(messageString);
                        var message = new Message(messageBytes);
                        message.ContentEncoding = "utf-8";
                        message.ContentType = "application/json";
                        //send messages to hub                        
                        await deviceClient.SendEventAsync("temperatureOutput", message);
                        Console.WriteLine($"\t{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToLongTimeString()}> Sending message: {counter}, Body: {messageString}");
                        Thread.Sleep(2000);   
                    }
                }
                catch (Exception ex)

                {

                    Console.WriteLine($"[ERROR] Unexpected Exception {ex.Message}");

                    Console.WriteLine($"\t{ex.ToString()}");

                }
            }
        }

    }

   
}
