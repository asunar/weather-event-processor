using System;
using System.IO;
using Amazon.S3.Model;
using FluentAssertions;
using Xunit;

namespace BulkEvents.Tests
{
    public class BulkEventLambdaUnitTests
    {
        // ReSharper disable once InconsistentNaming
        private const string TEST_DATA_PATH = "../../../testData";
        [Fact]
        public void ShouldReadWeatherEvents()
        {
            var bulkEventsLambda = new BulkEventsLambda(null, null, "dummyTopic");
            var mockS3Response = new GetObjectResponse
            {
                ResponseStream = File.OpenRead(Path.Combine(TEST_DATA_PATH, "sampledata.json"))
            };

            var weatherEvents = bulkEventsLambda.ReadWeatherEvents(mockS3Response);

            weatherEvents.Count.Should().Be(3);

            weatherEvents[0].LocationName.Should().Be("New York, NY");
            weatherEvents[0].Temperature.Should().Be(91);
            weatherEvents[0].Timestamp.Should().Be(1564428897);
            weatherEvents[0].Latitude.Should().Be(40.70);
            weatherEvents[0].Longitude.Should().Be(-73.99);
        }

        [Fact]
        public void ShouldThrowWithBadData()
        {
            var mockS3Response = new GetObjectResponse
            {
                ResponseStream = File.OpenRead(Path.Combine(TEST_DATA_PATH, "baddata.json"))
            };

            var lambda = new BulkEventsLambda(null, null, "dummy");

           Action act = () => lambda.ReadWeatherEvents(mockS3Response);
           act.Should().Throw<System.Text.Json.JsonException>();
        }

    }
}