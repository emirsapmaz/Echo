using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace echoo.Models
{
    public class Contact
    {
        public int Id { get; set; }

        public string OwnerId { get; set; }

        public string FriendId { get; set; }
        public User Owner { get; set; }
        public User Friend { get; set; }

    }
}
