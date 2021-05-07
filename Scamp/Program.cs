using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Scamp
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBot().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        private CommandService _commands;
        private IServiceProvider _services;
        public static List<CannedResponse> cannedResponses;

        private BotConfig botConfig;

        // Kick off the bot
        public async Task RunBot()
        {
            client = new DiscordSocketClient(); // Define _client
            _commands = new CommandService(); // Define _commands
            _services = new ServiceCollection() // Define _services
                .AddSingleton(client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            //_cannedResponses = new List<CannedResponse>();

            client.Log += Log; // Logging

            await ReadConfigJson("Config.json"); // Pull in the general config
            await ReadCannedTextJson("CannedResponses.json"); // Pull in the canned response config

            string botToken = botConfig.Token; // Make a string for the token
            if (string.IsNullOrWhiteSpace(botToken))
            {
                _ = Log(new LogMessage(LogSeverity.Error, ToString(), "Authentication token not specified."));
                throw new Exception();
            }

            await RegisterCommandsAsync();
            await client.LoginAsync(TokenType.Bot, botToken); // Log into the bot user
            await client.StartAsync(); // Start the bot user
            await client.SetGameAsync(botConfig.Game); // Set the game the bot is playing
            await Task.Delay(-1); // Delay for -1 to keep the console window opened
        }

        private async Task ReadConfigJson(string configFileName)
        {
            // If there isn't a bot config, create one
            if (!File.Exists(configFileName))
            {
                _ = Log(new LogMessage(LogSeverity.Warning, ToString(), "No general config file exists! Creating."));

                botConfig = new BotConfig()
                {
                    Prefix = "§",
                    Token = "",
                    Game = "Grr"
                };
                File.WriteAllText(configFileName, JsonConvert.SerializeObject(botConfig, Formatting.Indented));
            }
            else
            {
                botConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(configFileName));
                await Log(new LogMessage(LogSeverity.Info, this.ToString(), "General config file found and deserialized."));
            }
        }

        private async Task ReadCannedTextJson(string cannedResponseFileName)
        {
            if (!File.Exists(cannedResponseFileName))
            {
                _ = Log(new LogMessage(LogSeverity.Warning, ToString(), "No canned response config file exists! Creating."));

                cannedResponses = new List<CannedResponse>()
                {
                    new CannedResponse
                    {
                        Aliases = new List<string>
                        {
                            "fu",
                            "foo",
                            "fö"
                        },
                        CannedResponseText = "bar",
                        ContributorOnly = false
                    }
                };
                File.WriteAllText(cannedResponseFileName, JsonConvert.SerializeObject(cannedResponses, Formatting.Indented));
            }
            else
            {
                cannedResponses = JsonConvert.DeserializeObject<List<CannedResponse>>(File.ReadAllText(cannedResponseFileName));
                await Log(new LogMessage(LogSeverity.Info, this.ToString(), "Canned response config file found and deserialized."));
            }
        }

        public async Task ReloadAppAsync()
        {
            botConfig = default(BotConfig);
            await ReadConfigJson("Config.json");

            cannedResponses = default(List<CannedResponse>);
            await ReadCannedTextJson("CannedResponses.json");
        }

        private async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync; // hook up MessageReceived to the command handler

            // Here we discover all of the command modules in the entry assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services: null); // Add module to _commands
        }

        private Task Log(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"{message.Severity} in channel {cmdException.Context.Channel}: {message}");
                Console.WriteLine(cmdException);
            }
            else
            {
                Console.WriteLine($"{message.Severity}: {message}");
            }
            
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage; // Create a variable with the message as SocketUserMessage

            // Filter out empty messages and those sent by bots
            if (message is null || message.Author.IsBot)
            {
                return;
            }

            int argumentPos = 0; // to track where the prefix ends

            // React to pings and the command prefix
            if (message.HasStringPrefix(botConfig.Prefix, ref argumentPos)
                || message.HasMentionPrefix(client.CurrentUser, ref argumentPos))
            {
                _ = Log(new LogMessage(LogSeverity.Info, ToString(), $"Message from user {message.Author} looks like a bot invocation."));

                // Create a WebSocket-based command context based on the message
                var context = new SocketCommandContext(client, message);

                // Execute the command with the command context we just created, along with the service provider for precondition checks.
                var result = await _commands.ExecuteAsync(
                    context: context,
                    argPos: argumentPos,
                    services: _services);

                // Handle unsuccessful results
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                    await message.Channel.SendMessageAsync($"Something went wrong. I just want to bang on my drum.\n```{result.ErrorReason}```");
                }
                else
                {
                    _ = Log(new LogMessage(LogSeverity.Info, ToString(), $"Command processed successfully."));
                }
            }
            else
            {
                // Look for canned response whole-message triggers or partial message triggers
                foreach (var trigger in cannedResponses)
                {
                    if (
                        (trigger.WholeMessageTrigger && trigger.Aliases.Where( a =>
                            message.Content.ToLowerInvariant()
                            .Equals(a.ToLowerInvariant()))
                        .Count() > 0
                        ) || (
                        trigger.PartialMessageTrigger && trigger.Aliases.Where(a =>
                            message.Content.ToLowerInvariant()
                            .Contains(a.ToLowerInvariant()))
                        .Count() > 0
                        ))
                    {
                        await message.Channel.SendMessageAsync(trigger.CannedResponseText);
                    }
                }
            }
        }
    }
}
