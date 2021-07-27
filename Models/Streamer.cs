using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    /// <summary>
    /// 봇을 등록할 채널 정보를 가진 회원
    /// </summary>
    public class Streamer
    {
        public long StreamerId { get; set; }
        public string ChannelName { get; set; }

        public string RefreshToken { get; set; }

        public Streamer()
        {
        }
        public Streamer(long StreamerId, string ChannelName, string RefreshToken)
        {
            this.StreamerId = StreamerId;
            this.ChannelName = ChannelName;
            this.RefreshToken = RefreshToken;
        }

        public bool StreamerIsValid()
        {
            return !string.IsNullOrWhiteSpace(ChannelName) && !string.IsNullOrWhiteSpace(RefreshToken);
        }
    }
}
