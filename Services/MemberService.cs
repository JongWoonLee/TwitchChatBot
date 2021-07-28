using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using TwitchChatBot.Models;
using TwitchChatBot.Services;

namespace TwitchChatBot.Service
{
    public class MemberService : DBServiceBase
    {
        public string DefaultIP { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="ConnectionString">string DBConnectionString</param>
        /// <param name="DefaultIP">string Redirect Address</param>
        /// <param name="ClientSecret">string App ClientSecret</param>
        public MemberService(string ConnectionString, string DefaultIP, string ClientSecret) : base(ConnectionString, ClientSecret)
        {
            this.DefaultIP = DefaultIP;
        }

        /// <summary>
        /// StreamerId 값을 이용해 Streamer를 찾는다.
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <returns>Streamer ID로 검색된 스트리머</returns>
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

        /// <summary>
        /// 인가코드를 위한 주소를 얻어오는 메소드
        /// </summary>
        /// <returns>string Redirect URL</returns>
        public string GetRedirectURL()
        {
            string Url = "https://id.twitch.tv/oauth2/authorize";
            string RedirectUri = $"https://{DefaultIP}/member/index";
            string ResponseType = "code";
            return $"{Url}?client_id={ClientId}&redirect_uri={RedirectUri}&response_type={ResponseType}&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email";
        }

        /// <summary>
        /// Authentication Token 을 얻어온다.
        /// </summary>
        /// <param name="Code">인가코드</param>
        /// <returns>TwitchToken 유저 토큰</returns>
        public TwitchToken GettingTokens(string Code)
        {
            TwitchToken TwitchToken = null;

            string Url = "https://id.twitch.tv/oauth2/token";
            var Client = new WebClient();
            var Data = new NameValueCollection();

            Data["grant_type"] = "authorization_code";
            Data["client_id"] = ClientId;
            Data["client_secret"] = ClientSecret;
            Data["redirect_uri"] = $"https://{DefaultIP}/member/index"; ;
            Data["code"] = Code;
            try
            {
                var Response = Client.UploadValues(Url, "POST", Data);
                string Str = Encoding.Default.GetString(Response);
                TwitchToken = JsonConvert.DeserializeObject<TwitchToken>(Str);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return TwitchToken;
        }

        /// <summary>
        /// UserAccessToken을 이용해 Id 값을 읽어온다.
        /// </summary>
        /// <param name="AccessToken">유저 AccessToken</param>
        /// <returns>User 유저 정보</returns>
        public User ValidatingRequests(string AccessToken)
        {
            User User = null;
            string Url = "https://id.twitch.tv/oauth2/validate";
            var Client = new WebClient();
            try
            {

                Client.Headers.Add("Authorization", $"Bearer {AccessToken}");
                var Response = Client.DownloadString(Url);
                User = JsonConvert.DeserializeObject<User>(Response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            // client_id : string
            // login : string
            // scope : string[] 
            // user_id : long
            // expires_in : int
            return User;
        }


        /// <summary>
        /// StreamerInsert
        /// </summary>
        /// <param name="TwitchToken">Authentication Token</param>
        /// <param name="User">User 유저 데이터</param>
        /// <returns>int 쿼리 실행 결과 row 수</returns>
        public int InsertStreamer(TwitchToken TwitchToken, User User)
        {
            var Result = 0;
            string SQL = $"INSERT INTO streamer(streamer_id,channel_name,refresh_token) VALUES(@StreamerId, @ChannelName, @RefreshToken);";
            using (MySqlConnection Conn = GetConnection())
            {
                Conn.Open();
                MySqlTransaction Transaction = Conn.BeginTransaction();
                try
                {
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn, Transaction);
                    Cmd.Parameters.AddWithValue("@StreamerId", User.UserId);
                    Cmd.Parameters.AddWithValue("@ChannelName", User.Login);
                    Cmd.Parameters.AddWithValue("@RefreshToken", TwitchToken.RefreshToken);
                    Result = Cmd.ExecuteNonQuery();
                    if (Result == 1)
                    {
                        Console.WriteLine("Insert Success");
                        InsertStreamerDetail(Cmd, User.UserId);
                    }
                    else
                    {
                        Console.WriteLine("Insert Fail!!");
                        Transaction.Rollback();
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine("DB Connection Fail!!!!!!!!!!!");
                    Console.WriteLine(e.ToString());
                    Transaction.Rollback(); // 트렌젝션 롤백
                }
                Conn.Close();
                return Result;
            }
        }

        /// <summary>
        /// StreamerDetail Insert
        /// </summary>
        /// <param name="MySqlCommand">MySqlCommand SqlCommand</param>
        /// <param name="InheritedKey">long LastInsertKey</param>
        /// <returns></returns>
        private int InsertStreamerDetail(MySqlCommand MySqlCommand, long InheritedKey)
        {
            var Result = 0;
            try
            {
                MySqlCommand.CommandText = $"INSERT INTO streamer_detail(streamer_id) VALUES(@InheritedId);";
                MySqlCommand.Parameters.AddWithValue("@InheritedId", InheritedKey);
                Result = MySqlCommand.ExecuteNonQuery();
                if (Result == 1)
                {
                    Console.WriteLine("Insert Success");
                    MySqlCommand.Transaction.Commit();
                }
                else
                {
                    Console.WriteLine("Insert Fail!!");
                    MySqlCommand.Transaction.Rollback();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                MySqlCommand.Transaction.Rollback();
            }
            return Result;
        }

        /// <summary>
        /// 봇 사용 여부를 스트리머 ID로 검색
        /// </summary>
        /// <param name="UserId">string 스트리머 ID</param>
        /// <returns>int 봇 사용 여부</returns>
        public int FindBotInUseByUserId(string UserId)
        {
            long StreamerId = Convert.ToInt64(UserId);
            int Result = 0;
            string SQL = $"SELECT bot_in_use FROM streamer_detail where streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection())
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

        /// <summary>
        /// 스트리머 상세를 업데이트
        /// </summary>
        /// <param name="StreamerDetail">스트리머 상세 정보</param>
        /// <returns>int 쿼리 실행 row 수</returns>
        public int UpdateStreamerDetail(StreamerDetail StreamerDetail)
        {
            int Result = 0;
            string SQL = $"UPDATE streamer_detail SET donation_link = @DonationLink, greeting_message = @GreetingMessage, forbidden_word_limit = @ForbiddenWordLimit,forbidden_word_timeout = @ForbiddenWordTimeout WHERE streamer_id = {StreamerDetail.StreamerId};";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@DonationLink", StreamerDetail.DonationLink);
                    Cmd.Parameters.AddWithValue("@GreetingMessage", StreamerDetail.GreetingMessage);
                    Cmd.Parameters.AddWithValue("@ForbiddenWordLimit", StreamerDetail.ForbiddenWordLimit);
                    Cmd.Parameters.AddWithValue("@ForbiddenWordTimeout", StreamerDetail.ForbiddenWordTimeout);
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

        /// <summary>
        /// 금지어 목록을 읽어온다.
        /// </summary>
        /// <param name="StreamerId">long 스트리머 ID</param>
        /// <returns>List<string> 금지어 목록</returns>
        public List<string> FindForbiddenWordList(long StreamerId)
        {
            List<string> Result = new List<string>();
            string SQL = $"SELECT forbidden_word FROM forbidden_word WHERE streamer_id = {StreamerId};";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    using (var Reader = Cmd.ExecuteReader())
                    {
                        while (Reader.Read())
                            Result.Add(
                                Reader["forbidden_word"].ToString()
                                );
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

        /// <summary>
        /// 금지어 목록 업데이트
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <param name="ForbiddenWord">금지어</param>
        /// <param name="PrevWord">이전 금지어</param>
        /// <returns>int 쿼리 실행 결과 row 수</returns>
        public int UpdateForbiddenWord(long StreamerId, string ForbiddenWord, string PrevWord)
        {
            int Result = 0;
            string SQL = $"UPDATE forbidden_word SET forbidden_word = @ForbiddenWord WHERE streamer_id = {StreamerId} AND forbidden_word = @PrevWord;";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@ForbiddenWord", ForbiddenWord);
                    Cmd.Parameters.AddWithValue("@PrevWord", PrevWord);
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

        /// <summary>
        /// 금지어 삭제
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <param name="ForbiddenWord">금지어</param>
        /// <returns>int 쿼리 실행 결과 row 수</returns>
        public int DeleteForbiddenWord(long StreamerId, string ForbiddenWord)
        {
            int Result = 0;
            string SQL = $"DELETE FROM forbidden_word WHERE streamer_id = {StreamerId} AND forbidden_word = @ForbiddenWord;";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@ForbiddenWord", ForbiddenWord);
                    Result = Cmd.ExecuteNonQuery();
                    if (Result == 1)
                    {
                        Console.WriteLine("Delete Success");
                    }
                    else
                    {
                        Console.WriteLine("Delete Fail!!");
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

        /// <summary>
        /// 금지어 추가
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <param name="ForbiddenWord">금지어</param>
        /// <returns>int 쿼리 실행 row 수</returns>
        public int InsertForbiddenWord(long StreamerId, string ForbiddenWord)
        {
            int Result = 0;
            string SQL = $"INSERT INTO forbidden_word (streamer_id, forbidden_word) VALUES ({StreamerId},@ForbiddenWord);";
            using (MySqlConnection Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    MySqlCommand Cmd = new MySqlCommand(SQL, Conn);
                    Cmd.Parameters.AddWithValue("@ForbiddenWord", ForbiddenWord);
                    Result = Cmd.ExecuteNonQuery();
                    if (Result == 1)
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
                Conn.Close();
                return Result;
            }
        }

        /// <summary>
        /// StreamerDetail의 봇 사용 여부를 업데이트
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <param name="BotInUse">봇 사용 여부</param>
        /// <returns>int 쿼리 실행 row 수</returns>
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

        /// <summary>
        /// 스트리머 상세 정보를 가져온다.
        /// </summary>
        /// <param name="StreamerId">스트리머 ID</param>
        /// <returns>StreamerDetail 스트리머 상세 정보</returns>
        public StreamerDetail FindStreamerDetail(long StreamerId)
        {
            StreamerDetail StreamerDetail = null;
            string SQL = $"SELECT std.*, s.channel_name FROM streamer_detail std, streamer s WHERE std.streamer_id = s.streamer_id AND s.streamer_id = {StreamerId};";
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
                            StreamerDetail = new StreamerDetail(
                                Convert.ToInt64(Reader["streamer_id"]),
                                Reader["channel_name"].ToString(),
                                Convert.ToInt32(Reader["bot_in_use"]),
                                Reader["donation_link"].ToString(),
                                Reader["greeting_message"].ToString(),
                                Convert.ToInt32(Reader["forbidden_word_limit"]),
                                Convert.ToInt32(Reader["forbidden_word_timeout"])
                                );
                        }
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
    } // end class
} // end namespace
