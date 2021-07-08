using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TwitchChatBot.Models;

namespace TwitchChatBot.Service
{
    public class MemberService
    {
        private const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        public string DefaultIP { get; set; }
        public string ClientSecret { get; set; }
        public string ConnectionString { get; set; }

        public MemberService(string connectionString,string defaultIP, string clientSecret)
        {
            this.ConnectionString = connectionString;
            this.DefaultIP = defaultIP;
            this.ClientSecret = clientSecret;
        }
        public MemberService()
        {
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public TwitchToken ConnectReleasesWebClient(string code)
        {
            string url = "https://id.twitch.tv/oauth2/token";
            var client = new WebClient();
            var data = new NameValueCollection();

            data["grant_type"] = "authorization_code";
            data["client_id"] = ClientId;
            data["client_secret"] = this.ClientSecret;
            data["redirect_uri"] = $"https://{DefaultIP}/member/index"; ;
            data["code"] = code;

            var response = client.UploadValues(url, "POST", data);
            string str = Encoding.Default.GetString(response);
            TwitchToken twitchToken = JsonConvert.DeserializeObject<TwitchToken>(str);
            return twitchToken;
        }



        public User ValidatingRequests(string accessToken)
        {
            string url = "https://id.twitch.tv/oauth2/validate";
            var client = new WebClient();
            client.Headers.Add("Authorization", $"Bearer {accessToken}");
            var response = client.DownloadString(url);
            User user = JsonConvert.DeserializeObject<User>(response);
            
            // client_id : string
            // login : string
            // scope : string[] 
            // user_id : long(오는건 string인거 같은데)
            // expires_in : int
            return user;
        }

        public TwitchToken ValidateAccessToken(Streamer streamer)
        {
            string url = "https://id.twitch.tv/oauth2/authorize";
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

        public string GetRedirectURL()
        {
            string url = "https://id.twitch.tv/oauth2/authorize";
            string clientId = ClientId;
            string redirecUri = $"https://{DefaultIP}/member/index";
            string responseType = "code";
            return $"{url}?client_id={clientId}&redirect_uri={redirecUri}&response_type={responseType}&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email";
        }

        public int Insert(TwitchToken twitchToken, User user)
        {
            var result = 0;
            string SQL = $"INSERT INTO streamer(streamer_id,channel_name,refresh_token) VALUES(@StreamerId, @ChannelName, @RefreshToken);";
            using (MySqlConnection conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    cmd.Parameters.AddWithValue("@StreamerId", user.UserId);
                    cmd.Parameters.AddWithValue("@ChannelName", user.Login);
                    cmd.Parameters.AddWithValue("@RefreshToken", twitchToken.RefreshToken);
                    result = cmd.ExecuteNonQuery();
                    if (result == 1)
                    {
                        Console.WriteLine("Insert Success");
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
                conn.Close();
                return result;
            }
        }

        public int FindBotInUseByUserId(string userId)
        {
            long streamerId = Convert.ToInt64(userId);
            int result = 0;
            string SQL = "SELECT bot_in_use FROM streamer_detail where streamer_id = {@StreamerId};";
            using (MySqlConnection conn = GetConnection()) // 미리 생성된 Connection을 얻어온다.
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    cmd.Parameters.AddWithValue("@StreamerId", streamerId);
                    result = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
                return result;
            }
        }
    }
}
