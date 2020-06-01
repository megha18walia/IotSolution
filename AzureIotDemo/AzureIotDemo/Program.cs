using AzureIotCommon;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureIotDemo
{
    class Program
    {
        private const string deviceConnectionString = "HostName=testfirstiothub.azure-devices.net;DeviceId=dummydevice;SharedAccessKey=a+XregThmG5KlyHVzDwvaSdWDZgxhVwvdCjMcnrr/yw=";
        private static DeviceClient _device;
        private static TwinCollection _twinProperties;
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Initiating Band Connection String");
            _device = DeviceClient.CreateFromConnectionString(deviceConnectionString);
            await _device.OpenAsync();
            var recieveEventTask = ReceiveEvents(_device);
            await _device.SetMethodDefaultHandlerAsync(OtherDeviceMethods, null);
            await _device.SetMethodHandlerAsync("ShowMessage", ShowMessage, null);

            Console.WriteLine("Device is connected");
            await UpdateDeviceTwins(_device);
            await _device.SetDesiredPropertyUpdateCallbackAsync(UpdateProperties, null);

            Console.WriteLine("Press a key to perform an action");
            Console.WriteLine("q : to quit the system");
            Console.WriteLine("h : send happy feedback");
            Console.WriteLine("u : send unhappy feedback");
            Console.WriteLine("e : request emergency  help");

            var random = new Random();
            var quitrequested = false;

            while(!quitrequested)
            {
                Console.WriteLine("Select any Action........");
                var input = Console.ReadKey().KeyChar;

                var telemetryData = new Telemetry();
                telemetryData.Latitude = Convert.ToDecimal((random.Next(0, 90)* random.NextDouble()).ToString().Substring(0,9));
                telemetryData.Longitude = Convert.ToDecimal((random.Next(0, 180) * random.NextDouble()).ToString().Substring(0,9));
                telemetryData.Status = StatusType.NotSpecified;
                switch (input)
                {
                    case 'q':
                        quitrequested = true;
                        break;
                    case 'h':
                        telemetryData.Status = StatusType.Happy;
                        var payload = JsonConvert.SerializeObject(telemetryData);
                        var message = new Message(Encoding.ASCII.GetBytes(payload));
                        await _device.SendEventAsync(message);
                        Console.WriteLine("Message sent!");
                        break;
                    case 'u':
                        telemetryData.Status = StatusType.UnHappy;
                        var payload1 = JsonConvert.SerializeObject(telemetryData);
                        var message1 = new Message(Encoding.ASCII.GetBytes(payload1));
                        await _device.SendEventAsync(message1);
                        Console.WriteLine("Message sent!");
                        break;
                    case 'e':
                        telemetryData.Status = StatusType.Emergency;
                        var payload2 = JsonConvert.SerializeObject(telemetryData);
                        var message2 = new Message(Encoding.ASCII.GetBytes(payload2));
                        await _device.SendEventAsync(message2);
                        Console.WriteLine("Message sent!");
                        break;
                    default:
                        Console.WriteLine("Please enter a valid action");
                        break;
                }

                
            }

            //int count = 0;
            //while(true)
            //{
            //    count++;
            //    var telemetry = new Telemetry
            //    {
            //        Message = "Message Accepted from band agent",
            //        StatusCode = count
            //    };

            //    var telemetryJson = JsonConvert.SerializeObject(telemetry);
            //    var message = new Message(Encoding.ASCII.GetBytes(telemetryJson));
            //    await _device.SendEventAsync(message);

            //    Console.WriteLine("Message sent to cloud");
            //    Thread.Sleep(2000);
            //}
            Console.WriteLine("Press any key for exit");
            Console.ReadKey();
        }

        private static async Task UpdateDeviceTwins(DeviceClient client)
        {
            _twinProperties = new TwinCollection();
            _twinProperties["firmwareVersion"] = "1.0";
            _twinProperties["firmwareUpdateStatus"] = "n/a";
            await client.UpdateReportedPropertiesAsync(_twinProperties);
        }

        public static async Task ReceiveEvents(DeviceClient client)
        {
            while(true)
            {
                var message = await client.ReceiveAsync();
                if(message == null)
                {
                    continue;
                }
                var messageBody = Encoding.ASCII.GetString(message.GetBytes());
                
                Console.WriteLine($"Received message from cloud : {messageBody}");
                await client.CompleteAsync(message);

            }
        } 
        
        private static Task<MethodResponse> ShowMessage(MethodRequest request, object userContext)
        {
            Console.WriteLine("*** MESSAGE RECEIVED ****");
            Console.WriteLine(request.DataAsJson);

            var responsePayload = Encoding.ASCII.GetBytes("{\"response\" : \"Message Shown! \"}");
            return Task.FromResult(new MethodResponse(responsePayload, 200));
        }

        private static Task<MethodResponse> OtherDeviceMethods(MethodRequest request, object userContext)
        {
            Console.WriteLine("*** OTHER METHOD CALLED ***");
            Console.WriteLine($"Method Name : {request.Name}");
            Console.WriteLine($"Payload : {request.DataAsJson}");

            var requestPayload = Encoding.ASCII.GetBytes("{\"response\" : \" This Method is not found\" }");
            return Task.FromResult(new MethodResponse(requestPayload, 404));
        }

        private static Task UpdateProperties(TwinCollection desiredProperties, object userContext)
        {
            var currentFirmwareVersion = (string)_twinProperties["firmwareVersion"];
            var reportedFirmwareVersion = (string)desiredProperties["firmwareVersion"];

            if(currentFirmwareVersion != reportedFirmwareVersion)
            {
                Console.WriteLine($"Firmware Update Requested. Current Version : {currentFirmwareVersion}, Desired Firmware Version : {reportedFirmwareVersion}");
                ApplyFirmwareUpdate(reportedFirmwareVersion);

            }

            return Task.CompletedTask;

        }

        private static async Task ApplyFirmwareUpdate(string targetversion)
        {
            Console.WriteLine("Beginning Firmware Update...");

            _twinProperties["firmwareUpdateStatus"] = $"Downloading zip for firmware {targetversion}";
            await _device.UpdateReportedPropertiesAsync(_twinProperties);
            Thread.Sleep(5000);

            _twinProperties["firmwareUpdateStatus"] = $"Unzipping Package";
            await _device.UpdateReportedPropertiesAsync(_twinProperties);
            Thread.Sleep(5000);

            _twinProperties["firmwareUpdateStatus"] = $"Applying Updates";
            await _device.UpdateReportedPropertiesAsync(_twinProperties);
            Thread.Sleep(5000);

            Console.WriteLine("Firmware Update completed");

            _twinProperties["firmwareUpdateStatus"] = "n/a";
            _twinProperties["firmwareVersion"] = targetversion;
            await _device.UpdateReportedPropertiesAsync(_twinProperties);
            Thread.Sleep(5000);
        }
    }
}
