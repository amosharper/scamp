using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace Scamp
{
    class CannedResponseHandler
    {
        public static string ParseCannedResponse(SocketGuild guild, string responseText)
        {
            Regex ItemRegex = new Regex(@"\{.+?\}", RegexOptions.Compiled);

            var modifiedResponseText = responseText;

            foreach (Match match in ItemRegex.Matches(responseText))
            {
                string subOriginalPhrase = match.Value.ToString();
                subOriginalPhrase = subOriginalPhrase.TrimStart('{').TrimEnd('}');

                string subReplacementPhrase = "";

                // TODO: consider how to handle channel mentions within randoms, and similar recursion

                if (subOriginalPhrase.IndexOf(":") < 0)
                {
                    continue;
                }

                // Grab the stated value type from the start of the group
                var subTagType = subOriginalPhrase.Substring(0, subOriginalPhrase.IndexOf(":", StringComparison.Ordinal));

                if (subTagType == null || subTagType.Length == 0)
                {
                    continue;
                }

                // Grab the list of 0 or more values from the end of the group
                var subTagString = subOriginalPhrase.Substring(subTagType.Length + 1, subOriginalPhrase.Length - (subTagType.Length + 1));
                var subTagValues = subTagString.Split(';').ToList();

                // Get rid of any empty values
                subTagValues = subTagValues.Where(v => v.Length > 0).ToList();

                if (subTagValues.Count() == 0)
                {
                    continue;
                }

                // Do the appropriate action depending on the sub tag
                switch (subTagType)
                {
                    case "channel":
                        subReplacementPhrase = GetChannelMentionByName(guild, subTagValues);
                        break;
                    case "random":
                        subReplacementPhrase = GetRandomResult(subTagValues);
                        break;
                    default:
                        break;
                }

                // Replace the first occurrence of the original phrase with the replacement phrase
                modifiedResponseText = ReplaceFirst(modifiedResponseText, $"{{{subOriginalPhrase}}}", subReplacementPhrase);
            }

            return modifiedResponseText;
        }

        private static string GetChannelMentionByName(SocketGuild guild, List<string> channelList)
        {
            var channelMentions = new List<string>();

            // Try to find a channel mention string for each channel string
            foreach (var channelName in channelList)
            {
                channelName.Replace(@"#", string.Empty); // lose any hash symbols

                var channelMatch = guild.Channels.Where(c => c.Name == channelName).SingleOrDefault();

                if (channelMatch == null)
                {
                    channelMentions.Add($"#{channelName}"); // fall back to the plain-text name provided
                }
                else
                {
                    channelMentions.Add(MentionUtils.MentionChannel(channelMatch.Id));
                }
            }

            return string.Join(", ", channelMentions);
        }

        private static string GetRandomResult(List<string> randomStringList)
        {
            var r = new Random();
            int randomItemIndex = r.Next(randomStringList.Count);
            return randomStringList[randomItemIndex];
        }

        // Helper classes

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
