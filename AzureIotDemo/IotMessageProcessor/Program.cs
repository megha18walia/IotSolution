using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IotMessageProcessor
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            string hubName = "iothub-ehub-testfirsti-3484531-6b930b554a";
            string iotHUbConnectionString = "Endpoint=sb://ihsuprodblres094dednamespace.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=vGviUNQYq4ObkGUmlCiGmyih1hD5ObxShm3wdtFnkMM=;EntityPath=iothub-ehub-testfirsti-3484531-6b930b554a";
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=testazureiothubstorage;AccountKey=iZ0CNasiUXichk09KLT/HzGzTgx6k1Wtf876AXkAY/W3Y0omvSpiJ1PfMnuI4Rm2LkZ0/9eI1gA8otZCQHJ+1Q==;EndpointSuffix=core.windows.net";
            string storageContainerName = "message-processor-host";
            string consumerGroupName = PartitionReceiver.DefaultConsumerGroupName;

            var processor = new EventProcessorHost(hubName, consumerGroupName, iotHUbConnectionString, storageConnectionString, storageContainerName);
            await processor.RegisterEventProcessorAsync<LoggingEventProcessor>();

            Console.WriteLine("Event Processor Started, press enter to exit....");
            Console.ReadLine();

            await processor.UnregisterEventProcessorAsync();

        }
    }
}
