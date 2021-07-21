using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using TwitchChatBot.Models;
using TwitchChatBot.Services;

namespace TwitchChatBot.Service
{
    public class MemberService
    {
        private string DefaultIP { get; set; }
        private string ConnectionString { get; set; }

        private const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        private string ClientSecret { get; set; }


        public MemberService(string ConnectionString, string DefaultIP, string ClientSecret)
        {
            this.ConnectionString = ConnectionString;
            this.DefaultIP = DefaultIP;
            this.ClientSecret = ClientSecret;
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
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


        public TwitchToken ConnectReleasesWebClient(string Code)
        {
            string Url = "https://id.twitch.tv/oauth2/token";
            var Client = new WebClient();
            var Data = new NameValueCollection();

            Data["grant_type"] = "authorization_code";
            Data["client_id"] = ClientId;
            Data["client_secret"] = ClientSecret;
            Data["redirect_uri"] = $"https://{DefaultIP}/member/index"; ;
            Data["code"] = Code;

            var Response = Client.UploadValues(Url, "POST", Data);
            string Str = Encoding.Default.GetString(Response);
            TwitchToken TwitchToken = JsonConvert.DeserializeObject<TwitchToken>(Str);
            return TwitchToken;
        }



        public User ValidatingRequests(string AccessToken)
        {
            string Url = "https://id.twitch.tv/oauth2/validate";
            var Client = new WebClient();
            Client.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var Response = Client.DownloadString(Url);
            User User = JsonConvert.DeserializeObject<User>(Response);

            // client_id : string
            // login : string
            // scope : string[] 
            // user_id : long(오는건 string인거 같은데)
            // expires_in : int
            return User;
        }

        public string GetRedirectURL()
        {
            string Url = "https://id.twitch.tv/oauth2/authorize";
            string RedirectUri = $"https://{DefaultIP}/member/index";
            string ResponseType = "code";
            return $"{Url}?client_id={ClientId}&redirect_uri={RedirectUri}&response_type={ResponseType}&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email";
            //return $"{Url}?client_id={ClientId}&redirect_uri={RedirectUri}&response_type={ResponseType}&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email channel:moderate channel_editor"; // User Dont need but Raid need channel_editor
        }

        public int InsertStreamer(TwitchToken TwitchToken, User User)
        {
            var Result = 0;
            string SQL = $"INSERT INTO streamer(streamer_id,channel_name,refresh_token) VALUES(@StreamerId, @ChannelName, @RefreshToken);";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@StreamerId", User.UserId);
                    Cmd.Parameters.AddWithValue("@ChannelName", User.Login);
                    Cmd.Parameters.AddWithValue("@RefreshToken", TwitchToken.RefreshToken);
                    Result = Cmd.ExecuteNonQuery();
                    if (Result == 1)
                    {
                        Console.WriteLine("Insert Success");
                        InsertStreamerDetail(Conn, User.UserId);
                    }
                    else
                    {
                        Console.WriteLine("Insert Fail!!");
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return Result;
            }
        }

        private int InsertStreamerDetail(MySqlConnection Conn, long InheritedKey)
        {
            var Result = 0;
            string SQL = $"INSERT INTO streamer_detail(streamer_id) VALUES(@StreamerId);";
            MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
            Cmd.Parameters.AddWithValue("@StreamerId", InheritedKey);
            Result = Cmd.ExecuteNonQuery();
            if (Result == 1)
            {
                Console.WriteLine("Insert Success");
            }
            else
            {
                Console.WriteLine("Insert Fail!!");
            }
            return Result;
        }

        public int UpdateStreamerDetailBotInUse(long StreamerId, int BotInUse)
        {
            var Result = 0;
            string SQL = $"UPDATE streamer_detail SET bot_in_use = @BotInUse WHERE streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@BotInUse", BotInUse);
                    Result = Cmd.ExecuteNonQuery();
                    if (Result == 1)
                    {
                        Console.WriteLine("Update Success");
                    }
                    else
                    {
                        Console.WriteLine("Update Fail!!");
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return Result;
            }

        }


        public int FindBotInUseByUserId(string UserId)
        {
            long StreamerId = Convert.ToInt64(UserId);
            int Result = 0;
            string SQL = $"SELECT bot_in_use FROM streamer_detail where streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Result = Convert.ToInt32(Cmd.ExecuteScalar());
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                Conn.Close();
                return Result;
            }
        }

        public StreamerDetail FindStreamerDetail(long StreamerId)
        {
            StreamerDetail StreamerDetail = null;
            string SQL = $"SELECT * FROM streamer_detail WHERE streamer_id = {StreamerId};";
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

                        }
                            //StreamerDetail = new StreamerDetail(
                            //    Convert.ToInt64(Reader["streamer_id"]),
                            //    Convert.ToInt32(Reader["channel_name"]),
                            //    Reader["donation_link"].ToString(),
                            //    Reader["greeting_message"].ToString(),
                            //    Convert.ToInt32(Reader["forbidden_word_timeout"]),
                            //    Convert.ToInt32(Reader["forbidden_word_limit"])
                            //    );
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
    }
}
