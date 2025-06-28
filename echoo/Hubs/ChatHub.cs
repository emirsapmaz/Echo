using System.Reflection;
using echoo.Data;
using echoo.Hubs;
using echoo.Models;
using echoo.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace echoo.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public static Dictionary<string, string> Users = new();

        public ChatHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                Users[Context.ConnectionId] = userId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Users.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        public async Task LeaveGroup(int groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        public async Task SendMessage(string toUserId, string message)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            Chat chat = new()
            {
                UserId = userId,
                ToUserId = toUserId,
                Message = message,
                Date = DateTime.Now,
                isStarred = false
            };

            await _context.Chats.AddAsync(chat);
            await _context.SaveChangesAsync();


            // send to the sender
            await Clients.Caller.SendAsync("Messages", chat);

            // send to the recipient if they're online
            var recipientConnection = Users.FirstOrDefault(p => p.Value == toUserId);
            if (!string.IsNullOrEmpty(recipientConnection.Key))
            {
                await Clients.Client(recipientConnection.Key).SendAsync("Messages", chat);
            }

            // refresh the user lists for both users
            await RefreshActiveUsersForCaller();

            if (!string.IsNullOrEmpty(recipientConnection.Key))
            {
                await RefreshActiveUsersForRecipient(recipientConnection.Key);
            }

        }

        private async Task RefreshActiveUsersForCaller()
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            if (currentUser == null)
                return;

            var combinedChats = await GetActiveUsersWithLastMessages(currentUser.Id);

            await Clients.Caller.SendAsync("UpdateActiveUsers", combinedChats);
        }

        private async Task RefreshActiveUsersForRecipient(string recipientConnectionId)
        {
            var recipientUser = Users.FirstOrDefault(p => p.Key == recipientConnectionId).Value;
            if (string.IsNullOrEmpty(recipientUser))
                return;

            var combinedChats = await GetActiveUsersWithLastMessages(recipientUser);

            await Clients.Client(recipientConnectionId).SendAsync("UpdateActiveUsers", combinedChats);
        }

        public async Task<List<ChatListViewModel>> GetActiveUsersWithLastMessages(string currentUserId)
        {
            var allChats = new List<ChatListViewModel>();

            // Get private chats
            var privateChats = await (
                from session in _context.ChatSessions
                where session.UserId == currentUserId || session.ToUserId == currentUserId
                let otherUserId = session.UserId == currentUserId ? session.ToUserId : session.UserId
                let lastMessageDate = _context.Chats
                    .Where(c =>
                        (c.UserId == currentUserId && c.ToUserId == otherUserId) ||
                        (c.ToUserId == currentUserId && c.UserId == otherUserId))
                    .Max(c => (DateTime?)c.Date)
                join user in _context.Users on otherUserId equals user.Id
                select new ChatListViewModel
                {
                    IsGroup = false,
                    User = user,
                    Group = null,
                    LastMessageDate = lastMessageDate ?? DateTime.MaxValue
                }
            ).ToListAsync();

            allChats.AddRange(privateChats);

            var groupChats = await (
                from member in _context.GroupMembers
                where member.UserId == currentUserId
                join groups in _context.Groups on member.GroupId equals groups.Id
                let lastGroupMessage = _context.GroupMessages
                    .Where(m => m.GroupId == groups.Id)
                    .Max(m => (DateTime?)m.Date)
                select new ChatListViewModel
                {
                    IsGroup = true,
                    User = null,
                    Group = groups,
                    LastMessageDate = lastGroupMessage ?? DateTime.MaxValue
                }
            ).ToListAsync();

            allChats.AddRange(groupChats);

            return allChats.OrderByDescending(x => x.LastMessageDate).ToList();
        }

        public async Task SendFileMessage(int fileMessageId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            var fileMessage = await _context.FileMessages.FindAsync(fileMessageId);
            if (fileMessage == null)
                return;

            var fileMessageDto = new
            {
                fileMessage.Id,
                fileMessage.UserId,
                fileMessage.ToUserId,
                fileMessage.FilePath,
                fileMessage.FileName,
                FileType = fileMessage.FileType.ToString(),
                fileMessage.Date,
                fileMessage.isStarred
            };

            await Clients.Caller.SendAsync("FileMessages", fileMessageDto);

            var recipientConnection = Users.FirstOrDefault(p => p.Value == fileMessage.ToUserId);
            if (!string.IsNullOrEmpty(recipientConnection.Key))
            {
                await Clients.Client(recipientConnection.Key).SendAsync("FileMessages", fileMessageDto);
            }

            if (!string.IsNullOrEmpty(recipientConnection.Key))
            {
                await RefreshActiveUsersForRecipient(recipientConnection.Key);
            }
        }

        public async Task SendGroupMessage(int groupId, string message)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;


            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember)
                return;

            var groupMessage = new GroupMessages
            {
                GroupId = groupId,
                UserId = userId,
                Message = message,
                isStarred = false,
                Date = DateTime.Now,
            };

            _context.GroupMessages.Add(groupMessage);
            await _context.SaveChangesAsync();

            // you need to load this object because otherwise user is null 
            var sender = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new {
                    u.FirstName,
                    u.LastName,
                    u.ProfilePicturePath
                })
                .FirstOrDefaultAsync();
            var messageDto = new
            {
                groupMessage.Id,
                groupMessage.GroupId,
                groupMessage.UserId,    
                groupMessage.Message,
                groupMessage.Date,
                groupMessage.isStarred,
                SenderName = (sender?.FirstName + " " + sender?.LastName),
                SenderProfilePicture = sender?.ProfilePicturePath
            };
            // broadcast to group
            await Clients.Group(groupId.ToString()).SendAsync("GroupMessages", messageDto);

            //refresh the list for every user in the group 
            await RefreshActiveUsersForCaller();

            var groupMemberIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            foreach (var memberId in groupMemberIds)
            {
                if (memberId != userId) // Skip the sender as we already refreshed for them
                {
                    var memberConnection = Users.FirstOrDefault(p => p.Value == memberId);
                    if (!string.IsNullOrEmpty(memberConnection.Key))
                    {
                        await RefreshActiveUsersForRecipient(memberConnection.Key);
                    }
                }
            }


        }

        public async Task SendGroupFileMessage(int fileMessageId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
                return;

            var fileMessage = await _context.FileMessages.FindAsync(fileMessageId);
            if (fileMessage == null || !fileMessage.GroupId.HasValue)
                return;

            var groupMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == fileMessage.GroupId.Value)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var sender = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new {
                    u.FirstName,
                    u.LastName,
                    u.ProfilePicturePath
                })
                .FirstOrDefaultAsync();
            var fileMessageDto = new
            {
                fileMessage.Id,
                fileMessage.UserId,
                fileMessage.GroupId,
                fileMessage.FilePath,
                fileMessage.FileName,
                FileType = fileMessage.FileType.ToString(),
                fileMessage.Date,
                fileMessage.isStarred,
                SenderName = (sender?.FirstName + " " + sender?.LastName),
                SenderProfilePicture = sender?.ProfilePicturePath
            };

            await Clients.Caller.SendAsync("GroupFileMessages", fileMessageDto);

            foreach (var memberId in groupMembers.Where(id => id != userId))
            {
                var memberConnection = Users.FirstOrDefault(p => p.Value == memberId);
                if (!string.IsNullOrEmpty(memberConnection.Key))
                {
                    await Clients.Client(memberConnection.Key).SendAsync("GroupFileMessages", fileMessageDto);
                }
            }

        }


    }
}

