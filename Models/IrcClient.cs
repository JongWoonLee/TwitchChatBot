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

        public IrcClient(string ip, int port, string userName, string password, string channel)
        {
            try
            {
                this.UserName = userName;
                this.Channel = channel;

                TcpClient = new TcpClient(ip, port);
                InputStream = new StreamReader(TcpClient.GetStream());
                OutputStream = new StreamWriter(TcpClient.GetStream());

                // Try to join the room
                OutputStream.WriteLine("PASS " + password);
                OutputStream.WriteLine("NICK " + userName);
                OutputStream.WriteLine("USER " + userName + " 8 * :" + userName);
                OutputStream.WriteLine("JOIN #" + channel);
                OutputStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SendIrcMessage(string message)
        {
            try
            {
                OutputStream.WriteLine(message);
                OutputStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SendPublicChatMessage(string message)
        {
            try
            {
                SendIrcMessage(":" + UserName + "!" + UserName + "@" + UserName +
                ".tmi.twitch.tv PRIVMSG #" + Channel + " :" + message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public string ReadMessage()
        {
            try
            {
                string message = InputStream.ReadLine();
                return message;
            }
            catch (Exception e)
            {
                return "Error receiving message: " + e.Message;
            }
        }
    }
}

