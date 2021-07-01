using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    /// <summary>
    /// 봇을 등록할 채널 정보를 가진 회원
    /// </summary>
    public class Member
    {
        public int Id { get; set; }
        public string ChannelName { get; set; }
    }
}
