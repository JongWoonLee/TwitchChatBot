using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Text;
using System.Threading;
using TwitchChatBot.Models;

namespace TwitchChatBot
{
    public class PingSender
    {
        private IrcClient irc;
        private Thread pingSender;

        // Empty constructor makes instance of Thread
        public PingSender(IrcClient irc)
        {
            this.irc = irc;
            pingSender = new Thread(new ThreadStart(this.Run));
            Start();
        }

        // Starts the thread
        public void Start()
        {
            pingSender.IsBackground = true;
            pingSender.Start();
        }

        // Send PING to irc server every 1 minutes
        public void Run()
        {
            while (true)
            {
                irc.SendIrcMessage("PING irc.twitch.tv");
                Thread.Sleep(60000); // 1 minutes
            }
        }
    }
}
