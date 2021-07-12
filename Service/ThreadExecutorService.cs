using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatBot.Models;

namespace TwitchChatBot.Service
{
    public class ThreadExecutorService
    {
        private const string Ip = "irc.chat.twitch.tv";
        private const int Port = 6667;
        private const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        private string ConnectionString { get; set; }
        private string ClientSecret { get; set; }

        public TwitchToken BotToken { get; set; }
        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        private List<Command> Commands;

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }


        public ThreadExecutorService(string ConnectionString, string ClientSecret)
        {
            this.ConnectionString = ConnectionString;
            this.ClientSecret = ClientSecret;
            this.BotToken = ValidateAccessToken(FindBotRefreshToken());
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.Commands = FindCommands();
            Initialize();
        }

        private void Initialize()
        {
            string SQL = "select s.channel_name, sdt.* from streamer s, streamer_detail sdt where bot_in_use = 1 and s.streamer_id = sdt.streamer_id";
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
                            var StreamerId = Convert.ToInt64(Reader["streamer_id"]);
                            var Channel = Reader["channel_name"].ToString();
                            var Password = BotToken.AccessToken;
                            IrcClient IrcClient = new IrcClient(Ip, Port, Channel, Password, Channel);
                            ManagedBot.Add(StreamerId, new SimpleTwitchBot(
                                IrcClient,
                                new PingSender(IrcClient),
                                this.Commands,
                                ConnectionString
                                )); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                        }
                    }
                }
                catch (MySqlException E)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(E.ToString());
                }
                Conn.Close();
            }
        }


        private List<Command> FindCommands()
        {
            List<Command> List = new List<Command>();
            string SQL = "SELECT * FROM command;";
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
                            List.Add(new Command(
                                Reader["command_head"].ToString(),
                                Reader["command_body"].ToString()
                            )); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                        }
                    }
                }
                catch (MySqlException E)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(E.ToString());
                }
                Conn.Close();
                return List;
            }
        }

        /// <summary>
        /// RefreshToken 값을 이용해 처음 BotToken 값을 Validate
        /// </summary>
        /// <param name="RefreshToken"></param>
        /// <returns></returns>
        public TwitchToken ValidateAccessToken(string RefreshToken)
        {
            string Url = "https://id.twitch.tv/oauth2/token";
            var Client = new WebClient();
            var Data = new NameValueCollection();
            Data["grant_type"] = "refresh_token";
            Data["client_id"] = ClientId;
            Data["client_secret"] = this.ClientSecret;
            Data["refresh_token"] = RefreshToken;

            var Response = Client.UploadValues(Url, "POST", Data);
            string Str = Encoding.Default.GetString(Response);
            TwitchToken TwitchToken = JsonConvert.DeserializeObject<TwitchToken>(Str);

            return TwitchToken;
        }

        public void ValidateAccessTokenEveryHour()
        {
            string Url = "https://id.twitch.tv/oauth2/token";
            var Client = new WebClient();
            var Data = new NameValueCollection();
            Data["grant_type"] = "refresh_token";
            Data["client_id"] = ClientId;
            Data["client_secret"] = this.ClientSecret;
            Data["refresh_token"] = this.BotToken.RefreshToken;

            var Response = Client.UploadValues(Url, "POST", Data);
            string Str = Encoding.Default.GetString(Response);
            this.BotToken = JsonConvert.DeserializeObject<TwitchToken>(Str);
        }

        private string FindBotRefreshToken()
        {
            string BotToken = "";
            string SQL = "SELECT s.refresh_token FROM streamer as s JOIN bot_token AS bt ON s.streamer_id = bt.streamer_id;";
            using (MySqlConnection Conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    BotToken = Cmd.ExecuteScalar().ToString();
                    if (!string.IsNullOrWhiteSpace(BotToken))
                    {
                        Console.WriteLine("Bot Found");
                    }
                    else
                    {
                        Console.WriteLine("Bot Cannot Be Found");
                    }
                }
                catch (MySqlException E)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(E.ToString());
                }
                Conn.Close();
                return BotToken;
            }
        }

        public void RegisterBot(long Id, string UserName, string Channel)
        {
            var IrcClient = new IrcClient(Ip, Port, UserName, "oauth:" + BotToken.AccessToken, Channel);
            ManagedBot.Add(Id, new SimpleTwitchBot(
            IrcClient,
            new PingSender(IrcClient),
            this.Commands,
            ConnectionString
            ));
        }
    }
}
