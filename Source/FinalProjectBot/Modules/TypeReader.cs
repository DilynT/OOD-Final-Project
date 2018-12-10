using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class TypeReader : ModuleBase<SocketCommandContext>
    {
        //Simple Command that takes input from command and attaches it to a prebuilt message
        [Command("typeread")]
        public async Task typereadAsync(string name = "Bob")
        {
            await ReplyAsync($"{name} is attempting to leave a message");
        }
    }
}
