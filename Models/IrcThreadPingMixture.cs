﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TwitchChatBot.Models
{
    public class IrcThreadPingMixture
    {
        private Thread _thread;
        public IrcClient IrcClient { get; set; }
        private PingSender PingSender;
        public List<Command> Commands;
        private string ConnectionString;

        public IrcThreadPingMixture(IrcClient ircClient, PingSender pingSender, List<Command> commands, string connectionString)
        {
            this.IrcClient = ircClient;
            this.PingSender = pingSender;
            this.Commands = commands;
            this.ConnectionString = connectionString;
            _thread = new Thread(new ThreadStart(this.Run));
            Start();
        }

        public void Start()
        {
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Run()
        {
            while (true)
            {
                // Read any message from the chat room
                string message = IrcClient.ReadMessage();
                Console.WriteLine(message); // Print raw irc messages
                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("PRIVMSG"))
                    {
                        // Messages from the users will look something like this (without quotes):
                        // Format: ":[user]![user]@[user].tmi.twitch.tv PRIVMSG #[channel] :[message]"

                        // Modify message to only retrieve user and message
                        int intIndexParseSign = message.IndexOf('!');
                        string userName = message.Substring(1, intIndexParseSign - 1); // parse username from specific section (without quotes)
                                                                                       // Format: ":[user]!"
                                                                                       // Get user's message
                        intIndexParseSign = message.IndexOf(" :");
                        message = message.Substring(intIndexParseSign + 2);

                        //Console.WriteLine(message); // Print parsed irc message (debugging only)

                        // General commands anyone can use
                        if (message.Equals("!hello"))
                        {
                            IrcClient.SendPublicChatMessage("Hello World!");
                        }
                    }
                }
            }
        }
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private List<ForbiddenWord> FindForbiddenWords()
        {
            List<ForbiddenWord> list = new List<ForbiddenWord>();
            return list;
        }

        private class ForbiddenWord
        {
            public long StreamerId { get;}
            public string forbiddenWord { get; set; }
            ForbiddenWord(long streamerId, string forbiddenWord)
            {
                this.StreamerId = streamerId;
                this.forbiddenWord = forbiddenWord;
            }
        }
    }
}
