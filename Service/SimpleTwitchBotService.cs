using AsyncAwaitBestPractices;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatBot.Models;
using TwitchChatBot.Service.ChatBotEx.bot;

namespace TwitchChatBot.Service
{
    public class SimpleTwitchBotService
    {
        public Dictionary<string, SimpleTwitchBot> ManagedBot { get; set; }
        public string ConnectionString { get; set; }

        public List<Command> Commands { get; set; }

        public List<StreamerDetail> CurrentBotInUseUserList { get; set; }


        public SimpleTwitchBotService(string connectionString)
        {
            this.ConnectionString = connectionString;
            this.ManagedBot = new Dictionary<string, SimpleTwitchBot>();
            this.Commands = FindCommands();
            //Initialize();
        }
        
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async void StartBot(string channelName, string token)
        {

            string password = $"oauth:{token}";
            string botUsername = "simple_irc_bot";

            SimpleTwitchBot simpleTwitchBot = new SimpleTwitchBot(botUsername, password);
            simpleTwitchBot.Start().SafeFireAndForget();
            ManagedBot.Add(channelName, simpleTwitchBot);
            await simpleTwitchBot.JoinChannel($"{channelName}");
            await simpleTwitchBot.SendMessage($"{channelName}", "Here comes a View Bot");

            simpleTwitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");
                //Listen for !hey command
                //if (twitchChatMessage.Message.StartsWith("!hey"))
                //{
                //    await simpleTwitchBot.SendMessage(twitchChatMessage.Channel, $"Hey there {twitchChatMessage.Sender}");
                //}
                foreach (var cmd in Commands)
                { 
                    if(twitchChatMessage.Message.StartsWith(cmd.CommandHead))
                    {
                    await simpleTwitchBot.SendMessage(twitchChatMessage.Channel, $"Hey there {twitchChatMessage.Sender}");
                    }
                }
            };

            await Task.Delay(-1);
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
                catch (Exception e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
                return list;
            }
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
                            CurrentBotInUseUserList.Add(
                                new StreamerDetail()
                            ); // 읽어온 데이터들을 이용해서 새로운 객체를 list에 담는다.
                            //StartBot();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
            }
        }
    }
}
