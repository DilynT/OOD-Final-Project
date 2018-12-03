using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class Embed : ModuleBase<SocketCommandContext>
    {
        [Command("embed")]
        public async Task EmbedAsync()
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Title!")
                .WithDescription("This is sample description")
                .WithColor(Color.Blue);
            //addfield, addinlinefield Fields are Dope

            await ReplyAsync("", false, builder.Build());

        }
    }
}
