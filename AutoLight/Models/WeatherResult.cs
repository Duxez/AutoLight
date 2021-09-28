using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutoLight.Models
{
    public struct Coords
    {
        public float Lon { get; set; }
        public float Lan { get; set; }
    }

    public struct Weather
    {
        public uint Id { get; set; }
        public string Main { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public struct Main
    {
        public float Temp { get; set; }
        [JsonProperty("feels_like")]
        public float FeelsLike { get; set; }
        [JsonProperty("temp_min")]
        public float TempMin { get; set; }
        [JsonProperty("temp_max")]
        public float TempMax { get; set; }
        public float Pressure { get; set; }
        public float Humidity { get; set; }
    }

    public struct Wind
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
        public double Gust { get; set; }
    }

    public struct Clouds
    {
        public int All { get; set; }
    }

    public struct Sys
    {
        public int Type { get; set; }
        public uint Id { get; set; }
        public string Country { get; set; }
        public uint Sunrise { get; set; }
        public uint Sunset { get; set; }
    }
    
    public class WeatherResult
    {
        public Coords Coords { get; set; }
        [JsonProperty("weather")]
        public List<Weather> Weathers { get; set; }
        public string Base { get; set; }
        public Main Main { get; set; }
        public float Visibilty { get; set; }
        public Wind Wind { get; set; }
        public Clouds Clouds { get; set; }
        public uint Dt { get; set; }
        public Sys Sys { get; set; }
        public uint Timezone { get; set; }
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint Cod { get; set; }
    }
}