using AzureIotCommon;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace IotMessageProcessor
{
    class LoggingEventProcessor : IEventProcessor
    {
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"LoggingEventProcessor closed, processing partition: {context.PartitionId}, reason : {reason}");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine($"LoggingEventProcessor opened, processing partition: {context.PartitionId}");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"LoggingEventProcessor error, processing partition: {context.PartitionId}, reason : {error.Message}");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            Console.WriteLine($"Batch of events received on partition, {context.PartitionId}");

            foreach(var eventdata in messages)
            {
                var payload = Encoding.ASCII.GetString(eventdata.Body.Array, eventdata.Body.Offset, eventdata.Body.Count);
                var deviceId = eventdata.SystemProperties["iothub-connection-device-id"];
                Console.WriteLine($"Message received on partition '{context.PartitionId}', deviceId : {deviceId}, payload : {payload} ");
                var data = JsonConvert.DeserializeObject<Telemetry>(payload);
                
                var address = getAddress(Convert.ToDouble(data.Latitude.ToString().Length > 9 ? data.Latitude.ToString().Substring(0,9) : data.Latitude.ToString()), Convert.ToDouble(data.Latitude.ToString().Length > 9 ? data.Longitude.ToString().Substring(0,9): data.Latitude.ToString()));
                string loc = $"{address.address?.country ?? ""}, {address.address?.state ?? ""}, {address.address?.city ?? ""}, {address.address?.suburb ?? ""}, {address.address?.road ?? ""}, {address.address?.postcode ?? ""}";
                switch (data.Status)
                {
                    case StatusType.Emergency:
                        Console.WriteLine($"person at localtion : {loc} is in Emergency, Please help");
                        break;
                    case StatusType.Happy:
                        Console.WriteLine($"person at localtion : {loc} is feeling Happy");
                        break;
                    case StatusType.UnHappy:
                        Console.WriteLine($"person at localtion : {loc} is Feeling UnHappy");
                        break;

                }
            }
            return context.CheckpointAsync(); 
        }

        public static RootObject getAddress(double lat, double lon)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.Headers.Add("Referer", "http://www.microsoft.com");
            var jsonData = webClient.DownloadData("http://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lon);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RootObject));
            RootObject rootObject = (RootObject)ser.ReadObject(new MemoryStream(jsonData));
            return rootObject;

        }


    }
}
