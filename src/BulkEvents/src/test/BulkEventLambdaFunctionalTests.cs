using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.SimpleNotificationService;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace BulkEvents.Tests
{
    public class BulkEventLambdaFunctionalTests
    {
        private readonly S3Event _fakeS3Event;
        private readonly AmazonSimpleNotificationServiceClient _fakeSns;
        private readonly AmazonS3Client _fakeS3;
        private readonly GetObjectRequest _s3GetObjectRequest;
        private readonly string _snsTopic;
        // ReSharper disable once InconsistentNaming
        private const string TEST_DATA_PATH = "../../../testData";

        public BulkEventLambdaFunctionalTests()
        {
            _fakeS3Event = new S3Event()
            {
                Records = new List<S3EventNotification.S3EventNotificationRecord>
                {
                    new S3EventNotification.S3EventNotificationRecord
                    {
                        S3 = new S3EventNotification.S3Entity
                        {
                            Bucket = new S3EventNotification.S3BucketEntity
                            {
                                Name = "fakeBucket"
                            },
                            Object = new S3EventNotification.S3ObjectEntity
                            {
                                Key = "fakeKey"
                            }
                        }
                    }
                }
            };
            _fakeSns = A.Fake<AmazonSimpleNotificationServiceClient>();
            _fakeS3 = A.Fake<AmazonS3Client>();
            _s3GetObjectRequest = new GetObjectRequest()
            {
                BucketName = "fakeBucket",
                Key = "fakeKey"
            };
            _snsTopic = "test-topic";
        }

        [Fact]
        public void ShouldPublishForEachWeatherEvent()
        {

            // Create mock object response from sampledata.json
            var s3ResponseWithSampleData = new GetObjectResponse
            {
                ResponseStream = File.OpenRead(Path.Combine(TEST_DATA_PATH, "sampledata.json"))
            };

            // Tell fake S3 to return to return the mock object response created above.
            A.CallTo(() => 
                _fakeS3.GetObjectAsync(A<GetObjectRequest>.That.Matches(x => x.BucketName == _s3GetObjectRequest.BucketName && x.Key == _s3GetObjectRequest.Key), A<CancellationToken>._))
                .Returns(Task.FromResult(s3ResponseWithSampleData));


            var bulkEventsLambda = new BulkEventsLambda(_fakeS3, _fakeSns, _snsTopic);
            bulkEventsLambda.S3EventHandler(_fakeS3Event);

            // 3 events in sampledata.json => expect 3 sns publish calls.
            A.CallTo(() =>
                    _fakeSns.PublishAsync(_snsTopic, A<string>._, CancellationToken.None))
                .MustHaveHappened(3, Times.Exactly);


            var expectedMessage = "{\"LocationName\":\"New York, NY\",\"Temperature\":91,\"Timestamp\":1564428897,\"Longitude\":-73.99,\"Latitude\":40.7}" ;
            A.CallTo(() =>
                    _fakeSns.PublishAsync(_snsTopic, expectedMessage, CancellationToken.None))
                .MustHaveHappenedOnceExactly();

        }

        [Fact]
        public void ShouldThrowWithInvalidJson()
        {
            var s3ResponseWithBadData = new GetObjectResponse
            {
                ResponseStream = File.OpenRead(Path.Combine(TEST_DATA_PATH, "baddata.json"))
            };

            // Tell fake S3 to return to return the mock object response created above.
            A.CallTo(() => 
                    _fakeS3.GetObjectAsync(A<GetObjectRequest>.That.Matches(x => x.BucketName == _s3GetObjectRequest.BucketName && x.Key == _s3GetObjectRequest.Key), A<CancellationToken>._))
                .Returns(Task.FromResult(s3ResponseWithBadData));

            var bulkEventsLambda = new BulkEventsLambda(_fakeS3, _fakeSns, _snsTopic);
            Action act = () => bulkEventsLambda.S3EventHandler(_fakeS3Event);

            act.Should().Throw<System.Text.Json.JsonException>();

        }
    }
}