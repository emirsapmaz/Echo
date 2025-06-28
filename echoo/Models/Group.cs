namespace echoo.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhotoPath { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<GroupMember> Members { get; set; } 



    }
}
