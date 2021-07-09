using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    public class Command
    {
        public string CommandHead { get;set; }
        public string CommandBody { get; set; }

        public Command(string CommandHead, string CommandBody)
        {
            this.CommandHead = CommandHead;
            this.CommandBody = CommandBody;
        }

        public Command()
        {
        }
    }
}
