using System;
using System.Linq;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using DataPipeline.Common;

[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace SingleEvent
{
    public class SingleEventLambda
    {
        public void SnsEventHandler(SNSEvent snsEvent)
        {
            snsEvent.Records.ToList().ForEach(x =>
            {
                Console.WriteLine($"Message received: {x.Sns.Message}");
                var weatherEvent = JsonSerializer.Deserialize<WeatherEvent>(x.Sns.Message);
                LogEvent(weatherEvent);

            });
        }

        private static void LogEvent(WeatherEvent weatherEvent)
        {
            Console.WriteLine("Received weather event:");
            Console.WriteLine($"Location: {weatherEvent.LocationName}");
            Console.WriteLine($"Temperature: {weatherEvent.Temperature}");
            Console.WriteLine($"Latitude: {weatherEvent.Latitude}");
            Console.WriteLine($"Longitude: {weatherEvent.Longitude}");
        }
    }
}