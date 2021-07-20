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
                //if(TcpClient.Connected) // Connected 는 항상 true 만 뱉는다.. 뭐가 문제지 client.poll이 해결해주진 않는데..
                InputStream = new StreamReader(TcpClient.GetStream());
                InputStream.BaseStream.ReadTimeout = 1000; // 500 이게 기본값..
                OutputStream = new StreamWriter(TcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };
                
            }
            catch (Exception E)
            {
                Console.WriteLine("Error occurred in IrcClient Initialize : " + E.Message);
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
                OutputStream.Flush();
            }
            catch (Exception E)
            {
                Console.WriteLine("SendFirstConnectMessage : " + E.Message);
            }
        }

        public void SendIrcMessage(string Message)
        {
            try
            {
                OutputStream.WriteLineAsync(Message);
                OutputStream.FlushAsync();
            }
            catch (Exception E)
            {
                Console.WriteLine("SendIrcMessage : " + E.Message);
            }
        }

        public void SendPublicChatMessage(string Message)
        {
            try
            {
                SendIrcMessage(":" + UserName + "!" + UserName + "@" + UserName +
                ".tmi.twitch.tv PRIVMSG #" + Channel + " :" + Message);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        public async Task<string> ReadMessage()
        {
            try
            {
                return await InputStream.ReadLineAsync();
            }
            catch (IOException IOE)
            {
                CloseTcpClient();
                return "Error receiving Read IOE " + IOE.Message;
            }
            catch (Exception E)
            {
                return "Error receiving read message: " + E.Message;
            }
        }
        public void CloseTcpClient()
        {
            try
            {
                this.TcpClient.GetStream().Close(); // Connected는 항상 true 이므로 제거해보자..
                this.TcpClient.Close();
            }
            catch (ObjectDisposedException E)
            {
                Console.WriteLine("Object Dispose Exception: " + E.Message);
            }
        }

        //Outside of start we need to define ValidateServerCertificate
        //private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        //{
        //    return sslPolicyErrors == SslPolicyErrors.None;
        //}
    }
}

