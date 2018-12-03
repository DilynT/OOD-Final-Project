using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class Context : ModuleBase<SocketCommandContext>
    {
        [Command("context")]
        public async Task ContextAsync()
        {

            await ReplyAsync($"{Context.Client.CurrentUser.Mention}   ||   {Context.User.Mention} sent {Context.Message.Content} in {Context.Guild.Name}");
        }
    }
}