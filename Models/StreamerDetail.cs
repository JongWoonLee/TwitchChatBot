using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    public class StreamerDetail
    {
        public StreamerDetail()
        {
        }

        public StreamerDetail(long StreamerId, string ChannelName, int BotInUse, string DonationLink, string GreetingMessage, int ForbiddenWordLimit, int ForbiddenWordTimeout)
        {
            this.StreamerId = StreamerId;
            this.ChannelName = ChannelName;
            this.BotInUse = BotInUse == 1 ? true : false;
            this.DonationLink = DonationLink;
            this.GreetingMessage = GreetingMessage;
            this.ForbiddenWordLimit = ForbiddenWordLimit == 1 ? true : false;
            this.ForbiddenWordTimeout = ForbiddenWordTimeout;
        }

        public StreamerDetail(long StreamerId, string ChannelName, string DonationLink, string GreetingMessage, int ForbiddenWordLimit, int ForbiddenWordTimeout)
        {
            this.StreamerId = StreamerId;
            this.ChannelName = ChannelName;
            this.DonationLink = DonationLink;
            this.GreetingMessage = GreetingMessage;
            this.ForbiddenWordLimit = ForbiddenWordLimit == 1 ? true : false;
            this.ForbiddenWordTimeout = ForbiddenWordTimeout;
        }

        public long StreamerId { get; set; }
        public string ChannelName { get; set; }
        public bool BotInUse { get; set; }
        public string DonationLink { get; set; }

        public string GreetingMessage { get; set; }
        public bool ForbiddenWordLimit { get; set; }
        public int ForbiddenWordTimeout { get; set; }
        //public int LongTextLimit { get; set; }
        //public int LongTextLimitTimeout { get; set; }
        //public int LongTextLimitLength { get; set; }

    }
}
