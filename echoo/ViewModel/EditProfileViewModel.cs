namespace echoo.ViewModel
{
    public class EditProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? About { get; set; }

        public string? email { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        public string? ProfilePicturePath { get; set; }

        public string? Id { get; set; }
    }
}
