using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;
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

        private TwitchToken BotToken;
        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        private List<Command> Commands;

        private Thread BotTokenRefresher;

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }


        public ThreadExecutorService(string connectionString, string clientSecret)
        {
            this.ConnectionString = connectionString;
            this.ClientSecret = clientSecret;
            this.BotToken = ValidateAccessToken(FindBotRefreshToken());
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.Commands = FindCommands();
            Initialize();
            this.BotTokenRefresher = new Thread(new ThreadStart(this.Run));
            Start();
        }

        private void Initialize()
        {
            string SQL = "select s.channel_name, sdt.* from streamer s, streamer_detail sdt where bot_in_use = 1 and s.streamer_id = sdt.streamer_id";
            using (MySqlConnection conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    using (var reader = cmd.ExecuteReader()) // Query 실행 결과를 읽어오는 ExecuteReader
                    {
                        while (reader.Read())
                        {
                            var streamerId = Convert.ToInt64(reader["streamer_id"]);
                            var channel = reader["channel_name"].ToString();
                            var password = BotToken.AccessToken;
                            IrcClient ircClient = new IrcClient(Ip, Port, channel, password, channel);
                            ManagedBot.Add(streamerId, new SimpleTwitchBot(
                                ircClient,
                                new PingSender(ircClient),
                                this.Commands,
                                ConnectionString
                                )); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
            }
        }


        private List<Command> FindCommands()
        {
            List<Command> list = new List<Command>();
            string SQL = "SELECT * FROM command;";
            using (MySqlConnection conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    using (var reader = cmd.ExecuteReader()) // Query 실행 결과를 읽어오는 ExecuteReader
                    {
                        while (reader.Read())
                        {
                            list.Add(new Command(
                                reader["command_head"].ToString(),
                                reader["command_body"].ToString()
                            )); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
                return list;
            }
        }

        public TwitchToken ValidateAccessToken(string refreshToken)
        {
            string url = "https://id.twitch.tv/oauth2/token";
            var client = new WebClient();
            var data = new NameValueCollection();
            data["grant_type"] = "refresh_token";
            data["client_id"] = ClientId;
            data["client_secret"] = this.ClientSecret;
            data["refresh_token"] = refreshToken;

            var response = client.UploadValues(url, "POST", data);
            string str = Encoding.Default.GetString(response);
            TwitchToken twitchToken = JsonConvert.DeserializeObject<TwitchToken>(str);

            return twitchToken;
        }

        private string FindBotRefreshToken()
        {
            string botToken = "";
            string SQL = "SELECT s.refresh_token FROM streamer as s JOIN bot_token AS bt ON s.streamer_id = bt.streamer_id;";
            using (MySqlConnection conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    botToken = cmd.ExecuteScalar().ToString();
                    if (string.IsNullOrWhiteSpace(botToken))
                    {
                        Console.WriteLine("Bot Cannot Be Found");
                    }
                    else
                    {
                        Console.WriteLine("Bot Found");
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
                return botToken;
            }
        }

        public void RegisterBot(long id, string userName, string channel)
        {
            var ircClient = new IrcClient(Ip, Port, userName, "oauth:" + BotToken.AccessToken, channel);
            ManagedBot.Add(id, new SimpleTwitchBot(
            ircClient,
            new PingSender(ircClient),
            this.Commands,
            ConnectionString
            ));
        }

        public void Start()
        {
            BotTokenRefresher.IsBackground = true;
            BotTokenRefresher.Start();
        }

        // 한시간에 한번씩 Token Validate 하기
        public void Run()
        {
            while (true)
            {
                try
                {
                    ValidateAccessToken(BotToken.RefreshToken);
                    Thread.Sleep(3600000); // 1 시간
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("newThread inturrupted");
                }
            }
        }
    }
}
