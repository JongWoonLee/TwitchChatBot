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
        public Dictionary<string, Command> Commands;
        private string ConnectionString;
        private bool ThreadDoWorkRun;

        /// <summary>
        /// 주입받은 값을 이용해 초기 세팅을 한다.
        /// </summary>
        /// <param name="IrcClient">연결을 맺은 IrcClient</param>
        /// <param name="PingSender">IrcClient에 주기적으로 핑을 보내는 PingSender</param>
        /// <param name="Commands">봇 기본 명령어</param>
        /// <param name="ConnectionString">Connection생성을 위한 ConnectionString</param>
        public SimpleTwitchBot(IrcClient IrcClient, PingSender PingSender, Dictionary<string, Command> Commands, string ConnectionString)
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
        public async void Run()
        {
            IrcClient.SendPublicChatMessage("Connect Message");

            while (ThreadDoWorkRun)
            {
                // 채팅방에 메세지를 읽는다.

                string Message = await IrcClient.ReadMessage();
                if (!string.IsNullOrWhiteSpace(Message))
                {
                    Console.WriteLine(Message); // IRC 메세지를 출력

                    string pattern = @$":(\w+)!(\w+)@(\w+).tmi.twitch.tv\s(\w+)\s#{this.IrcClient.Channel}\s:!(\w+)";
                    Match match = Regex.Match(Message.Trim(), pattern);
                    var v = match.Success;
                    if (match.Success && match.Groups[4].Value.Trim().Equals("PRIVMSG"))
                    {
                        //match.Groups[1].Value.Trim(); // User
                        //match.Groups[4].Value.Trim().Equals("PRIVMSG"); // Message Type
                        //match.Groups[5].Value.Trim(); // Command Target IndexParseSign 뒤로 짜르면 저게 target인지 확인해야할듯(x 구현 할지 안할지 모름);
                        var RawCommand = match.Groups[5].Value.Trim();
                        int IntIndexParseSign = RawCommand.IndexOf(' ');
                        string CommandHead = IntIndexParseSign == -1 ? RawCommand : RawCommand.Substring(0, IntIndexParseSign); // Command

                        foreach (KeyValuePair<string, Command> Cmd in Commands)
                        {
                            if (CommandHead.Equals(Cmd.Key))
                            {
                                switch (Cmd.Value.CommandType)
                                {
                                    case "T":
                                        IrcClient.SendPublicChatMessage(TwitchCommandOutput(CommandHead, Cmd.Value.CommandBody));
                                        break;
                                    case "?":
                                        //IrcClient.SendPublicChatMessage(QuestionComandOutput());
                                        break;
                                    default:
                                        IrcClient.SendPublicChatMessage(Cmd.Value.CommandBody);
                                        break;
                                }
                            }
                        }

                    }

                    

                    //if (Message.Contains("PRIVMSG"))
                    //{
                    //    // 메세지 예시:
                    //    // ":[user]![user]@[user].tmi.twitch.tv PRIVMSG #[channel] :[message]"

                    //    // 메세지 파싱부
                    //    int IntIndexParseSign = Message.IndexOf('!');
                    //    string UserName = Message.Substring(1, IntIndexParseSign - 1);
                    //    IntIndexParseSign = Message.IndexOf(" :");
                    //    Message = Message.Substring(IntIndexParseSign + 2);

                    //    // Commands
                    //    if (Message.Equals("!hello"))
                    //    {
                    //        IrcClient.SendPublicChatMessage("Hello World!");
                    //    }
                    //}
                }
            }
        }

        private string TwitchCommandOutput(string CommandHead, string CommandBody)
        {
            string Result = "";
            switch (CommandHead)
            {
                case "":
                    break;
                default:
                    break;
            }



            return Result;
        }

        /// <summary>
        /// 객체를 제거할때 백그라운드 쓰레드를 중지 시키고 폐기한다.
        /// </summary>
        public void StopDoWork()
        {
            if (ThreadDoWorkRun)
            {
                ThreadDoWorkRun = false; // Run() loop 탈출을 위한 flag 값 설정
                this.Thread.Join();
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
            public long StreamerId { get; }
            public string ForbiddenWordList { get; set; }
            ForbiddenWord(long streamerId, string forbiddenWord)
            {
                this.StreamerId = streamerId;
                this.ForbiddenWordList = forbiddenWord;
            }
        }
    }
}
