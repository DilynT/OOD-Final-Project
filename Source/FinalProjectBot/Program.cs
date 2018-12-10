#define DEBUG_VERBOSE
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;
using FinalProjectBot.Services;
using FinalProjectBot.Modules;
using FinalProjectBot.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FinalProjectBot
{
    class Program
    {
        //Reroutes the functionality of Main to RunBotAsync()
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();


        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        //Main Body Of Program, Establishes Initial Connection
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<AudioService>()
                .AddSingleton<ChatService>()
                .AddSingleton<CustomService>()
                .BuildServiceProvider();

            string botPrefix = Config.Token;

            //Event Subscriptions
            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botPrefix);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        //Quick message for when a new user joins the chat
        private async Task AnnounceUserJoined(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.DefaultChannel;
            await channel.SendMessageAsync($"New Member, {user.Mention}, has joined the chat. May god have mercy.");
        }
        
        //Basic Event logging to report errors in commands
        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        //Registers command input
        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

        }

        //Establishes that a command of some kind was given and sorts out the prefix
        public async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot)
            {
                return;
            }

            int argPos = 0;

            if (message.HasStringPrefix("!", ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_client, message);

                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }

        }
    }
}
