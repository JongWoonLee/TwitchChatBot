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

        public Tuple<TwitchToken, User> ConnectReleasesWebClient(string code)
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
            User user = ValidatingRequests(twitchToken.AccessToken);

            return new Tuple<TwitchToken, User>(twitchToken, user);
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

        public string GetRedirectURL()
        {
            string url = "https://id.twitch.tv/oauth2/authorize";
            string clientId = ClientId;
            string redirecUri = $"https://{DefaultIP}/member/index";
            string responseType = "code";
            return $"{url}?client_id={clientId}&redirect_uri={redirecUri}&response_type={responseType}&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email";
        }
    }
}
