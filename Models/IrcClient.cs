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

                TcpClient = new TcpClient(Ip, Port);
                NetworkStream Stream = TcpClient.GetStream();
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
            } // end try
        }

        /// <summary>
        /// 채팅방에 입장하게 해주는 Connect Message
        /// </summary>
        /// <param name="Password"></param>
        private void SendJoinMessage(string Password)
        {
            SendIrcMessage("PASS " + Password); // 
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
            } // end try
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
                this.TcpClient.GetStream().Close();
                this.TcpClient.Close();
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine("Object Dispose Exception: " + e.Message);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("InvalidOperationException: " + e.ToString());
            } // end try
        }

        /// <summary>
        /// 읽을 Data가 있는지 동기적으로 확인할 수 있는 Method
        /// </summary>
        /// <returns></returns>
        public bool IsDataAvailable() // 기껏 비동기로 짠 이유가 없는데
        {
            NetworkStream Stream = (NetworkStream)InputStream.BaseStream;
            Console.WriteLine("DataAvailable : " + Stream.DataAvailable);
            return Stream.DataAvailable;
        }

        class CallbackArg { }
        class PrimeCallbackArg : CallbackArg
        {
            public int Prime;

                public PrimeCallbackArg(int prime)
            {
                this.Prime = prime;
            }
        }
    }
}

