using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class TypeReader : ModuleBase<SocketCommandContext>
    {
        [Command("typeread")]
        public async Task typereadAsync(string name = "Bob")
        {
            await ReplyAsync($"{name} is noobie");
        }
    }
}
