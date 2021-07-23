namespace TwitchChatBot.Models
{
    public class Command
    {
        public string CommandHead { get; set; }
        public string CommandBody { get; set; }
        public int CommandType { get; set; }
        public bool Block { get; set; }
        public int CommandCoolDown {get;set;}

        public Command(string CommandHead, string CommandBody, int CommandType, int CommandCoolDown)
        {
            this.CommandHead = CommandHead;
            this.CommandBody = CommandBody;
            this.CommandType = CommandType;
            this.CommandCoolDown = CommandCoolDown;
        }

        public Command()
        {
        }
    }
}
