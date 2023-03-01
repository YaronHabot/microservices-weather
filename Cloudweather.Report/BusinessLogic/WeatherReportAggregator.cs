using Cloudweather.Report.Config;
using Cloudweather.Report.DataAccess;
using Cloudweather.Report.Models;
using Cloudweather.Temperature.DataAccess;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Cloudweather.Report.BusinessLogic
{
    /// <summary>
    /// Aggregates data from multiple external sources to build a weather report
    /// </summary>
    public interface IWeatherReportAggregator
    {
        /// <summary>
        /// Builds and returns a weather report.
        /// Persists WeatherReport data
        /// </summary>
        /// <param name="zip"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public Task<WeatherReport> BuildReport(string zip, int days);
    }

    public class WeatherReportAggregator : IWeatherReportAggregator
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<WeatherReportAggregator> _logger;
        private readonly WeatherDataConfig _weatherDataConfig;
        private readonly WeatherReportDbContext _db;

        public WeatherReportAggregator(
            IHttpClientFactory http, 
            ILogger<WeatherReportAggregator> logger, 
            IOptions<WeatherDataConfig> weatherConfig, 
            WeatherReportDbContext db)
        {
            _http = http;
            _logger = logger;
            _weatherDataConfig = weatherConfig.Value;
            _db = db;
        }

        public async Task<WeatherReport> BuildReport(string zip, int days)
        {
            var httpClient = _http.CreateClient();
            var precipData = await FetchPrecipitationData(httpClient, zip, days);
            var totalSnow = GetTotalSnow(precipData);
            var totalRain = GetTotalRain(precipData);
            _logger.LogInformation(
                $"zip: {zip} over last {days} days: " +
                $"total snow: {totalSnow}, rain: {totalRain}"
            );

            var tempData = await FetchTemperatureData(httpClient, zip, days);
            var averageHighTemp = tempData.Average(t => t.TempHighF);
            var averageLowTemp = tempData.Average(t => t.TempLowF);
            _logger.LogInformation(
                $"zip: {zip} over last {days} days: " +
                $"low temperature: {averageLowTemp}, high temperature: {averageHighTemp}"
            );

            var weatherReport = new WeatherReport
            {
                AverageHighF = Math.Round(averageHighTemp, 1),
                AverageLowF = Math.Round(averageLowTemp, 1),
                RainfallTotalInches = totalRain,
                SnowTotalInches = totalSnow,
                ZipCode = zip,
                CreatedOn = DateTime.UtcNow,
            };

            //TODO: Use cached weather reports instead of making round trips
            _db.Add(weatherReport);
            await _db.SaveChangesAsync();

            return weatherReport;
        }

        private static decimal GetTotalSnow(IEnumerable<PrecipitationModel> precipData)
        {
            var totalSnow = precipData
                .Where(p => p.WeatherType == "snow")
                .Sum(p => p.AmountInches);
            return Math.Round(totalSnow, 1);
        }

        private static decimal GetTotalRain(IEnumerable<PrecipitationModel> precipData)
        {
            var totalSnow = precipData
                .Where(p => p.WeatherType == "rain")
                .Sum(p => p.AmountInches);
            return Math.Round(totalSnow, 1);
        }

        private async Task<List<TemperatureModel>> FetchTemperatureData(HttpClient httpClient, string zip, int days)
        {
            var endpoint = BuildTemperatureServiceEndpoint(zip, days);
            var temperatureRecords = await httpClient.GetAsync(endpoint);
            var jsonSerializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var temperatureData = await temperatureRecords
                .Content
                .ReadFromJsonAsync<List<TemperatureModel>>(jsonSerializationOptions);
            return temperatureData ?? new List<TemperatureModel>();
        }

        private string BuildTemperatureServiceEndpoint(string zip, int days)
        {
            var protocol = _weatherDataConfig.TempDataProtocoal;
            var host = _weatherDataConfig.TempDataHost;
            var port = _weatherDataConfig.TempDataPort;
            return $"{protocol}://{host}:{port}/observation/{zip}?days={days}";
        }

        private async Task<List<PrecipitationModel>> FetchPrecipitationData(HttpClient httpClient, string zip, int days)
        {
            var endpoint = BuildPrecipitationServiceEndpoint(zip, days);
            var precipRecords = await httpClient.GetAsync(endpoint);
            var jsonSerializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var precipData = await precipRecords
                .Content
                .ReadFromJsonAsync<List<PrecipitationModel>>(jsonSerializationOptions);
            return precipData ?? new List<PrecipitationModel>();

        }

        private string BuildPrecipitationServiceEndpoint(string zip, int days)
        {
            var protocol = _weatherDataConfig.PrecipDataProtocoal;
            var host = _weatherDataConfig.PrecipDataHost;
            var port = _weatherDataConfig.PrecipDataPort;
            return $"{protocol}://{host}:{port}/observation/{zip}?days={days}";
        }

    }
}
