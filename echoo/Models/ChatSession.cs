
namespace echoo.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string UserId { get; set; }  //  user who opened the chat
        public string ToUserId { get; set; } //  reciptent in the chat
        public DateTime DateStarted { get; set; }
        public int? GroupId { get; set; }  // used when it's a group chat

    }
}
