using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FinalProjectBot.Modules
{
    public class CompactChatModule : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        public async Task replayAsync([Remainder] string relay)
        {
            await ReplyAsync(relay);
            
        }

        [Command("botstatus")]
        public async Task botStatusAsync([Remainder] string botstatus)
        {
            await Context.Client.SetGameAsync(botstatus);
        }
    }
}