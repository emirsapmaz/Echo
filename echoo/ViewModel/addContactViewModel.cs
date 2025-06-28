using System.ComponentModel.DataAnnotations;
using echoo.Models;
using Microsoft.AspNetCore.Identity;

namespace echoo.ViewModel
{
    public class addContactViewModel
    {
        [Required]
        [EmailAddress] 
        public string Email { get; set; }
        public string? FriendId { get; set; }
        public List<User> SuggestedContacts { get; set; } = new();


    }
}
