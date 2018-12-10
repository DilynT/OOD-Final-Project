using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FinalProjectBot.Modules
{
    public class HelpAndVersions : ModuleBase<SocketCommandContext>
    {
        //using the Ember Builder Feature we can provide a user friendly help menu sent to the direct message
        [Command("help"), Alias("h"),
        Remarks("Sends a direct message to the user who called the command.")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Check private messages for details.");
            var dmUser = await Context.User.GetOrCreateDMChannelAsync();
            var cString = Context.Guild?.Name ?? "Dms with me";

            var newBuild = new EmbedBuilder()
            {
                Title = "Assistance and Support",
                Description = $"These are the commands available for you to use in {cString}",
                Color = new Color(114, 137, 218),
                Timestamp = DateTimeOffset.Now,
                ImageUrl = "https://i.kym-cdn.com/entries/icons/original/000/022/017/thumb.png",
                ThumbnailUrl = Context.User.GetAvatarUrl(),

            };
            

            await dmUser.SendMessageAsync("", false, newBuild.Build());
        }
    }

}
