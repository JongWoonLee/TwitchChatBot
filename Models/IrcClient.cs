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
        private string Channel;

        private TcpClient TcpClient;
        private StreamReader InputStream;
        private StreamWriter OutputStream;

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
                Console.WriteLine(E.Message);
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
            }
            catch (Exception E)
            {
                return "Error receiving message: " + E.Message;
            }
        }
    }
}

