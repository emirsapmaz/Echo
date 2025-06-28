namespace echoo.Models
{
    public class Chat
    {

        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool isStarred { get; set; } = false;
    }
}
