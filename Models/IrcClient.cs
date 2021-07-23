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
        public bool InitSuccess;


        private TcpClient TcpClient;
        private StreamReader InputStream;
        private StreamWriter OutputStream;

        /// <summary>
        /// TcpClient를 이용한 통신 전담 객체
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

                TcpClient = new TcpClient(Ip, Port);
                TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
                NetworkStream Stream = TcpClient.GetStream();
                InputStream = new StreamReader(Stream);
                OutputStream = new StreamWriter(Stream) { NewLine = "\r\n", AutoFlush = true };
                SendFirstConnectMessage(Password);
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

        private void SendFirstConnectMessage(string Password)
        {
            SendIrcMessage("PASS " + Password);
            SendIrcMessage("NICK " + UserName);
            SendIrcMessage("USER " + UserName + " 8 * :" + UserName);
            SendIrcMessage("JOIN #" + Channel);
            SendIrcMessage("CAP REQ :twitch.tv/commands");
            SendIrcMessage("CAP REQ :twitch.tv/tags");
        }

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

        public async Task<string> ReadMessage()
        {
            {
                try
                {
                    return await InputStream.ReadLineAsync();
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
        public void CloseTcpClient()
        {
            try
            {
                this.TcpClient.GetStream().Close(); // Connected는 항상 true 이므로 제거해보자..
                this.TcpClient.Close();
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

        public bool IsDataAvailable()
        {
            NetworkStream Stream = (NetworkStream)InputStream.BaseStream;
            Console.WriteLine("DataAvailable : " + Stream.DataAvailable);
            return Stream.DataAvailable;
        }
    }
}

