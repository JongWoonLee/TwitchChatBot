using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TwitchChatBot.Models;
using TwitchChatBot.Services;

namespace TwitchChatBot.Service
{
    public class ThreadExecutorService
    {
        private const string Ip = "irc.chat.twitch.tv";
        private const int Port = 6667;
        private const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        public TwitchToken BotToken { get; set; }
        private string ConnectionString { get; set; }

        private string ClientSecret { get; set; }

        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        private Dictionary<string, Command> Commands;



        public ThreadExecutorService(string ConnectionString, string ClientSecret)
        {
            this.ConnectionString = ConnectionString;
            this.ClientSecret = ClientSecret;
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.BotToken = ValidateAccessToken(FindBotRefreshToken());
            this.Commands = FindCommands();
            Initialize();
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

            try
            {
                var Response = Client.UploadValues(Url, "POST", Data);
                string Str = Encoding.Default.GetString(Response);
                TwitchToken TwitchToken = JsonConvert.DeserializeObject<TwitchToken>(Str);

                return TwitchToken;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private void Initialize()
        {
            string SQL = "select s.channel_name, sdt.* from streamer s, streamer_detail sdt where bot_in_use = 1 and s.streamer_id = sdt.streamer_id;";
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
                            //var RefreshToken = Reader["refresh_token"].ToString();
                            var Password = "oauth:" + BotToken.AccessToken;
                            var IrcClient = new IrcClient(Ip, Port, Channel, Channel);
                            IrcClient.SendFirstConnectMessage(Channel, "oauth:" + BotToken.AccessToken, Channel);
                            ManagedBot.Add(StreamerId, new SimpleTwitchBot(
                                StreamerId,
                                IrcClient,
                                new PingSender(IrcClient),
                                this.Commands,
                                ConnectionString,
                                new TwitchToken(FindStreamer(StreamerId).RefreshToken)
                                )); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                            Thread.Sleep(1);
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                }
                Conn.Close();
            }
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public Streamer FindStreamer(long StreamerId)
        {
            Streamer Streamer = new Streamer();
            string SQL = $"SELECT * FROM streamer WHERE streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    using (var Reader = Cmd.ExecuteReader())
                    {
                        while (Reader.Read())
                            Streamer = new Streamer(
                                Convert.ToInt64(Reader["streamer_id"]),
                                Reader["channel_name"].ToString(),
                                Reader["refresh_token"].ToString()
                                );
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return Streamer;
            }
        }


        private Dictionary<string, Command> FindCommands()
        {
            Dictionary<string, Command> Dictionary = new Dictionary<string, Command>();
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
                            var CommandHead = Reader["command_head"].ToString();
                            var CommandBody = Reader["command_body"].ToString();
                            var CommandType = Reader["command_type"].ToString();
                            Dictionary.Add(
                                CommandHead, new Command(CommandHead, CommandBody, CommandType)
                            ); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                        }
                    }
                }
                catch (MySqlException E)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(E.ToString());
                }
                Conn.Close();
                return Dictionary;
            }
        }

        public void ValidateBotTokenEveryHour()
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

        // Twitch Token을 받아서 Refresh 하게 바뀐다고 치면 지금까지는 Bot 을 등록할때 Login을 해서 가지고 온거니까 가지고 온 데이터를 기반으로 RefreshToken값을 가져오는게 중요하겠다.

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
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return BotToken;
            }
        }

        public void RegisterBot(long Id, string UserName, string Channel, TwitchToken TwitchToken)
        {
            var IrcClient = new IrcClient(Ip, Port, UserName, Channel);
            IrcClient.SendFirstConnectMessage(UserName, "oauth:" + BotToken.AccessToken, Channel);
            try
            {
                ManagedBot.Add(Id, new SimpleTwitchBot(
                    Id,
                    IrcClient,
                    new PingSender(IrcClient),
                    this.Commands,
                    ConnectionString,
                    TwitchToken
                    ));
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public string DisposeBot(long Id)
        {
            SimpleTwitchBot SimpleTwitchBot = null;
            try
            {
                if (ManagedBot.TryGetValue(Id, out SimpleTwitchBot))
                {
                    SimpleTwitchBot.PingSender.StopDoWork();
                    SimpleTwitchBot.IrcClient.CloseTcpClient();
                    SimpleTwitchBot.StopDoWork();
                    ManagedBot.Remove(Id);
                    return "success";
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
            return "";
        }

        public void UpdateCommandData()
        {
            string Url = "https://corona-live.com/";
            //ChromeOptions Options = new ChromeOptions();
            //Options.AddArgument("headless");
            //using (IWebDriver driver = new ChromeDriver(Options))

            using (IWebDriver driver = new ChromeDriver())
            {
                try
                {
                    driver.Navigate().GoToUrl(Url);
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                    string PageSource = driver.PageSource;
                    string p = "([0-9]{1,},?[0-9]{1,})<span"; // , 도 포함하게 해야함..
                    Match match = Regex.Match(PageSource, p);
                    if (match.Success)
                    {
                        Commands["covid"].CommandBody = "일 확진자수 : " + match.Groups[1].Value.Trim() + "명";
                    }
                }
                finally
                {
                    driver.Quit();
                    Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");

                    foreach (var chromeDriverProcess in chromeDriverProcesses)
                    {
                        chromeDriverProcess.Kill();
                    }
                }
            }
        }
    }
}
