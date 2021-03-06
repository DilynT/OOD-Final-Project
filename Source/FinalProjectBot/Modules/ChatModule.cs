﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FinalProjectBot.Services;

namespace FinalProjectBot.Modules
{
    [Name("Chat")]
    [Summary("Chat module to interact with text chat.")]
    public class ChatModule : CustomModule
    {
        private readonly ChatService m_Service;

        public ChatModule(ChatService service)
        {
            m_Service = service;
            m_Service.SetParentModule(this); // Reference to this from the service.
        }

        //Change bot status
        [Command("botGameMessage")]
        [Alias("botGameMessage")]
        
        [Summary("Allows admins to set the bot's current game to [status]")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetBotStatus([Remainder] string botStatus)
        {
            m_Service.SetStatus(botStatus);
            await Task.Delay(0);
        }

        //Repeat content given in Command
        [Command("input")]
        [Alias("giveinput")]
        
        [Summary("The bot will respond in the same channel with the message said.")]
        public async Task Say([Remainder] string userMsg = "No Text Provided")
        {
            m_Service.SayMessage(userMsg);
            await Task.Delay(0);
        }
        //Clear previous messages in the chat
        [Command("Clear")]
        [Remarks("!clear [num]")]
        [Summary("Allows admins to clear [num] amount of messages from current channel")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearMessages([Remainder] int num = 0)
        {
            await m_Service.ClearMessagesAsync(Context.Guild, Context.Channel, Context.User, num);
        }

    }
}
