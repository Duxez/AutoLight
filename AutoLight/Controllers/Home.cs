using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoLight.Models;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AutoLight.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private string[] groupToMatch = { "2", "3", "5", "6", "7"};
        // private string[] codesToMatch = { "804" };
        private readonly ILogger<HomeController> _logger;
        private List<string> Jobs = new List<string>();
        
        //Home Assistant Token
        private const string token =
            "";

        //OpenWeathMap API key & City
        private const string weatherApiKey = "";
        private const string city = "";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            await SwitchLightJob("on");
            CreateJob();
            return $"Working on it";
        }

        private async void CreateJob()
        {
            var sunsetRoute = "https://api.sunrise-sunset.org/json?lat=51.5562404&lng=5.0886014";
            var client = new HttpClient();
            var timesResult = await client.GetAsync(sunsetRoute);
            var sunriseSunset =
                JsonConvert.DeserializeObject<SunriseSunset>(await timesResult.Content.ReadAsStringAsync());
            var sunrise = DateTime.Parse(sunriseSunset.Results.Sunrise).ToLocalTime();
            var sunset = DateTime.Parse(sunriseSunset.Results.Sunset).ToLocalTime();

            if (DateTime.Now < sunrise)
            {
                _logger.LogInformation("Scheduling light to turn off at sunrise");
                await SwitchLightJob("on");
                var job = BackgroundJob.Schedule(() => SwitchLightJob("off"), sunrise);
                WriteToFile(job);
            }
            else if (DateTime.Now > sunrise && DateTime.Now < sunset)
            {
                _logger.LogInformation("Scheduling light to turn on at sunset");
                await SwitchLightJob("off");
                var job = BackgroundJob.Schedule(() => SwitchLightJob("on"), sunset.AddHours(-1));
                _logger.LogInformation("Job ID: {0}", job);
                WriteToFile(job);
                
                RecurringJob.AddOrUpdate("WeatherCheck", () => CheckWeather(), Cron.Minutely);
            }
            else if (DateTime.Now > sunset)
            {
                _logger.LogInformation("Scheduling light to turn off at sunrise");
                await SwitchLightJob("on");
                var secondTimeResult = await client.GetAsync($"{sunsetRoute}&date={DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}");
                var nextDay = JsonConvert.DeserializeObject<SunriseSunset>(await secondTimeResult.Content.ReadAsStringAsync());
                var job = BackgroundJob.Schedule(() => SwitchLightJob("off"), DateTime.Parse(nextDay.Results.Sunrise).ToLocalTime());
                WriteToFile(job);
            }
        }

        [HttpGet]
        public async Task<string> TurnOff()
        {
            var result = await SwitchLightJob("off");
            ClearJobs();

            return $"{result.StatusCode}: {await result.Content.ReadAsStringAsync()}";
        }

        private void ClearJobs()
        {
            RecurringJob.RemoveIfExists("WeatherCheck");
            if (System.IO.File.Exists("jobs.txt"))
            {
                using (StreamReader r = new StreamReader("jobs.txt"))
                {
                    var all = r.ReadToEnd().Split(",");
                    foreach (var job in all)
                    {
                        if (!string.IsNullOrEmpty(job))
                            BackgroundJob.Delete(job);
                    }

                    r.Dispose();
                }
                System.IO.File.Delete("jobs.txt");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> SwitchLightJob(string state)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{token}");
            //entity to switch state of
            var entity_id = "{\"entity_id\": \"light.office_light\"}";
            HttpContent content = new StringContent(entity_id, Encoding.UTF8, "application/json");
            //Home Assistant api url:
            return await client.PostAsync(new Uri($"http://localhost:8123/api/services/light/turn_{state}"), content);
        }

        private void WriteToFile(string jobId)
        {
            using (StreamWriter w = System.IO.File.AppendText("jobs.txt"))
            {
                w.Write($"{jobId},");
                w.Dispose();
            }
        }
        [HttpGet]
        public async Task CheckWeather()
        {
            bool turnOff = true;
            var client = new HttpClient();
            var result =
                await client.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={weatherApiKey}");
            var weatherResult = JsonConvert.DeserializeObject<WeatherResult>(await result.Content.ReadAsStringAsync());
            foreach (var group in groupToMatch)
            {
                if (weatherResult.Weathers[0].Id.ToString().StartsWith(group))
                {
                    await SwitchLightJob("on");
                    turnOff = false;
                    break;
                }
            }

            if (turnOff)
                await SwitchLightJob("off");
        }
    }
}