﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using TwitchChatBot.Models;

namespace TwitchChatBot.Service
{
    public class ThreadExecutorService
    {
        private const string Ip = "irc.chat.twitch.tv";
        private const int Port = 6667;
        public Dictionary<long, SimpleTwitchBot> ManagedBot { get; set; }
        public string ConnectionString { get; set; }

        private List<Command> Commands;

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public ThreadExecutorService(string connectionString)
        {
            this.ManagedBot = new Dictionary<long, SimpleTwitchBot>();
            this.ConnectionString = connectionString;
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
                            ManagedBot.Add(1,
                                new SimpleTwitchBot(null,null,null,ConnectionString)
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

        public void RegisterBot(long id , string userName, string password, string channel)
        {
            var ircClient =  new IrcClient(Ip, Port, userName, password, channel);

            ManagedBot.Add(id, new SimpleTwitchBot(
                ircClient,
                new PingSender(ircClient),
                this.Commands,
                ConnectionString
                )) ;
        }
    }
}
