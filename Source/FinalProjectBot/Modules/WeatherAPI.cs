using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace FinalProjectBot.Modules
{
    class WeatherAPI : ModuleBase<SocketCommandContext>
    {
        private readonly string _apiKey = "07ea5132956ee811708d6a696589907e";

        [Command("weather")]
        public async Task weatherAsync(string city)
        {
            string _city = city;
            private float = temp;
            private float = tempHigh;
            private float = tempLow;

        public void CheckWeather()
        {
            WeatherAPI DataAPI = new WeatherAPI(city); 

        }
    }

    }
}
