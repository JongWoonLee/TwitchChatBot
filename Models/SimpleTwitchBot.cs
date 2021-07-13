using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace TwitchChatBot.Models
{
    public class SimpleTwitchBot
    {
        private Thread Thread;
        public IrcClient IrcClient { get; set; }
        public PingSender PingSender;
        public List<Command> Commands;
        private string ConnectionString;
        private bool ThreadDoWorkRun;

        /// <summary>
        /// 주입받은 값을 이용해 초기 세팅을 한다.
        /// </summary>
        /// <param name="IrcClient">연결을 맺은 IrcClient</param>
        /// <param name="PingSender">IrcClient에 주기적으로 핑을 보내는 PingSender</param>
        /// <param name="Commands">봇 기본 명령어</param>
        /// <param name="ConnectionString">Connection생성을 위한 ConnectionString</param>
        public SimpleTwitchBot(IrcClient IrcClient, PingSender PingSender, List<Command> Commands, string ConnectionString)
        {
            this.IrcClient = IrcClient;
            this.PingSender = PingSender;
            this.Commands = Commands;
            this.ConnectionString = ConnectionString;
            this.ThreadDoWorkRun = true;
            Thread = new Thread(new ThreadStart(this.Run));
            Start();
        }

        /// <summary>
        /// 쓰레드 시작
        /// </summary>
        public void Start()
        {
            Thread.IsBackground = true;
            Thread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            IrcClient.SendPublicChatMessage("Here Comes A Connect Message");

            while (ThreadDoWorkRun)
            {
                // Read any message from the chat room
                string Message = IrcClient.ReadMessage();
                if (!string.IsNullOrWhiteSpace(Message))
                {
                    Console.WriteLine(Message); // Print raw irc messages
                    string pattern = @$":{this.IrcClient.Channel}!{this.IrcClient.Channel}@{this.IrcClient.Channel}.tmi.twitch.tv\s(\w+)\s#(\w+)\s:!(\w+)";
                    Match match = Regex.Match(Message, pattern);
                    if (match.Success)
                    {
                        match.Groups[1].Value.Trim().Equals("PRIVMSG"); // Message Type
                        match.Groups[2].Value.Trim(); // Message Sender
                        match.Groups[3].Value.Trim(); // Command
                        //match.Groups[4].Value.Trim(); // Command Target(x 구현 할지 안할지 모르는데 일단은 없이 동작하게 봄);
                    }

                    foreach (var Ele in Commands)
                    {
                        if (Message.StartsWith(Ele.CommandHead))
                        {
                            IrcClient.SendPublicChatMessage(Ele.CommandBody);
                        }
                    }

                    if (Message.Contains("PRIVMSG"))
                    {
                        // Messages from the users will look something like this (without quotes):
                        // Format: ":[user]![user]@[user].tmi.twitch.tv PRIVMSG #[channel] :[message]"

                        // Modify message to only retrieve user and message
                        int IntIndexParseSign = Message.IndexOf('!');
                        string UserName = Message.Substring(1, IntIndexParseSign - 1); // parse username from specific section (without quotes)
                                                                                       // Format: ":[user]!"
                                                                                       // Get user's message
                        IntIndexParseSign = Message.IndexOf(" :");
                        Message = Message.Substring(IntIndexParseSign + 2);

                        //Console.WriteLine(message); // Print parsed irc message (debugging only)

                        // General commands anyone can use
                        if (Message.Equals("!hello"))
                        {
                            IrcClient.SendPublicChatMessage("Hello World!");
                        }
                    }
                }
            }
            //IrcClient.SendPublicChatMessage("GoodBye Message");
        }

        /// <summary>
        /// 객체를 제거할때 백그라운드 쓰레드를 중지 시키고 폐기한다.
        /// </summary>
        public void StopDoWork()
        {
            if (ThreadDoWorkRun)
            {
                ThreadDoWorkRun = false; // Run() loop 탈출을 위한 flag 값 설정
                Thread.Join();
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
            public string ForbiddenWordList { get; set; }
            ForbiddenWord(long streamerId, string forbiddenWord)
            {
                this.StreamerId = streamerId;
                this.ForbiddenWordList = forbiddenWord;
            }
        }
    }
}
