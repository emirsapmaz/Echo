namespace echoo.Models
{
    public class StarredItemViewModel
    {
        public int Id { get; set; }
        public string MessageText { get; set; }      // For chat messages
        public string FilePath { get; set; }         // For files
        public FileType FileType { get; set; }         // "Image", "Document", etc.
        public string FileName { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public bool IsFile { get; set; }             // Distinguish type

        public int? GroupId { get; set; }
        public string SenderName { get; set; }
    }



}
