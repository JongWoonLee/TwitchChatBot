using MySql.Data.MySqlClient;
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

        /// <summary>
        /// Command Type Enum
        /// </summary>
        enum CommandType
        {
            Default = 0, // 기본 명령어
            Personal = 1, // 개인 명령어 , CommandBody가 StreamerDetail에 저장된 데이터를 이용
            Twitch = 2 // Twitch API 호출 명령어
        }

        /// <summary>
        /// 봇 생성자
        /// </summary>
        /// <param name="StreamerId">long 채널 주인 ID</param>
        /// <param name="IrcClient">IrcClient 채널 채팅방에 입장한 IrcClient</param>
        /// <param name="PingSender">PingSender 채널에 주기적으로 핑을 보내는 PingSender</param>
        /// <param name="Commands">Dictionary<string,Command> 봇 기본 명령어</param>
        /// <param name="ConnectionString">string DB ConnectionString</param>
        /// <param name="StreamerToken">TwitchToken 채널 중재자가 아니라 채널 소유자 권한의 명령어가 필요할때 쓰는 StreamerToken</param>
        public SimpleTwitchBot(long StreamerId, IrcClient IrcClient, PingSender PingSender, Dictionary<string, Command> Commands, string ConnectionString, TwitchToken StreamerToken)
        {
            this.StreamerId = StreamerId;
            this.IrcClient = IrcClient;
            this.PingSender = PingSender;
            this.Commands = Commands;
            this.ConnectionString = ConnectionString;
            this.ThreadDoWorkRun = true; // Thread flag 값 true로 설정
            this.StreamerToken = StreamerToken;
            this.ForbiddenWordList = FindForbiddenWords(); // 금지어 리스트 읽어오기
            this.StreamerDetail = FindStreamerDetail(StreamerId); // 개인 명령어에 필요한 StreamerDetail
            Thread = new Thread(new ThreadStart(this.Run));
            Start();
        } // end constructor

        /// <summary>
        /// 쓰레드 시작
        /// </summary>
        public void Start()
        {
            Thread.IsBackground = true;
            Thread.Start();
        } // end Start

        /// <summary>
        /// 쓰레드 동작
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
                    Console.WriteLine(Message); // IRC 메세지를 출력

                    string pattern = $@"name=(.*)(;em.*);id=(.*)(;mod.*);user-(.*)\s:(\w+)!(\w+)@(\w+).tmi.twitch.tv\s(\w+)\s#{this.IrcClient.Channel}\s:(!?.*)";
                    Match match = Regex.Match(Message.Trim(), pattern);
                    if (match.Success && match.Groups[9].Value.Trim().Equals("PRIVMSG"))
                    {

                        if (IsContainsForbiddenWord(match.Groups[10].Value.Trim()))
                        {
                            IrcClient.SendPublicChatMessage($"/delete {match.Groups[3].Value.Trim()}"); // 명령어 수정 요망 
                            IrcClient.SendPublicChatMessage($"/timeout {match.Groups[6].Value.Trim()} {StreamerDetail.ForbiddenWordTimeout}");
                        } // end if

                        var RawCommand = match.Groups[10].Value.Trim();
                        int IntIndexParseSign = RawCommand.IndexOf(' ');
                        string CommandHead = IntIndexParseSign == -1 ? RawCommand : RawCommand.Substring(0, IntIndexParseSign); // Command

                        foreach (KeyValuePair<string, Command> Cmd in Commands)
                        {
                            // 메세지와 Command가 일치하면
                            if (CommandHead.Equals(Cmd.Key))
                            {
                                // Command 별로 Cooldown이 존재해서 Block == true 일 경우엔 발동 하지 않는다.
                                if (!Cmd.Value.Block)
                                {
                                    SetTimeout(Cmd.Value); // Command 에 설정된 CoolDown Trigger
                                                           // CommandType 에 맞게 명령어 처리
                                    switch (Cmd.Value.CommandType)
                                    {
                                        case (int)CommandType.Default:
                                            IrcClient.SendPublicChatMessage(Cmd.Value.CommandBody); // 기본 명령어로, 저장된 CommandBody를 출력
                                            break;
                                        case (int)CommandType.Personal:
                                            IrcClient.SendPublicChatMessage(PersonalCommandOutput(Cmd.Value)); // PersonalCommandOutput에서 CommandBody를 생성
                                            break;
                                        case (int)CommandType.Twitch:
                                            IrcClient.SendPublicChatMessage(TwitchCommandOutput(Cmd.Value)); // TwitchCommandOutput에서 CommandBody를 생성
                                            break;
                                    } // end switch
                                } // end if(!Cmd.Value)
                            } // end if(CommandHead)
                        } // end foreach

                    } // end if (match.Success)

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
                } // end try
            } // end while
        } // end run

        /// <summary>
        /// 개인 명령어 출력부 생성
        /// </summary>
        /// <param name="Command">Input Command</param>
        /// <returns></returns>
        private string PersonalCommandOutput(Command Command)
        {
            string Result = "";
            // 명령어를 비교해서 일치하는 부분 출력
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

        /// <summary>
        /// 쿨타임 설정
        /// </summary>
        /// <param name="Command"></param>
        private void SetTimeout(Command Command)
        {
            // 들어오면 Command의 CoolDown 동안 Block 을 true로 설정
            Command.Block = true;
            Task.Run(async () =>
            {
                await Task.Delay(Command.CommandCoolDown);
                Command.Block = false;
            });
        }

        /// <summary>
        /// 트위치 명령어 출력부 생성
        /// </summary>
        /// <param name="Command">Input Command</param>
        /// <returns></returns>
        private string TwitchCommandOutput(Command Command)
        {
            string Result = "";
            // 명령어를 비교해서 일치하는 부분 출력
            switch (Command.CommandHead)
            {
                case "!투하":
                    Result = IsLive().ToString();
                    break;
            }
            return Result;
        }

        /// <summary>
        /// 개인 명령어를 위해 StreamerDetail 데이터를 봇도 가지고 있다.
        /// </summary>
        /// <param name="StreamerId">채널 주인 ID</param>
        /// <returns></returns>
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
                        } // end while
                    } // end using
                } 
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                } // end try
                Conn.Close();
            } // end using


            return StreamerDetail;
        }

        /// <summary>
        /// 객체를 제거할때 백그라운드 쓰레드를 중지 시키고 폐기한다.
        /// </summary>
        public void StopDoWork()
        {
            if (ThreadDoWorkRun)
            {
                ThreadDoWorkRun = false; // 메서드 Run() loop 탈출을 위한 flag 값 설정
                this.Thread.Join();
            }
        }

        /// <summary>
        /// ConnectionString을 이용해 Connection객체를 얻어온다.
        /// </summary>
        /// <returns>DBConnection</returns>
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        /// <summary>
        /// 금지어 리스트를 정규식 패턴 형태로 얻어온다.
        /// </summary>
        /// <returns>금지어 리스트</returns>
        private string FindForbiddenWords()
        {
            string ForbiddenWordList = "";
            string SQL = $"SELECT fw.forbidden_word FROM forbidden_word fw JOIN streamer s ON s.streamer_id  = fw.streamer_id and s.streamer_id = {StreamerId};";
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
                            ForbiddenWordList += $"{Reader["forbidden_word"]}|";
                        } // end while
                        ForbiddenWordList = ForbiddenWordList.Substring(0, ForbiddenWordList.Length == 0 ? 0 : ForbiddenWordList.Length - 1); // 읽어온 결과가 없으면 0으로 반환
                    } // end using
                }
                catch (Exception e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                } // end try
                Conn.Close();
                return ForbiddenWordList;
            }
        }

        /// <summary>
        /// 금지어
        /// </summary>
        private class ForbiddenWord
        {
            public long StreamerId { get; }
            public string ForbiddenWordList { get; set; }
            public ForbiddenWord(long StreamerId, string ForbiddenWord)
            {
                this.StreamerId = StreamerId;
                this.ForbiddenWordList = ForbiddenWord;
            }
        }

        /// <summary>
        /// 금지어가 포함되어 있는지를 확인한다.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns>금지어 포함 여부</returns>
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

        /// <summary>
        /// Twitch Api에 현재 채널이 방송중인지 여부를 확인해준다.
        /// </summary>
        /// <returns>방송 중인지 여부</returns>
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