using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    public class IrcThreadPingMixture
    {
        public Thread _thread;
        public IrcClient IrcClient { get; set; }
        public PingSender PingSender { get; set; }
        public List<Command> Commands;

        public IrcThreadPingMixture(IrcClient ircClient, PingSender pingSender, List<Command> commands)
        {
            this.IrcClient = ircClient;
            this.PingSender = pingSender;
            this.Commands = commands;
            IrcThreadPingMixture ircThreadPingMixture = this;
            _thread = new Thread(new ThreadStart(ircThreadPingMixture.Run));
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
}
