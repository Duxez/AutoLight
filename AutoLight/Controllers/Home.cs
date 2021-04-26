using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

        private readonly ILogger<HomeController> _logger;
        private List<string> Jobs = new List<string>();
        
        //Home Assistant Token
        private const string token =
            "";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var sunsetRoute = "https://api.sunrise-sunset.org/json?lat=51.5562404&lng=5.0886014";
            var client = new HttpClient();
            SwitchLightJob("on");
            var timesResult = await client.GetAsync(sunsetRoute);
            var sunriseSunset =
                JsonConvert.DeserializeObject<SunriseSunset>(await timesResult.Content.ReadAsStringAsync());
            var sunrise = DateTime.Parse(sunriseSunset.Results.Sunrise).ToLocalTime();
            var sunset = DateTime.Parse(sunriseSunset.Results.Sunset).ToLocalTime();

            if (DateTime.Now < sunrise)
            {
                _logger.LogInformation("Scheduling light to turn off at sunrise");
                SwitchLightJob("on");
                var job = BackgroundJob.Schedule(() => SwitchLightJob("off"), sunrise);
                WriteToFile(job);
            }
            else if (DateTime.Now > sunrise && DateTime.Now < sunset)
            {
                _logger.LogInformation("Scheduling light to turn on at sunset");
                SwitchLightJob("off");
                var job = BackgroundJob.Schedule(() => SwitchLightJob("on"), sunset);
                WriteToFile(job);
            }
            else if (DateTime.Now > sunset)
            {
                _logger.LogInformation("Scheduling light to turn off at sunrise");
                SwitchLightJob("on");
                var secondTimeResult = await client.GetAsync($"{sunsetRoute}&date={DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}");
                var nextDay = JsonConvert.DeserializeObject<SunriseSunset>(await secondTimeResult.Content.ReadAsStringAsync());
                var job = BackgroundJob.Schedule(() => SwitchLightJob("off"), DateTime.Parse(nextDay.Results.Sunrise).ToLocalTime());
                WriteToFile(job);
            }
            return $"Working on it";
        }

        [HttpGet]
        public async Task<string> TurnOff()
        {
            var result = await SwitchLightJob("off");
            using (StreamReader r = new StreamReader("jobs.txt"))
            {
                var all = r.ReadToEnd().Split(",");
                foreach (var job in all)
                {
                    if(!string.IsNullOrEmpty(job))
                        BackgroundJob.Delete(job);
                }
                
                r.Dispose();
            }
            System.IO.File.Delete("jobs.txt");
            return $"{result.StatusCode}: {await result.Content.ReadAsStringAsync()}";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> SwitchLightJob(string state)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{token}");
            //entity to switch state of
            var entity_id = "{\"entity_id\": \"light.yeelight_color_0x00000000080192ae\"}";
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
    }
}