using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using RokuConsole.App.Servies;

namespace roku_console_app
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();

            var awsAccessKey = config.GetValue(typeof(string), "AWS:AccessKey").ToString();
            var awsSecretKey = config.GetValue(typeof(string), "AWS:SecretAccessKey").ToString();

            var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var sqsClient = new AmazonSQSClient(awsCredentials, RegionEndpoint.USEast1);

            var consumeMessageService = new ConsumeMessageService(sqsClient);

            await consumeMessageService.ConsumeMessages();
        }
    }
}
