namespace echoo.ViewModel
{
    public class UserViewModel//for security purposes, if we passed the actual user object, intruders may have used the attributes
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string About { get; set; }
        public string ProfilePicturePath { get; set; } = "/img/Default.jpg";
        public DateTime? LastMessageDate { get; set; }

    }
}
