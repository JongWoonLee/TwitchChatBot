﻿using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        public TwitchToken StreamerToken;

        private const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        private string ForbiddenWordList;
        private long StreamerId;
        private StreamerDetail StreamerDetail;

        enum CommandType
        {
            Default = 0,
            Personal = 1,
            Twitch = 2
        }

       

        /// <summary>
        /// 주입받은 값을 이용해 초기 세팅을 한다.
        /// </summary>
        /// <param name="IrcClient">연결을 맺은 IrcClient</param>
        /// <param name="PingSender">IrcClient에 주기적으로 핑을 보내는 PingSender</param>
        /// <param name="Commands">봇 기본 명령어</param>
        /// <param name="ConnectionString">Connection생성을 위한 ConnectionString</param>
        public SimpleTwitchBot(long StreamerId, IrcClient IrcClient, PingSender PingSender, Dictionary<string, Command> Commands, string ConnectionString, TwitchToken StreamerToken)
        {
            this.StreamerId = StreamerId;
            this.IrcClient = IrcClient;
            this.PingSender = PingSender;
            this.Commands = Commands;
            this.ConnectionString = ConnectionString;
            this.ThreadDoWorkRun = true;
            this.StreamerToken = StreamerToken;
            this.ForbiddenWordList = FindForbiddenWords();
            this.StreamerDetail = FindStreamerDetail(StreamerId);
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
                try
                {
                    string Message = await IrcClient.ReadMessage();

                    if (!string.IsNullOrWhiteSpace(Message))
                    {
                        Console.WriteLine(Message); // IRC 메세지를 출력
                        //    // 메세지 예시:
                        //    // ":[user]![user]@[user].tmi.twitch.tv PRIVMSG #[channel] :[message]"
                        //string pattern = $@":(\w+)!(\w+)@(\w+).tmi.twitch.tv\s(\w+)\s#{this.IrcClient.Channel}\s:!(\w+)";
                        string pattern = $@"name=(.*)(;em.*);id=(.*)(;mod.*);user-(.*)\s:(\w+)!(\w+)@(\w+).tmi.twitch.tv\s(\w+)\s#{this.IrcClient.Channel}\s:(!?.*)";
                        //string pattern = $@":(\w+)!(\w+)@(\w+).tmi.twitch.tv\s(\w+)\s#{this.IrcClient.Channel}\s:(!?\w+)";
                        Match match = Regex.Match(Message.Trim(), pattern);
                        var v = match.Success;
                        //if (match.Success && match.Groups[4].Value.Trim().Equals("PRIVMSG"))
                        if (match.Success && match.Groups[9].Value.Trim().Equals("PRIVMSG"))
                        {

                            if (IsContainsForbiddenWord(match.Groups[10].Value.Trim()))
                            {
                                IrcClient.SendPublicChatMessage($"/delete {match.Groups[3].Value.Trim()}"); // 명령어 수정 요망 
                                IrcClient.SendPublicChatMessage($"/timeout {match.Groups[6].Value.Trim()} {StreamerDetail.ForbiddenWordTimeout}");
                            }
                            //if (Message.Contains("!raid"))
                            //{
                                //string Ip = "irc.chat.twitch.tv";
                                //int Port = 6667;
                                //var IrcClient = new IrcClient(Ip, Port, Channel, Channel);

                                //IrcClient.SendPublicChatMessage("host mbnv262");
                                //IrcClient.SendPublicChatMessage("/host mbnv262");

                               

                            //}
                            var RawCommand = match.Groups[10].Value.Trim();
                            int IntIndexParseSign = RawCommand.IndexOf(' ');
                            string CommandHead = IntIndexParseSign == -1 ? RawCommand : RawCommand.Substring(0, IntIndexParseSign); // Command
                                                                                                                                    //이걸 Pattern 화 해서 그게 맞는지를 읽어오는게 

                            // KeyValuePair 순회가 아니라 startsWith ! 인지를 확인해서 Command를 돌기
                            foreach (KeyValuePair<string, Command> Cmd in Commands)
                            {
                                if (CommandHead.Equals(Cmd.Key))
                                {
                                    if (!Cmd.Value.Block)
                                    {
                                        SetTimeout(Cmd.Value);
                                        switch (Cmd.Value.CommandType)
                                        {
                                            case (int)CommandType.Twitch:
                                                IrcClient.SendPublicChatMessage(TwitchCommandOutput(Cmd.Value));
                                                break;
                                            case (int)CommandType.Personal:
                                                IrcClient.SendPublicChatMessage(PersonalCommandOutput(Cmd.Value));
                                                break;
                                            case (int)CommandType.Default:
                                                IrcClient.SendPublicChatMessage(Cmd.Value.CommandBody);
                                                break;
                                        }
                                    }
                                }
                            }

                        }
                    }

                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e.ToString());
                    IrcClient.CloseTcpClient();
                    StopDoWork();
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.ToString());
                    IrcClient.CloseTcpClient();
                    StopDoWork();
                }
            }
        }

        private string PersonalCommandOutput(Command Command)
        {
            string Result = "";
            switch (Command.CommandHead)
            {
                case "!하이":
                    Result = StreamerDetail.GreetingMessage;
                    break;
                case "!도네":
                    Result = StreamerDetail.DonationLink;
                    break;
            }

            return Result;
        }

        private void SetTimeout(Command Command)
        {
            Command.Block = true;
            Task.Run(async () =>
            {
                await Task.Delay(Command.CommandCoolDown);
                Command.Block = false;
            });
        }

        private string TwitchCommandOutput(Command Command)
        {
            string Result = "";
            switch (Command.CommandHead)
            {
                case "!투하":
                    Result = IsLive().ToString();
                    break;
                default:
                    break;
            }

            return Result;
        }

        public StreamerDetail FindStreamerDetail(long StreamerId)
        {
            StreamerDetail StreamerDetail = null;
            string SQL = $"SELECT std.*, s.channel_name FROM streamer_detail std, streamer s WHERE std.streamer_id = s.streamer_id AND s.streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    using (var Reader = Cmd.ExecuteReader())
                    {
                        while (Reader.Read())
                        {
                            StreamerDetail = new StreamerDetail(
                                Convert.ToInt64(Reader["streamer_id"]),
                                Reader["channel_name"].ToString(),
                                Convert.ToInt32(Reader["bot_in_use"]),
                                Reader["donation_link"].ToString(),
                                Reader["greeting_message"].ToString(),
                                Convert.ToInt32(Reader["forbidden_word_timeout"]),
                                Convert.ToInt32(Reader["forbidden_word_limit"])
                                );
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
            }


            return StreamerDetail;
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

        private string FindForbiddenWords()
        {
            string ForbiddenWordList = "";
            string SQL = $"SELECT fw.forbidden_word FROM forbidden_word fw JOIN streamer s ON s.streamer_id  = fw.streamer_id and s.streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    using (var Reader = Cmd.ExecuteReader()) // Query 실행 결과를 읽어오는 ExecuteReader
                    {
                        while (Reader.Read())
                        {
                            ForbiddenWordList += $"{Reader["forbidden_word"]}|";
                        }
                        ForbiddenWordList = ForbiddenWordList.Substring(0, ForbiddenWordList.Length == 0 ? 0 : ForbiddenWordList.Length - 1);
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return ForbiddenWordList;
            }
        }

        private class ForbiddenWord
        {
            public long StreamerId { get; }
            public string ForbiddenWordList { get; set; }
            public ForbiddenWord(long streamerId, string forbiddenWord)
            {
                this.StreamerId = streamerId;
                this.ForbiddenWordList = forbiddenWord;
            }
        }

        private bool IsContainsForbiddenWord(string Message)
        {
            string Pattern = this.ForbiddenWordList;
            if (string.IsNullOrWhiteSpace(Pattern))
            {
                return false;
            }
            Match Match = Regex.Match(Message.Trim(), Pattern, RegexOptions.IgnoreCase);
            return Match.Success;
        }

        private bool IsLive()
        {
            try
            {
                string Url = "https://api.twitch.tv/helix/search/channels?query=" + StreamerDetail.ChannelName;
                var Client = new WebClient();
                Client.Headers.Add("Authorization", $"Bearer {StreamerToken.AccessToken}");
                Client.Headers.Add("client-id", ClientId);
                var Response = Client.DownloadString(Url);
                JObject JSONResponse = JObject.Parse(Response);
                var BroadcasterList = JSONResponse["data"].Select(r => JsonConvert.DeserializeObject<Broadcaster>(r.ToString())).ToList();
                var Broadcaster = BroadcasterList.Find(b => b.BroadcasterLogin.Equals(StreamerDetail.ChannelName));
                //BroadcasterList BroadcasterList = JsonConvert.DeserializeObject<BroadcasterList>(Response);
                //Broadcaster b = Array.Find(BroadcasterList.Data, b => b.BroadcasterLogin.Equals(Channel));

                return Broadcaster.IsLive == 1 ? true : false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private class Broadcaster
        {
            [JsonProperty(PropertyName = "id")]
            private long Id { get; set; }
            [JsonProperty(PropertyName = "broadcaster_login")]
            public string BroadcasterLogin { get; set; }
            [JsonProperty(PropertyName = "is_live")]
            public int IsLive { get; set; }
            [JsonProperty(PropertyName = "broadcaster_language")]
            private string BroadcasterLanguage { get; set; }
            [JsonProperty(PropertyName = "display_name")]
            private string DisplayName { get; set; }
            [JsonProperty(PropertyName = "game_id")]
            private string GameId { get; set; }
            [JsonProperty(PropertyName = "game_name")]
            private string GameName { get; set; }
            [JsonProperty(PropertyName = "tag_ids")]
            private string[] TagIds { get; set; }
            [JsonProperty(PropertyName = "thumbnail_url")]
            private string ThumbnailUrl { get; set; }
            [JsonProperty(PropertyName = "title")]
            private string title { get; set; }
            [JsonProperty(PropertyName = "started_at")]
            private string StartedAt { get; set; }
        }
    }
}