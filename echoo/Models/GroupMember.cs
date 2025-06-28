namespace echoo.Models
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsAdmin { get; set; }
        public Group Group { get; set; }
        public User User { get; set; } //navigation property tells Entity Framework that this class is related to the User entity.
    }
}
