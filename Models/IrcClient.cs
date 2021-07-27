using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TwitchChatBot.Models
{
    public class IrcClient
    {
        public string UserName;
        public string Channel;


        private TcpClient TcpClient;
        private StreamReader InputStream;
        private StreamWriter OutputStream;

        /// <summary>
        /// TcpClient를 이용한 IRC Chat 통신 전담 객체
        /// </summary>
        /// <param name="Ip">IP Address</param>
        /// <param name="Port">연결 포트번호</param>
        /// <param name="UserName">접속하는 유저명</param>
        /// <param name="Password">User Oauth AccessToken</param>
        /// <param name="Channel">접속할 채널</param>
        public IrcClient(string Ip, int Port, string UserName, string Password ,string Channel)
        {
            try
            {
                this.UserName = UserName;
                this.Channel = Channel;

                TcpClient = new TcpClient(Ip, Port); // 연결을 맺고
                NetworkStream Stream = TcpClient.GetStream(); // Stream을 얻어와서
                InputStream = new StreamReader(Stream);
                OutputStream = new StreamWriter(Stream) { NewLine = "\r\n", AutoFlush = true };
                SendJoinMessage(Password);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred in IrcClient Initialize : " + e.Message);
            }
        }

        /// <summary>
        /// 챗봇 서버에 Message를 보낸다.
        /// </summary>
        /// <param name="Message">전송할 Message</param>
        public void SendIrcMessage(string Message)
        {
            try
            {
                OutputStream.WriteLineAsync(Message);
                OutputStream.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("SendIrcMessage : " + e.Message);
            } 
        }

        /// <summary>
        /// 채팅방에 입장하게 해주는 Connect Message
        /// </summary>
        /// <param name="Password">봇 AccessToken</param>
        private void SendJoinMessage(string Password)
        {
            SendIrcMessage("PASS " + Password); // 
            SendIrcMessage("NICK " + UserName);
            SendIrcMessage("USER " + UserName + " 8 * :" + UserName);
            SendIrcMessage("JOIN #" + Channel);
            SendIrcMessage("CAP REQ :twitch.tv/commands");
            SendIrcMessage("CAP REQ :twitch.tv/tags"); 
        }

        /// <summary>
        /// PRIVMSG 형식으로 메세지를 출력(전체 메세지)
        /// </summary>
        /// <param name="Message">보내려는 Message</param>
        public void SendPublicChatMessage(string Message)
        {
            try
            {
                SendIrcMessage(":" + UserName + "!" + UserName + "@" + UserName +
                ".tmi.twitch.tv PRIVMSG #" + Channel + " :" + Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            } 
        }

        /// <summary>
        /// 메세지를 수신한다.(있을때까지 대기)
        /// </summary>
        /// <returns>Task<string> Message</returns>
        public async Task<string> ReadMessage()
        {
            {
                try
                {
                    return await InputStream.ReadLineAsync(); // 비동기적으로 메세지를 읽는다.
                }
                catch (IOException ioe)
                {
                    CloseTcpClient();
                    throw ioe;
                }
                catch (Exception e)
                {
                    return "Error receiving read message: " + e.Message;
                }
            }
        }

        /// <summary>
        /// Tcp연결을 닫는다.
        /// </summary>
        public void CloseTcpClient()
        {
            try
            {
                this.TcpClient.GetStream().Close(); // 스트림을 먼저 닫고,
                this.TcpClient.Close(); // TcpClient도 닫아준다.
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine("Object Dispose Exception: " + e.Message);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("InvalidOperationException: " + e.ToString());
            }
        }

        /// <summary>
        /// 읽을 Data가 있는지 동기적으로 확인할 수 있는 Method
        /// </summary>
        /// <returns>bool 수신 데이터 여부</returns>
        public bool IsDataAvailable()
        {
            NetworkStream Stream = (NetworkStream)InputStream.BaseStream;
            Console.WriteLine("DataAvailable : " + Stream.DataAvailable);
            return Stream.DataAvailable;
        }
    }
}

