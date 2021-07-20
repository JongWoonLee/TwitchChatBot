using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    public class StreamerDetail
    {
        public int StreamerId { get; set; }
        public string ChannelName { get; set; }
        public bool BotInUse { get; set; }
        public string DonationLink { get; set; }
        public string GreetingMessage { get; set; }
        public bool ForbiddenWordLimit { get; set; }
        public int ForbiddenWordTimeout { get; set; }
        public int LongTextLimit { get; set; }
        public int LongTextLimitTimeout { get; set; }
        public int LongTextLimitLength { get; set; }

    }
}
