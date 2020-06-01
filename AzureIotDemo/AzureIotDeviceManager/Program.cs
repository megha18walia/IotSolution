using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AzureIotDeviceManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceConnectionString = "HostName=testfirstiothub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=YF4jJZ3SRKNYOo2pRGk7w0u7hwJ9BT2sWIwlsCCOJvY=";
            var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
            var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);

            var feedback = ReceiveFeedback(serviceClient);
            while(true)
            {
                Console.WriteLine("Which device you wish to send message to? > ");
                var deviceId = Console.ReadLine();
                //await CallDirectMethod(serviceClient, deviceId);
                // await SendCloudToDeviceMessage(serviceClient, deviceId); 
                await UpdateDeviceFirmware(registryManager, deviceId);
            }


        }

        private static async Task UpdateDeviceFirmware(RegistryManager registryManager, string deviceId)
        {
            var deviceTwin = await registryManager.GetTwinAsync(deviceId);
            var twinPatch = new
            {
                properties = new
                {
                    desired = new
                    {
                        firmwareVersion = "4.0"
                    }
                }
            };

            var twinPatchJson = JsonConvert.SerializeObject(twinPatch);
            await registryManager.UpdateTwinAsync(deviceId, twinPatchJson, deviceTwin.ETag);
            Console.WriteLine($"Firmware update sent to device '{deviceId}'");
            while (true)
            {
                var twinData = await registryManager.GetTwinAsync(deviceId);
                Console.WriteLine("Firmware Status : " + twinData.Properties.Reported["firmwareUpdateStatus"]);
                if(twinData.Properties.Reported["firmwareVersion"] == "4.0")
                {
                    Console.WriteLine("Firmware Updated Successfully");
                    break;
                }

            }
        }

        private static async Task SendCloudToDeviceMessage(ServiceClient client, string deviceId)
        {
            Console.WriteLine("Which message would you like to send?");
            var payload = Console.ReadLine();
            var commandMessage = new Message(Encoding.ASCII.GetBytes(payload));
            commandMessage.MessageId = Guid.NewGuid().ToString();
            commandMessage.Ack = DeliveryAcknowledgement.Full;
            commandMessage.ExpiryTimeUtc = DateTime.UtcNow.AddSeconds(120);
            await client.SendAsync(deviceId, commandMessage);
        }
        private static async Task ReceiveFeedback(ServiceClient client)
        {
            var feedback = client.GetFeedbackReceiver();
            while(true)
            {
                var feedbackBatch = await feedback.ReceiveAsync();
                if(feedbackBatch == null)
                {
                    continue;
                }

                foreach( var rec in feedbackBatch.Records)
                {
                    var messageId = rec.OriginalMessageId;
                    var statusCode = rec.StatusCode;

                    Console.WriteLine($"Feedback for message '{messageId}' , status code : '{statusCode}'");
                }

                await feedback.CompleteAsync(feedbackBatch);
            }
        }

        private static async Task CallDirectMethod(ServiceClient client, string deviceId)
        {
            var method = new CloudToDeviceMethod("ShowMessage");
            method.SetPayloadJson("'This is a test message from code'");
            var response = await client.InvokeDeviceMethodAsync(deviceId, method);
            Console.WriteLine($"Response Status : {response.Status}, payload : {response.GetPayloadAsJson()}");
        }
    }
}
