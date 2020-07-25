# Upload test data to bucket
aws s3 cp  src/BulkEvents/src/test/testData/sampledata.json s3://chapter6-data-pipeline-223200533796-us-east-1-start --profile personal

sam logs -n BulkEventsLambda --stack-name chapter6-data-pipeline -s 'yesterday' -e 'tomorrow' --profile personal

sam logs -n SingleEventLambda --stack-name chapter6-data-pipeline -s 'yesterday' -e 'tomorrow' --profile personal
# Expected: Received weather event	
