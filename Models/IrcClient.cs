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
        public IrcClient(string Ip, int Port, string UserName, string Channel)
        {
            try
            {
                this.UserName = UserName;
                this.Channel = Channel;

                TcpClient = new TcpClient(Ip, Port);
                NetworkStream Stream = TcpClient.GetStream();
                InputStream = new StreamReader(Stream);
                OutputStream = new StreamWriter(Stream) { NewLine = "\r\n", AutoFlush = true };
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred in IrcClient Initialize : " + e.Message);
            }
        }

        public void SendFirstConnectMessage(string UserName, string Password, string Channel)
        {
            try
            {
                // Channel에 접속해 메세지를 읽어오기 위한 처음 메세지
                OutputStream.WriteLine("PASS " + Password);
                OutputStream.WriteLine("NICK " + UserName);
                OutputStream.WriteLine("USER " + UserName + " 8 * :" + UserName); // 분리한다고 해결되진 않았다.. catch 에서 닫자고 했지만
                OutputStream.WriteLine("JOIN #" + Channel);
                OutputStream.WriteLine("CAP REQ :twitch.tv/commands");
                OutputStream.WriteLine("CAP REQ :twitch.tv/tags");
                OutputStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("SendFirstConnectMessage : " + e.Message);
            }
        }

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
                    //return "Error receiving Read IOE " + IOE.Message;
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

        //Outside of start we need to define ValidateServerCertificate
        //private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        //{
        //    return sslPolicyErrors == SslPolicyErrors.None;
        //}
    }
}

