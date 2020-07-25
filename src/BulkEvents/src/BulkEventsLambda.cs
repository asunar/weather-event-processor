using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.SimpleNotificationService;
using DataPipeline.Common;

[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace BulkEvents
{
    
    public class BulkEventsLambda
    {
        private readonly AmazonS3Client _s3;
        private readonly AmazonSimpleNotificationServiceClient _sns;
        private readonly string _snsTopic;

        public BulkEventsLambda() : this(new AmazonS3Client(), new AmazonSimpleNotificationServiceClient(), Environment.GetEnvironmentVariable("FAN_OUT_TOPIC"))
        {

        }

        public BulkEventsLambda(AmazonS3Client s3, AmazonSimpleNotificationServiceClient sns, string snsTopic )
        {
            _s3 = s3;
            _sns = sns;

            if(string.IsNullOrEmpty(snsTopic)) throw new ArgumentNullException(nameof(snsTopic));
            _snsTopic = snsTopic;
        }

        public void S3EventHandler(S3Event s3Event)
        {
            var weatherEvents = new List<WeatherEvent>();
            foreach (var eventRecord in s3Event.Records)
            {
                var s3Object = GetObjectFromS3(eventRecord);
                weatherEvents = ReadWeatherEvents(s3Object);
                Console.WriteLine($"Received {weatherEvents.Count} events.");
            }

            weatherEvents.ForEach(PublishToSns);
        }

        private GetObjectResponse GetObjectFromS3(S3EventNotification.S3EventNotificationRecord s3Record)

        {
                var request = new GetObjectRequest
                {
                    BucketName = s3Record.S3.Bucket.Name,
                    Key = s3Record.S3.Object.Key
                };
                return _s3.GetObjectAsync(request).Result;
        }

        public List<WeatherEvent> ReadWeatherEvents(GetObjectResponse response)
        {
                using (var responseStream = response.ResponseStream)
                using (var reader = new StreamReader(responseStream))
                {
                    return JsonSerializer.Deserialize<List<WeatherEvent>>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});
                }
        }

        private void PublishToSns(WeatherEvent weatherEvent)
        {
            var response = _sns.PublishAsync(_snsTopic,JsonSerializer.Serialize(weatherEvent)).Result;
            if(response.HttpStatusCode != HttpStatusCode.OK) Console.WriteLine($"Failed to publish weather event for {weatherEvent.LocationName}");
        }
    }
}
