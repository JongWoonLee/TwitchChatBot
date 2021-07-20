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
        private IrcClient IrcClient;
        private Thread SendPingEveryMinute;
        private bool ThreadDoWorkRun;

        /// <summary>
        /// 60초 마다 한번씩 서버에 메세지를 보내 연결을 유지시키는 객체
        /// </summary>
        /// <param name="IrcClient">연결을 유지하고자 하는 IrcClient</param>
        public PingSender(IrcClient IrcClient)
        {
            this.IrcClient = IrcClient;
            this.ThreadDoWorkRun = true;
            SendPingEveryMinute = new Thread(new ThreadStart(this.Run));
            Start();
        }

        /// <summary>
        /// 쓰레드 시작
        /// </summary>
        public void Start()
        {
            SendPingEveryMinute.IsBackground = true;
            SendPingEveryMinute.Start();
        }

        /// <summary>
        /// Irc서버에 60초에 한번씩 핑을 보낸다.
        /// </summary>
        public void Run()
        {
            while (ThreadDoWorkRun)
            {
                try
                {
                    IrcClient.SendIrcMessage("PING irc.twitch.tv");
                    Thread.Sleep(60000); // 1 minutes
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("newThread inturrupted");
                }
            }
        }

        /// <summary>
        /// 객체를 제거할때 백그라운드 쓰레드를 중지 시키고 폐기한다.
        /// </summary>
        public void StopDoWork()
        {
            if (ThreadDoWorkRun)
            {
                ThreadDoWorkRun = false;
                SendPingEveryMinute.Interrupt(); // Sleep 상태인 쓰레드를 깨운다.
                SendPingEveryMinute.Join();
            }
        }
    }
}
