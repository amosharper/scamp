using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;

namespace Scamp
{
    public class MiscCommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        private async Task Ping()
        {
            await ReplyAsync("Pong! 🏓 " + Program.client.Latency + "ms");
        }

        [Command("text")]
        [Summary("Returns canned text matching the user's argument.")]
        [Alias("t", "txt", "bark")]
        private async Task CannedText(
            [Summary("The canned text keyword.")] string key = "",
            [Remainder] string restToIgnore = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                await ReplyAsync($"To retrieve canned text, I first need the key.");
                return;
            }

            // Grab a list of the responses with matching trigger keywords; we'll call them all.
            List<CannedResponse> matchingRawResponses = Program.cannedResponses
                .Where(cr => cr.Aliases.Count(a => a.ToString() == key) > 0)
                .ToList();

            // If we know what the phrase is, process and send each canned response, otherwise throw error
            if (matchingRawResponses.Count() > 0)
            {
                foreach (var rawResponse in matchingRawResponses)
                {
                    string processedResponse = CannedResponseHandler.ParseCannedResponse(Context.Guild, rawResponse.CannedResponseText);
                    await ReplyAsync(processedResponse);
                }
            }
            else
            {
                await ReplyAsync($"\"{key}\"? I have no idea what you want from me, {Context.User.Username}.");
            }
        }
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("reload")]
        [Summary("Kills the bot to update the bot config.")]
        [Alias("reboot", "refresh")]
        public async Task Reload()
        {
            await ReplyAsync($"Fare thee well, {Context.User.Username}.");
            Environment.Exit(exitCode: 0);
        }
    }

    /// Example and test commands

    // Test module with no prefix
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        // §say hello world -> hello world
        // Remainder means it treats all of the remaining string as a single argument without needing quote qualifiers
        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string textToEcho)
            => ReplyAsync(textToEcho);

        // Get basic user info
        [Command("userinfo")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync(
            [Summary("The (optional) user to get info from")]
        SocketUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator} is currently {userInfo.Status.ToString().ToLower()}.");
        }
    }

    // Group - effectively a prefixed module
    [Group("math")]
	public class SampleModule : ModuleBase<SocketCommandContext>
	{
		// §math square 20 -> 400
		[Command("square")]
		[Summary("Squares a number.")]
		public async Task SquareAsync([Summary("The number to square.")] int num)
		{
			// We can also access the channel from the Command Context.
			await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
		}

        [Command("multiply")]
        [Summary("Multiplies two numbers.")]
        public async Task MultiplyAsync(
            [Summary("The first number to multiply.")] int numOne,
            [Summary("The second number to multiply.")] int numTwo)
        {
            await Context.Channel.SendMessageAsync($"{numOne} × {numTwo} = {numOne*numTwo}");
        }
    }
}
