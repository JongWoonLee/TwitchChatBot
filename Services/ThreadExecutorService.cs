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
    public class ThreadExecutorService : DBServiceBase
    {
        private const string Ip = "irc.chat.twitch.tv";
        private const int Port = 6667;
        public TwitchToken BotToken { get; set; }
        

        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        private Dictionary<string, Command> Commands;


        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="ConnectionString">string DBConnectionString</param>
        /// <param name="ClientSecret">string App ClientSecret</param>
        public ThreadExecutorService(string ConnectionString, string ClientSecret) : base(ConnectionString, ClientSecret)
        {
            this.ConnectionString = base.ConnectionString;
            this.ClientSecret = base.ClientSecret;
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.BotToken = ValidateAccessToken(FindBotRefreshToken()); // 봇 유저를 찾아서 봇 토큰을 얻어온다.
            this.Commands = FindCommands(); // 공통 Command이므로 매번 읽어오는게 아니라 Service가 가지고 있고 봇이 추가될때 주입해준다.
            Initialize();
        } // end constructor

        /// <summary>
        /// RefreshToken 값을 이용해 처음 BotToken 값을 Validate
        /// </summary>
        /// <param name="RefreshToken">string Bot RefreshToken</param>
        /// <returns>봇의 토큰</returns>
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
            } // end try
        } // end ValidateAccessToken

        /// <summary>
        /// 초기 값 세팅
        /// </summary>
        private void Initialize()
        {
            string SQL = "SELECT s.channel_name, sdt.* FROM streamer s, streamer_detail sdt WHERE bot_in_use = 1 AND s.streamer_id = sdt.streamer_id;";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    using (var Reader = Cmd.ExecuteReader())
                    {
                        // 채널명과 스트리머 설정값을 이용해 Bot 을 추가한다.
                        while (Reader.Read())
                        {
                            var StreamerId = Convert.ToInt64(Reader["streamer_id"]);
                            var Channel = Reader["channel_name"].ToString();
                            var Password = "oauth:" + BotToken.AccessToken;
                            var IrcClient = new IrcClient(Ip, Port, Channel, Password, Channel);
                            ManagedBot.Add(StreamerId, new SimpleTwitchBot( // ManagedBot 리스트에 추가
                                StreamerId,
                                IrcClient,
                                new PingSender(IrcClient),
                                this.Commands,
                                ConnectionString,
                                new TwitchToken(FindStreamer(StreamerId).RefreshToken)
                                ));
                            Thread.Sleep(1);
                        } // end while
                    } // end using
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                } // end try
                Conn.Close();
            } // end using
        } // end Initialize

        

        /// <summary>
        /// 시작시 등록되는 봇의 Streamer정보를 알아오기 위한 메서드
        /// </summary>
        /// <param name="StreamerId">long 스트리머 ID</param>
        /// <returns>스트리머 정보</returns>
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
                    } // end using
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                } // end try
                Conn.Close();
                return Streamer;
            } // end using
        } // end FindStreamer

        /// <summary>
        /// 명령어 목록을 읽어온다.
        /// </summary>
        /// <returns>명령어 Dictionary</returns>
        private Dictionary<string, Command> FindCommands()
        {
            Dictionary<string, Command> Dictionary = new Dictionary<string, Command>();
            string SQL = "SELECT * FROM command;";
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
                            var CommandHead = Reader["command_head"].ToString();
                            var CommandBody = Reader["command_body"].ToString();
                            var CommandType = Convert.ToInt32(Reader["command_type"]);
                            var CommandCoolDown = Convert.ToInt32(Reader["command_cooldown"]);
                            Dictionary.Add(
                                CommandHead, new Command(CommandHead, CommandBody, CommandType, CommandCoolDown)
                            );
                        } // end while
                    } // end using
                }
                catch (MySqlException E)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(E.ToString());
                } // end try
                Conn.Close();
                return Dictionary;
            } // end using
        } // end FindCommands

        /// <summary>
        /// 매 시간 BotToken의 값을 갱신
        /// </summary>
        public void ValidateBotTokenEveryHour()
        {
            try
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            } // end try
        } // end ValidateBotTokenEveryHour


        /// <summary>
        /// 봇의 처음 토큰 값을 읽어온다.
        /// </summary>
        /// <returns>Bot RefreshToken</returns>
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
                } // end try
                Conn.Close();
                return BotToken;
            } // end using 
        } // end FindBotRefreshToken

        /// <summary>
        /// 봇을 등록
        /// </summary>
        /// <param name="Id">long 스트리머 ID</param>
        /// <param name="UserName">string 봇 이름</param>
        /// <param name="Channel">string 접속 채널명</param>
        /// <param name="TwitchToken">TwitchToken Authentication Token</param>
        public void RegisterBot(long Id, string UserName, string Channel, TwitchToken TwitchToken)
        {
            // 스트리머 정보를 이용해 봇을 등록한다.
            var IrcClient = new IrcClient(Ip, Port, UserName, "oauth:" + BotToken.AccessToken, Channel);
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
            } // end try
        } // end RegisterBot

        /// <summary>
        /// 정지 된 봇을 폐기
        /// </summary>
        /// <param name="StreamerId">long StreamerId</param>
        /// <returns>폐기 성공 여부</returns>
        public string DisposeBot(long StreamerId)
        {
            SimpleTwitchBot SimpleTwitchBot = null;
            try
            {
                if (ManagedBot.TryGetValue(StreamerId, out SimpleTwitchBot))
                {
                    SimpleTwitchBot.PingSender.StopDoWork();
                    SimpleTwitchBot.IrcClient.CloseTcpClient();
                    SimpleTwitchBot.StopDoWork();
                    ManagedBot.Remove(StreamerId);
                    return "success";
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            } // end try
            return "";
        } // end DisposeBot

        /// <summary>
        /// BackGroundUpdateCommandService 에 의해 
        /// 일정 시간마다 바뀌는 Command 의 값을 바꾼다.
        /// Selenium을 이용해 동적 페이지를 파싱해서 값을 찾는다.
        /// </summary>
        public void UpdateCommandData()
        {
            string Url = "https://corona-live.com/";
            ChromeOptions Options = new ChromeOptions();
            Options.AddArgument("headless"); // 브라우저 숨김 옵션
            using (IWebDriver driver = new ChromeDriver(Options))
            {
                try
                {
                    driver.Navigate().GoToUrl(Url); // URL 방문
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10); // 페이지 로딩 시간
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // 대기시간

                    string PageSource = driver.PageSource; // PageDocument
                    string p = "([0-9]{1,},?[0-9]{1,})<span"; // , 도 포함하게 해야함..
                    Match match = Regex.Match(PageSource, p);
                    if (match.Success)
                    {
                        Commands["!covid"].CommandBody = "일 확진자수 : " + match.Groups[1].Value.Trim() + "명";
                    } // end if 
                }
                finally
                {
                    driver.Quit();
                    Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");

                    foreach (var chromeDriverProcess in chromeDriverProcesses)
                    {
                        chromeDriverProcess.Kill(); // 작업 완료시 chromedriver kill
                    } 
                } // end try
            } // end using
        } // end UpdateCommandData
    } // end class
} // end namespace
