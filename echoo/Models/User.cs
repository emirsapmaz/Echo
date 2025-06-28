using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace echoo.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string About { get; set; } = "Hi there, I am using Echo!";
        public string ProfilePicturePath { get; set; } = "/img/Default.jpg";

    }
}
