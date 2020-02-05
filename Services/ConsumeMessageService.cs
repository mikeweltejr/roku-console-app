using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using RestSharp;
using RokuConsole.App.Models;
using static RokuConsole.App.Models.Roku;

namespace RokuConsole.App.Servies
{
    public class ConsumeMessageService
    {
        IAmazonSQS _sqsClient { get; set; }
        public ConsumeMessageService(IAmazonSQS sqsClient)
        {
            _sqsClient = sqsClient;
        }
        public async Task ConsumeMessages()
        {
            var queue = await _sqsClient.GetQueueUrlAsync("my-roku-skill-queue", default(CancellationToken));
            var queueUrl = queue.QueueUrl;

            Console.WriteLine("Consuming Messages...");

            while(true)
            {
                var queueRequest = new ReceiveMessageRequest() {
                    QueueUrl = queueUrl,
                    MessageAttributeNames = new List<string>() { "All" },
                    WaitTimeSeconds = 20,
                    MaxNumberOfMessages = 10
                };

                var response = new ReceiveMessageResponse();

                try
                {
                    response = await _sqsClient.ReceiveMessageAsync(queueRequest);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                var messageBody = string.Empty;
                foreach(var message in response.Messages)
                {
                    messageBody = message.Body;

                    try
                    {
                        messageBody = message.Body;
                        var roku =  JsonConvert.DeserializeObject<Roku>(messageBody);
                        var rokuCommand = roku.Command;
                        var room = roku.Room;

                        var client = new RestClient("http://192.168.86.192:8060");

                        if (room == RoomType.Bedroom) client = new RestClient("http://192.168.86.32:8060");

                        // TODO need to do install function
                        // TODO need to do other functions?

                        if (rokuCommand == CommandType.KeyPress)
                        {
                            var restRequest = new RestRequest($"keypress/{Enum.GetName(typeof(ButtonType), roku.Button)}");
                            var rokuResp = await client.ExecutePostAsync(restRequest);
                        }
                        if (rokuCommand == CommandType.Launch)
                        {
                            await LaunchRokuApp(client, roku);
                        }

                        await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, default(CancellationToken));
                    }
                    catch(Exception ex)
                    {
                        await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, default(CancellationToken));
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        public async Task LaunchRokuApp(RestClient client, Roku roku)
        {
            var restRequest = new RestRequest("query/apps");
            var appResp = await client.ExecuteGetAsync(restRequest);

            var apps = XElement.Parse(appResp.Content);
            var appsList = apps.Elements("app").ToList();

            var app = appsList.FirstOrDefault(a => a.Value.Contains(roku.LaunchApplication));

            if (app != null)
            {
                var appId = app.Attribute("id");

                var launchRestRequest = new RestRequest($"launch/{appId.Value}");
                await client.ExecutePostAsync(launchRestRequest);
            }
        }
    }
}
