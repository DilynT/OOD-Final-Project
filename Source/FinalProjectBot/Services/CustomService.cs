using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using FinalProjectBot.Modules;

namespace FinalProjectBot.Services
{
    // Enum to direct the string to output. Reference Log()
    //Most Not used at the moment, but here for future uses.
    public enum E_LogOutput { Console, Reply, Playing };

    /**
     * CustomService
     * Class that handles serves as a wrapper for services.
     * Add shared functionality here and shared properties between all services.
     */
    public class CustomService
    {
        //We have a reference to the parent module to perform actions like replying and setting the current game properly
        private CustomModule m_ParentModule = null;

        // This should always be called in the module constructor to 
        // provide a direct reference to the parent module.
        public void SetParentModule(Modules.CustomModule parent) { m_ParentModule = parent; }

        // Replies in the text channel using the parent module and optional embed.
        protected async void DiscordReply(string s, EmbedBuilder emb = null)
        {
            if (m_ParentModule == null) return;
            if (emb != null)
                await m_ParentModule.ServiceReplyAsync(s, emb);
            else
                await m_ParentModule.ServiceReplyAsync(s);
        }

        //  Sets the playing status using the parent module.
        protected async void DiscordPlaying(string s)
        {
            if (m_ParentModule == null) return;
            await m_ParentModule.ServicePlayingAsync(s);
        }

        protected void Log(string s, int output = (int)E_LogOutput.Console)
        {
            string withDate = $"{DateTime.Now.ToString("hh:mm:ss")} DiscordBot {s}";
#if (DEBUG_VERBOSE)
            Console.WriteLine("AudioService [DEBUG] -- " + str);
#endif

        }

    }
}
