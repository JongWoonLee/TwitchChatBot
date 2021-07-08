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
        public string ClientSecret { get; set; }
        private const long BotId = 702431058;
        public string Password { private get; set; }

        private TwitchToken BotToken;
        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        public string ConnectionString { get; set; }

        private List<Command> Commands;

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        //@Scheduled()
        //public Scheduler()
        //{
        //    TwitchToken twitchToken = new TwitchToken();
        //    if(twitchToken.ExpiresIn < 10000)
        //    {
        //        ValidateAccessToken();
        //    }
        //}


        public ThreadExecutorService(string connectionString, string clientSecret)
        {
            this.ConnectionString = connectionString;
            this.ClientSecret = clientSecret;
            this.BotToken = ValidateAccessToken(FindBotInfo());
            this.Password = BotToken.AccessToken; // 사실상 BotToken 이 있으면 없어도 되긴함;
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.Commands = FindCommands();
            Initialize();
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
                            var password = Password;
                            IrcClient ircClient = ConstructIrcCleint(channel, "oauth:" + password);
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

        private IrcClient ConstructIrcCleint(string channel, string password)
        {
            return new IrcClient(Ip, Port, channel, password, channel);
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

        public TwitchToken ValidateAccessToken(Streamer streamer)
        {
            string url = "https://id.twitch.tv/oauth2/token";
            var client = new WebClient();
            var data = new NameValueCollection();
            data["grant_type"] = "refresh_token";
            data["client_id"] = ClientId;
            data["client_secret"] = this.ClientSecret;
            data["refresh_token"] = streamer.RefreshToken;

            var response = client.UploadValues(url, "POST", data);
            string str = Encoding.Default.GetString(response);
            TwitchToken twitchToken = JsonConvert.DeserializeObject<TwitchToken>(str);

            return twitchToken;
        }

        private Streamer FindBotInfo()
        {
            Streamer streamer = new Streamer();
            string SQL = $"SELECT * FROM streamer where streamer_id = {BotId};";
            using (MySqlConnection conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    using (var reader = cmd.ExecuteReader()) // Query 실행 결과를 읽어오는 ExecuteReader
                    {
                        while (reader.Read())
                        {   streamer = new Streamer(
                                Convert.ToInt64(reader["streamer_id"]),
                                reader["channel_name"].ToString(),
                                reader["refresh_token"].ToString()
                            );
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
                return streamer;
            }
        }

        public void RegisterBot(long id , string userName, string channel)
        {
            var ircClient =  new IrcClient(Ip, Port, userName, "oauth:"+Password, channel);
                ManagedBot.Add(id, new SimpleTwitchBot(
                ircClient,
                new PingSender(ircClient),
                this.Commands,
                ConnectionString
                ));
        }
    }
}
