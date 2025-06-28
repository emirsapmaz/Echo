using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using echoo.Models;

namespace echoo.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<FileMessage> FileMessages { get; set; }
        public DbSet<GroupMessages> GroupMessages { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Group> Groups { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Contact>()
                .HasKey(c => c.Id); 

            builder.Entity<Contact>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Contact>()
                .HasOne(c => c.Friend)
                .WithMany()
                .HasForeignKey(c => c.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }




    }
}
