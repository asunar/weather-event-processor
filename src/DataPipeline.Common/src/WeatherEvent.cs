namespace DataPipeline.Common
{
    public class WeatherEvent
    {
        public string LocationName { get; set; }
        public double Temperature { get; set; }
        public long Timestamp { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
