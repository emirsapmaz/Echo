namespace echoo.Models
{
    public class GroupMessages
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool isStarred { get; set; } = false;

        // Navigation properties
        public Group Group { get; set; }
        public User User { get; set; }

    }
}
