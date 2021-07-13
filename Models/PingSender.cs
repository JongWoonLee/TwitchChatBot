﻿using System;
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
        private IrcClient IrcClient;
        private Thread SendPingEveryMinute;
        private bool ThreadDoWorkRun;


        // Empty constructor makes instance of Thread
        public PingSender(IrcClient IrcClient)
        {
            this.IrcClient = IrcClient;
            this.ThreadDoWorkRun = true;
            SendPingEveryMinute = new Thread(new ThreadStart(this.Run));
            Start();
        }

        // Starts the thread
        public void Start()
        {
            SendPingEveryMinute.IsBackground = true;
            SendPingEveryMinute.Start();
        }

        // Send PING to irc server every 1 minutes
        public void Run()
        {
            while (ThreadDoWorkRun)
            {
                try
                {
                    IrcClient.SendIrcMessage("PING irc.twitch.tv");
                    Thread.Sleep(60000); // 1 minutes
                }
                catch (ThreadInterruptedException E)
                {
                    Console.WriteLine(E.Message);
                    Console.WriteLine("newThread inturrupted");
                }
            }
        }

        public void StopDoWork()
        {
            if (ThreadDoWorkRun)
            {
                ThreadDoWorkRun = false;
                SendPingEveryMinute.Interrupt();
                SendPingEveryMinute.Join();
            }
        }
    }
}
