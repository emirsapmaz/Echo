using echoo.Models;

namespace echoo.ViewModel
{
    public class ChatListViewModel
    {
        public bool IsGroup { get; set; }
        public User User { get; set; }
        public Group Group { get; set; }
        public DateTime LastMessageDate { get; set; }
    }
}
