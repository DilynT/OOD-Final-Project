using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class Tester : ModuleBase<SocketCommandContext>
    {
        [Command("tester")]
        public async Task TestAsync()
        {
            await ReplyAsync("Hello World");
        }
    }
}
