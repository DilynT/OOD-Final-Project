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
        //Short Test of Embed feature in the discord libraries
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
