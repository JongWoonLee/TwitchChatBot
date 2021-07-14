using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

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
        /// TcpClient를 이용한 통신 전담 객체
        /// </summary>
        /// <param name="Ip">IP Address</param>
        /// <param name="Port">연결 포트번호</param>
        /// <param name="UserName">접속하는 유저명</param>
        /// <param name="Password">User Oauth AccessToken</param>
        /// <param name="Channel">접속할 채널</param>
        public IrcClient(string Ip, int Port, string UserName, string Password, string Channel)
        {
            try
            {
                this.UserName = UserName;
                this.Channel = Channel;

                TcpClient = new TcpClient(Ip, Port);
                InputStream = new StreamReader(TcpClient.GetStream());
                OutputStream = new StreamWriter(TcpClient.GetStream());

                // Try to join the room
                OutputStream.WriteLine("PASS " + Password);
                OutputStream.WriteLine("NICK " + UserName);
                OutputStream.WriteLine("USER " + UserName + " 8 * :" + UserName);
                OutputStream.WriteLine("JOIN #" + Channel);
                OutputStream.Flush();
            }
            catch (Exception E)
            {
                Console.WriteLine("Error occurred in IRCClient Initialize : " +E.Message);
            }
        }

        public void SendIrcMessage(string Message)
        {
            try
            {
                OutputStream.WriteLine(Message);
                OutputStream.Flush();
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
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

        public string ReadMessage()
        {
            try
            {
                string Message = InputStream.ReadLine();
                return Message;
            }catch (IOException IOE)
            {
                //CloseTcpClient();
                return IOE.Message;
            }
            catch (Exception E)
            {
                return "Error receiving message: " + E.Message;
            }
        }
        public void CloseTcpClient()
        {
            try 
            { 
                this.TcpClient.GetStream().Close();
                this.TcpClient.Close();
            }
            catch (ObjectDisposedException E)
            {
                Console.WriteLine("Object Dispose Exception: "+E.Message);
            }
        }
    }
}

