using echoo.Models;

namespace echoo.ViewModel
{
    public class EditGroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CreatedByUserId { get; set; }
        public IFormFile Photo { get; set; }
        public string? ProfilePicturePath { get; set; }
        //public List<Contact>? Contacts { get; set; }
        public ICollection<GroupMember> Members { get; set; }
        public List<string> SelectedMemberIds { get; set; } // List of selected member IDs
    }
}
