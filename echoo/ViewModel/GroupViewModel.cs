using echoo.Models;

namespace echoo.ViewModel
{
    public class GroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhotoPath { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; }
        public ICollection<GroupMember> Members { get; set; }
        public ICollection<User> Admins { get; set; }


    }
}
