using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace echoo.Models
{
    public class FileMessage
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string? ToUserId { get; set; }
        public int? GroupId { get; set; }       // for group message

        public string FilePath { get; set; } 

        public string FileName { get; set; }

        public FileType FileType { get; set; }

        public DateTime Date { get; set; }

        public bool isStarred { get; set; }

    }

    public enum FileType
    {
        Image,
        Document
    }



}
