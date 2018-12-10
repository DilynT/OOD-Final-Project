using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class Context : ModuleBase<SocketCommandContext>
    {
        //Uses Context command to showcase what basic information can be pulled from the server regarding the input
        [Command("context")]
        public async Task ContextAsync()
        {
            await ReplyAsync($"{Context.Client.CurrentUser.Mention}   ||   {Context.User.Mention} sent {Context.Message.Content} in {Context.Guild.Name}");
        }
    }
}