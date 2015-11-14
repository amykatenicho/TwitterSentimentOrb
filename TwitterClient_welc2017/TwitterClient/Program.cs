//********************************************************* 
// 
//    Copyright (c) Microsoft. All rights reserved. 
//    This code is licensed under the Microsoft Public License. 
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF 
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY 
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR 
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT. 
// 
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Newtonsoft.Json;

namespace TwitterClient
{
    class Program
    {

        public static int SentimentValue(string inputText, HttpClient httpClient)
        {
            // get sentiment
            string inputTextEncoded = HttpUtility.UrlEncode(inputText);
            string sentimentRequest = "data.ashx/amla/text-analytics/v1/GetSentiment?Text=" + inputTextEncoded;
            var responseTask = httpClient.GetAsync(sentimentRequest);
            responseTask.Wait();
            var response = responseTask.Result;
            var contentTask = response.Content.ReadAsStringAsync();
            contentTask.Wait();
            var content = contentTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Call to get sentiment failed with HTTP status code: " +
                                    response.StatusCode + " and contents: " + content);
            }

            SentimentResult sentimentResult = JsonConvert.DeserializeObject<SentimentResult>(content);
            Console.WriteLine("Sentiment score: " + sentimentResult.Score);
            Console.WriteLine("Tweet: " + inputText);

            int sentimentScore = (int)(sentimentResult.Score * 100);

            if(sentimentScore <= 0)
            {
                sentimentScore = 0;
            }

            return sentimentScore;
        }

        static void Main(string[] args)
        {
            /* Get hashtag phrase that will be listened to / analysed by the Text Analytics API from user input. Basic use is to ask user:
             * Which hashtag do you want to listen to?
             * Which Event Hub do you want to send the sentiment score to?
            */

            //string hashtag = "microsoft";
            //string eventhubName = "";
            //string eventhubConn = "";

            //Console.WriteLine("Enter hashtag phrase (without the #): ");
            //hashtag = Console.ReadLine();
            //ConfigurationManager.AppSettings["twitter_keywords"] = hashtag;
            //Console.WriteLine("Enter Event Hub Name: ");
            //eventhubName = Console.ReadLine();
            //Console.WriteLine("Enter Event Hub Connection String: ");
            //eventhubConn = Console.ReadLine();

            //var config = new EventHubConfig();
            //config.ConnectionString = eventhubConn;
            //config.EventHubName = eventhubName;
            //var myEventHubObserver = new EventHubObserver(config);

            //Console.WriteLine("START\n");


            //Configure Twitter OAuth
            var oauthToken = ConfigurationManager.AppSettings["oauth_token"];
            var oauthTokenSecret = ConfigurationManager.AppSettings["oauth_token_secret"];
            var oauthCustomerKey = ConfigurationManager.AppSettings["oauth_consumer_key"];
            var oauthConsumerSecret = ConfigurationManager.AppSettings["oauth_consumer_secret"];
            var keywords = ConfigurationManager.AppSettings["twitter_keywords"];
            var accountKey = ConfigurationManager.AppSettings["accountkey"];

            //Configure EventHub (comment this out if using user input code above)
            var config = new EventHubConfig();
            config.ConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];
            config.EventHubName = ConfigurationManager.AppSettings["EventHubName"];
            var myEventHubObserver = new EventHubObserver(config);

            //Azure ML Service
            string ServiceBaseUri = "https://api.datamarket.azure.com/";
            var httpClient = new HttpClient();
            
            httpClient.BaseAddress = new Uri(ServiceBaseUri);
            string creds = "AccountKey:" + accountKey;
            string authorizationHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(creds));
            httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            //Original Sentiment demo code
            var datum = Tweet.StreamStatuses(new TwitterConfig(oauthToken, oauthTokenSecret, oauthCustomerKey, oauthConsumerSecret,
            keywords)).Select(tweet => Sentiment.ComputeScore(tweet, keywords)).Select(tweet => new Payload { CreatedAt=Convert.ToDateTime(tweet.CreatedAt),Topic = tweet.Topic, SentimentScore= SentimentValue(tweet.Text,httpClient), Text = tweet.Text});

            datum.ToObservable().Subscribe(myEventHubObserver);


        }

        /// <summary>
        /// Class to hold result of Sentiment call
        /// </summary>
        public class SentimentResult
        {
            public double Score { get; set; }
        }
    }
}
