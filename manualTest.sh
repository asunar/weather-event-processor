# Upload test data to bucket
aws s3 cp  src/BulkEvents/src/test/testData/sampledata.json s3://weather-event-processor-223200533796-us-east-1-start

sam logs -n BulkEventsLambda --stack-name weather-event-processor -s 'yesterday' -e 'tomorrow' --profile personal

sam logs -n SingleEventLambda --stack-name weather-event-processor -s 'yesterday' -e 'tomorrow' --profile personal
# Expected: Received weather event	
