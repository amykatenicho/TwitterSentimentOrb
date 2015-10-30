using System;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using System.Net.Http;
using Newtonsoft.Json.Linq;

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
                        var result = Encoding.UTF8.GetString(data.GetBytes());
                        dynamic resultJson = JObject.Parse(result);
                        var avgScore = resultJson.avgsentiment;

                        Console.WriteLine(result);
                        Console.WriteLine("Average Score: " + avgScore + "\n");

                        //create sentimentdata object
                        var sentimentData = new SentimentData() { AverageSentiment = avgScore, EventHubName = ehName };

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