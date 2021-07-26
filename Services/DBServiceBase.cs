using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Services
{
    public class DBServiceBase
    {
        
        public string ConnectionString { get; set; }
        public const string ClientId = "jjvh028bmtssj5x8fov8lu3snk3wut";
        public string ClientSecret { get; set; }

        public DBServiceBase(string ConnectionString, string ClientSecret)
        {
            this.ConnectionString = ConnectionString;
            this.ClientSecret = ClientSecret;
        }

        public DBServiceBase(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
