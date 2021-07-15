namespace TwitchChatBot.Models
{
    public class Command
    {
        public string CommandHead { get; set; }
        public string CommandBody { get; set; }
        public string CommandType { get; set; }

        public Command(string CommandHead, string CommandBody, string CommandType)
        {
            this.CommandHead = CommandHead;
            this.CommandBody = CommandBody;
            this.CommandType = CommandType;
        }

        public Command()
        {
        }
    }
}
