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
        //This is the simplified way of doing the commands in Chat Module
        //Say command repeats whatever is said after the command
        [Command("say")]
        public async Task replayAsync([Remainder] string relay)
        {
            await ReplyAsync(relay);
            
        }

        //Changes the current status of the bot
        //This version doesn't come with admin checking so anyone can use it
        [Command("botstatus")]
        public async Task botStatusAsync([Remainder] string botstatus)
        {
            await Context.Client.SetGameAsync(botstatus);
        }
    }
}