using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Xunit;

namespace DataPipelineTests
{
    public class DataPipelineTest
    {
        private readonly string _stackName;
        private readonly AmazonCloudFormationClient _cloudFormationClient;
        private AmazonCloudWatchLogsClient _logsClient;
        private const string TEST_DATA_PATH = "../../../testData";

        public DataPipelineTest(string stackName="chapter6-data-pipeline")
        {
            if(string.IsNullOrEmpty(stackName)) throw new ArgumentNullException(nameof(stackName));
            _stackName = stackName;
            _cloudFormationClient = new AmazonCloudFormationClient();
        }

        private string ResolvePhysicalId(string logicalId)
        {
            var req = new DescribeStackResourceRequest()
                {StackName = _stackName, LogicalResourceId = logicalId};
            var result = _cloudFormationClient.DescribeStackResourceAsync(req).Result;

            return result.StackResourceDetail.PhysicalResourceId;
        }

        private List<string> GetLogMessages(string lambdaName)
        {
            var logGroup = GetLogGroup(lambdaName);
            _logsClient = new AmazonCloudWatchLogsClient();

            var logStreams = _logsClient
                .DescribeLogStreamsAsync(new DescribeLogStreamsRequest(logGroup)).Result
                .LogStreams;

            var responses = new List<GetLogEventsResponse>();
            foreach (var logStream in logStreams)
            {
                responses.Add(_logsClient.GetLogEventsAsync(new GetLogEventsRequest(logGroup, logStream.LogStreamName)).Result);
            }

            // Each response has many events so
            // simple Select would yield a nested array [[event1, event2,...event99] , [event101, event102,...event199] , [event200, event201,...event299]]
            // SelectMany flattens the array [event1, event2,...event299]]
            var messages = responses.SelectMany(x => x.Events)
                .Where(x => x.Message.StartsWith("Location:"))
                .Select(x => x.Message)
                .ToList();

            return messages;
        }

        private string GetLogGroup(string lambdaName)
        {
            return $"/aws/lambda/{lambdaName}";
        }

        [Fact]
        public void DataPipelineEndtoEndTest()
        {
            string bucketName = ResolvePhysicalId("PipelineStartBucket");
            string key = Guid.NewGuid().ToString();

            var s3 = new AmazonS3Client();

            s3.PutObjectAsync(new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = key,
                FilePath = Path.Combine(TEST_DATA_PATH, "sampledata.json")
            });

            Thread.Sleep(30000);

            var messages = GetLogMessages(ResolvePhysicalId("SingleEventLambda"));
            messages.Should().ContainMatch("*New York, NY E2E Test*");
            messages.Should().ContainMatch("*Manchester, UK E2E Test*");
            messages.Should().ContainMatch("*Arlington, VA E2E Test*");


            // 3. Delete object from S3 bucket (to allow a clean CloudFormation teardown)
            s3.DeleteObjectAsync(bucketName, key);

            // 4. Delete Lambda log groups
            var singleEventLambda = ResolvePhysicalId("SingleEventLambda");
            _logsClient.DeleteLogGroupAsync(
                new DeleteLogGroupRequest(GetLogGroup(singleEventLambda)));

            var bulkEventsLambda = ResolvePhysicalId("BulkEventsLambda");
            _logsClient.DeleteLogGroupAsync(
                new DeleteLogGroupRequest(GetLogGroup(bulkEventsLambda)));
        }

    }
}
