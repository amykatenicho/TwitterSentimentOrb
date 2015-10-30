using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure;
using System.Net.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

namespace DisplayAvgSentiment
{
    class Program
    {
        class SentimentData
        {
            public int AverageSentiment { get; set; }
            public string EventHubName { get; set; }
        }


        static void Main(string[] args)
        {
            string ehName = "EVENT HUB NAME";
            string connection = "Endpoint=sb://SERVICEBUSNAMESPACE.servicebus.windows.net/;SharedAccessKeyName=ACCESSKEYNAME;SharedAccessKey=ACCESSKEY;TransportType=Amqp";
            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connection);
            EventHubClient ehub = factory.CreateEventHubClient(ehName);
            EventHubConsumerGroup group = ehub.GetDefaultConsumerGroup();
            EventHubReceiver reciever = group.CreateReceiver("0");


            while (true)
            {
                EventData data = reciever.Receive();
                if (data != null)
                {
                    try
                    {
                        string message = Encoding.UTF8.GetString(data.GetBytes());
                        string value = message.Substring(16,4);
                        var avg = Convert.ToDouble(value);
                        var integerValue = Convert.ToInt16(avg);

                        Console.WriteLine(message);
                        Console.WriteLine(value);
                        Console.WriteLine(integerValue);

                        //var result = Encoding.UTF8.GetString(data.GetBytes());
                        //var eventHubResult = JsonConvert.DeserializeObject<SentimentData>(result);
                        //var score = Convert.ToInt16(eventHubResult.AverageSentiment);
                        //Console.WriteLine(score);
                        //Console.WriteLine();

                        //create sentimentdata object
                        var sentimentData = new SentimentData() { AverageSentiment = integerValue, EventHubName = ehName };

                        //post sentimentdata to api
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri("CLIENT BASE ADDRESS FOR WEB API FOR SENTIMENT DATA");
                            var response = client.PostAsJsonAsync("/api/sentimentdata", sentimentData).Result;
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }


        }
    }
}